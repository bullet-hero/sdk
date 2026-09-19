using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat
{
    // Draw order, all of it, in one place: this is the only thing in the converter that cannot be
    // decided one object at a time, and it is resolved for a WHOLE LEVEL at once - its own objects
    // and every prefab template's - rather than per list. Per-list resolves stacked with offsets is
    // what produced the numbers this replaced: 386 placements each given a layer of their own above
    // whatever the level reached, templates ranked independently underneath them, and one real
    // level arriving spread over -520 to +401 with 894 distinct layers on 4151 objects.
    //
    // THE PLAYER LINE, measured in the source game rather than assumed. Afterbeat sorts by
    // sortingOrder in a single sorting layer: an ordinary object gets 0 (ObjectManager, the Default
    // branch), the player gets 61 (VGPlayer.Init), an AbovePlayer object gets 62 + (60 - depth),
    // and a Background object is drawn by a different camera entirely. So the whole Default band -
    // depth 0 included - is BEHIND the player, and depth orders it only by z (0.1 * depth, ordinary
    // draw distance). This format's avatar sits at layer 0.5 (Services.Shared's AvatarInitData
    // .BaseLayer), so ValueRules.FirstLayerAbovePlayer and up is in front of it and
    // ValueRules.LastLayerBehindPlayer and down behind: Default lands at 0 and
    // down, AbovePlayer at 1 and up, Background below Default. An earlier reading of the source put
    // the player between depth 0 and depth 1, which pulled one object per level in front of it and
    // pushed the whole rest of the level into the negatives to make room.
    //
    // THE THREE BANDS are laid out as consecutive stretches rather than interleaved, because that
    // is what the source game does: they are separate sorting ranges over there, and no depth in
    // one can reach into another.
    //
    // DEPTH IS THE ONLY ORDERING INPUT, and the source editor's own layer/bin grouping is
    // deliberately not one. It orders nothing over there - it never reaches sortingOrder - so
    // spending draw order on it reproduces an organisation the source game does not draw, at a
    // measured 6.3x the layers (221 where 35 order the level). Two objects sharing a depth share a
    // layer here, which is faithful: sharing a depth over there means sharing both sortingOrder and
    // z, i.e. an undefined order the source game does not define either, and this format already
    // separates coplanar objects deterministically (ValueRules.LayerZOffsetStep). An author who
    // wants the source editor's grouping expressed as draw order asks for it by name - that is what
    // OnlyEditor and DepthAndEditor are.

    /// <summary> One .vgd's draw order, resolved into this format's effective (absolute,
    /// pre-parent-subtraction) layers. </summary>
    public static class ABLayerMap
    {
        /// <summary> How many depths the source format has - 0 through 60, both included. </summary>
        public const int DepthSpan = VgdObject.MaxDepth - VgdObject.MinDepth + 1;

        /// <summary> How many render bands the source format has. </summary>
        public const int BandCount = 3;

        /// <summary> Lowest editor bin the source editor allows. </summary>
        public const int MinEditorBin = 0;

        /// <summary> Highest editor bin the source editor allows. </summary>
        public const int MaxEditorBin = 14;

        /// <summary> How many bins one source editor layer holds - 0 through 14, both included. </summary>
        public const int EditorBinSpan = MaxEditorBin - MinEditorBin + 1;

        /// <summary> Draw order of the three bands, back to front. Not the enum's own numbering -
        /// that one is the source format's and has Background last. </summary>
        public static int ToBandRank(ABRenderLayer band) => band switch
        {
            ABRenderLayer.Background => 0,
            ABRenderLayer.AbovePlayer => 2,
            _ => 1,
        };

        /// <summary> An object's band, reading anything the source format does not define as
        /// Default - which is what it writes when the author never touched the setting. </summary>
        public static ABRenderLayer ToBand(VgdObject source)
        {
            var value = source?.RenderLayer ?? 0;
            return value is (int)ABRenderLayer.AbovePlayer or (int)ABRenderLayer.Background
                ? (ABRenderLayer)value
                : ABRenderLayer.Default;
        }

        /// <summary> One object's place in the source editor's own ordering, bins inside layers.
        /// Larger is drawn in front. An object carrying no editor block reads as layer 1 - the same
        /// group as the first one, not a group of its own: an object nobody sorted belongs with the
        /// ones nobody sorted either. </summary>
        public static int ToEditorIndex(VgdObject source)
        {
            var editorLayer = Math.Max(1, source?.Editor?.Layer ?? 0);
            var bin = Math.Clamp(source?.Editor?.Bin ?? 0, MinEditorBin, MaxEditorBin);
            return (editorLayer - 1) * EditorBinSpan + bin;
        }

        /// <summary> One object's depth, clamped into what the source format allows. </summary>
        public static int ToDepth(VgdObject source)
            => Math.Clamp(source?.Depth ?? VgdObject.DefaultDepth, VgdObject.MinDepth, VgdObject.MaxDepth);

        #region Plan

        // One object's whole ordering input as a single sortable number: the band above the depth,
        // the depth already reversed so that larger is drawn in front. The range is 3 * 61 = 183
        // values, small enough that the Auto ranking is an array walk rather than a sort, and small
        // enough that Auto CANNOT reach ValueRules.MinLayer no matter what a level contains - the
        // deepest layer it can produce is -121. That bound is the point of the whole design: the
        // range a converted level occupies is a property of the source FORMAT, not of how large or
        // how finely organised the level happens to be.

        /// <summary> Where one object sits in the whole ordering, before it is turned into a
        /// layer. </summary>
        private static int ToKey(VgdObject source)
            => ToBandRank(ToBand(source)) * DepthSpan + VgdObject.MaxDepth - ToDepth(source);

        /// <summary> One level's draw order, resolved once and read by every list in it - the
        /// level's own objects and every prefab template's alike, so a template object and a level
        /// object at the same depth land on the same layer. </summary>
        public sealed class Plan
        {
            private readonly ABLayerImport _mode;
            private readonly int _stride;
            private readonly int _span;
            private readonly int[] _layerByKey;

            /// <summary> Lowest and highest layer anything in the whole level landed on. </summary>
            public int Lowest { get; }

            /// <summary> The largest layer the plan will hand out. </summary>
            public int Highest { get; }

            /// <summary> Built by the planner alone, so the numbers it holds always agree with each other. </summary>
            internal Plan(ABLayerImport mode, int stride, int span, int[] layerByKey,
                int lowest, int highest)
            {
                _mode = mode;
                _stride = stride;
                _span = span;
                _layerByKey = layerByKey;
                Lowest = lowest;
                Highest = highest;
            }

            /// <summary> The effective layer of one source object. </summary>
            public int Get(VgdObject source)
            {
                if (source == null) return 0;

                if (_layerByKey != null) return _layerByKey[ToKey(source)];

                var bandBase = (ToBandRank(ToBand(source)) - 2) * _span
                               + ValueRules.FirstLayerAbovePlayer;
                return Math.Clamp(bandBase + ToOrder(source, _mode, _stride),
                    ValueRules.MinLayer, ValueRules.MaxLayer);
            }
        }

        /// <summary> Resolves the draw order of every list a level is made of at once. Pass every
        /// object list the level holds - its own and each template's - or the result orders them
        /// against each other by accident. </summary>
        public static Plan Build(IReadOnlyList<IReadOnlyList<VgdObject>> lists, ABOptions options,
            InteropReport report = null, string path = null)
        {
            options ??= new ABOptions();

            return options.LayerImport == ABLayerImport.Auto
                ? BuildAuto(lists)
                : BuildBanded(lists, options, report, path);
        }

        /// <summary> The plan for a single list, for a template imported on its own - a bare .vgp
        /// has no level to be ordered against. </summary>
        public static Plan Build(IReadOnlyList<VgdObject> sources, ABOptions options,
            InteropReport report = null, string path = null)
            => Build(new[] { sources }, options, report, path);

        #endregion

        #region Auto

        // Every ordering key the level actually uses gets its own layer, and inside a band those
        // layers are consecutive - so a level using six depths costs six rows rather than the
        // sixty-one its depth range spans, and two objects the source game drew in front of one
        // another still are. Rank, in other words, which is why it cannot be computed one object at
        // a time.
        //
        // EACH BAND IS PACKED AGAINST ITS OWN EDGE OF THE PLAYER LINE, and that is what "flattened
        // onto 0" means here: Default's frontmost key is ValueRules.LastLayerBehindPlayer and it
        // grows downwards, AbovePlayer's backmost is ValueRules.FirstLayerAbovePlayer and it grows
        // upwards. An ordinary level - one where
        // nothing is marked AbovePlayer or Background, which is nearly all of them - therefore
        // reaches exactly layer 0 and steps down from there, and the object an author sees at the top
        // of the level is the one the source game drew nearest the camera.
        //
        // PACKING THE THREE BANDS INTO ONE RUN INSTEAD WOULD LOSE THE BAND. Nothing on an object
        // here records which band it came from - a layer is all there is - so the export infers it
        // from where the layer falls, against the fixed boundaries OnlyDepth lays the bands out on.
        // A single run puts a Background key a few layers under the Default ones, i.e. inside the
        // stretch the export reads as Default, and the level comes back with its background drawn
        // as ordinary content. Anchoring each band inside its own boundaries costs a gap between
        // Default's last used layer and Background's first, and only for the levels that actually
        // use the Background band - it is empty in every level of the corpus.
        private static Plan BuildAuto(IReadOnlyList<IReadOnlyList<VgdObject>> lists)
        {
            var used = new bool[BandCount * DepthSpan];

            if (lists != null)
                foreach (var sources in lists)
                {
                    if (sources == null) continue;
                    foreach (var source in sources)
                        if (source != null)
                            used[ToKey(source)] = true;
                }

            var layerByKey = new int[used.Length];
            var lowest = int.MaxValue;
            var highest = int.MinValue;

            for (var band = 0; band < BandCount; band++)
            {
                var first = band * DepthSpan;
                var last = first + DepthSpan - 1;

                // The two bands behind the player fill downwards from their own top; the one in
                // front fills upwards from the player line. Both directions start at the edge the
                // export measures that band from, so a packed layer never leaves its own stretch.
                var forward = band == ToBandRank(ABRenderLayer.AbovePlayer);
                var next = forward ? ValueRules.FirstLayerAbovePlayer
                    : band == ToBandRank(ABRenderLayer.Default) ? ValueRules.LastLayerBehindPlayer
                    : ValueRules.LastLayerBehindPlayer - DepthSpan;

                for (var step = 0; step < DepthSpan; step++)
                {
                    var key = forward ? first + step : last - step;
                    if (!used[key]) continue;

                    var layer = next;
                    next += forward ? 1 : -1;
                    layerByKey[key] = layer;

                    if (layer < lowest) lowest = layer;
                    if (layer > highest) highest = layer;
                }
            }

            // An ordinary level occupies layer 0 and down, and a level with nothing in it occupies
            // nothing at all - so the range is seeded empty and collapsed here rather than started at
            // zero, which would report a band the level does not occupy - and the parallax is placed
            // below whatever this says.
            if (lowest > highest) (lowest, highest) = (0, 0);

            return new Plan(ABLayerImport.Auto, 0, 0, layerByKey, lowest, highest);
        }

        #endregion

        #region Banded

        // Each band is one stretch `span` layers wide and the three sit back to back, with Default
        // ending at ValueRules.LastLayerBehindPlayer so that the band the player is in front of ends
        // where the player is: Default
        // occupies [-span+1, 0], AbovePlayer [1, span], Background [-2*span+1, -span]. `span` is
        // the source format's whole depth range for OnlyDepth - fixed, because that mode's promise
        // is that a depth means the same layer in every level - and the widest order the level
        // actually reaches for the two editor-driven modes, whose ordering has no fixed extent to
        // be measured against.
        private static Plan BuildBanded(IReadOnlyList<IReadOnlyList<VgdObject>> lists,
            ABOptions options, InteropReport report, string path)
        {
            var mode = options.LayerImport;
            var stride = Math.Max(1, options.EditorGroupStride);

            var span = DepthSpan;
            if (mode != ABLayerImport.OnlyDepth)
            {
                var widest = 0;
                if (lists != null)
                    foreach (var sources in lists)
                    {
                        if (sources == null) continue;
                        foreach (var source in sources)
                            if (source != null)
                                widest = Math.Max(widest, ToOrder(source, mode, stride));
                    }

                span = widest + 1;
            }

            var lowest = int.MaxValue;
            var highest = int.MinValue;
            var clamped = false;

            if (lists != null)
                foreach (var sources in lists)
                {
                    if (sources == null) continue;
                    foreach (var source in sources)
                    {
                        if (source == null) continue;

                        var raw = (ToBandRank(ToBand(source)) - 2) * span
                                  + ToOrder(source, mode, stride)
                                  + ValueRules.FirstLayerAbovePlayer;
                        var layer = Math.Clamp(raw, ValueRules.MinLayer, ValueRules.MaxLayer);

                        if (layer != raw) clamped = true;
                        if (layer < lowest) lowest = layer;
                        if (layer > highest) highest = layer;
                    }
                }

            if (lowest > highest) (lowest, highest) = (0, 0);

            if (clamped)
                report?.Approximated("layers_clamped",
                    "This level is organised more finely than there is draw order to spend on it, so the outermost objects share a layer. The Auto layer mode packs the same level into far fewer layers.",
                    path);

            return new Plan(mode, stride, span, null, lowest, highest);
        }

        private static int ToOrder(VgdObject source, ABLayerImport mode, int stride) => mode switch
        {
            ABLayerImport.OnlyDepth => VgdObject.MaxDepth - ToDepth(source),
            ABLayerImport.OnlyEditor => ToEditorIndex(source),
            ABLayerImport.DepthAndEditor
                => ToEditorIndex(source) * stride + VgdObject.MaxDepth - ToDepth(source),
            _ => VgdObject.MaxDepth - ToDepth(source),
        };

        #endregion

        #region Resolve

        /// <summary> What one list's resolve produced: a layer per object, plus the range they
        /// occupy. </summary>
        public readonly struct Result
        {
            /// <summary> One layer per source object, in the order they were handed over. </summary>
            public int[] Layers { get; }

            /// <summary> The same, by the source object's own id. Objects with no id are absent. </summary>
            public Dictionary<string, int> ById { get; }

            /// <summary> Lowest and highest layer anything in THIS list landed on; both 0 for an
            /// empty list. The level's whole range is the plan's, not this. </summary>
            public int Lowest { get; }

            /// <summary> The largest layer actually assigned. </summary>
            public int Highest { get; }

            /// <summary> The layer assigned to each object, plus the range they ended up covering. </summary>
            public Result(int[] layers, Dictionary<string, int> byId, int lowest, int highest)
            {
                Layers = layers;
                ById = byId;
                Lowest = lowest;
                Highest = highest;
            }

            /// <summary> The layer of one source object, or 0 for one this resolve never saw. </summary>
            public int Get(string sourceId)
                => sourceId != null && ById != null && ById.TryGetValue(sourceId, out var layer) ? layer : 0;
        }

        /// <summary> Reads one list's layers off a plan. A null plan resolves the list on its own,
        /// which is what a template imported outside any level gets. </summary>
        public static Result Resolve(IReadOnlyList<VgdObject> sources, ABOptions options,
            InteropReport report = null, string path = null, Plan plan = null)
        {
            var count = sources?.Count ?? 0;
            var layers = new int[count];
            if (count == 0) return new Result(layers, new Dictionary<string, int>(), 0, 0);

            plan ??= Build(sources, options, report, path);

            var byId = new Dictionary<string, int>(count);
            var lowest = int.MaxValue;
            var highest = int.MinValue;

            for (var i = 0; i < count; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                var layer = plan.Get(source);
                layers[i] = layer;
                if (!string.IsNullOrEmpty(source.Id)) byId[source.Id] = layer;

                if (layer < lowest) lowest = layer;
                if (layer > highest) highest = layer;
            }

            if (lowest > highest) return new Result(layers, byId, 0, 0);
            return new Result(layers, byId, lowest, highest);
        }

        #endregion
    }
}