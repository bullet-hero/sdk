using System;
using System.Collections.Generic;
using BH.SDK.Rules;

namespace BH.SDK.Interop.AfterBeat
{
    // THE TIMELINE ROW IS THE LAYER in this editor, so how a converted level is laid out on screen
    // and what it draws in front of are one decision - and ABLayerMap's Auto spent all of it on
    // depth. That is the right ORDER and the wrong LAYOUT: a real level leaves most of its objects
    // at the default depth, so several hundred clips landed on one row, 128 of them alive at once.
    // A level authored here (new-zero-demo is the reference) does the opposite, and this packer is
    // that reference read as rules:
    //
    //   ROLE BANDS AROUND THE PLAYER LINE. Default content fills downwards from layer 0, AbovePlayer
    //   upwards from 1, Background below everything Default used - the same bands ABLayerMap lays
    //   out, with the same player line.
    //
    //   TIME IS NOT A LAYER AXIS; INSIDE A BAND, INTERVALS ARE PACKED. A unit takes the first row
    //   that is free for its whole lifetime, so rows are reused round-robin once free and two
    //   objects that never share the screen share a row.
    //
    //   CHILDREN TAKE THEIR OWN LAYER 1..N by sibling order, never 0 and never negative, so a
    //   subtree is a contiguous block of rows under its root.
    //
    // DEPTH STILL DECIDES WHAT DRAWS IN FRONT, strictly, between anything that is on screen at the
    // same time: a unit is placed wholly below every NEARER unit it overlaps (Default, filled
    // nearest-first) or wholly above every FARTHER one (AbovePlayer, filled farthest-first). Equal
    // depths only have to keep off each other's rows - over there they share a sorting order and a
    // draw distance, i.e. their order is undefined in the source game too.
    //
    // A PLACEMENT OCCUPIES ONE ROW AND DRAWS OVER MANY. In the editor's Partial view its copies are
    // hidden, so the row it needs is its own; but its template's content draws at its layer plus
    // whatever the template packed to, and that RENDER range is what the ordering constraints read.
    // Templates are packed first for exactly that reason.
    //
    // What a contiguous block cannot express is reported rather than hidden: a unit whose own
    // children span depths on both sides of an overlapping unit's cannot be both above and below
    // it, and a child farther than its parent still draws in front of it (its layer is above).
    //
    // Pure: DTOs in, layers out, nothing read from a model - so it can be tested on its own and a
    // later mod_* generator can reuse it on a native level.

    /// <summary> Lays one scope's objects out on rows: packed in time, ordered by depth. </summary>
    public static class ABLayoutPacker
    {
        /// <summary> Which stretch of rows a unit is packed into. </summary>
        public enum Band
        {
            /// <summary> Behind the player, filled downwards from layer 0. </summary>
            Default = 0,

            /// <summary> In front of the player, filled upwards from layer 1. </summary>
            AbovePlayer = 1,

            /// <summary> Below everything Default used. </summary>
            Background = 2,

            /// <summary> Below everything Background used. </summary>
            Parallax = 3,
        }

        /// <summary> Which kind of scope is being packed. </summary>
        public enum Scope
        {
            /// <summary> A level: bands around the player line. </summary>
            Level = 0,

            /// <summary> A prefab template: everything packs upwards from layer 1, above
            /// <c>Prefab.Root</c>, and the placement carries the band. </summary>
            Template = 1,
        }

        /// <summary> One object or placement to lay out. </summary>
        public sealed class Entry
        {
            /// <summary> Unique within the scope. </summary>
            public string Id;

            /// <summary> Another entry's id, or anything else (null, "camera", a missing id) for a
            /// root. </summary>
            public string ParentId;

            /// <summary> A placement is a unit of its own wherever it hangs. </summary>
            public bool IsPlacement;

            /// <summary> First frame, resolved. </summary>
            public int Start;

            /// <summary> End boundary, exclusive - <c>[Start, End)</c>. </summary>
            public int End;

