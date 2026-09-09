using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // The one property everything else rests on is CollectChain reaching a NESTED placement copy.
    // Leaving one behind is not a cosmetic miss: its ObjectIds table names ids in the template's own
    // scope, and the moment the outer placement stops covering it, PrefabMaterializer.FindPlacements
    // reads it as a genuine placement and the next resync writes over whatever host objects happen to
    // carry those ids. The Unity project's PrefabFlattenResyncTests is what proves that end to end;
    // this is the pure half.

    /// <summary> Which objects one flatten has to touch, and what a flattened placement becomes. </summary>
    public class PrefabFlattenUtilsTests
    {
        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static PrefabObject AddPlacement(Level level, string name = "placement")
        {
            var placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = name,
                Span = FrameSpan.FromBounds(0, 60),
            };
            level.Game.Objects.Add(placement.ObjectId, placement);
            return placement;
        }

        private static ShapeObject AddShape(Level level, string name = "shape")
        {
            var shape = new ShapeObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = name,
                Span = FrameSpan.FromBounds(0, 60),
            };
            level.Game.Objects.Add(shape.ObjectId, shape);
            return shape;
        }

        // Owns, as the model means it: the copy is an ordinary entry in the same scope and the
        // placement's remap table is the only thing saying it came from a template.
        private static void Own(PrefabObject placement, RectObject copy, int innerId)
            => placement.ObjectIds[new ObjectId(innerId)] = copy.ObjectId;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectChain_TakesThePlacementAlone_WhenItOwnsNoPlacements()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level);
            Own(placement, AddShape(level), 1);
            Own(placement, AddShape(level), 2);

            var collected = new List<ObjectId>();
            PrefabFlattenUtils.CollectChain(level.Game, placement, collected);

            CollectionAssert.AreEqual(new[] { placement.ObjectId }, collected,
                "plain copies are already ordinary objects - only placements have anything to drop");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectChain_ReachesANestedPlacementCopy_AndItsOwn()
        {
            var level = CreateLevel();
            var outer = AddPlacement(level, "outer");

            // What a template holding a nested placement materializes: the nested placement copied as
            // an ordinary entry, plus everything IT had already flattened into that template.
            var nested = AddPlacement(level, "nested-copy");
            Own(outer, nested, 1);
            var deepest = AddPlacement(level, "deepest-copy");
            Own(nested, deepest, 1);
            Own(deepest, AddShape(level), 1);

            var collected = new List<ObjectId>();
            PrefabFlattenUtils.CollectChain(level.Game, outer, collected);

            CollectionAssert.AreEquivalent(
                new[] { outer.ObjectId, nested.ObjectId, deepest.ObjectId }, collected);
        }

        // A remap table pointing at an id nothing holds is ordinary (a template object removed while
        // the placement was not resynced), and it must not take the walk down with it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectChain_IgnoresAnIdNothingInTheScopeHolds()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level);
            placement.ObjectIds[new ObjectId(1)] = new ObjectId(9999);

            var collected = new List<ObjectId>();
            PrefabFlattenUtils.CollectChain(level.Game, placement, collected);

            CollectionAssert.AreEqual(new[] { placement.ObjectId }, collected);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectAll_TakesEveryPlacement_GenuineOrMaterialized()
        {
            var level = CreateLevel();
            var outer = AddPlacement(level, "outer");
            var nested = AddPlacement(level, "nested-copy");
            Own(outer, nested, 1);
            var standalone = AddPlacement(level, "standalone");
            AddShape(level);

            var collected = new List<ObjectId>();
            PrefabFlattenUtils.CollectAll(level.Game, collected);

            CollectionAssert.AreEquivalent(
                new[] { outer.ObjectId, nested.ObjectId, standalone.ObjectId }, collected,
                "whichever way a PrefabObject got here, it has to stop being one");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Flatten_KeepsIdentityAndTransform_AndIsNoLongerAPlacement()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level, "named");
            placement.ParentObjectId = new ObjectId(7);
            placement.Layer = 4;
            placement.Active = false;
            placement.Span = FrameSpan.FromBounds(12, 90);
            placement.Positions.Add(new PosKey());
            placement.PrefabId = PrefabId.NewId();
            Own(placement, AddShape(level), 1);

            var flattened = PrefabFlattenUtils.Flatten(placement);

            Assert.IsFalse(flattened is PrefabObject, "the whole point is that it stops being one");
            Assert.AreEqual(placement.ObjectId, flattened.ObjectId, "children point at this id");
            Assert.AreEqual(placement.ParentObjectId, flattened.ParentObjectId);
            Assert.AreEqual("named", flattened.Name);
            Assert.AreEqual(4, flattened.Layer);
            Assert.IsFalse(flattened.Active);
            Assert.AreEqual(placement.Span, flattened.Span);
            Assert.AreEqual(1, flattened.Positions.Count, "keyframe tracks come across");
        }

        // The table is dropped rather than resolved because ApplyModifications already wrote every
        // override ONTO the copies - so nothing an author typed is lost with it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Flatten_DoesNotMutateTheOriginal()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level);
            var prefabId = PrefabId.NewId();
            placement.PrefabId = prefabId;
            Own(placement, AddShape(level), 1);

            PrefabFlattenUtils.Flatten(placement);

            Assert.AreEqual(prefabId, placement.PrefabId, "undo restores this very instance");
            Assert.AreEqual(1, placement.ObjectIds.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CollectReferencedPrefabs_SkipsAnEmptyPlacement()
        {
            var level = CreateLevel();
            var used = PrefabId.NewId();
            AddPlacement(level).PrefabId = used;
            AddPlacement(level); // created but never pointed at a template

            var referenced = new HashSet<PrefabId>();
            PrefabFlattenUtils.CollectReferencedPrefabs(level.Game, referenced);

            CollectionAssert.AreEquivalent(new[] { used }, referenced);
        }
    }
}