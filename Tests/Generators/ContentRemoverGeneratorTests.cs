using BH.SDK.Generators;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Events;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The generator that only ever DELETES, so the properties worth pinning are the ones a spawning
    // generator has no equivalent of: that both modes are exact opposites on a point and deliberately
    // NOT opposites on a span (partial overlap survives either way), that the level stays structurally
    // whole after the cut (no orphaned child, no broken prefab remap table), that the three content
    // switches are independent, and that the journal can put a deleted audio track back.

    /// <summary> The generator that only ever deletes: that the two modes are opposites on a point and
    /// deliberately not on a span, that the level stays structurally whole after the cut, and that undo restores it. </summary>
    public class ContentRemoverGeneratorTests
    {
        private const int FrameDuration = 600;
        // The exclusive end of the whole-level window: a span ends ON the timeline's length.
        private const int WholeEnd = FrameDuration;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = FrameDuration;
            return level;
        }

        private static ShapeObject AddObject(Level level, int startFrame, int endFrame,
            ObjectId parent = default, string name = "obj")
        {
            var obj = new ShapeObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                ParentObjectId = parent,
                Name = name,
                Span = FrameSpan.FromBounds(startFrame, endFrame),
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static PrefabObject AddPlacement(Level level, int startFrame, int endFrame)
        {
            var placement = new PrefabObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = "placement",
                Span = FrameSpan.FromBounds(startFrame, endFrame),
            };
            level.Game.Objects.Add(placement.ObjectId, placement);
            return placement;
        }

        private static LevelTrack AddTrack(Level level, int startFrame, int endFrame)
        {
            var track = new LevelTrack
            {
                AudioId = level.Settings.GetNextAudioId(),
                Span = FrameSpan.FromBounds(startFrame, endFrame),
                Name = "track",
            };
            level.Audio.Tracks.Add(track.AudioId, track);
            return track;
        }

        /// <summary> Defaults to the whole level as the window - what the editor's "Whole Level"
        /// switch produces. Every test passes its mode explicitly; see DefaultsToRemovingInside. </summary>
        private static GeneratorResult Run(Level level, ContentRemoverGenerator.Parameters parameters = null,
            int start = 0, int end = WholeEnd)
        {
            var generator = new ContentRemoverGenerator();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(start, end));
            return generator.Run(context, parameters ?? new ContentRemoverGenerator.Parameters());
        }

        /// <summary> Remove what lies inside the window - the shipped default, spelled out here so a
        /// changed default shows up as a failing default test rather than as every other test
        /// quietly measuring something else. </summary>
        private static ContentRemoverGenerator.Parameters Inside(bool objects = true) =>
            new() { Invert = false, Objects = objects };

        /// <summary> Remove what lies outside the window. </summary>
        private static ContentRemoverGenerator.Parameters Outside(bool objects = true,
            bool audio = false, bool events = false) =>
            new() { Invert = true, Objects = objects, Audio = audio, EventFrames = events };

        /// <summary> Out of the box this cuts the section the author framed, not everything around it
        /// - the narrower of the two modes, since the wider one erases a level's worth of work from a
        /// window someone may have left where the playhead happened to be. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DefaultsToRemovingInside()
        {
            var parameters = new ContentRemoverGenerator().CreateDefaultParameters()
                as ContentRemoverGenerator.Parameters;

            Assert.IsNotNull(parameters);
            Assert.IsFalse(parameters.Invert);
            Assert.IsTrue(parameters.Objects);
            Assert.IsFalse(parameters.Audio);
            Assert.IsFalse(parameters.EventFrames);

            var level = CreateLevel();
            var inside = AddObject(level, 120, 180);
            var outside = AddObject(level, 300, 400);

            Run(level, parameters, 100, 200);

            Assert.IsFalse(level.Game.Objects.ContainsKey(inside.ObjectId));
            Assert.IsTrue(level.Game.Objects.ContainsKey(outside.ObjectId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesObjectsOutsideTheWindow()
        {
            var level = CreateLevel();
            var inside = AddObject(level, 0, 100);
            var outside = AddObject(level, FrameDuration, FrameDuration + 100);

            Run(level, Outside());

            Assert.IsTrue(level.Game.Objects.ContainsKey(inside.ObjectId));
            Assert.IsFalse(level.Game.Objects.ContainsKey(outside.ObjectId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesObjectsInsideTheWindowWhenNotInverted()
        {
            var level = CreateLevel();
            var inside = AddObject(level, 120, 180);
            var outside = AddObject(level, 300, 400);

            Run(level, Inside(), 100, 200);

            Assert.IsFalse(level.Game.Objects.ContainsKey(inside.ObjectId));
            Assert.IsTrue(level.Game.Objects.ContainsKey(outside.ObjectId));
        }

        /// <summary> An object on the window's own edge frames is inside it - the range is inclusive
        /// on both ends, and an off-by-one here silently eats or spares content. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TreatsBothWindowEdgesAsInside()
        {
            var level = CreateLevel();
            var edge = AddObject(level, 100, 200);

            Run(level, Outside(), 100, 200);
            Assert.IsTrue(level.Game.Objects.ContainsKey(edge.ObjectId), "Invert must keep it");

            Run(level, Inside(), 100, 200);
            Assert.IsFalse(level.Game.Objects.ContainsKey(edge.ObjectId), "not inverted must remove it");
        }

        /// <summary> Partial overlap is the one case both modes spare: a span hanging over the edge is
        /// neither wholly outside nor wholly inside, and deleting it either way would make the flag a
        /// trap rather than a mode. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void KeepsPartiallyOverlappingObjectsInBothModes()
        {
            var level = CreateLevel();
            var overlapping = AddObject(level, 50, 150);

            Run(level, Outside(), 100, 200);
            Assert.IsTrue(level.Game.Objects.ContainsKey(overlapping.ObjectId), "Invert must keep it");

            Run(level, Inside(), 100, 200);
            Assert.IsTrue(level.Game.Objects.ContainsKey(overlapping.ObjectId), "not inverted must keep it too");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void KeepsARemovableParentOfASurvivingChild()
        {
            var level = CreateLevel();
            var parent = AddObject(level, FrameDuration, FrameDuration + 50, name: "parent");
            var child = AddObject(level, 0, 100, parent.ObjectId, "child");

            Run(level, Outside());

            Assert.IsTrue(level.Game.Objects.ContainsKey(child.ObjectId));
            Assert.IsTrue(level.Game.Objects.ContainsKey(parent.ObjectId),
                "deleting the parent would leave the surviving child with a dangling ParentObjectId");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesAWholeSubtreeWhenNothingInItSurvives()
        {
            var level = CreateLevel();
            var parent = AddObject(level, FrameDuration, FrameDuration + 50, name: "parent");
            var child = AddObject(level, FrameDuration + 10, FrameDuration + 20, parent.ObjectId, "child");

            Run(level, Outside());

            Assert.IsFalse(level.Game.Objects.ContainsKey(parent.ObjectId));
            Assert.IsFalse(level.Game.Objects.ContainsKey(child.ObjectId));
            Assert.AreEqual(0, level.Game.Objects.Count);
        }

        /// <summary> A surviving placement's ObjectIds table points straight at its materialized
        /// children, so one of them falling into the removed range must not be deleted on its own. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void KeepsMaterializedChildrenOfASurvivingPlacement()
        {
            var level = CreateLevel();
            var placement = AddPlacement(level, 0, 100);
            var materialized = AddObject(level, FrameDuration, FrameDuration + 10, placement.ObjectId, "inner");
            placement.ObjectIds.Add(new ObjectId(1), materialized.ObjectId);

            Run(level, Outside());

            Assert.IsTrue(level.Game.Objects.ContainsKey(placement.ObjectId));
            Assert.IsTrue(level.Game.Objects.ContainsKey(materialized.ObjectId),
                "deleting it would leave the placement's ObjectIds table pointing at nothing");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LeavesAudioAndEventsAloneByDefault()
        {
            var level = CreateLevel();
            var track = AddTrack(level, FrameDuration, FrameDuration + 100);
            level.Game.Events.Markers.Add(new Marker("late", string.Empty, new Color4Value(), FrameDuration));

            Run(level, Outside());

            Assert.IsTrue(level.Audio.Tracks.ContainsKey(track.AudioId));
            Assert.AreEqual(1, level.Game.Events.Markers.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesAudioTracksOutsideTheWindowWhenAsked()
        {
            var level = CreateLevel();
            var inside = AddTrack(level, 0, 200);
            var outside = AddTrack(level, FrameDuration, FrameDuration + 100);

            Run(level, Outside(objects: false, audio: true));

            Assert.IsTrue(level.Audio.Tracks.ContainsKey(inside.AudioId));
            Assert.IsFalse(level.Audio.Tracks.ContainsKey(outside.AudioId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesLevelGlobalEventKeysOutsideTheWindowWhenAsked()
        {
            var level = CreateLevel();
            level.Game.Events.Markers.Add(new Marker("kept", string.Empty, new Color4Value(), 100));
            level.Game.Events.Markers.Add(new Marker("cut", string.Empty, new Color4Value(), FrameDuration));
            level.Game.Events.Checkpoints.Add(new Checkpoint { Frame = FrameDuration + 5 });
            level.Game.CameraEvents.Zooms.Add(new ZoomKey { Frame = 50 });
            level.Game.CameraEvents.Zooms.Add(new ZoomKey { Frame = FrameDuration + 50 });
            level.Game.PlayerEvents.Visibilities.Add(new BoolKey { Frame = FrameDuration });

            Run(level, Outside(objects: false, events: true));

            Assert.AreEqual(1, level.Game.Events.Markers.Count);
            Assert.AreEqual("kept", level.Game.Events.Markers[0].Name);
            Assert.AreEqual(0, level.Game.Events.Checkpoints.Count);
            Assert.AreEqual(1, level.Game.CameraEvents.Zooms.Count);
            Assert.AreEqual(50, level.Game.CameraEvents.Zooms[0].Frame);
            Assert.AreEqual(0, level.Game.PlayerEvents.Visibilities.Count);
        }

        /// <summary> A keyframe is a point, so the two modes really are exact opposites on one -
        /// wiping a section's events is the same call with the flag off. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemovesLevelGlobalEventKeysInsideTheWindowWhenNotInverted()
        {
            var level = CreateLevel();
            level.Game.Events.Markers.Add(new Marker("before", string.Empty, new Color4Value(), 50));
            level.Game.Events.Markers.Add(new Marker("inside", string.Empty, new Color4Value(), 150));
            level.Game.Events.Markers.Add(new Marker("edge", string.Empty, new Color4Value(), 200));
            level.Game.CameraEvents.Zooms.Add(new ZoomKey { Frame = 150 });

            Run(level, new ContentRemoverGenerator.Parameters
            {
                Invert = false, Objects = false, EventFrames = true,
            }, 100, 201);

            Assert.AreEqual(1, level.Game.Events.Markers.Count);
            Assert.AreEqual("before", level.Game.Events.Markers[0].Name);
            Assert.AreEqual(0, level.Game.CameraEvents.Zooms.Count);
        }

        /// <summary> Deleting an audio track goes through the journal like everything else, so undo
        /// puts it back under the same AudioId. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void UndoRestoresEverythingItRemoved()
        {
            var level = CreateLevel();
            var outsideObject = AddObject(level, FrameDuration, FrameDuration + 10);
            var outsideTrack = AddTrack(level, FrameDuration, FrameDuration + 10);
            var before = level.Game.Copy();

            var result = Run(level, Outside(audio: true));

            Assert.IsFalse(level.Game.Objects.ContainsKey(outsideObject.ObjectId));
            Assert.IsFalse(level.Audio.Tracks.ContainsKey(outsideTrack.AudioId));

            result.Log.Revert();

            Assert.IsTrue(before.Equals(level.Game));
            Assert.IsTrue(level.Audio.Tracks.ContainsKey(outsideTrack.AudioId));

            result.Log.Reapply();

            Assert.IsFalse(level.Game.Objects.ContainsKey(outsideObject.ObjectId));
            Assert.IsFalse(level.Audio.Tracks.ContainsKey(outsideTrack.AudioId));
        }

        #region IsDangerous

        private static bool IsDangerous(Level level, ContentRemoverGenerator.Parameters parameters,
            int start = 0, int end = WholeEnd)
            => new ContentRemoverGenerator().IsDangerous(new GeneratorContext(level, FrameSpan.FromBounds(start, end)), parameters);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Dangerous_WhenInverted_EvenWithATinyWindow()
        {
            var level = CreateLevel();

            Assert.IsTrue(IsDangerous(level, new ContentRemoverGenerator.Parameters { Invert = true }, 100, 101));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Dangerous_WhenTheWindowIsTheWholeTimeline_WithoutInvert()
        {
            var level = CreateLevel();

            Assert.IsTrue(IsDangerous(level, Inside()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NotDangerous_WhenASectionIsRemovedWithoutInvert()
        {
            var level = CreateLevel();

            Assert.IsFalse(IsDangerous(level, Inside(), 100, 200));
        }

        // Prefab Mode: the window is bounded by the template's own FrameDuration, so "the whole
        // timeline" is the template's, not the hosting level's much longer one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Dangerous_InPrefabScope_MeasuredAgainstTheTemplateFrameDuration()
        {
            var level = CreateLevel();
            var prefab = new Prefab { FrameDuration = 100 };

            var whole = new GeneratorContext(prefab, prefab, level.Settings, level.Resources, FrameSpan.FromBounds(0, 100));
            var section = new GeneratorContext(prefab, prefab, level.Settings, level.Resources, FrameSpan.FromBounds(0, 50));
            var generator = new ContentRemoverGenerator();

            Assert.IsTrue(generator.IsDangerous(whole, Inside()));
            Assert.IsFalse(generator.IsDangerous(section, Inside()));
        }

        #endregion
    }
}