            /// <summary> Where the entry's source asked to be drawn. </summary>
            public Band Band;

            /// <summary> 0 (nearest) .. 60 (farthest). For a placement: its template's nearest. </summary>
            public int Depth;

            /// <summary> For a placement: its template's farthest depth. Ignored otherwise. </summary>
            public int FarDepth;

            /// <summary> Draws something - an empty does not, and its depth orders nothing. </summary>
            public bool Rendered = true;

            /// <summary> Can hurt the player. For a placement: anything in its template can. Read
            /// only when a pack is asked to lift colliders above the player line. </summary>
            public bool Collides;

            /// <summary> For a placement: how many rows above its own its template draws. </summary>
            public int RenderHeight;

            /// <summary> Source order, the last tie-break. </summary>
            public int Order;
        }

        /// <summary> What one pack produced. </summary>
        public sealed class Result
        {
            /// <summary> Each entry's own (parent-relative) layer. </summary>
            public Dictionary<string, int> OwnLayers { get; } = new();

            /// <summary> Each entry's effective (parent-chain-summed) layer. </summary>
            public Dictionary<string, int> EffectiveLayers { get; } = new();

            /// <summary> Lowest effective layer used; 0 for an empty scope. </summary>
            public int Lowest { get; internal set; }

            /// <summary> Highest effective layer anything draws at, render ranges included. For a
            /// template this is the placement's <see cref="Entry.RenderHeight"/>. </summary>
            public int Highest { get; internal set; }

            /// <summary> Entries whose layer had to be clamped into the format's range. </summary>
            public int Clamped { get; internal set; }

            /// <summary> Pairs of overlapping entries drawn in an order their depths contradict. </summary>
            public int Approximated { get; internal set; }

            /// <summary> Template scope: the band most of its drawn entries asked for. </summary>
            public Band MajorityBand { get; internal set; }

            /// <summary> Template scope: whether its drawn entries asked for more than one band. </summary>
            public bool MixedBands { get; internal set; }

            /// <summary> Whether anything in the scope can hurt the player. </summary>
            public bool AnyCollides { get; internal set; }

            /// <summary> Nearest depth anything drawn in the scope has; 60 when nothing draws. </summary>
            public int NearestDepth { get; internal set; } = MaxDepth;

            /// <summary> Farthest depth anything drawn in the scope has; 0 when nothing draws. </summary>
            public int FarthestDepth { get; internal set; }

            /// <summary> The own layer of one entry, 0 for one this pack never saw. </summary>
            public int GetOwn(string id) => id != null && OwnLayers.TryGetValue(id, out var layer) ? layer : 0;
        }

        private const int MaxDepth = 60;

        private sealed class Unit
        {
            public Entry Root;
            public readonly List<Entry> Members = new();
            public int Start;
            public int End;
            public int Key;
            public int Far;
            public Band Band;
            public int RowHeight;
            public int RenderTop;
            public int Layer;
        }

