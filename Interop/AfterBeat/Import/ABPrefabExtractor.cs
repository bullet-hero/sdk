using System;
using System.Collections.Generic;
using System.Globalization;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Interop.AfterBeat.Import
{
    // PREFAB STRUCTURE IS DECIDED ON THE SOURCE DOCUMENT, BEFORE ANYTHING IS MINTED, because the
    // layout packer needs the final set of entries: an object that is about to become part of a
    // template must not take a row in the level, and a placement that is about to exist must. So
    // this pass rewrites a COPY of the parsed .vgd and the importer then reads it as if the source
    // author had written it that way. The caller's document is never touched.
    //
    // WHAT AFTERBEAT BAKES WHEN IT EXPANDS AN INSTANCE is the one fact everything in step b rests on,
    // read from its own code rather than from the wiki (ObjectManager.AddExpandedPrefabToLevel, and
    // AddPrefabToLevel for a live placement): every template object is deep-copied, given a fresh
    // id, parent references remapped through the id table, `st += placement.t - prefab.Offset`, and
    // pre_id / pre_iid stamped on. NOTHING ELSE CHANGES - the placement's position, scale and
    // rotation are not baked into the copies at all, which is why an expanded instance sits where the
    // template put it, and why a restored placement carries an identity transform. The editor block
    // is overwritten with the current editor layer, so it is never compared. prefabSourceObjectID,
    // which would pair a copy with its template object directly, is [JsonIgnore] over there and
    // never reaches a file - so copies are paired by name and relative order.
    //
    // NO NESTED PREFAB LEAVES THIS PASS. The source format nests (a template's own pobjs) and the
    // author ruled nesting out of anything this converter produces, so a nested placement is inlined
    // into its outer template - under an empty carrying the placement's transform - rather than
    // dropped. The editor's own paste of such a template (ObjectEditor.EditorAddPrefabExpandedToLevel)
    // times the nested placement on the same timeline as the template's objects, which is the timing
    // the inlined copies get.
    //
    // THE TWO STEPS THAT INVENT STRUCTURE are off by default: a repeated subtree and a duplicate
    // template are guesses about what the author meant, where an expanded instance is a record of
    // what they did.

    /// <summary> Recovers and creates prefab structure on a parsed .vgd before it is imported. </summary>
    public static class ABPrefabExtractor
    {
        /// <summary> The document the importer should read: the input itself when no option asks for
        /// anything, a rewritten copy otherwise. </summary>
        public static VgdLevel Apply(VgdLevel source, ABOptions options, InteropReport report)
        {
            if (source == null || options == null || !options.ImportPrefabs) return source;
            if (!options.FlattenNestedPrefabs && !options.RestoreExpandedInstances
                                              && !options.ExtractRepeatedSubtrees
                                              && !options.MergeDuplicateTemplates)
                return source;

            report ??= new InteropReport();
            var level = Clone(source);
            level.Objects ??= new List<VgdObject>();
            level.Prefabs ??= new List<VgpPrefab>();
            level.PrefabPlacements ??= new List<VgdPrefabPlacement>();

            if (options.FlattenNestedPrefabs) FlattenNested(level, report);

            var candidates = new List<Candidate>();
            if (options.RestoreExpandedInstances) CollectExpandedInstances(level, candidates, report);
            if (options.ExtractRepeatedSubtrees) CollectRepeatedSubtrees(level, candidates);

            ApplyCandidates(level, candidates, report);

            if (options.MergeDuplicateTemplates) MergeDuplicateTemplates(level, report);

            return level;
        }

        private static VgdLevel Clone(VgdLevel source)
            => ABSerialization.Deserialize<VgdLevel>(ABSerialization.Serialize(source, false));

        #region Nested

        /// <summary> Placement transform of an inlined nested placement, and the parent type that
        /// makes every channel of it reach the inlined content. </summary>
        public const string FullParentType = "111";

        // Resolved innermost-first with a stack guarding the recursion: a template placing itself,
        // directly or through another, has no finite expansion and is reported rather than looped.
        private static void FlattenNested(VgdLevel level, InteropReport report)
        {
            var byId = IndexTemplates(level.Prefabs);
            var flattened = 0;
            var cyclic = 0;
            var done = new HashSet<VgpPrefab>();

            foreach (var template in level.Prefabs)
                Flatten(template, new HashSet<VgpPrefab>());

            if (flattened > 0)
                report.Info("prefab_nested_flattened",
                    $"{flattened} prefab placements inside other prefabs were inlined into those prefabs as ordinary objects under an empty carrying the placement's transform; this format holds no nested prefabs.",
                    "prefabs");
            if (cyclic > 0)
                report.Dropped("prefab_nested_cycle",
                    $"{cyclic} nested prefab placements place a prefab inside itself, which never finishes expanding; they were dropped.",
                    "prefabs");

            void Flatten(VgpPrefab template, HashSet<VgpPrefab> stack)
            {
                if (template == null || done.Contains(template)) return;
                if (template.Placements is not { Count: > 0 })
                {
                    done.Add(template);
                    return;
                }

                stack.Add(template);
                template.Objects ??= new List<VgdObject>();

                foreach (var placement in template.Placements)
                {
                    if (placement == null || string.IsNullOrEmpty(placement.PrefabId)) continue;
                    if (!byId.TryGetValue(placement.PrefabId, out var inner) || inner == null) continue;

                    if (stack.Contains(inner))
                    {
                        cyclic++;
                        continue;
                    }

                    Flatten(inner, stack);
                    Inline(template, placement, inner);
                    flattened++;
                }

                template.Placements.Clear();
                stack.Remove(template);
                done.Add(template);
            }
        }

        private static void Inline(VgpPrefab outer, VgdPrefabPlacement placement, VgpPrefab inner)
        {
            var baseTime = placement.StartTime - inner.Offset;
            var prefix = $"{placement.Id}:";
            var holderId = $"{placement.Id}:{inner.Id}";

            var copies = new List<VgdObject>(inner.Objects?.Count ?? 0);
            var ids = new Dictionary<string, string>();
            if (inner.Objects != null)
                foreach (var obj in inner.Objects)
                    if (obj != null && !string.IsNullOrEmpty(obj.Id))
                        ids[obj.Id] = prefix + obj.Id;

            var start = baseTime;
            var end = baseTime;
            var endless = false;

            if (inner.Objects != null)
                foreach (var obj in inner.Objects)
                {
                    if (obj == null) continue;

                    var copy = CloneObject(obj);
                    copy.Id = ids.TryGetValue(obj.Id ?? string.Empty, out var mapped) ? mapped : prefix + copies.Count;
                    copy.StartTime = obj.StartTime + baseTime;
                    copy.SourcePrefabId = string.Empty;
                    copy.SourcePlacementId = string.Empty;

                    if (string.IsNullOrEmpty(obj.ParentId))
                    {
                        copy.ParentId = holderId;
                        copy.ParentType = FullParentType;
                    }
                    else if (ids.TryGetValue(obj.ParentId, out var parent))
                        copy.ParentId = parent;

                    start = Math.Min(start, copy.StartTime);
                    if ((ABAutokillType)copy.AutokillType == ABAutokillType.OldStyleNoAutokill) endless = true;
                    else end = Math.Max(end, ABTimeMap.ResolveEndTime(copy));

                    copies.Add(copy);
                }

            var holder = CreateEmpty(holderId, inner.Name, start, placement.ParentId,
                placement.GetValue(VgdPrefabPlacement.TrackIndex.Position, 0),
                placement.GetValue(VgdPrefabPlacement.TrackIndex.Position, 1),
                placement.GetValue(VgdPrefabPlacement.TrackIndex.Scale, 0, 1f),
                placement.GetValue(VgdPrefabPlacement.TrackIndex.Scale, 1, 1f),
                placement.GetValue(VgdPrefabPlacement.TrackIndex.Rotation, 0));

            if (endless) holder.AutokillType = (int)ABAutokillType.OldStyleNoAutokill;
            else
            {
                holder.AutokillType = (int)ABAutokillType.FixedTime;
                holder.AutokillOffset = Math.Max(end - start, ABTimeMap.SourceTimeStep);
            }

            outer.Objects.Add(holder);
            outer.Objects.AddRange(copies);
        }

        /// <summary> An empty Afterbeat object carrying one static transform - what a placement's
        /// transform becomes once the placement itself is gone. </summary>
        private static VgdObject CreateEmpty(string id, string name, float start, string parentId,
            float x, float y, float width, float height, float degrees)
        {
            var empty = new VgdObject
            {
                Id = id,
                Name = name ?? string.Empty,
                ObjectType = (int)ABObjectType.AlphaEmpty,
                StartTime = start,
                ParentId = parentId ?? string.Empty,
                ParentType = FullParentType,
            };

            empty.Move.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float> { x, y } });
            empty.Scale.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float> { width, height } });
            empty.Rotate.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float> { degrees } });
            empty.Color.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float> { 0f } });
            return empty;
        }

        #endregion

        #region Candidates

        // Every template this pass would CREATE is a candidate first and a template only once the
        // cap has had its say - LevelRules.MaxPrefabs counts the templates the source already has,
        // and a level at the limit keeps its content expanded rather than losing templates it
        // authored. Restoring onto an EXISTING template costs nothing against the cap, so those are
        // candidates too only to be applied through the same code.

        private sealed class Candidate
        {
            /// <summary> The objects that leave the level, one list per placement. </summary>
            public List<List<VgdObject>> Groups = new();

            /// <summary> The template the placements place; null when it already exists. </summary>
            public VgpPrefab Created;

            /// <summary> The existing template's id, when <see cref="Created"/> is null. </summary>
            public string ExistingId;

            /// <summary> One placement per group, same order. </summary>
            public List<VgdPrefabPlacement> Placements = new();

            /// <summary> Forks rank last whatever they save. </summary>
            public bool IsFork;

            /// <summary> Which report line this counts towards. </summary>
            public string Kind;

            public int Saved => Groups.Count == 0 ? 0 : Groups[0].Count * (Groups.Count - 1);
        }

        private static void ApplyCandidates(VgdLevel level, List<Candidate> candidates, InteropReport report)
        {
            if (candidates.Count == 0) return;

            var created = new List<Candidate>();
            foreach (var candidate in candidates)
                if (candidate.Created != null)
                    created.Add(candidate);

            // Stable: equal savings keep the order they were found in, which is file order.
            var ranked = new List<(Candidate Candidate, int Index)>(created.Count);
            for (var i = 0; i < created.Count; i++) ranked.Add((created[i], i));
            ranked.Sort((a, b) =>
            {
                if (a.Candidate.IsFork != b.Candidate.IsFork) return a.Candidate.IsFork ? 1 : -1;
                var bySaved = b.Candidate.Saved.CompareTo(a.Candidate.Saved);
                return bySaved != 0 ? bySaved : a.Index.CompareTo(b.Index);
            });

            var room = Math.Max(0, LevelRules.MaxPrefabs - level.Prefabs.Count);
            var rejected = new HashSet<Candidate>();
            for (var i = room; i < ranked.Count; i++) rejected.Add(ranked[i].Candidate);

            var removed = new HashSet<VgdObject>();
            var counts = new Dictionary<string, int>();

            foreach (var candidate in candidates)
            {
                if (rejected.Contains(candidate)) continue;

                if (candidate.Created != null) level.Prefabs.Add(candidate.Created);
                level.PrefabPlacements.AddRange(candidate.Placements);
                foreach (var group in candidate.Groups)
                foreach (var obj in group)
                    removed.Add(obj);

                counts[candidate.Kind] = counts.GetValueOrDefault(candidate.Kind) + candidate.Groups.Count;
            }

            if (removed.Count > 0) level.Objects.RemoveAll(removed.Contains);

            if (counts.TryGetValue(KindRestored, out var restored))
                report.Info(KindRestored,
                    $"{restored} prefab instances Afterbeat's editor had expanded into ordinary objects were turned back into placements of their prefab.",
                    "objects");
            if (counts.TryGetValue(KindForked, out var forked))
                report.Info(KindForked,
                    $"{forked} expanded prefab instances were edited after expanding (or their prefab is gone), so each became a placement of a new prefab holding exactly what it draws.",
                    "objects");
            if (counts.TryGetValue(KindSubtrees, out var subtrees))
                report.Info(KindSubtrees,
                    $"{subtrees} identical groups of objects were turned into placements of a shared prefab.",
                    "objects");
            if (rejected.Count > 0)
                report.Approximated("prefabs_over_cap",
                    $"This format allows {LevelRules.MaxPrefabs} prefabs per level; {rejected.Count} prefabs this import would have created did not fit, so their content stays as ordinary objects.",
                    "prefabs");
        }

        private const string KindRestored = "instances_restored";
        private const string KindForked = "instances_forked";
        private const string KindSubtrees = "subtrees_extracted";

        #endregion

        #region Expanded instances

        private static void CollectExpandedInstances(VgdLevel level, List<Candidate> candidates,
            InteropReport report)
        {
            var groups = new Dictionary<string, List<VgdObject>>();
            var order = new List<string>();

            foreach (var obj in level.Objects)
            {
                if (obj == null || string.IsNullOrEmpty(obj.SourcePlacementId)
                                || string.IsNullOrEmpty(obj.SourcePrefabId))
                    continue;

                if (!groups.TryGetValue(obj.SourcePlacementId, out var group))
                {
                    groups[obj.SourcePlacementId] = group = new List<VgdObject>();
                    order.Add(obj.SourcePlacementId);
                }

                group.Add(obj);
            }

            if (order.Count == 0) return;

            // Only templates the importer will actually keep: one past the cap is dropped with its
            // placements, and restoring onto it would turn content that draws into content that
            // does not. Such a group forks instead, and competes for the cap like any other.
            var templates = IndexTemplates(level.Prefabs.Count > LevelRules.MaxPrefabs
                ? level.Prefabs.GetRange(0, LevelRules.MaxPrefabs)
                : level.Prefabs);
            var usedIds = CollectIds(level);
            var kept = 0;

            foreach (var instanceId in order)
            {
                var group = groups[instanceId];
                if (!IsSelfContained(group, level.Objects))
                {
                    kept++;
                    continue;
                }

                templates.TryGetValue(group[0].SourcePrefabId, out var template);

                if (template != null && TryMatch(group, template, out var shift))
                {
                    var restored = new Candidate { ExistingId = template.Id, Kind = KindRestored };
                    restored.Groups.Add(group);
                    restored.Placements.Add(CreatePlacement(Unique(instanceId, usedIds), template.Id,
                        shift + template.Offset));
                    candidates.Add(restored);
                    continue;
                }

                var forkId = Unique($"{instanceId}:fork", usedIds);
                var fork = CreateTemplate(forkId,
                    template != null ? $"{template.Name} (edited)" : NameOf(group), group,
                    template?.Type ?? (int)ABPrefabType.Misc1, out var baseTime);

                var forked = new Candidate { Created = fork, IsFork = true, Kind = KindForked };
                forked.Groups.Add(group);
                forked.Placements.Add(CreatePlacement(Unique(instanceId, usedIds), forkId, baseTime));
                candidates.Add(forked);
            }

            if (kept > 0)
                report.Info("instances_kept_expanded",
                    $"{kept} expanded prefab instances are tied to objects outside themselves (a parent, a child, or the camera), which a placement cannot express; they stay as ordinary objects.",
                    "objects");
        }

        // A placement is one static transform over a closed set of objects. A group that parents out
        // of itself, is parented INTO by an object staying in the level, or hangs off the camera
        // cannot be carried by one - moving it into a template would cut those links.
        //
        // A parent that exists NOWHERE is not a link: the importer reads it as a root either way
        // (parent_missing), inside a template as much as in the level. That is the ordinary shape of
        // an instance whose author deleted one of its objects after expanding it.
        private static bool IsSelfContained(List<VgdObject> group, List<VgdObject> all)
        {
            var ids = new HashSet<string>();
            foreach (var obj in group)
                if (!string.IsNullOrEmpty(obj.Id))
                    ids.Add(obj.Id);

            var existing = new HashSet<string>();
            foreach (var obj in all)
                if (obj != null && !string.IsNullOrEmpty(obj.Id))
                    existing.Add(obj.Id);

            foreach (var obj in group)
            {
                if (string.IsNullOrEmpty(obj.ParentId) || ids.Contains(obj.ParentId)) continue;
                if (obj.IsParentedToCamera || existing.Contains(obj.ParentId)) return false;
            }

            var members = new HashSet<VgdObject>(group);
            foreach (var obj in all)
                if (obj != null && !members.Contains(obj)
                                && !string.IsNullOrEmpty(obj.ParentId) && ids.Contains(obj.ParentId))
                    return false;

            return true;
        }

        // Paired by name and then by relative order among the objects sharing that name - the pairing
        // survives an author reordering unrelated objects, and is exact for a group nobody touched.
        // Everything the expand does not touch has to be identical; the start times have to differ
        // by ONE shift, which is the placement's own time.
        private static bool TryMatch(List<VgdObject> group, VgpPrefab template, out float shift)
        {
            shift = 0f;
            var objects = template.Objects;
            if (objects == null || objects.Count != group.Count || group.Count == 0) return false;

            // Among objects sharing a name, one whose content is identical wins over the next in
            // order - an instance whose author reordered two same-named objects still pairs up.
            var byName = new Dictionary<string, List<(VgdObject Object, JObject Json)>>();
            foreach (var obj in objects)
            {
                if (obj == null) return false;
                var name = obj.Name ?? string.Empty;
                if (!byName.TryGetValue(name, out var list)) byName[name] = list = new List<(VgdObject, JObject)>();
                list.Add((obj, Strip(obj)));
            }

            var pairs = new List<(VgdObject Copy, VgdObject Original)>(group.Count);
            var copyToOriginal = new Dictionary<string, string>();
            foreach (var copy in group)
            {
                if (!byName.TryGetValue(copy.Name ?? string.Empty, out var list) || list.Count == 0)
                    return false;

                var json = Strip(copy);
                var index = list.FindIndex(candidate => JToken.DeepEquals(candidate.Json, json));
                if (index < 0) index = 0;

                var original = list[index].Object;
                list.RemoveAt(index);
                pairs.Add((copy, original));
                if (!string.IsNullOrEmpty(copy.Id)) copyToOriginal[copy.Id] = original.Id;
            }

            var first = true;
            foreach (var (copy, original) in pairs)
            {
                var delta = copy.StartTime - original.StartTime;
                if (first)
                {
                    shift = delta;
                    first = false;
                }
                else if (Math.Abs(delta - shift) > TimeTolerance) return false;

                var mappedParent = string.IsNullOrEmpty(copy.ParentId)
                    ? string.Empty
                    : copyToOriginal.GetValueOrDefault(copy.ParentId, copy.ParentId);
                if (mappedParent != (original.ParentId ?? string.Empty)) return false;

                if (!JToken.DeepEquals(Strip(copy), Strip(original))) return false;
            }

            return true;
        }

        /// <summary> Half the source format's own time grid: two times closer than this are the same
        /// time there. </summary>
        private const float TimeTolerance = ABTimeMap.SourceTimeStep * 0.5f;

        private static readonly string[] IdentityKeys =
        {
            ABNames.ObjectId, ABNames.ObjectPrefabId, ABNames.ObjectPrefabInstanceId,
            ABNames.ObjectParentId, ABNames.ObjectStartTime, ABNames.ObjectEditor,
        };

        /// <summary> An object's JSON without the fields an expand rewrites. </summary>
        private static JObject Strip(VgdObject obj)
        {
            var json = JObject.FromObject(obj, ABSerialization.GetSerializer());
            foreach (var key in IdentityKeys) json.Remove(key);
            return json;
        }

        #endregion

        #region Repeated subtrees

        // A subtree is a root object plus everything hanging off it. Two subtrees are the same
        // content when every object is identical once ids become positions, start times become
        // offsets from the root's, and the root's own placement - its first position, scale and
        // rotation keyframe - is taken out. Which of those three CAN be taken out depends on the
        // children: a placement's transform reaches everything under it, while the root's own
        // reaches a child only through the matching parent-type bit, so a channel some direct child
        // does not inherit stays inside the template and has to match exactly.
        private static void CollectRepeatedSubtrees(VgdLevel level, List<Candidate> candidates)
        {
            var taken = new HashSet<VgdObject>();
            foreach (var candidate in candidates)
            foreach (var group in candidate.Groups)
            foreach (var obj in group)
                taken.Add(obj);

            var children = new Dictionary<string, List<VgdObject>>();
            foreach (var obj in level.Objects)
            {
                if (obj == null || string.IsNullOrEmpty(obj.ParentId)) continue;
                if (!children.TryGetValue(obj.ParentId, out var list))
                    children[obj.ParentId] = list = new List<VgdObject>();
                list.Add(obj);
            }

            var buckets = new Dictionary<string, List<Subtree>>();
            var order = new List<string>();

            foreach (var root in level.Objects)
            {
                if (root == null || !string.IsNullOrEmpty(root.ParentId) || taken.Contains(root)) continue;
                if (string.IsNullOrEmpty(root.Id)) continue;

                var members = new List<VgdObject>();
                Collect(root, children, members, new HashSet<VgdObject>());
                if (members.Count < MinSubtreeSize) continue;

                var clean = true;
                foreach (var member in members)
                    if (taken.Contains(member) || member.IsParentedToCamera)
                    {
                        clean = false;
                        break;
                    }

                if (!clean || !TryDescribe(root, members, children, out var subtree)) continue;

                if (!buckets.TryGetValue(subtree.Signature, out var bucket))
                {
                    buckets[subtree.Signature] = bucket = new List<Subtree>();
                    order.Add(subtree.Signature);
                }

                bucket.Add(subtree);
            }

            var usedIds = CollectIds(level);

            foreach (var signature in order)
            {
                var bucket = buckets[signature];
                if (bucket.Count < 2) continue;

                var first = bucket[0];
                var templateId = Unique($"{first.Root.Id}:template", usedIds);
                var template = CreateTemplate(templateId, NameOf(first.Members), first.Members,
                    (int)ABPrefabType.Misc1, out _);
                ResetExtracted(template.Objects[0], first.Extracted);

                var candidate = new Candidate { Created = template, Kind = KindSubtrees };
                foreach (var subtree in bucket)
                {
                    candidate.Groups.Add(subtree.Members);
                    var placement = CreatePlacement(Unique($"{subtree.Root.Id}:placement", usedIds),
                        templateId, subtree.Root.StartTime);
                    WriteTransform(placement, subtree.Root, subtree.Extracted);
                    candidate.Placements.Add(placement);
                }

                candidates.Add(candidate);
            }
        }

        /// <summary> Smallest subtree worth a template - a single object is a placement of one
        /// object, which saves nothing. </summary>
        public const int MinSubtreeSize = 2;

        private sealed class Subtree
        {
            public VgdObject Root;
            public List<VgdObject> Members;
            public string Signature;
            public Channels Extracted;
        }

        [Flags]
        private enum Channels
        {
            None = 0,
            Position = 1,
            Scale = 2,
            Rotation = 4,
        }

        private static void Collect(VgdObject obj, Dictionary<string, List<VgdObject>> children,
            List<VgdObject> into, HashSet<VgdObject> seen)
        {
            if (!seen.Add(obj)) return;
            into.Add(obj);
            if (string.IsNullOrEmpty(obj.Id) || !children.TryGetValue(obj.Id, out var list)) return;
            foreach (var child in list) Collect(child, children, into, seen);
        }

        private static bool TryDescribe(VgdObject root, List<VgdObject> members,
            Dictionary<string, List<VgdObject>> children, out Subtree subtree)
        {
            subtree = null;

            // A placement is static over there, so a root whose transform MOVES keeps it all.
            if (!IsStaticChannel(root.Move) || !IsStaticChannel(root.Scale) || !IsStaticChannel(root.Rotate))
                return false;

            var extracted = Channels.Position | Channels.Scale | Channels.Rotation;
            if (children.TryGetValue(root.Id, out var direct))
                foreach (var child in direct)
                {
                    if (!HasBit(child, 0)) extracted &= ~Channels.Position;
                    if (!HasBit(child, 1)) extracted &= ~Channels.Scale;
                    if (!HasBit(child, 2)) extracted &= ~Channels.Rotation;
                }

            var index = new Dictionary<string, int>();
            for (var i = 0; i < members.Count; i++)
                if (!string.IsNullOrEmpty(members[i].Id))
                    index[members[i].Id] = i;

            var signature = new JArray();
            for (var i = 0; i < members.Count; i++)
            {
                var obj = members[i];
                var json = Strip(obj);
                json["#i"] = i;
                json["#p"] = string.IsNullOrEmpty(obj.ParentId) ? -1 : index.GetValueOrDefault(obj.ParentId, -2);
                json["#t"] = Math.Round(obj.StartTime - root.StartTime, 3).ToString(CultureInfo.InvariantCulture);

                if (i == 0) RemoveExtracted(json, extracted);
                signature.Add(json);
            }

            subtree = new Subtree
            {
                Root = root,
                Members = members,
                Signature = signature.ToString(Newtonsoft.Json.Formatting.None),
                Extracted = extracted,
            };
            return true;
        }

        private static bool IsStaticChannel(VgdTrack track)
        {
            if (track?.Keyframes == null || track.Keyframes.Count == 0) return true;
            if (track.Keyframes.Count > 1) return false;

            var key = track.Keyframes[0];
            return key != null && key.Time == 0f && key.RandomType == 0;
        }

        private static bool HasBit(VgdObject child, int index)
        {
            var type = string.IsNullOrEmpty(child.ParentType) ? VgdObject.DefaultParentType : child.ParentType;
            return index < type.Length && type[index] == '1';
        }

        // The extracted channel's keyframe VALUES leave the signature; the keyframe itself stays, so
        // a root authored with and one authored without a key still read as different content.
        private static void RemoveExtracted(JObject json, Channels extracted)
        {
            if (json[ABNames.ObjectTracks] is not JArray tracks) return;

            Clear(VgdObject.TrackIndex.Move, Channels.Position);
            Clear(VgdObject.TrackIndex.Scale, Channels.Scale);
            Clear(VgdObject.TrackIndex.Rotate, Channels.Rotation);

            void Clear(int track, Channels channel)
            {
                if ((extracted & channel) == 0 || track >= tracks.Count) return;
                if (tracks[track]?[ABNames.TrackKeyframes] is not JArray keys) return;
                foreach (var key in keys)
                    if (key is JObject keyObject)
                        keyObject.Remove(ABNames.KeyframeValues);
            }
        }

        private static void ResetExtracted(VgdObject root, Channels extracted)
        {
            if ((extracted & Channels.Position) != 0) SetFirst(root.Move, 0f, 0f);
            if ((extracted & Channels.Scale) != 0) SetFirst(root.Scale, 1f, 1f);
            if ((extracted & Channels.Rotation) != 0) SetFirst(root.Rotate, 0f);
        }

        private static void SetFirst(VgdTrack track, params float[] values)
        {
            if (track == null) return;
            if (track.Keyframes.Count == 0) track.Keyframes.Add(new VgdKeyframe());
            track.Keyframes[0].Values = new List<float>(values);
        }

        private static void WriteTransform(VgdPrefabPlacement placement, VgdObject root, Channels extracted)
        {
            if ((extracted & Channels.Position) != 0)
                placement.Tracks[VgdPrefabPlacement.TrackIndex.Position].Values =
                    new List<float> { First(root.Move, 0, 0f), First(root.Move, 1, 0f) };
            if ((extracted & Channels.Scale) != 0)
                placement.Tracks[VgdPrefabPlacement.TrackIndex.Scale].Values =
                    new List<float> { First(root.Scale, 0, 1f), First(root.Scale, 1, 1f) };
            if ((extracted & Channels.Rotation) != 0)
                placement.Tracks[VgdPrefabPlacement.TrackIndex.Rotation].Values =
                    new List<float> { First(root.Rotate, 0, 0f) };
        }

        private static float First(VgdTrack track, int index, float fallback)
        {
            var values = track?.Keyframes is { Count: > 0 } keys ? keys[0]?.Values : null;
            return values != null && index < values.Count ? values[index] : fallback;
        }

        #endregion

        #region Duplicate templates

        private static void MergeDuplicateTemplates(VgdLevel level, InteropReport report)
        {
            var survivors = new Dictionary<string, VgpPrefab>();
            var redirect = new Dictionary<string, string>();

            foreach (var template in level.Prefabs)
            {
                if (template == null || string.IsNullOrEmpty(template.Id)) continue;

                var signature = Signature(template);
                if (survivors.TryGetValue(signature, out var survivor)) redirect[template.Id] = survivor.Id;
                else survivors[signature] = template;
            }

            if (redirect.Count == 0) return;

            level.Prefabs.RemoveAll(t => t != null && redirect.ContainsKey(t.Id ?? string.Empty));
            foreach (var placement in level.PrefabPlacements)
                if (placement != null && redirect.TryGetValue(placement.PrefabId ?? string.Empty, out var target))
                    placement.PrefabId = target;

            report.Info("templates_merged",
                $"{redirect.Count} prefabs held exactly the same objects as another one and were merged into it; their placements now place the survivor.",
                "prefabs");
        }

        private static string Signature(VgpPrefab template)
        {
            var index = new Dictionary<string, int>();
            var objects = template.Objects ?? new List<VgdObject>();
            for (var i = 0; i < objects.Count; i++)
                if (objects[i] != null && !string.IsNullOrEmpty(objects[i].Id))
                    index[objects[i].Id] = i;

            var signature = new JArray { template.Offset.ToString("R", CultureInfo.InvariantCulture) };
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                var json = Strip(obj);
                json["#p"] = string.IsNullOrEmpty(obj.ParentId) ? "" :
                    index.TryGetValue(obj.ParentId, out var p) ? p.ToString(CultureInfo.InvariantCulture) :
                    obj.ParentId;
                json["#t"] = obj.StartTime.ToString("R", CultureInfo.InvariantCulture);
                signature.Add(json);
            }

            return signature.ToString(Newtonsoft.Json.Formatting.None);
        }

        #endregion

        #region Helpers

        private static Dictionary<string, VgpPrefab> IndexTemplates(List<VgpPrefab> prefabs)
        {
            var byId = new Dictionary<string, VgpPrefab>();
            if (prefabs == null) return byId;
            foreach (var prefab in prefabs)
                if (prefab != null && !string.IsNullOrEmpty(prefab.Id))
                    byId[prefab.Id] = prefab;
            return byId;
        }

        private static HashSet<string> CollectIds(VgdLevel level)
        {
            var ids = new HashSet<string>();
            foreach (var obj in level.Objects)
                if (obj != null && !string.IsNullOrEmpty(obj.Id))
                    ids.Add(obj.Id);
            foreach (var placement in level.PrefabPlacements)
                if (placement != null && !string.IsNullOrEmpty(placement.Id))
                    ids.Add(placement.Id);
            foreach (var prefab in level.Prefabs)
                if (prefab != null && !string.IsNullOrEmpty(prefab.Id))
                    ids.Add(prefab.Id);
            return ids;
        }

        /// <summary> A deterministic id nothing else in the document carries - derived from the
        /// source's own ids rather than a fresh Guid, so importing the same file twice mints the
        /// same prefabs. </summary>
        private static string Unique(string wanted, HashSet<string> used)
        {
            var id = wanted;
            for (var i = 1; !used.Add(id); i++) id = $"{wanted}#{i}";
            return id;
        }

        private static VgdPrefabPlacement CreatePlacement(string id, string prefabId, float time)
        {
            var placement = new VgdPrefabPlacement { Id = id, PrefabId = prefabId, StartTime = time };
            placement.Tracks[VgdPrefabPlacement.TrackIndex.Position].Values = new List<float> { 0f, 0f };
            placement.Tracks[VgdPrefabPlacement.TrackIndex.Scale].Values = new List<float> { 1f, 1f };
            placement.Tracks[VgdPrefabPlacement.TrackIndex.Rotation].Values = new List<float> { 0f };
            return placement;
        }

        /// <summary> A template holding copies of <paramref name="objects"/>, re-timed so the earliest
        /// starts at zero; <paramref name="baseTime"/> is what was subtracted - the placement's
        /// time. The first object is kept first, which is the root for a subtree. </summary>
        private static VgpPrefab CreateTemplate(string id, string name, List<VgdObject> objects,
            int type, out float baseTime)
        {
            baseTime = float.MaxValue;
            foreach (var obj in objects) baseTime = Math.Min(baseTime, obj.StartTime);
            if (objects.Count == 0) baseTime = 0f;

            var template = new VgpPrefab { Id = id, Name = name ?? string.Empty, Type = type };
            foreach (var obj in objects)
            {
                var copy = CloneObject(obj);
                copy.StartTime = obj.StartTime - baseTime;
                copy.SourcePrefabId = string.Empty;
                copy.SourcePlacementId = string.Empty;
                template.Objects.Add(copy);
            }

            return template;
        }

        private static string NameOf(List<VgdObject> group)
        {
            foreach (var obj in group)
                if (string.IsNullOrEmpty(obj.ParentId) && !string.IsNullOrEmpty(obj.Name))
                    return obj.Name;
            return group.Count > 0 ? group[0].Name ?? string.Empty : string.Empty;
        }

        private static VgdObject CloneObject(VgdObject obj)
            => JObject.FromObject(obj, ABSerialization.GetSerializer())
                .ToObject<VgdObject>(ABSerialization.GetSerializer());

        #endregion
    }
}