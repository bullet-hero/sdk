using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat.Export
{
    // One scope's layers into Afterbeat's depth, band and editor row, decided from every layer the
    // scope uses at once - because two of the three cannot be decided one object at a time.
    //
    // DEPTH. Afterbeat has 61 depths per band and this format 2001 layers. A level whose layers fit
    // the fixed linear mapping (Default at 0 and down to -60, Background under it, AbovePlayer from
    // 1 up) keeps it: that mapping is the exact inverse of the OnlyDepth import and preserves every
    // Auto one, which is what the round-trip tests hold it to. A level that does NOT fit - a packed
    // import spends a row per simultaneous object, and a real one goes far past -121 - is mapped by
    // RANK instead, monotonically and quantised evenly into 0..60, so its order survives where the
    // linear mapping would have clamped its lower half onto one depth.
    //
    // THE BAND UNDER RANK MAPPING IS THE SIGN, and Background is not inferred. A layer records no
    // band; the linear mapping reads one off fixed boundaries, and a packed level's Default content
    // simply continues past them. So once the level does not fit, 1 and up is AbovePlayer and
    // everything else Default. A level that fits the linear mapping but was packed - its Default
    // content reaching -61..-121 - is indistinguishable from one with a Background band, and reads as
    // one; the export says so.
    //
    // THE EDITOR ROW is the rank of the layer counted from the top, so Afterbeat's timeline shows the
    // layout this editor showed: row 0 is the highest layer, each distinct layer below it one row
    // further down, bins inside editor layers. A layout wider than the source editor's 90 rows is
    // quantised onto them rather than piled onto the last.

    /// <summary> One scope's layers as Afterbeat depth, band and editor row. </summary>
    public sealed class ABDrawOrderMap
    {
        /// <summary> Lowest layer the linear mapping covers - depth 60 of the Background band. </summary>
        public const int LinearLowest = ValueRules.LastLayerBehindPlayer - 2 * ABLayerMap.DepthSpan + 1;

        /// <summary> Highest layer the linear mapping covers - depth 0 of the AbovePlayer band. </summary>
        public const int LinearHighest = ValueRules.FirstLayerAbovePlayer + VgdObject.MaxDepth;

        /// <summary> How many rows the source editor offers from the export's first layer on. </summary>
        public const int EditorRows = (ABObjectExporter.MaxEditorLayer - ABObjectExporter.FirstEditorLayer + 1)
                                      * ABObjectExporter.EditorBinsPerLayer;

        private readonly Dictionary<int, (ABRenderLayer Band, int Depth)> _ranked;
        private readonly Dictionary<int, int> _rows = new();
        private readonly ABRenderLayer? _band;

        /// <summary> True when the scope's layers fit the fixed linear mapping and use it. </summary>
        public bool IsLinear => _ranked == null;

        private ABDrawOrderMap(Dictionary<int, (ABRenderLayer, int)> ranked, ABRenderLayer? band)
        {
            _ranked = ranked;
            _band = band;
        }

        /// <summary> The map for every layer a scope uses. <paramref name="band"/> forces one band
        /// on the whole scope - a template's content is drawn in whatever band its placements sit
        /// in, not where its own layers (1 and up, above Prefab.Root) would put it. </summary>
        public static ABDrawOrderMap Build(IEnumerable<int> layers, ABRenderLayer? band = null)
        {
            var distinct = new SortedSet<int>();
            if (layers != null)
                foreach (var layer in layers)
                    distinct.Add(layer);

            var linear = band == null;
            foreach (var layer in distinct)
                if (layer < LinearLowest || layer > LinearHighest)
                {
                    linear = false;
                    break;
                }

            ABDrawOrderMap map;
            if (linear) map = new ABDrawOrderMap(null, null);
            else
            {
                var ranked = new Dictionary<int, (ABRenderLayer, int)>();
                var above = new List<int>();
                var below = new List<int>();
                foreach (var layer in distinct)
                {
                    var layerBand = band ?? (layer >= ValueRules.FirstLayerAbovePlayer
                        ? ABRenderLayer.AbovePlayer
                        : ABRenderLayer.Default);
                    (layerBand == ABRenderLayer.AbovePlayer ? above : below).Add(layer);
                }

                // Frontmost first in both: the highest layer is depth 0 of its band.
                above.Reverse();
                below.Reverse();
                Rank(above, ABRenderLayer.AbovePlayer, ranked);
                Rank(below, band is ABRenderLayer.Background ? ABRenderLayer.Background : ABRenderLayer.Default, ranked);
                map = new ABDrawOrderMap(ranked, band);
            }

            var rows = new List<int>(distinct);
            rows.Reverse();
            for (var i = 0; i < rows.Count; i++)
                map._rows[rows[i]] = rows.Count <= EditorRows ? i : (int)((long)i * EditorRows / rows.Count);

            return map;
        }

        private static void Rank(List<int> frontFirst, ABRenderLayer band,
            Dictionary<int, (ABRenderLayer, int)> into)
        {
            var count = frontFirst.Count;
            for (var i = 0; i < count; i++)
            {
                var depth = count <= ABLayerMap.DepthSpan ? i : (int)((long)i * ABLayerMap.DepthSpan / count);
                into[frontFirst[i]] = (band, Math.Clamp(depth, VgdObject.MinDepth, VgdObject.MaxDepth));
            }
        }

        /// <summary> Band and depth of one layer. A layer the map was not built from is mapped
        /// linearly - it can only come from a caller that skipped it. </summary>
        public (ABRenderLayer Band, int Depth) Map(int layer)
        {
            if (_ranked != null && _ranked.TryGetValue(layer, out var ranked)) return ranked;
            if (_band is { } band && _ranked != null) return (band, VgdObject.MaxDepth);
            return MapLinear(layer);
        }

        /// <summary> The editor row of one layer, counted from the top. </summary>
        public int RowOf(int layer)
            => _rows.TryGetValue(layer, out var row) ? row : Math.Clamp(-layer, 0, EditorRows - 1);

        // The inverse of the importer's OnlyDepth mapping - the one of the four legacy layer modes
        // that is a bijection - so a level that came from Afterbeat under it goes back unchanged,
        // and one authored here lands where the same layer would have drawn.
        //
        // Which BAND a layer belongs to is decided the same way the import decided it, and the
        // player line is what splits them: this format's avatar occupies (0, 1), the source game
        // draws its own in front of every Default object and behind every AbovePlayer one, so layer
        // 1 and up is AbovePlayer, 0 down to -60 is the Default band with depth 0 at its top, and
        // everything under that is Background. Clamped at both ends, since this format has 2001
        // layers to spend and Afterbeat has 183.

        /// <summary> The fixed linear mapping, on its own. </summary>
        public static (ABRenderLayer Band, int Depth) MapLinear(int effectiveLayer)
        {
            const int span = ABLayerMap.DepthSpan;

            var band = effectiveLayer >= ValueRules.FirstLayerAbovePlayer ? ABRenderLayer.AbovePlayer
                : effectiveLayer >= ValueRules.LastLayerBehindPlayer - span + 1 ? ABRenderLayer.Default
                : ABRenderLayer.Background;

            // The layer depth 0 of this band sits on; every deeper depth steps one further down.
            var frontmost = band switch
            {
                ABRenderLayer.AbovePlayer => VgdObject.MaxDepth + ValueRules.FirstLayerAbovePlayer,
                ABRenderLayer.Default => ValueRules.LastLayerBehindPlayer,
                _ => ValueRules.LastLayerBehindPlayer - span,
            };

            return (band, Math.Clamp(frontmost - effectiveLayer, VgdObject.MinDepth, VgdObject.MaxDepth));
        }

        /// <summary> True when a linear map reads some layer as Background - which a packed level
        /// reaching past -60 would be misread as. </summary>
        public bool ReadsBackground(IEnumerable<int> layers)
        {
            if (!IsLinear || layers == null) return false;
            foreach (var layer in layers)
                if (MapLinear(layer).Band == ABRenderLayer.Background)
                    return true;
            return false;
        }
    }
}