        /// <summary> Lays one scope out. Pack every template before the level that places them - a
        /// placement's <see cref="Entry.RenderHeight"/> is its template's
        /// <see cref="Result.Highest"/>. </summary>
        public static Result Pack(IReadOnlyList<Entry> entries, Scope scope,
            InteropReport report = null, string path = null, bool collidersAbovePlayer = false)
        {
            var result = new Result();
            if (entries == null || entries.Count == 0) return result;

            var byId = new Dictionary<string, Entry>(entries.Count);
            foreach (var entry in entries)
                if (entry != null && !string.IsNullOrEmpty(entry.Id))
                    byId[entry.Id] = entry;

            var children = new Dictionary<Entry, List<Entry>>();
            foreach (var entry in byId.Values)
            {
                if (entry.IsPlacement) continue;
                if (entry.ParentId == null || !byId.TryGetValue(entry.ParentId, out var parent)) continue;
                if (!children.TryGetValue(parent, out var list)) children[parent] = list = new List<Entry>();
                list.Add(entry);
            }

            foreach (var list in children.Values) list.Sort(CompareSiblings);

            // Offsets are relative to the parent; height(n) is the rows its subtree spans above it,
            // which for this stacking is simply how many descendants it has.
            var offsets = new Dictionary<Entry, int>();
            var heights = new Dictionary<Entry, int>();
            var units = new List<Unit>();

            foreach (var entry in byId.Values)
            {
                var isRoot = entry.IsPlacement || entry.ParentId == null || !byId.ContainsKey(entry.ParentId);
                if (!isRoot) continue;

                var unit = new Unit { Root = entry };
                Layout(entry, children, offsets, heights, unit.Members, new HashSet<Entry>());
                Describe(unit, heights, collidersAbovePlayer);
                units.Add(unit);
                foreach (var member in unit.Members)
                    if (member.Collides) result.AnyCollides = true;
            }

            if (scope == Scope.Template)
            {
                SummarizeBands(units, result);
                PackUp(units, ValueRules.FirstLayerAbovePlayer, result);
            }
            else
            {
                var above = units.FindAll(u => u.Band == Band.AbovePlayer);
                PackUp(above, ValueRules.FirstLayerAbovePlayer, result);

                var top = ValueRules.LastLayerBehindPlayer;
                foreach (var band in new[] { Band.Default, Band.Background, Band.Parallax })
                {
                    var group = units.FindAll(u => u.Band == band);
                    if (group.Count == 0) continue;
                    top = PackDown(group, top, result) - 1;
                }
            }

            Resolve(units, offsets, byId, result);

            if (result.Clamped > 0)
                report?.Approximated("layers_clamped",
                    $"{result.Clamped} objects would have landed outside this format's layer range and were clamped onto its edge; they share a layer with their neighbours.",
                    path);
            if (result.Approximated > 0)
                report?.Approximated("draw_order_approximated",
                    $"{result.Approximated} times a group of objects that has to stay together spans depths on both sides of something it overlaps (or a child is farther than its parent), so its draw order there follows the group rather than the depths.",
                    path);
            if (result.MixedBands)
                report?.Approximated("band_mixed",
                    "Some prefabs hold objects in more than one render band; each is placed in the band most of its objects use.",
                    path);

            return result;
        }

        #region Units

        // Farther first, so a subtree reads bottom-up the way it draws; then time, then file order.
        private static int CompareSiblings(Entry left, Entry right)
        {
            var byDepth = right.Depth.CompareTo(left.Depth);
            if (byDepth != 0) return byDepth;
            var byStart = left.Start.CompareTo(right.Start);
            return byStart != 0 ? byStart : left.Order.CompareTo(right.Order);
        }

        private static int Layout(Entry entry, Dictionary<Entry, List<Entry>> children,
            Dictionary<Entry, int> offsets, Dictionary<Entry, int> heights, List<Entry> members,
            HashSet<Entry> seen)
        {
            if (!seen.Add(entry)) return 0;
            members.Add(entry);

            var height = 0;
            if (children.TryGetValue(entry, out var list))
                foreach (var child in list)
                {
                    offsets[child] = height + 1;
                    height += Layout(child, children, offsets, heights, members, seen) + 1;
                }

            heights[entry] = height;
            return height;
        }

