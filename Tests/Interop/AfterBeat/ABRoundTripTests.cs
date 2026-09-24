using System.Collections.Generic;
using System.Linq;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Export;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // Two different round trips, and they answer different questions.
    //
    // The PARSER round trip (.vgd -> model -> .vgd) is about fidelity to a foreign document: it must
    // keep even the keys this build has never heard of, because the wiki these models came from is
    // openly behind the game. That one is compared as JSON.
    //
    // The CONVERSION round trip (Afterbeat -> this format -> Afterbeat) can never be exact - the two
    // formats disagree about what a level even contains - so it is compared on the STABLE SUBSET:
    // the things both formats have a field for. Asserting more than that would be asserting that
    // nothing was lost, which is false by construction and documented as such.

    /// <summary> Out and back again: what survives an export followed by an import of the same level. </summary>
    public class ABRoundTripTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Parser_KeysThisBuildDoesNotKnow_SurviveARoundTrip()
        {
            const string json =
                "{" +
                "  \"objects\": [" +
                "    { \"id\": \"a\", \"st\": 1.5, \"future_key\": { \"nested\": [1, 2, 3] } }" +
                "  ]," +
                "  \"some_whole_section_we_never_heard_of\": { \"x\": 7 }" +
                "}";

            var parsed = ABSerialization.Deserialize<VgdLevel>(json);
            var written = ABSerialization.Serialize(parsed);

            StringAssert.Contains("future_key", written);
            StringAssert.Contains("some_whole_section_we_never_heard_of", written);
            StringAssert.Contains("nested", written);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Parser_KnownKeys_SurviveARoundTrip()
        {
            var source = ABMockData.CreateFullLevel();
            var reparsed = ABSerialization.Deserialize<VgdLevel>(
                ABSerialization.Serialize(source));

            Assert.AreEqual(source.Objects.Count, reparsed.Objects.Count);
            Assert.AreEqual(source.Themes.Count, reparsed.Themes.Count);
            Assert.AreEqual(source.Prefabs.Count, reparsed.Prefabs.Count);
            Assert.AreEqual(source.PrefabPlacements.Count, reparsed.PrefabPlacements.Count);
            Assert.AreEqual(source.Editor.Bpm.Value, reparsed.Editor.Bpm.Value, 1e-4f);
            Assert.AreEqual(VgdLevel.EventTrackCount, reparsed.Events.Count);

            var sourceObject = source.Objects[0];
            var reparsedObject = reparsed.Objects[0];
            Assert.AreEqual(sourceObject.StartTime, reparsedObject.StartTime, 1e-4f);
            Assert.AreEqual(sourceObject.Move.Keyframes.Count, reparsedObject.Move.Keyframes.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_StableSubset_SurvivesBothDirections()
        {
            var source = ABMockData.CreateLevel();
            // OnlyDepth is the one layer mode the export is the exact inverse of - the other three
            // deliberately spend draw order on things Afterbeat has no field for, so a depth that
            // survives both directions is a statement about this mode alone.
            var imported = ABLevelImporter.Import(source, ABMockData.CreateMeta(),
                new ABOptions(60) { LayerImport = ABLayerImport.OnlyDepth });

            var exported = ABLevelExporter.Export(imported.Level, imported.Meta);
            Assert.IsNotNull(exported.Level);

            var original = source.Objects[0];
            var returned = exported.Level.Objects.Single();

            Assert.AreEqual(original.StartTime, returned.StartTime, 1e-2f, "lifetime start");
            Assert.AreEqual(original.Shape, returned.Shape, "shape family");
            // Not the same NUMBER: a hitting object is written back as Normal (0), which is what
            // real levels carry, rather than as the documented Hit (4) - that number means Solid in
            // the numbering those files use. Both read back as hitting, which is what has to hold.
            Assert.AreEqual((int)ABObjectType.Hit, original.ObjectType);
            Assert.AreEqual((int)ABObjectType.Normal, returned.ObjectType, "hit or not");
            Assert.AreEqual(original.Depth, returned.Depth, "draw order");
            Assert.AreEqual(original.Move.Keyframes.Count, returned.Move.Keyframes.Count);

            Assert.AreEqual(1, exported.Level.Themes.Count);
            Assert.AreEqual(1, exported.Level.Markers.Count);
            Assert.AreEqual(1, exported.Level.Checkpoints.Count);
            Assert.AreEqual(source.Editor.Bpm.Value, exported.Level.Editor.Bpm.Value, 1e-3f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_CheckpointPosition_SurvivesBothDirections()
        {
            var source = ABMockData.CreateLevel();
            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var exported = ABLevelExporter.Export(imported.Level, null);

            var returned = exported.Level.Checkpoints.Single();
            Assert.AreEqual(3f, returned.Position.X, 1e-3f);
            Assert.AreEqual(-4f, returned.Position.Y, 1e-3f);
        }

        // Rotation is the one track where a mistake in either direction cancels out under a naive
        // round trip, so it is checked against the ORIGINAL deltas rather than against itself.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_Rotation_ComesBackAsTheSameDeltas()
        {
            var source = new VgdLevel();
            source.Objects.Add(ABMockData.CreateRotatingObject());

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var exported = ABLevelExporter.Export(imported.Level, null);

            var returned = exported.Level.Objects.Single().Rotate.Keyframes
                .OrderBy(k => k.Time)
                .Select(k => k.GetValue(0))
                .ToArray();

            Assert.AreEqual(2, returned.Length);
            Assert.AreEqual(90f, returned[0], 1e-2f);
            Assert.AreEqual(90f, returned[1], 1e-2f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Export_AudioAndEffects_AreReportedAsLost()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateLevel(), null,
                new ABOptions(60));

            imported.Level.Resources.Effects[new Models.Primitives.EffectId(System.Guid.NewGuid())] =
                new Models.Data.EffectData();

            var exported = ABLevelExporter.Export(imported.Level, null);
            var codes = exported.Report.Issues.Select(i => i.Code).ToArray();

            CollectionAssert.Contains(codes, "effect_resources");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Export_InactiveObject_IsSkippedAndReported()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateLevel(), null,
                new ABOptions(60));
            foreach (var pair in imported.Level.Game.Objects) pair.Value.Active = false;

            var exported = ABLevelExporter.Export(imported.Level, null);

            Assert.IsEmpty(exported.Level.Objects);
            CollectionAssert.Contains(exported.Report.Issues.Select(i => i.Code).ToArray(), "inactive_objects");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Interop_ThemeFile_RoundTripsThroughText()
        {
            var themeJson = ABSerialization.Serialize(ABMockData.CreateTheme());

            var theme = ABInterop.ImportTheme(themeJson);
            Assert.IsNotNull(theme);

            var written = ABInterop.ExportTheme(theme);
            var reparsed = ABSerialization.Deserialize<VgtTheme>(written);

            CollectionAssert.AreEqual(
                ABMockData.CreateTheme().Objects, reparsed.Objects);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Interop_PrefabFile_RoundTripsThroughText()
        {
            var prefab = new VgpPrefab { Id = "p1", Name = "Burst" };
            prefab.Objects.Add(ABMockData.CreateObject("inner"));

            var imported = ABInterop.ImportPrefab(ABSerialization.Serialize(prefab));
            Assert.IsNotNull(imported);
            Assert.AreEqual(1, imported.Objects.Count);

            var reparsed = ABSerialization.Deserialize<VgpPrefab>(
                ABInterop.ExportPrefab(imported));
            Assert.AreEqual(1, reparsed.Objects.Count);
            Assert.AreEqual("Burst", reparsed.Name);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Interop_ExportLevel_ProducesBothDocuments()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateLevel(),
                ABMockData.CreateMeta(), new ABOptions(60));

            var exported = ABInterop.ExportLevel(imported.Level, imported.Meta);

            Assert.IsNotEmpty(exported.LevelJson);
            Assert.IsNotEmpty(exported.MetaJson);
            StringAssert.Contains("Test Song", exported.MetaJson);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Export_KeepsTheLevelsOwnFramerate_NotTheCallers()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateLevel(), null,
                new ABOptions(30));

            // Frames are being turned back into seconds; reading them at 120 would retime the level.
            var exported = ABLevelExporter.Export(imported.Level, null, new ABOptions(120));

            Assert.AreEqual(1f, exported.Level.Objects.Single().StartTime, 1e-2f);
        }

        #region Draw order

        /// <summary> A level holding one object per (band, depth) pair, each named after the pair so
        /// it can be found again on the way back. </summary>
        private static VgdLevel DrawOrderLevel(params int[] depths)
        {
            var level = new VgdLevel();

            foreach (var band in new[]
                     {
                         ABRenderLayer.Background,
                         ABRenderLayer.Default,
                         ABRenderLayer.AbovePlayer,
                     })
            foreach (var depth in depths)
            {
                var source = ABMockData.CreateObject($"b{(int)band}-d{depth}");
                source.Name = source.Id;
                source.Depth = depth;
                source.RenderLayer = (int)band;
                level.Objects.Add(source);
            }

            return level;
        }

        /// <summary> Where one exported object sits in Afterbeat's own ordering, back to front -
        /// the band above the depth, exactly as the source game sorts. </summary>
        private static int SourceOrder(VgdObject source)
            => ABLayerMap.ToBandRank(ABLayerMap.ToBand(source)) * ABLayerMap.DepthSpan
                + VgdObject.MaxDepth - ABLayerMap.ToDepth(source);

        // The bijection, stated across all three bands rather than inside one: the bands are only
        // right relative to each other, and the export decides which band a layer belongs to by the
        // same boundaries the import placed them on. A drift in either one shows up here as a level
        // that comes back a band out - drawn in front of the player where it used to be behind it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_UnderOnlyDepth_EveryBandAndDepthComesBackUnchanged()
        {
            var source = DrawOrderLevel(0, 1, 20, 59, 60);
            var imported = ABLevelImporter.Import(source, null,
                new ABOptions(60) { LayerImport = ABLayerImport.OnlyDepth });

            var exported = ABLevelExporter.Export(imported.Level, null);
            var returned = exported.Level.Objects.ToDictionary(o => o.Name);

            foreach (var original in source.Objects)
            {
                var back = returned[original.Name];
                Assert.AreEqual(original.Depth, back.Depth, $"{original.Name}: depth");
                Assert.AreEqual(original.RenderLayer, back.RenderLayer, $"{original.Name}: band");
            }
        }

        // Auto is not a bijection and is not meant to be - it packs the depths a level does not use
        // out of the way, so a level that used depths 0 and 60 comes back using 0 and 1. What it
        // must never do is REORDER, and that is what survives both directions: whatever the source
        // level drew in front still draws in front after a full round trip.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Conversion_UnderAuto_PacksTheDepthsButKeepsTheirOrder()
        {
            var source = DrawOrderLevel(0, 7, 20, 45, 60);
            var imported = ABLevelImporter.Import(source, null, new ABOptions(60) { LayerImport = ABLayerImport.Auto });

            var exported = ABLevelExporter.Export(imported.Level, null);
            var returned = exported.Level.Objects.ToDictionary(o => o.Name);

            foreach (var first in source.Objects)
            foreach (var second in source.Objects)
            {
                var expected = SourceOrder(first).CompareTo(SourceOrder(second));
                var actual = SourceOrder(returned[first.Name]).CompareTo(SourceOrder(returned[second.Name]));

                Assert.AreEqual(expected, actual,
                    $"{first.Name} against {second.Name}: the order the source game drew them in");
            }

            foreach (var original in source.Objects)
                Assert.AreEqual(original.RenderLayer, returned[original.Name].RenderLayer,
                    $"{original.Name}: a packed layer never leaves its own band");

            // Packed, not preserved: five depths went out and five come back, but as the five
            // smallest ones rather than the five the author happened to pick.
            var ordinary = returned.Values
                .Where(o => ABLayerMap.ToBand(o) == ABRenderLayer.Default)
                .ToArray();

            Assert.AreEqual(5, ordinary.Select(o => o.Depth).Distinct().Count());
            Assert.AreEqual(4, ordinary.Max(o => o.Depth),
                "packed means the range is the level's own, not the format's");
        }

        // The consequence of the player line for a level authored HERE rather than imported: this
        // format's avatar sits at 0.5, so an object on the default layer 0 draws BEHIND it and
        // exports as an ordinary object, which is what makes a level authored here arrive over
        // there as an ordinary level rather than an entirely above-player one. Covering the player
        // is the explicit act on both sides - layer 1 here, the AbovePlayer band there.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Export_LayerOne_IsAbovePlayer_AndZeroIsTheTopOfTheOrdinaryBand()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateLevel(), null, new ABOptions(60));
            var only = imported.Level.Game.Objects.Values.Single();

            only.Layer = ValueRules.FirstLayerAbovePlayer;
            var above = ABLevelExporter.Export(imported.Level, null).Level.Objects.Single();
            Assert.AreEqual((int)ABRenderLayer.AbovePlayer, above.RenderLayer,
                "layer 1 draws in front of this format's avatar, so it is not an ordinary object there");
            Assert.AreEqual(VgdObject.MaxDepth, above.Depth, "and it is the backmost of that band");

            only.Layer = ValueRules.LastLayerBehindPlayer;
            var ordinary = ABLevelExporter.Export(imported.Level, null).Level.Objects.Single();
            Assert.AreEqual((int)ABRenderLayer.Default, ordinary.RenderLayer);
            Assert.AreEqual(VgdObject.MinDepth, ordinary.Depth,
                "the last layer behind the player is the frontmost ordinary depth");
        }

        // A materialized placement's copies are what the export writes, and they hang off a
        // placement that now takes no draw order of its own - so their depth has to come from the
        // template's own layers rather than from where the placement sits in the level's list.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Export_AMaterializedPlacementsCopies_CarryTheirOwnDepth()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null,
                new ABOptions(60) { LayerImport = ABLayerImport.Auto });

            var placement = imported.Level.Game.Objects.Values.OfType<PrefabObject>().Single();
            Assert.AreEqual(0, placement.Layer, "a placement is not a render band");

            // Materializing is the host's job, so the copy is written by hand here - one template
            // object, parented to the placement, carrying the layer the level-wide plan gave it.
            var template = imported.Level.Resources.Prefabs.Values.Single();
            var inner = template.Objects.Values.Single();
            var copy = (ShapeObject)inner.Copy();
            copy.ObjectId = imported.Level.Settings.GetNextObjectId();
            copy.ParentObjectId = placement.ObjectId;
            copy.Name = "materialized";
            imported.Level.Game.Objects[copy.ObjectId] = copy;

            var exported = ABLevelExporter.Export(imported.Level, null).Level;
            var back = exported.Objects.Single(o => o.Name == "materialized");

            Assert.AreEqual((int)ABRenderLayer.Default, back.RenderLayer);
            Assert.AreEqual(ValueRules.LastLayerBehindPlayer - inner.Layer, back.Depth,
                "the copy's own depth, not one derived from where the placement sat");

            // The placement itself has to be in the document too, or the copy's parent reference
            // names nothing - which the source game reads as a root, dropping the position, scale
            // and rotation the whole subtree was placed at.
            var node = exported.Objects.Single(o => o.Id == ABExportContext.ToSourceId(placement.ObjectId));

            Assert.AreEqual(node.Id, back.ParentId, "the copy still hangs off its placement");
            Assert.AreEqual((int)ABObjectType.AlphaEmpty, node.ObjectType,
                "a placement draws nothing of its own - its content is the copies");

            var moved = node.Move.Keyframes[0].Values;
            Assert.AreEqual(1f, moved[0], 1e-4f, "the placement's own position went with it");
            Assert.AreEqual(2f, moved[1], 1e-4f);
        }

        // A placement's scale is the multiplier its whole subtree sits inside, and this format keeps
        // that on Scales rather than on Sizes - reading Sizes wrote a placement scaled to nothing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Export_APlacementsScale_ComesFromItsScaleTrack()
        {
            var imported = ABLevelImporter.Import(ABMockData.CreateFullLevel(), null,
                new ABOptions(60));

            var placement = imported.Level.Game.Objects.Values.OfType<PrefabObject>().Single();
            placement.Scales.Clear();
            placement.Scales.Add(new ScaKey(new Vector2Value(3f, 4f), 0));

            // Moving, so it takes the flatten path this test is about - a static placement crosses
            // as a prefab_objects entry instead (ABPlacementExportTests).
            placement.Positions.Add(new PosKey(new Vector2Value(5f, 5f), 30));

            var exported = ABLevelExporter.Export(imported.Level, null).Level;
            var node = exported.Objects.Single(o => o.Id == ABExportContext.ToSourceId(placement.ObjectId));

            var scaled = node.Scale.Keyframes[0].Values;
            Assert.AreEqual(3f, scaled[0], 1e-4f);
            Assert.AreEqual(4f, scaled[1], 1e-4f);
        }

        // The editor row an export writes is what makes OnlyEditor the inverse of it, and the two
        // halves live in different files - so the only way to state it is to run both.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Export_TheEditorRow_DecodesBackToTheDepthItWasWrittenFrom()
        {
            var source = new VgdLevel();
            for (var depth = VgdObject.MinDepth; depth <= VgdObject.MaxDepth; depth++)
            {
                var one = ABMockData.CreateObject($"d{depth}");
                one.Depth = depth;
                source.Objects.Add(one);
            }

            var imported = ABLevelImporter.Import(source, null,
                new ABOptions(60) { LayerImport = ABLayerImport.OnlyDepth });
            var exported = ABLevelExporter.Export(imported.Level, null).Level;

            foreach (var back in exported.Objects)
                Assert.AreEqual(back.Depth, ABLayerMap.ToEditorIndex(back),
                    $"depth {back.Depth}: the row it was filed on has to name it again");
        }

        #endregion

        #region Origins

        // Text measures its origin from the opposite side - see ABObjectExporter.ApplyOrigin. Writing
        // it the way a shape's is written mirrors every off-centre text on the way out, which is the
        // same bug the import had on the way in and is invisible in anything but a round trip.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Conversion_ATextsOrigin_ComesBackOnTheSameSide()
        {
            var source = new VgdLevel();
            var text = ABMockData.CreateObject("text");
            text.Shape = (int)ABShape.Text;
            text.Text = "left";
            text.Origin = new VgdVector2(0.25f, -0.5f);
            source.Objects.Add(text);

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var back = ABLevelExporter.Export(imported.Level, null).Level.Objects.Single();

            Assert.AreEqual(0.25f, back.Origin.X, 1e-4f, "an origin to the right stays to the right");
            Assert.AreEqual(-0.5f, back.Origin.Y, 1e-4f);
        }

        // A Triangle's reference point sits at its centroid over there, and the import folds that
        // into the pivot because this format has nowhere else to keep it. Writing the pivot straight
        // back out hands Afterbeat the offset a second time, on top of the one its own shape already
        // has - so an object nobody ever moved comes back moved.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Conversion_AShapesOwnReferencePoint_IsNotWrittenBackAsAnOrigin()
        {
            var source = new VgdLevel();
            var triangle = ABMockData.CreateObject("triangle");
            triangle.Shape = (int)ABShape.Triangle;
            triangle.ShapeOption = 0;
            source.Objects.Add(triangle);

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var shape = imported.Level.Game.Objects.Values.OfType<ShapeObject>().Single();
            Assert.AreEqual(1, shape.Pivots.Count, "the centroid has to live somewhere on the way in");

            var back = ABLevelExporter.Export(imported.Level, null).Level.Objects.Single();

            Assert.AreEqual(0f, back.Origin.X, 1e-4f);
            Assert.AreEqual(0f, back.Origin.Y, 1e-4f,
                "the shape carries its own reference point over there; the origin must stay empty");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Conversion_AnAuthoredOriginOnAShapeThatHasOne_SurvivesBothDirections()
        {
            var source = new VgdLevel();
            var triangle = ABMockData.CreateObject("triangle");
            triangle.Shape = (int)ABShape.Triangle;
            triangle.ShapeOption = 0;
            triangle.Origin = new VgdVector2(0.2f, -0.3f);
            source.Objects.Add(triangle);

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var back = ABLevelExporter.Export(imported.Level, null).Level.Objects.Single();

            Assert.AreEqual(0.2f, back.Origin.X, 1e-4f);
            Assert.AreEqual(-0.3f, back.Origin.Y, 1e-4f, "the author's own offset, and only it");
        }

        #endregion

        #region Post-processing

        private static VgdEventKeyframe EventKey(float time, params float[] values)
            => new()
            {
                Time = time,
                Values = Newtonsoft.Json.Linq.JArray.FromObject(values),
            };

        // The grain keyframe's third slot is the PRESET, not a size - the export used to write a
        // constant there, so every grain in an exported level came back as the same texture.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Conversion_TheGrainPreset_SurvivesBothDirections()
        {
            const float preset = 4f;

            var source = new VgdLevel();
            source.SetEvents(ABEventTrack.Grain,
                new List<VgdEventKeyframe> { EventKey(0f, 0.5f, 0f, preset, 1f) });

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            Assert.AreEqual(ABPostProcessingMap.ImportGrainType(preset),
                imported.Level.Game.PostProcessingEvents.Grains.Single().Type);

            var back = ABLevelExporter.Export(imported.Level, null).Level
                .GetEvents(ABEventTrack.Grain).Single();

            Assert.AreEqual(preset, back.GetFloat(2), 1e-4f, "the preset the author picked");
        }

        // Index 9 is one past the palette and means "no theme colour at all" - the value every real
        // level carries on the effects nobody opened. Matching its stand-in colour to the nearest
        // palette slot instead hands an untouched bloom a theme colour, which then follows every
        // theme switch in the level.
        [TestCase(ABEventTrack.Bloom, 2)]
        [TestCase(ABEventTrack.Vignette, 6)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Conversion_AnEffectWithNoThemeColour_KeepsHavingNone(ABEventTrack track, int slot)
        {
            var source = ABMockData.CreateLevel();
            var values = new float[slot + 1];
            values[0] = 0.5f;
            values[slot] = ABEventsImporter.EffectColorNone;
            source.SetEvents(track, new List<VgdEventKeyframe> { EventKey(0f, values) });

            var imported = ABLevelImporter.Import(source, null, new ABOptions(60));
            var back = ABLevelExporter.Export(imported.Level, null).Level.GetEvents(track).Single();

            Assert.AreEqual(ABEventsImporter.EffectColorNone, back.GetFloat(slot), 1e-4f,
                "an effect nobody tinted must not come back tinted");
        }

        #endregion

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Report_AggregatesByCause_RatherThanAccumulating()
        {
            var report = new BH.SDK.Interop.InteropReport();
            for (var i = 0; i < 500; i++) report.Dropped("same_cause", "message", $"objects[{i}]");

            Assert.AreEqual(1, report.Issues.Count);
            Assert.AreEqual(500, report.Issues[0].Count);
            Assert.AreEqual("objects[0]", report.Issues[0].FirstPath, "the first one is the one to go look at");
        }
    }
}