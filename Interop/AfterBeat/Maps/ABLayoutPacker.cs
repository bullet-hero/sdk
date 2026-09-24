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

            /// <summary> Units placed where depth order with something alive at the same time could
            /// not be kept. </summary>
            public int Approximated { get; internal set; }

            /// <summary> Units that found no free row anywhere in their band and share one. </summary>
            public int Shared { get; internal set; }

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

            /// <summary> Every entry of the unit with its row offset above the root, parents
            /// before their children. </summary>
            public List<(Entry Entry, int Offset)> Members;

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

            var units = new List<Unit>();

            foreach (var entry in byId.Values)
            {
                var isRoot = entry.IsPlacement || entry.ParentId == null || !byId.ContainsKey(entry.ParentId);
                if (!isRoot) continue;

                var block = BuildBlock(entry, children, new HashSet<Entry>(), result);
                var unit = new Unit { Root = entry, Members = block.Members };
                Describe(unit, block, collidersAbovePlayer);
                units.Add(unit);
                foreach (var (member, _) in unit.Members)
                    if (member.Collides)
                        result.AnyCollides = true;
            }

            if (scope == Scope.Template)
            {
                SummarizeBands(units, result);
                Place(units, true, ValueRules.FirstLayerAbovePlayer, result);
            }
            else
            {
                var above = units.FindAll(u => u.Band == Band.AbovePlayer);
                Place(above, true, ValueRules.FirstLayerAbovePlayer, result);

                var top = ValueRules.LastLayerBehindPlayer;
                foreach (var band in new[] { Band.Default, Band.Background, Band.Parallax })
                {
                    var group = units.FindAll(u => u.Band == band);
                    if (group.Count == 0) continue;
                    Place(group, false, top, result);
                    var lowest = top;
                    foreach (var unit in group) lowest = Math.Min(lowest, unit.Layer);
                    top = lowest - 1;
                }
            }

            Resolve(units, byId, result);

            if (result.Clamped > 0)
                report?.Approximated("layers_clamped",
                    $"{result.Clamped} objects would have landed outside this format's layer range and were clamped onto its edge; they share a layer with their neighbours.",
                    path);
            if (result.Shared > 0)
                report?.Approximated("rows_shared",
                    $"{result.Shared} groups of objects found no free row anywhere in their band - more is alive at once than there are layers - so each shares the rows where the fewest of its objects overlap someone else.",
                    path);
            if (result.Approximated > 0)
                report?.Approximated("draw_order_approximated",
                    $"{result.Approximated} groups of objects could not keep depth order with everything alive at the same time (a group that has to stay together spans depths on both sides of something it overlaps), so they draw behind what they must not cover.",
                    path);
            if (result.MixedBands)
                report?.Approximated("band_mixed",
                    "Some prefabs hold objects in more than one render band; each is placed in the band most of its objects use.",
                    path);

            return result;
        }

        #region Units

        // A SUBTREE IS PACKED THE WAY THE LEVEL IS, one level of the tree at a time: siblings are
        // laid out farthest-first, each at the LOWEST offset above its parent that is free for every
        // one of its own members' lifetimes and above every farther sibling it overlaps. Stacking
        // every descendant on a row of its own is what this replaced, and it cost a real level its
        // whole range: a root emitting hundreds of four-second bursts over three minutes reserved a
        // row per burst for all three minutes, where a few dozen are ever alive at once. A child is
        // still never at 0 or below its parent, and siblings alive together still stack in depth
        // order.

        private sealed class Block
        {
            public Entry Root;
            public readonly List<(Entry Entry, int Offset)> Members = new();
            public int Height;
            public int Start;
            public int End;
            public int Key = int.MaxValue;
            public int Far = int.MinValue;
            public Entry Nearest;
        }

        private static Block BuildBlock(Entry entry, Dictionary<Entry, List<Entry>> children,
            HashSet<Entry> seen, Result result)
        {
            var block = new Block { Root = entry, Start = entry.Start, End = entry.End };
            block.Members.Add((entry, 0));
            Note(block, entry);
            if (!seen.Add(entry) || !children.TryGetValue(entry, out var list)) return Finish(block);

            var subs = new List<Block>(list.Count);
            foreach (var child in list)
                if (!seen.Contains(child))
                    subs.Add(BuildBlock(child, children, seen, result));

            subs.Sort((a, b) =>
            {
                var byKey = b.Key.CompareTo(a.Key);
                if (byKey != 0) return byKey;
                var byStart = a.Start.CompareTo(b.Start);
                return byStart != 0 ? byStart : a.Root.Order.CompareTo(b.Root.Order);
            });

            // Depth order between siblings is kept object against object, exactly as between units
            // (see Place) - judging a whole subtree by its whole lifetime is what let one rig of a
            // real level grow taller than the band it has to fit in.
            var rows = new Rows();
            var drawn = new DrawIndex();
            // AN EMPTY WITH ONE CHILD SHARES ITS ROW WITH IT. It draws nothing, so no depth order
            // is at stake between the two - only the timeline's one-clip-per-row, which is worth
            // less than the range: a real level whose every burst hangs off a pivot empty needed
            // two rows per burst and ran the band out, where one each leaves it near 0. The child
            // then carries own layer 0, the one place in a packed import a child does.
            var floor = !entry.Rendered && subs.Count == 1 ? 0 : 1;

            foreach (var sub in subs)
            {
                var (lower, upper) = Bounds(sub.Members, drawn);
                var from = Math.Max(floor, lower);

                int? found = null;
                for (var at = from; at <= upper; at++)
                    if (rows.IsFree(sub.Members, at))
                    {
                        found = at;
                        break;
                    }

                if (found == null)
                {
                    result.Approximated++;
                    var at = from;
                    while (!rows.IsFree(sub.Members, at)) at++;
                    found = at;
                }

                rows.Take(sub.Members, found.Value);
                drawn.Add(sub.Members, found.Value);
                var placedAt = found.Value;

                foreach (var (member, offset) in sub.Members) block.Members.Add((member, offset + placedAt));
                block.Height = Math.Max(block.Height, placedAt + sub.Height);
                block.Start = Math.Min(block.Start, sub.Start);
                block.End = Math.Max(block.End, sub.End);
                if (sub.Nearest == null) continue;

                if (sub.Key < block.Key)
                {
                    block.Key = sub.Key;
                    block.Nearest = sub.Nearest;
                }

                block.Far = Math.Max(block.Far, sub.Far);
            }

            return Finish(block);
        }

        private static void Note(Block block, Entry entry)
        {
            if (!entry.Rendered) return;

            var near = Math.Clamp(entry.Depth, 0, MaxDepth);
            var far = Math.Clamp(entry.IsPlacement ? Math.Max(entry.Depth, entry.FarDepth) : entry.Depth, 0, MaxDepth);
            if (near < block.Key)
            {
                block.Key = near;
                block.Nearest = entry;
            }

            block.Far = Math.Max(block.Far, far);
        }

        // A subtree that draws nothing is ordered by its root's own depth.
        private static Block Finish(Block block)
        {
            if (block.Nearest == null) block.Key = block.Far = Math.Clamp(block.Root.Depth, 0, MaxDepth);
            if (block.End <= block.Start) block.End = block.Start + 1;
            return block;
        }

        // THE PROJECT'S OWN CONVENTION, opt-in: what can hurt the player sits at 1 and up, what
        // cannot at 0 and down. Afterbeat draws every Default object behind its player, hitting or
        // not, so this is a departure from the source and stays off unless asked for. A unit is
        // lifted whole when any member collides - it is one block of rows either way.
        private static void Describe(Unit unit, Block block, bool collidersAbovePlayer)
        {
            unit.Start = block.Start;
            unit.End = block.End;
            unit.Key = block.Key;
            unit.Far = block.Far;
            unit.Band = (block.Nearest ?? unit.Root).Band;
            if (collidersAbovePlayer && unit.Band == Band.Default)
                foreach (var (member, _) in unit.Members)
                    if (member.Collides)
                    {
                        unit.Band = Band.AbovePlayer;
                        break;
                    }

            unit.RowHeight = block.Height;
            unit.RenderTop = unit.Root.IsPlacement
                ? Math.Max(unit.RowHeight, Math.Max(0, unit.Root.RenderHeight))
                : unit.RowHeight;
        }

        private static void SummarizeBands(List<Unit> units, Result result)
        {
            var counts = new int[4];
            foreach (var unit in units)
            foreach (var (member, _) in unit.Members)
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

        // STRICT ORDER IS A RULE BETWEEN OBJECTS THAT ARE ON SCREEN TOGETHER, and it is checked at
        // exactly that grain: a member of the unit being placed has to sit below every placed
        // object it overlaps in time that is strictly nearer, and above every one that is strictly
        // farther. Checking it unit against unit - the whole block below any nearer unit alive at
        // any point of the block's life - was the first version, and a root that lives for three
        // minutes and emits four-second bursts then pushed everything it ever overlapped under its
        // whole height: one real level needed 3400 rows where 1170 objects are ever alive at once.
        //
        // WHERE THE RULES CANNOT ALL HOLD, the layout stays near 0 rather than running off the
        // range. A unit whose nearer and farther neighbours leave it no window is placed as if only
        // the nearer ones counted (it draws behind what it must not cover), and is reported; a unit
        // with no free row left in the whole band takes the row where the fewest of its members
        // share time with someone else, which is two clips on a row instead of hundreds piled on
        // the band's floor.

        private static void Place(List<Unit> units, bool upwards, int edge, Result result)
        {
            units.Sort((a, b) =>
            {
                var byKey = upwards ? b.Key.CompareTo(a.Key) : a.Key.CompareTo(b.Key);
                if (byKey != 0) return byKey;
                var byLength = (b.End - b.Start).CompareTo(a.End - a.Start);
                if (byLength != 0) return byLength;
                var byStart = a.Start.CompareTo(b.Start);
                return byStart != 0 ? byStart : a.Root.Order.CompareTo(b.Root.Order);
            });

            var rows = new Rows();
            var drawn = new DrawIndex();

            foreach (var unit in units)
            {
                var (lower, upper) = Bounds(unit.Members, drawn);

                int first, last;
                if (upwards)
                {
                    first = edge;
                    last = ValueRules.MaxLayer - unit.RenderTop;
                }
                else
                {
                    first = ValueRules.MinLayer;
                    last = edge - unit.RenderTop;
                }

                var from = Math.Max(first, lower);
                var to = Math.Min(last, upper);
                var layer = Search(rows, unit, from, to, upwards);

                if (layer == null)
                {
                    result.Approximated++;

                    // Keep the side that matters for what the unit covers: filling downwards, stay
                    // under the nearer ones; filling upwards, stay over the farther ones.
                    layer = upwards
                        ? Search(rows, unit, Math.Max(first, lower), last, true)
                        : Search(rows, unit, first, Math.Min(last, upper), false);
                    layer ??= Search(rows, unit, first, last, upwards);
                    layer ??= LeastShared(rows, unit, first, last, upwards);
                }

                unit.Layer = layer.Value;
                if (!rows.IsFree(unit.Members, unit.Layer)) result.Shared++;
                rows.Take(unit.Members, unit.Layer);
                drawn.Add(unit.Members, unit.Layer);
            }
        }

        /// <summary> The window the unit's base row may take so that every one of its drawn members
        /// keeps depth order with every placed object it shares time with. </summary>
        private static (int Lower, int Upper) Bounds(List<(Entry Entry, int Offset)> members, DrawIndex drawn)
        {
            var lower = int.MinValue;
            var upper = int.MaxValue;

            foreach (var (member, offset) in members)
            {
                if (!member.Rendered) continue;

                var near = Math.Clamp(member.Depth, 0, MaxDepth);
                var far = member.IsPlacement ? Math.Clamp(Math.Max(member.Depth, member.FarDepth), 0, MaxDepth) : near;
                var top = offset + (member.IsPlacement ? Math.Max(0, member.RenderHeight) : 0);

                foreach (var other in drawn.Overlapping(member.Start, Math.Max(member.End, member.Start + 1)))
                {
                    if (near > other.Far) upper = Math.Min(upper, other.Bottom - top - 1);
                    else if (far < other.Near) lower = Math.Max(lower, other.Top - offset + 1);
                }
            }

            return (lower, upper);
        }

        /// <summary> The first base row in <c>[from, to]</c>, walking from the end nearest the
        /// band's edge, where every member's row is free for that member's lifetime. </summary>
        private static int? Search(Rows rows, Unit unit, int from, int to, bool upwards)
        {
            if (from > to) return null;

            if (upwards)
            {
                for (var layer = from; layer <= to; layer++)
                    if (rows.IsFree(unit.Members, layer))
                        return layer;
            }
            else
            {
                for (var layer = to; layer >= from; layer--)
                    if (rows.IsFree(unit.Members, layer))
                        return layer;
            }

            return null;
        }

        private static int LeastShared(Rows rows, Unit unit, int first, int last, bool upwards)
        {
            if (first > last) return upwards ? first : last;

            var best = upwards ? first : last;
            var bestCount = int.MaxValue;
            for (var i = 0; i <= last - first; i++)
            {
                var layer = upwards ? first + i : last - i;
                var count = rows.CountTaken(unit.Members, layer);
                if (count >= bestCount) continue;

                best = layer;
                bestCount = count;
                if (count == 0) break;
            }

            return best;
        }

        /// <summary> Every drawn object placed so far, findable by time. Rows and depths are
        /// stored as ranges because a placement draws its whole template over several rows. </summary>
        private sealed class DrawIndex
        {
            private const int BucketFrames = 64;

            public sealed class Drawn
            {
                public int Start;
                public int End;
                public int Near;
                public int Far;
                public int Bottom;
                public int Top;
            }

            private readonly Dictionary<int, List<Drawn>> _buckets = new();
            private readonly HashSet<Drawn> _seen = new();

            public void Add(List<(Entry Entry, int Offset)> members, int baseRow)
            {
                foreach (var (member, offset) in members)
                {
                    if (!member.Rendered) continue;

                    var near = Math.Clamp(member.Depth, 0, MaxDepth);
                    var drawn = new Drawn
                    {
                        Start = member.Start,
                        End = Math.Max(member.End, member.Start + 1),
                        Near = near,
                        Far = member.IsPlacement
                            ? Math.Clamp(Math.Max(member.Depth, member.FarDepth), 0, MaxDepth)
                            : near,
                        Bottom = baseRow + offset,
                        Top = baseRow + offset + (member.IsPlacement ? Math.Max(0, member.RenderHeight) : 0),
                    };

                    for (var bucket = Bucket(drawn.Start); bucket <= Bucket(drawn.End - 1); bucket++)
                    {
                        if (!_buckets.TryGetValue(bucket, out var list)) _buckets[bucket] = list = new List<Drawn>();
                        list.Add(drawn);
                    }
                }
            }

            public IEnumerable<Drawn> Overlapping(int start, int end)
            {
                _seen.Clear();
                for (var bucket = Bucket(start); bucket <= Bucket(end - 1); bucket++)
                {
                    if (!_buckets.TryGetValue(bucket, out var list)) continue;
                    foreach (var drawn in list)
                        if (drawn.Start < end && start < drawn.End && _seen.Add(drawn))
                            yield return drawn;
                }
            }

            private static int Bucket(int frame) =>
                frame >= 0 ? frame / BucketFrames : (frame - BucketFrames + 1) / BucketFrames;
        }

        /// <summary> Which frames each row is taken for: per row, disjoint half-open intervals
        /// sorted by start. </summary>
        // Per row, the half-open intervals it is taken for, sorted by start. Disjoint until a unit
        // is forced to share (see Place); a shared row is then searched linearly, since a binary
        // search over overlapping intervals can miss an early long one.
        private sealed class Rows
        {
            private readonly Dictionary<int, List<(int Start, int End)>> _byRow = new();
            private readonly HashSet<int> _shared = new();

            public bool IsFree(List<(Entry Entry, int Offset)> members, int baseRow)
            {
                foreach (var (member, offset) in members)
                    if (CountAt(baseRow + offset, member.Start, Math.Max(member.End, member.Start + 1), true) > 0)
                        return false;
                return true;
            }

            public void Take(List<(Entry Entry, int Offset)> members, int baseRow)
            {
                foreach (var (member, offset) in members)
                {
                    var row = baseRow + offset;
                    var start = member.Start;
                    var end = Math.Max(member.End, start + 1);
                    if (CountAt(row, start, end, true) > 0) _shared.Add(row);
                    if (!_byRow.TryGetValue(row, out var list)) _byRow[row] = list = new List<(int, int)>();
                    list.Insert(LowerBound(list, start), (start, end));
                }
            }

            /// <summary> How many taken intervals the members would land on - the cost of sharing. </summary>
            public int CountTaken(List<(Entry Entry, int Offset)> members, int baseRow)
            {
                var count = 0;
                foreach (var (member, offset) in members)
                    count += CountAt(baseRow + offset, member.Start, Math.Max(member.End, member.Start + 1), false);
                return count;
            }

            private int CountAt(int row, int start, int end, bool any)
            {
                if (!_byRow.TryGetValue(row, out var list)) return 0;

                if (!_shared.Contains(row))
                {
                    var index = LowerBound(list, start);
                    var hit = (index < list.Count && list[index].Start < end) ||
                              (index > 0 && list[index - 1].End > start);
                    if (any || !hit) return hit ? 1 : 0;
                }

                var count = 0;
                foreach (var (taken, until) in list)
                {
                    if (taken >= end) break;
                    if (until <= start) continue;
                    count++;
                    if (any) break;
                }

                return count;
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

        private static void Resolve(List<Unit> units, Dictionary<string, Entry> byId, Result result)
        {
            var lowest = int.MaxValue;
            var highest = int.MinValue;

            foreach (var unit in units)
            {
                foreach (var (member, offset) in unit.Members)
                {
                    var effective = unit.Layer + offset;
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