        // THE PROJECT'S OWN CONVENTION, opt-in: what can hurt the player sits at 1 and up, what
        // cannot at 0 and down. Afterbeat draws every Default object behind its player, hitting or
        // not, so this is a departure from the source and stays off unless asked for. A unit is
        // lifted whole when any member collides - it is one block of rows either way.
        private static void Describe(Unit unit, Dictionary<Entry, int> heights, bool collidersAbovePlayer)
        {
            unit.Start = int.MaxValue;
            unit.End = int.MinValue;
            unit.Key = int.MaxValue;
            unit.Far = int.MinValue;
            Entry nearest = null;

            foreach (var member in unit.Members)
            {
                unit.Start = Math.Min(unit.Start, member.Start);
                unit.End = Math.Max(unit.End, member.End);
                if (!member.Rendered) continue;

                var near = Math.Clamp(member.Depth, 0, MaxDepth);
                var far = Math.Clamp(member.IsPlacement ? Math.Max(member.Depth, member.FarDepth) : member.Depth, 0, MaxDepth);
                if (near < unit.Key)
                {
                    unit.Key = near;
                    nearest = member;
                }

                unit.Far = Math.Max(unit.Far, far);
            }

            if (nearest == null)
            {
                nearest = unit.Root;
                unit.Key = unit.Far = Math.Clamp(unit.Root.Depth, 0, MaxDepth);
            }

            if (unit.End <= unit.Start) unit.End = unit.Start + 1;
            unit.Band = nearest.Band;
            if (collidersAbovePlayer && unit.Band == Band.Default)
                foreach (var member in unit.Members)
                    if (member.Collides)
                    {
                        unit.Band = Band.AbovePlayer;
                        break;
                    }
            unit.RowHeight = heights[unit.Root];
            unit.RenderTop = unit.Root.IsPlacement
                ? Math.Max(unit.RowHeight, Math.Max(0, unit.Root.RenderHeight))
                : unit.RowHeight;
        }

        private static void SummarizeBands(List<Unit> units, Result result)
        {
            var counts = new int[4];
            foreach (var unit in units)
                foreach (var member in unit.Members)
                    if (member.Rendered)
                    {
                        counts[(int)member.Band]++;
                        result.NearestDepth = Math.Min(result.NearestDepth, Math.Clamp(member.Depth, 0, MaxDepth));
                        result.FarthestDepth = Math.Max(result.FarthestDepth, Math.Clamp(member.Depth, 0, MaxDepth));
                    }

            var best = 0;
            var used = 0;
            for (var i = 0; i < counts.Length; i++)
            {
                if (counts[i] > 0) used++;
                if (counts[i] > counts[best]) best = i;
            }

            result.MajorityBand = (Band)best;
            result.MixedBands = used > 1;
        }

        #endregion

        #region Packing

        private static bool Overlaps(Unit a, Unit b) => a.Start < b.End && b.Start < a.End;

        // Farthest first, each unit at the LOWEST row that is free for its lifetime and above the
        // render range of every farther unit it overlaps.
        private static void PackUp(List<Unit> units, int floor, Result result)
        {
            units.Sort((a, b) =>
            {
                var byKey = b.Key.CompareTo(a.Key);
                if (byKey != 0) return byKey;
                var byStart = a.Start.CompareTo(b.Start);
                return byStart != 0 ? byStart : a.Root.Order.CompareTo(b.Root.Order);
            });

            var rows = new Rows();
            var placed = new List<Unit>();

            foreach (var unit in units)
            {
                var lower = floor;
                foreach (var other in placed)
                {
                    if (other.Key <= unit.Key || !Overlaps(unit, other)) continue;
                    lower = Math.Max(lower, other.Layer + other.RenderTop + 1);
                    if (unit.Far > other.Key) result.Approximated++;
                }

                var layer = lower;
                while (!rows.IsFree(layer, layer + unit.RowHeight, unit.Start, unit.End)) layer++;

                unit.Layer = layer;
                rows.Take(layer, layer + unit.RowHeight, unit.Start, unit.End);
                placed.Add(unit);
            }
        }

