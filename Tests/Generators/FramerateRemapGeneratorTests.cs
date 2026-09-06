using System;
using System.Collections.Generic;
using BH.SDK.Generators;
using BH.SDK.Generators.External;
using BH.SDK.Generators.Modifiers;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Events;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // Retiming is the one generator whose correctness is arithmetic rather than placement, and whose
    // failure mode is silent: a level that still opens, still validates, and plays at the wrong
    // speed or has quietly lost a key. So the bulk of this file is the packing policy - what happens
    // when two frames resample onto one - because that is where content is actually destroyed, and
    // the numbers are chosen so every branch of FindSlot is reached by name:
    //
    //   60 -> 30 : frames 3 and 4 both sample to 2      (the plain collision)
    //   60 -> 10 : frames 0,1,2 all sample to 0         (a collision with no room below)
    //   60 -> 10 : frames 3,4,5 all sample to 1         (room below, so the backward nudge fires)
    //
    // Rounding is away-from-zero, which is why 3 -> 1.5 lands on 1 rather than 0 and why 4/60 and
    // 3/60 collide at 30 while 4 and 6 do not.
    public class FramerateRemapGeneratorTests
    {
        private const int Framerate = 60;
        private const int FrameDuration = 600;

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = Framerate;
            level.Settings.FrameDuration = FrameDuration;
            return level;
        }

        private static FramerateRemapGenerator.Parameters Params(int framerate, int maxKeyShift = 1,
            bool objects = true, bool audio = false, bool events = false) =>
            new()
            {
                Framerate = framerate,
                MaxKeyShift = maxKeyShift,
                RemapObjects = objects,
                RemapAudio = audio,
                RemapEvents = events,
            };

        private static GeneratorResult Run(Level level, FramerateRemapGenerator.Parameters parameters)
        {
            var generator = new FramerateRemapGenerator();
            var context = new GeneratorContext(level, new FrameSpan(0, level.Settings.FrameDuration));
            return generator.Run(context, parameters);
        }

        private static ShapeObject AddObject(Level level, int startFrame, int endFrame,
            params int[] positionFrames)
        {
            var obj = new ShapeObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Name = "obj",
                Span = FrameSpan.FromBounds(startFrame, endFrame),
            };
            foreach (var frame in positionFrames)
                obj.Positions.Add(new PosKey(new Vector2Value(frame, 0f), frame));
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static LevelTrack AddTrack(Level level, int startFrame, int endFrame,
            params int[] volumeFrames)
        {
            var track = new LevelTrack
            {
                AudioId = level.Settings.GetNextAudioId(),
                Span = FrameSpan.FromBounds(startFrame, endFrame),
                Name = "track",
                OffsetTime = 1.25f,
            };
            foreach (var frame in volumeFrames)
                track.Effects.Volumes.Add(new FloatKey(new FloatValue(1f), frame));
            level.Audio.Tracks.Add(track.AudioId, track);
            return track;
        }

        private static List<int> Frames(List<PosKey> track)
        {
            var frames = new List<int>(track.Count);
            foreach (var key in track) frames.Add(key.Frame);
            return frames;
        }

        #region Settings

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void HalvingTheFramerate_HalvesTheTimelineLength()
        {
            var level = CreateLevel();

            Run(level, Params(30));

            Assert.AreEqual(30, level.Settings.Fps);
            Assert.AreEqual(FrameDuration / 2, level.Settings.FrameDuration);
        }

        /// <summary> FrameDuration scales with no switch of its own: a timeline keeping its old length
        /// at twice the framerate is a level that silently got twice as short. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DoublingTheFramerate_DoublesTheTimelineLength()
        {
            var level = CreateLevel();

            Run(level, Params(120));

            Assert.AreEqual(120, level.Settings.Fps);
            Assert.AreEqual(FrameDuration * 2, level.Settings.FrameDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheSameFramerate_ChangesNothingAtAll()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 30, 210, 0, 3, 4);
            var before = level.Game.Copy();

            var result = Run(level, Params(Framerate));

            Assert.AreEqual(Framerate, level.Settings.Fps);
            Assert.AreEqual(FrameDuration, level.Settings.FrameDuration);
            Assert.AreEqual(0, result.Log.Count, "a no-op run must journal nothing");
            Assert.IsTrue(before.Equals(level.Game));
            Assert.AreEqual(3, ((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ClampsTheTargetFramerateToTheFormatRange()
        {
            var level = CreateLevel();

            Run(level, Params(FrameRules.MaxFramerate + 5_000));

            Assert.AreEqual(FrameRules.MaxFramerate, level.Settings.Fps);
        }

        /// <summary> CurrentFramerate is a display mirror the host fills in. A run must take its own
        /// arithmetic off the level, so a wrong or unfilled mirror cannot retime against a value that
        /// was never true. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void IgnoresCurrentFramerate_WhichIsForDisplayOnly()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 100, 100);

            var parameters = Params(30);
            ((ICurrentFramerateInput)parameters).CurrentFramerate = 7;

            Run(level, parameters);

            Assert.AreEqual(FrameDuration / 2, level.Settings.FrameDuration);
            Assert.AreEqual(50, ((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions[0].Frame);
        }

        #endregion

        #region Objects

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemapsObjectBoundsAndKeyframes()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 60, 300, 0, 20, 100);

            Run(level, Params(30));

            var remapped = (ShapeObject)level.Game.Objects[obj.ObjectId];
            Assert.AreEqual(30, remapped.Span.StartFrame);
            Assert.AreEqual(150, remapped.Span.EndFrame);
            CollectionAssert.AreEqual(new[] { 0, 10, 50 }, Frames(remapped.Positions));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LeavesObjectsAloneWhenNotAsked()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 60, 300, 0, 20, 100);

            Run(level, Params(30, objects: false));

            var untouched = (ShapeObject)level.Game.Objects[obj.ObjectId];
            Assert.AreEqual(30, level.Settings.Fps, "the framerate itself still changes");
            Assert.AreEqual(60, untouched.Span.StartFrame);
            Assert.AreEqual(300, untouched.Span.EndFrame);
            CollectionAssert.AreEqual(new[] { 0, 20, 100 }, Frames(untouched.Positions));
        }

        /// <summary> Nothing may end up past the timeline it was just resampled into - the format
        /// rejects a frame outside [0, FrameDuration) ([RuleLevelFrame]). </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void KeepsEverythingInsideTheNewTimeline()
        {
            var level = CreateLevel();
            AddObject(level, 0, FrameDuration - 1, 0, FrameDuration - 1);

            Run(level, Params(30));

            var last = level.Settings.FrameDuration - 1;
            foreach (var obj in level.Game.Objects.Values)
            {
                Assert.LessOrEqual(obj.Span.EndFrame, last + 1);
                Assert.GreaterOrEqual(obj.Span.StartFrame, FrameRules.MinFrame);
                foreach (var track in ObjectTracks.Of(obj, ObjectTrackMask.All))
                    for (var i = 0; i < track.Count; i++)
                    {
                        Assert.GreaterOrEqual(track.FrameAt(i), FrameRules.MinFrame);
                        Assert.LessOrEqual(track.FrameAt(i), last);
                    }
            }
        }

        #endregion

        #region Keyframe packing

        /// <summary> Keys far enough apart to survive the resample keep every one of their frames. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void KeepsEveryKeyWhenNoneCollide()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 0, 4, 8, 12);

            Run(level, Params(30));

            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6 },
                Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions));
        }

        /// <summary> Frames 3 and 4 both sample onto 2. With one frame of slack the loser is nudged
        /// forward instead of dropped - which is the whole point of the default. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void NudgesACollidingKeyForwardWithinTheShiftLimit()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 3, 4);

            Run(level, Params(30));

            CollectionAssert.AreEqual(new[] { 2, 3 },
                Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions));
        }

        /// <summary> Zero slack is "never move a key off its own frame": the collision is resolved by
        /// dropping, not by shifting. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DropsACollidingKeyWhenTheShiftLimitIsZero()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 3, 4);

            Run(level, Params(30, maxKeyShift: 0));

            CollectionAssert.AreEqual(new[] { 2 },
                Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions));
        }

        /// <summary> Frames 0, 1 and 2 all sample onto 0 at a sixth of the rate. The first keeps it,
        /// the second takes the only free neighbour, and the third has nowhere left inside one frame
        /// - below zero is not a frame - so it goes. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DropsAKeyWithNoFreeSlotWithinTheLimit()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 0, 1, 2);

            Run(level, Params(10));

            CollectionAssert.AreEqual(new[] { 0, 1 },
                Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions));
        }

        /// <summary> Frames 3, 4 and 5 all sample onto 1, and this time frame 0 is free. The third key
        /// finds it by searching backwards after forward is taken - the branch a forward-only packer
        /// would lose a key on. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void NudgesBackwardWhenTheFrameAheadIsTaken()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 3, 4, 5);

            Run(level, Params(10));

            var frames = Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, frames);
            Assert.AreEqual(0, frames[2], "the last key had to go below its own sampled frame");
        }

        /// <summary> A bigger allowance saves keys a smaller one drops - the parameter is a policy,
        /// not a tie-breaker. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ABiggerShiftLimitSavesMoreKeys()
        {
            var tight = CreateLevel();
            var tightObject = AddObject(tight, 0, 200, 0, 1, 2, 3);
            Run(tight, Params(10, maxKeyShift: 1));

            var loose = CreateLevel();
            var looseObject = AddObject(loose, 0, 200, 0, 1, 2, 3);
            Run(loose, Params(10, maxKeyShift: 3));

            var tightFrames = Frames(((ShapeObject)tight.Game.Objects[tightObject.ObjectId]).Positions);
            var looseFrames = Frames(((ShapeObject)loose.Game.Objects[looseObject.ObjectId]).Positions);

            Assert.Less(tightFrames.Count, looseFrames.Count);
            Assert.AreEqual(4, looseFrames.Count, "with three frames of slack nothing has to go");
            CollectionAssert.AllItemsAreUnique(looseFrames);
        }

        /// <summary> Raising the framerate spreads keys apart, so nothing can ever collide and
        /// MaxKeyShift never bites - not even at zero. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ExpandingNeverDropsAKey()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 0, 1, 2, 3, 4);

            Run(level, Params(120, maxKeyShift: 0));

            CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8 },
                Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions));
        }

        /// <summary> The invariant the whole packer exists for: a track's frames must stay unique
        /// ([RuleCollectionUnique]), whatever the ratio and whatever the slack. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryTrackKeepsUniqueFrames_AcrossRatiosAndLimits()
        {
            foreach (var target in new[] { 1, 7, 10, 24, 30, 59, 61, 120, 240 })
            foreach (var shift in new[] { 0, 1, 2, 5, 50 })
            {
                var level = CreateLevel();
                var obj = new ShapeObject
                {
                    ObjectId = level.Settings.GetNextObjectId(),
                    Name = "dense",
                    Span = FrameSpan.FromBounds(0, 400),
                };
                for (var frame = 0; frame < 200; frame++)
                    obj.Positions.Add(new PosKey(new Vector2Value(frame, 0f), frame));
                level.Game.Objects.Add(obj.ObjectId, obj);

                Run(level, Params(target, shift));

                var frames = Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions);
                CollectionAssert.AllItemsAreUnique(frames, $"{Framerate}->{target}, shift {shift}");

                var last = level.Settings.FrameDuration - 1;
                foreach (var frame in frames)
                {
                    Assert.GreaterOrEqual(frame, FrameRules.MinFrame, $"{Framerate}->{target}");
                    Assert.LessOrEqual(frame, last, $"{Framerate}->{target}");
                }
            }
        }

        // Checked as "every surviving frame sits within the allowance of SOME original's sampled
        // frame" rather than by rebuilding the original -> survivor mapping: the mapping is exactly
        // what the packer decides, so reconstructing it here would be re-implementing the thing under
        // test and would pass for the same reason it is wrong. This phrasing still catches the
        // failure that matters - a key flung somewhere it was never allowed to go.

        /// <summary> A key is only ever moved as far as it was allowed to be, measured from its
        /// sampled frame - which is what MaxKeyShift is defined against, not where the key started. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void NoKeyMovesFurtherThanTheShiftLimit()
        {
            const int target = 20;

            foreach (var shift in new[] { 0, 1, 3 })
            {
                var level = CreateLevel();
                var obj = new ShapeObject
                {
                    ObjectId = level.Settings.GetNextObjectId(),
                    Name = "dense",
                    Span = FrameSpan.FromBounds(0, 400),
                };

                var ideals = new HashSet<int>();
                for (var frame = 0; frame < 60; frame++)
                {
                    obj.Positions.Add(new PosKey(new Vector2Value(frame, 0f), frame));
                    ideals.Add((int)Math.Round(frame * (double)target / Framerate, MidpointRounding.AwayFromZero));
                }
                level.Game.Objects.Add(obj.ObjectId, obj);

                Run(level, Params(target, shift));

                var frames = Frames(((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions);
                Assert.IsNotEmpty(frames, $"shift {shift}: everything was dropped");
                CollectionAssert.AllItemsAreUnique(frames, $"shift {shift}");

                foreach (var frame in frames)
                {
                    var placed = false;
                    foreach (var ideal in ideals)
                    {
                        var distance = frame - ideal;
                        if (distance < 0) distance = -distance;
                        if (distance > shift) continue;
                        placed = true;
                        break;
                    }
                    Assert.IsTrue(placed,
                        $"shift {shift}: key at {frame} is further than {shift} from every sampled frame");
                }
            }
        }

        #endregion

        #region Audio

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemapsAudioBoundsAndAutomationWhenAsked()
        {
            var level = CreateLevel();
            var track = AddTrack(level, 60, 300, 0, 20, 100);

            Run(level, Params(30, audio: true));

            var remapped = level.Audio.Tracks[track.AudioId];
            Assert.AreEqual(30, remapped.Span.StartFrame);
            Assert.AreEqual(150, remapped.Span.EndFrame);
            CollectionAssert.AreEqual(new[] { 0, 10, 50 },
                new List<int> { remapped.Effects.Volumes[0].Frame, remapped.Effects.Volumes[1].Frame,
                    remapped.Effects.Volumes[2].Frame });
        }

        /// <summary> OffsetTime is seconds INTO the clip, not a level frame: remapping it would move
        /// the playhead inside the audio and desync the very thing being retimed. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LeavesTheClipOffsetAlone()
        {
            var level = CreateLevel();
            var track = AddTrack(level, 60, 300, 0);

            Run(level, Params(30, audio: true));

            Assert.AreEqual(1.25f, level.Audio.Tracks[track.AudioId].OffsetTime);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LeavesAudioAloneWhenNotAsked()
        {
            var level = CreateLevel();
            var track = AddTrack(level, 60, 300, 0, 20, 100);

            Run(level, Params(30));

            var untouched = level.Audio.Tracks[track.AudioId];
            Assert.AreEqual(60, untouched.Span.StartFrame);
            Assert.AreEqual(300, untouched.Span.EndFrame);
            Assert.AreEqual(100, untouched.Effects.Volumes[2].Frame);
        }

        /// <summary> Audio automation goes through the same packer as everything else, so it loses
        /// keys under the same rule rather than by a policy of its own. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void PacksAudioAutomationByTheSameRule()
        {
            var level = CreateLevel();
            var track = AddTrack(level, 0, 300, 0, 1, 2);

            Run(level, Params(10, maxKeyShift: 1, audio: true));

            var volumes = level.Audio.Tracks[track.AudioId].Effects.Volumes;
            Assert.AreEqual(2, volumes.Count);
            Assert.AreEqual(0, volumes[0].Frame);
            Assert.AreEqual(1, volumes[1].Frame);
        }

        #endregion

        #region Level-global events

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RemapsEveryLevelGlobalTrackWhenAsked()
        {
            var level = CreateLevel();
            level.Game.Events.Markers.Add(new Marker("m", string.Empty, new Color4Value(), 100));
            level.Game.Events.Checkpoints.Add(new Checkpoint { Frame = 200 });
            level.Game.CameraEvents.Zooms.Add(new ZoomKey { Frame = 60 });
            level.Game.PlayerEvents.Visibilities.Add(new BoolKey { Frame = 300 });

            Run(level, Params(30, events: true));

            Assert.AreEqual(50, level.Game.Events.Markers[0].Frame);
            Assert.AreEqual(100, level.Game.Events.Checkpoints[0].Frame);
            Assert.AreEqual(30, level.Game.CameraEvents.Zooms[0].Frame);
            Assert.AreEqual(150, level.Game.PlayerEvents.Visibilities[0].Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void LeavesEventsAloneWhenNotAsked()
        {
            var level = CreateLevel();
            level.Game.Events.Markers.Add(new Marker("m", string.Empty, new Color4Value(), 100));

            Run(level, Params(30));

            Assert.AreEqual(100, level.Game.Events.Markers[0].Frame);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void PacksEventKeysByTheSameRule()
        {
            var level = CreateLevel();
            level.Game.Events.Markers.Add(new Marker("a", string.Empty, new Color4Value(), 0));
            level.Game.Events.Markers.Add(new Marker("b", string.Empty, new Color4Value(), 1));
            level.Game.Events.Markers.Add(new Marker("c", string.Empty, new Color4Value(), 2));

            Run(level, Params(10, maxKeyShift: 1, events: true));

            Assert.AreEqual(2, level.Game.Events.Markers.Count);
            Assert.AreEqual("a", level.Game.Events.Markers[0].Name);
            Assert.AreEqual("b", level.Game.Events.Markers[1].Name);
            Assert.AreEqual(0, level.Game.Events.Markers[0].Frame);
            Assert.AreEqual(1, level.Game.Events.Markers[1].Frame);
        }

        #endregion

        #region Undo

        /// <summary> Everything this run touches is journalled - including the two settings fields,
        /// which have no object to ride along on. A framerate left behind by an undo is a level
        /// playing at one rate with frame numbers written for another. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void UndoRestoresSettingsObjectsAudioAndEvents()
        {
            var level = CreateLevel();
            AddObject(level, 60, 300, 0, 3, 4, 100);
            var track = AddTrack(level, 60, 300, 0, 20, 100);
            level.Game.Events.Markers.Add(new Marker("m", string.Empty, new Color4Value(), 100));

            var gameBefore = level.Game.Copy();
            var audioBefore = level.Audio.Copy();

            var result = Run(level, Params(30, audio: true, events: true));
            var gameAfter = level.Game.Copy();
            var audioAfter = level.Audio.Copy();

            Assert.AreEqual(30, level.Settings.Fps);

            result.Log.Revert();

            Assert.AreEqual(Framerate, level.Settings.Fps);
            Assert.AreEqual(FrameDuration, level.Settings.FrameDuration);
            Assert.IsTrue(gameBefore.Equals(level.Game), "revert must restore objects and events exactly");
            Assert.IsTrue(audioBefore.Equals(level.Audio), "revert must restore audio exactly");
            Assert.AreEqual(100, level.Audio.Tracks[track.AudioId].Effects.Volumes[2].Frame);

            result.Log.Reapply();

            Assert.AreEqual(30, level.Settings.Fps);
            Assert.AreEqual(FrameDuration / 2, level.Settings.FrameDuration);
            Assert.IsTrue(gameAfter.Equals(level.Game), "redo must reproduce the run exactly");
            Assert.IsTrue(audioAfter.Equals(level.Audio), "redo must reproduce audio exactly");
        }

        /// <summary> A dropped key comes back on undo too - the packer removes through the journal,
        /// not behind its back. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void UndoBringsDroppedKeysBack()
        {
            var level = CreateLevel();
            var obj = AddObject(level, 0, 200, 0, 1, 2);
            level.Game.Events.Markers.Add(new Marker("a", string.Empty, new Color4Value(), 0));
            level.Game.Events.Markers.Add(new Marker("b", string.Empty, new Color4Value(), 1));
            level.Game.Events.Markers.Add(new Marker("c", string.Empty, new Color4Value(), 2));

            var before = level.Game.Copy();
            var result = Run(level, Params(10, maxKeyShift: 0, events: true));

            Assert.AreEqual(1, ((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions.Count);
            Assert.AreEqual(1, level.Game.Events.Markers.Count);

            result.Log.Revert();

            Assert.IsTrue(before.Equals(level.Game));
            Assert.AreEqual(3, ((ShapeObject)level.Game.Objects[obj.ObjectId]).Positions.Count);
            Assert.AreEqual(3, level.Game.Events.Markers.Count);
        }

        #endregion

        #region Contract

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DefaultsRemapObjectsOnly()
        {
            var parameters = new FramerateRemapGenerator().CreateDefaultParameters()
                as FramerateRemapGenerator.Parameters;

            Assert.IsNotNull(parameters);
            Assert.IsTrue(parameters.RemapObjects);
            Assert.IsFalse(parameters.RemapAudio);
            Assert.IsFalse(parameters.RemapEvents);
            Assert.AreEqual(1, parameters.MaxKeyShift);
        }

        /// <summary> A framerate change rewrites every frame number in the level at once, which is
        /// exactly what the host's confirmation step exists for - but leaving it where it is is not
        /// dangerous, it is nothing. </summary>
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void IsDangerousOnlyWhenTheFramerateActuallyChanges()
        {
            var level = CreateLevel();
            var generator = new FramerateRemapGenerator();
            var context = new GeneratorContext(level, new FrameSpan(0, FrameDuration));

            Assert.IsTrue(generator.IsDangerous(context, Params(30)));
            Assert.IsFalse(generator.IsDangerous(context, Params(Framerate)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MarksCurrentFramerateReadOnly()
        {
            var hints = new FramerateRemapGenerator().Hints;

            Assert.IsTrue(hints.IsReadOnly(nameof(FramerateRemapGenerator.Parameters.CurrentFramerate)));
            Assert.IsFalse(hints.IsReadOnly(nameof(FramerateRemapGenerator.Parameters.Framerate)));
        }

        #endregion
    }
}
