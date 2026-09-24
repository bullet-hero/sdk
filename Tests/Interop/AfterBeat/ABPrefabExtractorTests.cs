using System.Collections.Generic;
using System.Linq;
using BH.SDK.Interop;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // Every case here is built the way Afterbeat's own editor would have written it: an expanded
    // instance is the template's objects deep-copied with fresh ids, remapped parents, st shifted by
    // placement.t - prefab.Offset and pre_id / pre_iid stamped on - and nothing else changed
    // (ObjectManager.AddExpandedPrefabToLevel). A test that shifted anything more would be testing
    // an expand the source game does not do.

    /// <summary> Prefab structure recovered and created on a .vgd before it is imported. </summary>
    public class ABPrefabExtractorTests
    {
        private const string TemplateId = "tpl";

        #region Fixture

        private static VgpPrefab CreateTemplate(float offset = 0f)
        {
            var template = new VgpPrefab { Id = TemplateId, Name = "Burst", Offset = offset };

            var root = ABMockData.CreateObject("t-root");
            root.Name = "Root";
            root.StartTime = 0f;
            root.SourcePrefabId = TemplateId;
            template.Objects.Add(root);

            var child = ABMockData.CreateObject("t-child");
            child.Name = "Child";
            child.StartTime = 0.5f;
            child.ParentId = "t-root";
            child.SourcePrefabId = TemplateId;
            template.Objects.Add(child);

            return template;
        }

        /// <summary> What Afterbeat's expand writes for one placement at <paramref name="time"/>. </summary>
        private static List<VgdObject> Expand(VgpPrefab template, string instanceId, float time)
        {
            var ids = template.Objects.ToDictionary(o => o.Id, o => $"{instanceId}-{o.Id}");
            var copies = new List<VgdObject>();

            foreach (var original in template.Objects)
            {
                var copy = ABSerialization.Deserialize<VgdObject>(ABSerialization.Serialize(original));
                copy.Id = ids[original.Id];
                copy.ParentId = string.IsNullOrEmpty(original.ParentId) ? string.Empty : ids[original.ParentId];
                copy.StartTime = original.StartTime + time - template.Offset;
                copy.SourcePrefabId = template.Id;
                copy.SourcePlacementId = instanceId;
                copy.Editor.Layer = 3;
                copies.Add(copy);
            }

            return copies;
        }

        private static ABOptions RestoreOnly() => new()
        {
            RestoreExpandedInstances = true,
            FlattenNestedPrefabs = false,
            ExtractRepeatedSubtrees = false,
            MergeDuplicateTemplates = false,
        };

        private static bool Reported(InteropReport report, string code)
            => report.Issues.Any(i => i.Code == code);

        #endregion

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_ThreeExpandedInstancesOfOneTemplate_BecomeThreePlacements()
        {
            var level = new VgdLevel();
            var template = CreateTemplate(0.25f);
            level.Prefabs.Add(template);
            level.Objects.AddRange(Expand(template, "a", 2f));
            level.Objects.AddRange(Expand(template, "b", 5f));
            level.Objects.AddRange(Expand(template, "c", 9f));
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, RestoreOnly(), report);

            Assert.IsEmpty(result.Objects, "every expanded copy belongs to a placement again");
            Assert.AreEqual(1, result.Prefabs.Count, "an unedited instance is its own template, not a fork");
            Assert.AreEqual(3, result.PrefabPlacements.Count);
            Assert.That(result.PrefabPlacements.All(p => p.PrefabId == TemplateId));
            CollectionAssert.AreEquivalent(new[] { 2f, 5f, 9f },
                result.PrefabPlacements.Select(p => (float)System.Math.Round(p.StartTime, 3)).ToArray(),
                "the placement's time is what the expand added, lead time included");
            Assert.IsTrue(Reported(report, "instances_restored"));
            Assert.AreEqual(6, level.Objects.Count, "the caller's document is never rewritten");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_EditedInstance_ForksTheTemplate()
        {
            var level = new VgdLevel();
            var template = CreateTemplate();
            level.Prefabs.Add(template);
            var edited = Expand(template, "a", 3f);
            edited[1].Move.Keyframes[1].Values = new List<float> { 9f, 9f };
            level.Objects.AddRange(edited);
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, RestoreOnly(), report);

            Assert.IsEmpty(result.Objects);
            Assert.AreEqual(2, result.Prefabs.Count, "the original template plus the fork");
            var fork = result.Prefabs.Single(p => p.Id != TemplateId);
            Assert.AreEqual("Burst (edited)", fork.Name);
            Assert.AreEqual(2, fork.Objects.Count);
            Assert.AreEqual(0f, fork.Objects.Min(o => o.StartTime), 1e-4f,
                "a fork's timeline starts at its earliest object");

            var placement = result.PrefabPlacements.Single();
            Assert.AreEqual(fork.Id, placement.PrefabId);
            Assert.AreEqual(3f, placement.StartTime, 1e-4f);
            Assert.IsTrue(Reported(report, "instances_forked"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_InstanceParentedIntoFromOutside_StaysExpanded()
        {
            var level = new VgdLevel();
            var template = CreateTemplate();
            level.Prefabs.Add(template);
            var copies = Expand(template, "a", 3f);
            level.Objects.AddRange(copies);
            var outsider = ABMockData.CreateObject("outsider");
            outsider.ParentId = copies[0].Id;
            level.Objects.Add(outsider);
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, RestoreOnly(), report);

            Assert.AreEqual(3, result.Objects.Count, "moving the copies into a template would orphan the outsider");
            Assert.IsEmpty(result.PrefabPlacements);
            Assert.IsTrue(Reported(report, "instances_kept_expanded"));
        }

        // The ordinary shape of an instance whose author deleted one of its objects after expanding:
        // the rest still name it as their parent, and it exists nowhere. That is no link to keep -
        // the importer reads such a parent as a root in a template exactly as in the level.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_InstanceMissingAnObject_StillBecomesAPlacement()
        {
            var level = new VgdLevel();
            var template = CreateTemplate();
            level.Prefabs.Add(template);
            var copies = Expand(template, "a", 3f);
            level.Objects.Add(copies[1]);
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, RestoreOnly(), report);

            Assert.IsEmpty(result.Objects);
            Assert.AreEqual(1, result.PrefabPlacements.Count);
            Assert.IsTrue(Reported(report, "instances_forked"), "one object short of its template is an edit");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_NestedPlacements_AreInlinedAndNoneRemain()
        {
            var level = new VgdLevel();
            var inner = CreateTemplate(0.5f);
            var outer = new VgpPrefab { Id = "outer", Name = "Outer" };
            outer.Objects.Add(ABMockData.CreateObject("o-1"));
            var nested = new VgdPrefabPlacement { Id = "n1", PrefabId = TemplateId, StartTime = 2f };
            nested.Tracks[VgdPrefabPlacement.TrackIndex.Position].Values = new List<float> { 4f, -1f };
            outer.Placements.Add(nested);
            level.Prefabs.Add(inner);
            level.Prefabs.Add(outer);
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, new ABOptions
            {
                FlattenNestedPrefabs = true,
                RestoreExpandedInstances = false,
            }, report);

            Assert.That(result.Prefabs.All(p => p.Placements.Count == 0), "no nested placement survives");

            var flattened = result.Prefabs.Single(p => p.Id == "outer");
            Assert.AreEqual(1 + 1 + inner.Objects.Count, flattened.Objects.Count,
                "the outer's own object, the empty holding the placement's transform, and the inner copies");

            var holder = flattened.Objects.Single(o => o.ObjectType == (int)ABObjectType.AlphaEmpty);
            Assert.AreEqual(4f, holder.Move.Keyframes[0].Values[0]);
            Assert.AreEqual(-1f, holder.Move.Keyframes[0].Values[1]);

            var innerRoot = flattened.Objects.Single(o => o.Name == "Root");
            Assert.AreEqual(holder.Id, innerRoot.ParentId, "the inlined root hangs off the placement's transform");
            Assert.AreEqual(1.5f, innerRoot.StartTime, 1e-4f, "timed as the paste times it: t - inner lead time");

            var innerChild = flattened.Objects.Single(o => o.Name == "Child");
            Assert.AreEqual(innerRoot.Id, innerChild.ParentId, "internal parenting is remapped, not flattened");
            Assert.IsTrue(Reported(report, "prefab_nested_flattened"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Apply_EveryOptionOff_ReturnsTheInputUntouched()
        {
            var level = new VgdLevel();
            var template = CreateTemplate();
            level.Prefabs.Add(template);
            level.Objects.AddRange(Expand(template, "a", 2f));

            var result = ABPrefabExtractor.Apply(level, new ABOptions
            {
                RestoreExpandedInstances = false,
                FlattenNestedPrefabs = false,
                ExtractRepeatedSubtrees = false,
                MergeDuplicateTemplates = false,
            }, new InteropReport());

            Assert.AreSame(level, result);
            Assert.AreEqual(2, result.Objects.Count);
            Assert.IsEmpty(result.PrefabPlacements);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_RepeatedSubtrees_BecomeOneTemplateWithAPlacementEach()
        {
            var level = new VgdLevel();
            foreach (var (id, time, x) in new[] { ("a", 1f, 0f), ("b", 4f, 3f), ("c", 8f, -6f) })
            {
                var root = ABMockData.CreateObject(id);
                root.Name = "Vane";
                root.StartTime = time;
                root.Move.Keyframes.RemoveAt(1);
                root.Move.Keyframes[0].Values = new List<float> { x, 2f };
                root.ParentType = "111";
                level.Objects.Add(root);

                var child = ABMockData.CreateObject($"{id}-child");
                child.StartTime = time + 0.5f;
                child.ParentId = id;
                child.ParentType = "111";
                level.Objects.Add(child);
            }

            var report = new InteropReport();
            var result = ABPrefabExtractor.Apply(level, new ABOptions
            {
                RestoreExpandedInstances = false,
                FlattenNestedPrefabs = false,
                ExtractRepeatedSubtrees = true,
            }, report);

            Assert.IsEmpty(result.Objects);
            var template = result.Prefabs.Single();
            Assert.AreEqual(2, template.Objects.Count);
            Assert.AreEqual(0f, template.Objects[0].Move.Keyframes[0].Values[0],
                "the root's placement moved out of the template");
            Assert.AreEqual(3, result.PrefabPlacements.Count);
            CollectionAssert.AreEquivalent(new[] { 0f, 3f, -6f },
                result.PrefabPlacements.Select(p => p.GetValue(VgdPrefabPlacement.TrackIndex.Position, 0)).ToArray());
            Assert.IsTrue(Reported(report, "subtrees_extracted"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Apply_DuplicateTemplates_MergeAndRepointPlacements()
        {
            var level = new VgdLevel();
            var first = CreateTemplate();
            var second = CreateTemplate();
            second.Id = "tpl-copy";
            second.Name = "Burst copy";
            level.Prefabs.Add(first);
            level.Prefabs.Add(second);
            level.PrefabPlacements.Add(new VgdPrefabPlacement { Id = "p1", PrefabId = "tpl-copy", StartTime = 1f });
            var report = new InteropReport();

            var result = ABPrefabExtractor.Apply(level, new ABOptions
            {
                RestoreExpandedInstances = false,
                FlattenNestedPrefabs = false,
                MergeDuplicateTemplates = true,
            }, report);

            Assert.AreEqual(1, result.Prefabs.Count);
            Assert.AreEqual(TemplateId, result.PrefabPlacements.Single().PrefabId);
            Assert.IsTrue(Reported(report, "templates_merged"));
        }
    }
}