        // Nearest first, each unit at the HIGHEST row that is free for its lifetime, whose render
        // range stays at or under the band's top and wholly under every nearer unit it overlaps.
        // Returns the lowest row the band used, which is where the next band starts under.
        private static int PackDown(List<Unit> units, int top, Result result)
        {
            units.Sort((a, b) =>
            {
                var byKey = a.Key.CompareTo(b.Key);
                if (byKey != 0) return byKey;
                var byStart = a.Start.CompareTo(b.Start);
                return byStart != 0 ? byStart : a.Root.Order.CompareTo(b.Root.Order);
            });

            var rows = new Rows();
            var placed = new List<Unit>();
            var lowest = top + 1;

            foreach (var unit in units)
            {
                var upper = top - unit.RenderTop;
                foreach (var other in placed)
                {
                    if (other.Key >= unit.Key || !Overlaps(unit, other)) continue;
                    upper = Math.Min(upper, other.Layer - 1 - unit.RenderTop);
                    if (unit.Key < other.Far) result.Approximated++;
                }

                var layer = upper;
                while (!rows.IsFree(layer, layer + unit.RowHeight, unit.Start, unit.End)) layer--;

                unit.Layer = layer;
                rows.Take(layer, layer + unit.RowHeight, unit.Start, unit.End);
                placed.Add(unit);
                lowest = Math.Min(lowest, layer);
            }

            return lowest;
        }

        /// <summary> Which frames each row is taken for: per row, disjoint half-open intervals
        /// sorted by start. </summary>
        private sealed class Rows
        {
            private readonly Dictionary<int, List<(int Start, int End)>> _byRow = new();

            public bool IsFree(int from, int to, int start, int end)
            {
                for (var row = from; row <= to; row++)
                    if (_byRow.TryGetValue(row, out var list) && Intersects(list, start, end))
                        return false;
                return true;
            }

            public void Take(int from, int to, int start, int end)
            {
                for (var row = from; row <= to; row++)
                {
                    if (!_byRow.TryGetValue(row, out var list)) _byRow[row] = list = new List<(int, int)>();
                    list.Insert(LowerBound(list, start), (start, end));
                }
            }

            private static bool Intersects(List<(int Start, int End)> list, int start, int end)
            {
                var index = LowerBound(list, start);
                if (index < list.Count && list[index].Start < end) return true;
                return index > 0 && list[index - 1].End > start;
            }

            private static int LowerBound(List<(int Start, int End)> list, int start)
            {
                int low = 0, high = list.Count;
                while (low < high)
                {
                    var mid = (low + high) >> 1;
                    if (list[mid].Start < start) low = mid + 1;
                    else high = mid;
                }

                return low;
            }
        }

        #endregion

        #region Output

        private static void Resolve(List<Unit> units, Dictionary<Entry, int> offsets,
            Dictionary<string, Entry> byId, Result result)
        {
            var lowest = int.MaxValue;
            var highest = int.MinValue;

            foreach (var unit in units)
            {
                foreach (var member in unit.Members)
                {
                    // Members are in layout order, so a parent is always resolved before its children.
                    var effective = member == unit.Root
                        ? unit.Layer
                        : result.EffectiveLayers[member.ParentId] + offsets[member];

                    var clamped = Math.Clamp(effective, ValueRules.MinLayer, ValueRules.MaxLayer);
                    if (clamped != effective) result.Clamped++;

                    result.EffectiveLayers[member.Id] = clamped;
                    lowest = Math.Min(lowest, clamped);
                    highest = Math.Max(highest, clamped);
                }

                highest = Math.Max(highest, unit.Layer + unit.RenderTop);
            }

            // Own = effective minus the parent's effective. A unit root hanging off an entry of
            // another unit (a placement under an object) subtracts that entry's layer; a root with
            // no parent in this scope - the camera included - subtracts nothing.
            foreach (var pair in result.EffectiveLayers)
            {
                var entry = byId[pair.Key];
                var parentEffective = entry.ParentId != null
                                      && result.EffectiveLayers.TryGetValue(entry.ParentId, out var parent)
                    ? parent
                    : 0;
                result.OwnLayers[pair.Key] = Math.Clamp(pair.Value - parentEffective,
                    ValueRules.MinLayer, ValueRules.MaxLayer);
            }

            if (lowest > highest) (lowest, highest) = (0, 0);
            result.Lowest = lowest;
            result.Highest = Math.Min(highest, ValueRules.MaxLayer);
        }

        #endregion
    }
}
