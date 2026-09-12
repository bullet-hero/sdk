using System.Linq;
using BH.SDK.Interop.AfterBeat;
using BH.SDK.Interop.AfterBeat.Import;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using NUnit.Framework;

namespace BH.SDK.Tests.Interop.AfterBeat
{
    // Every other fixture in this folder builds MODELS and converts those, which is the right shape
    // for testing a conversion and the wrong shape for testing a transcription: a JSON key spelled
    // wrong is spelled the same wrong way on both sides of a round trip, so it passes.
    //
    // These tests are therefore written against JSON TEXT copied from a real level, one fragment per
    // thing the format's published description gets wrong. Each of them failed before the key it
    // names was read, and each failure was silent - a level that loads, plays, and looks wrong.
    //
    // The ids are the ones a real file carries: Afterbeat generates them out of arbitrary bytes, so
    // they are not readable and must not be tidied into "obj-1" here - part of what these fixtures
    // assert is that nothing along the way assumes an id is printable.

    /// <summary> The parser against documents as Afterbeat actually writes them - numbers as strings included -
    /// rather than as its schema suggests. </summary>
    public class ABRealFormatTests
    {
        private const int Framerate = 60;

        private static ABOptions Options() => new(Framerate);

        // The theme track is the only one carrying a string, and it carries it under "evs" rather
        // than inside "ev". Read out of "ev" it is empty, the level ends up with no theme keyframe
        // at all, and every theme-referenced colour in it resolves against nothing - which is a
        // whole level rendering white.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ThemeKeyframe_ReadsTheStringPayloadKey()
        {
            const string json = @"{
                ""themes"": [ { ""id"": ""theme-a"", ""name"": ""Easy Face"",
                    ""obj"": [""FFFFFF"",""F48F91""], ""base_bg"": ""EEEEEE"", ""base_gui"": ""212121"" } ],
                ""events"": [ [], [], [], [], [ { ""evs"": [""theme-a""] }, { ""t"": 19.68, ""evs"": [""theme-a""] } ],
                    [], [], [], [], [], [], [], [], [] ]
            }";

            var result = ABLevelImporter.ImportJson(json, null, Options());

            Assert.AreEqual(2, result.Level.Game.Events.Themes.Count, "both theme keyframes crossed");
            Assert.AreEqual(ABIdMap.ToThemeId("theme-a"), result.Level.Game.Events.Themes[0].ThemeId);
            Assert.AreEqual(FrameRules.MinFrame + 1181, result.Level.Game.Events.Themes[1].Frame,
                "19.68s at 60fps");
        }

        // A level defining themes but never switching to one is ordinary authored content there and
        // a colourless level here, so the first theme is placed on the first frame instead.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ThemesWithNoThemeKeyframe_GetOneOnTheFirstFrame()
        {
            const string json = @"{
                ""themes"": [ { ""id"": ""theme-a"", ""obj"": [""FFFFFF""], ""base_bg"": ""EEEEEE"" } ],
                ""objects"": []
            }";

            var result = ABLevelImporter.ImportJson(json, null, Options());

            Assert.AreEqual(1, result.Level.Game.Events.Themes.Count);
            Assert.AreEqual(FrameRules.MinFrame, result.Level.Game.Events.Themes[0].Frame);
            CollectionAssert.Contains(result.Report.Issues.Select(i => i.Code).ToArray(),
                "theme_track_synthesized");
        }

        // A placement's start is "t", not "st", and the format's published tree omits it entirely.
        // Read as zero, every placement in a level starts at frame 0 and the whole prefab library
        // plays in the first second.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_PrefabPlacement_ReadsItsOwnStartTime()
        {
            const string json = @"{
                ""prefabs"": [ { ""id"": ""prefab-a"", ""n"": ""Water on the hill"", ""objs"": [
                    { ""id"": ""inner-1"", ""ot"": 0, ""s"": 0, ""ak_t"": 3, ""ak_o"": 1.0,
                      ""e"": [ { ""k"": [ { ""ev"": [0.0, 0.0] } ] }, { ""k"": [ { ""ev"": [1.0, 1.0] } ] },
                               { ""k"": [ { ""ev"": [0.0] } ] }, { ""k"": [ { ""ev"": [0.0] } ] } ] } ] } ],
                ""prefab_objects"": [ { ""id"": ""place-1"", ""pid"": ""prefab-a"", ""t"": 7.6166,
                    ""e"": [ {}, { ""ev"": [1.0, 1.0] }, {} ] } ],
                ""objects"": []
            }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var placement = result.Level.Game.Objects.Values.OfType<PrefabObject>().Single();

            Assert.AreEqual(FrameRules.MinFrame + 457, placement.Span.StartFrame, "7.6166s at 60fps");

            // The length is the template's own, not the level's - a placement covering the whole
            // timeline is unreadable in the editor and useless to trim.
            var template = result.Level.Resources.Prefabs.Values.Single();
            Assert.AreEqual(template.Root.Span.FrameDuration, placement.Span.FrameDuration);
        }

        // Real levels write ot = 0 for an ordinary hitting object; the documented table starts at 4.
        // Against that table alone, 0 is "not Hit" and two thirds of a level arrives harmless.
        [TestCase(0, true, TestName = "Import_ObjectType_Normal_Hits")]
        [TestCase(4, true, TestName = "Import_ObjectType_Hit_Hits")]
        [TestCase(1, false, TestName = "Import_ObjectType_Helper_DoesNot")]
        [TestCase(2, false, TestName = "Import_ObjectType_Decoration_DoesNot")]
        [TestCase(5, false, TestName = "Import_ObjectType_NoHit_DoesNot")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ObjectType_DecidesWhetherThereIsACollider(int objectType, bool expectsCollider)
        {
            var json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": " + objectType + @", ""s"": 0,
                ""ak_t"": 3, ""ak_o"": 1.0,
                ""e"": [ { ""k"": [ { ""ev"": [0.0, 0.0] } ] }, { ""k"": [ { ""ev"": [1.0, 1.0] } ] },
                         { ""k"": [ { ""ev"": [0.0] } ] }, { ""k"": [ { ""ev"": [0.0] } ] } ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var shape = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            Assert.AreEqual(expectsCollider, shape.ColliderId.IsEnabled());
        }

        [TestCase(3, TestName = "Import_ObjectType_LegacyEmpty_HasNoGeometry")]
        [TestCase(6, TestName = "Import_ObjectType_Empty_HasNoGeometry")]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_EmptyObjectType_HasNoGeometry(int objectType)
        {
            var json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": " + objectType + @", ""ak_t"": 3, ""ak_o"": 1.0,
                ""e"": [ {}, {}, {}, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var imported = result.Level.Game.Objects.Values.Single();

            Assert.IsInstanceOf<RectObject>(imported);
            Assert.IsNotInstanceOf<ShapeObject>(imported);
        }

        // Depth is absolute there and Layer is parent-relative here, so a child's layer needs its
        // parent's. An object list is in no particular order, and a child written FIRST used to find
        // no parent depth at all and draw its whole branch at the wrong depth.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_ChildWrittenBeforeItsParent_StillGetsARelativeLayer()
        {
            const string json = @"{ ""objects"": [
                { ""id"": ""child"", ""p_id"": ""parent"", ""ot"": 0, ""s"": 0, ""d"": 15,
                  ""ak_t"": 3, ""ak_o"": 1.0, ""e"": [ {}, {}, {}, {} ] },
                { ""id"": ""parent"", ""ot"": 0, ""s"": 0, ""d"": 20,
                  ""ak_t"": 3, ""ak_o"": 1.0, ""e"": [ {}, {}, {}, {} ] } ] }";

            // OnlyDepth so the two depths survive as themselves - Auto, the default, packs the
            // 59 depths between them away and would make this a statement about packing instead.
            var options = Options();
            options.LayerImport = ABLayerImport.OnlyDepth;

            var result = ABLevelImporter.ImportJson(json, null, options);
            var objects = result.Level.Game.Objects.Values.ToArray();
            var parent = objects.Single(o => o.ParentObjectId == ObjectId.Null);
            var child = objects.Single(o => o.ParentObjectId == parent.ObjectId);

            Assert.AreEqual(-1 - VgdObject.DefaultDepth, parent.Layer,
                "the whole Default band draws behind the player, so depth 0 is -1 and this is deeper");
            Assert.AreEqual(5, child.Layer, "depth 15 under depth 20, expressed relative to the parent");
        }

        // Rotation is a delta per keyframe, so the track only integrates to the right animation in
        // time order - and the format guarantees keyframe times are unique, never sorted.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_UnsortedRotationTrack_IntegratesInTimeOrder()
        {
            const string json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": 0, ""s"": 0, ""ak_t"": 3, ""ak_o"": 3.0,
                ""e"": [ {}, {}, { ""k"": [ { ""t"": 2.0, ""ev"": [30.0] }, { ""ev"": [90.0] },
                                            { ""t"": 1.0, ""ev"": [-30.0] } ] }, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var rotations = result.Level.Game.Objects.Values.Single().Rotations;

            CollectionAssert.AreEqual(new[] { 0, 60, 120 },
                rotations.Select(k => k.Frame - FrameRules.MinFrame).ToArray());
            Assert.AreEqual(90f, Degrees(rotations[0]), 1e-3f, "first delta is measured from no rotation");
            Assert.AreEqual(60f, Degrees(rotations[1]), 1e-3f, "90 - 30");
            Assert.AreEqual(90f, Degrees(rotations[2]), 1e-3f, "60 + 30");
        }

        // The force track crosses now that PlayerEvents carries one. It is still reported as
        // deferred rather than as clean: the level arrives complete and plays as if the track were
        // not there, because the player does not read it yet.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_PlayerForce_LandsOnTheVelocityTrack()
        {
            const string json = @"{ ""events"": [ [], [], [], [], [], [], [], [], [], [], [], [], [],
                [ { ""ev"": [3.0, -4.0, 0.0] }, { ""t"": 1.0, ""ev"": [0.0, 0.0, 0.0] } ] ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var velocities = result.Level.Game.PlayerEvents.Velocities;

            Assert.AreEqual(2, velocities.Count);
            var force = (BH.SDK.Models.Values.Vector2Value)velocities[0].Force;
            Assert.AreEqual(3f, force.X, 1e-4f);
            Assert.AreEqual(-4f, force.Y, 1e-4f);
            Assert.AreEqual(FrameRules.MinFrame + 60, velocities[1].Frame, "1s at 60fps");

            var deferred = result.Report.Issues.Single(i => i.Code == "event_player_force");
            Assert.AreEqual(BH.SDK.Interop.InteropSeverity.Deferred, deferred.Severity);
        }

        // The editor's custom polygon: five numbers under a key the format's description never
        // mentions. Thousands of objects in an ordinary level use it, and every one of them used to
        // land on a Square with "shape_unknown" reported.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_CustomPolygonOnTheRungs_LandsOnABuiltInShape()
        {
            // sides 6, roundness 0, thickness 1 (filled), slices 6 (whole turn), not inverted -
            // every one of which is a rung the built-in library has, so this writes no geometry at
            // all. It used to write one shape resource per distinct custom polygon, and a level
            // leaning on the source editor's polygon slider arrived carrying dozens of them.
            const string json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": 0, ""s"": 5, ""so"": 6,
                ""csp"": [6.0, 0.0, 1.0, 6.0, 0.0], ""ak_t"": 3, ""ak_o"": 1.0,
                ""e"": [ {}, {}, {}, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var shape = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            Assert.AreEqual(ShapeId.Hexagon.Fill, shape.ShapeId);
            Assert.IsEmpty(result.Level.Resources.CompositeShapes, "nothing had to be built");
            CollectionAssert.DoesNotContain(result.Report.Issues.Select(i => i.Code).ToArray(),
                "shape_unknown");
        }

        // The other half of the same rule: a polygon whose thickness sits BETWEEN rungs, or whose
        // corners are rounded, still has no name in the built-in library and still becomes the
        // level's own geometry. Rounding is the axis this library has no rung for at all.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_RoundedCustomPolygon_BecomesLevelAuthoredGeometry()
        {
            const string json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": 0, ""s"": 5, ""so"": 6,
                ""csp"": [6.0, 0.8, 1.0, 6.0, 0.0], ""ak_t"": 3, ""ak_o"": 1.0,
                ""e"": [ {}, {}, {}, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var shape = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            Assert.AreEqual(1, result.Level.Resources.CompositeShapes.Count, "one shape resource");
            Assert.IsTrue(result.Level.Resources.CompositeShapes.ContainsKey(shape.ShapeId));
        }

        // The id is derived from the PARAMETERS, not from the (shape, option) pair - one pair stands
        // for every custom polygon in the level, so deriving from it would give a level's fifty
        // different custom shapes one geometry.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_TwoDifferentCustomPolygons_ProduceTwoShapes()
        {
            const string json = @"{ ""objects"": [
                { ""id"": ""a"", ""ot"": 0, ""s"": 1, ""so"": 9, ""csp"": [32.0, 0.0, 0.3, 14.0, 0.0],
                  ""ak_t"": 3, ""ak_o"": 1.0, ""e"": [ {}, {}, {}, {} ] },
                { ""id"": ""b"", ""ot"": 0, ""s"": 1, ""so"": 9, ""csp"": [32.0, 0.0, 0.3, 3.0, 0.0],
                  ""ak_t"": 3, ""ak_o"": 1.0, ""e"": [ {}, {}, {}, {} ] },
                { ""id"": ""c"", ""ot"": 0, ""s"": 1, ""so"": 9, ""csp"": [32.0, 0.0, 0.3, 14.0, 0.0],
                  ""ak_t"": 3, ""ak_o"": 1.0, ""e"": [ {}, {}, {}, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var shapes = result.Level.Game.Objects.Values.OfType<ShapeObject>()
                .Select(o => o.ShapeId).ToArray();

            Assert.AreEqual(2, result.Level.Resources.CompositeShapes.Count);
            Assert.AreEqual(shapes[0], shapes[2], "identical parameters share one resource");
            Assert.AreNotEqual(shapes[0], shapes[1]);
        }

        // A preset pair wins over stale csp: an object that was custom earlier and is not any more
        // still carries the parameters, and reading them would draw the shape it used to be.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_KnownShapeWithLeftoverCustomParameters_KeepsThePreset()
        {
            const string json = @"{ ""objects"": [ { ""id"": ""a"", ""ot"": 0, ""s"": 1, ""so"": 0,
                ""csp"": [6.0, 0.0, 1.0, 6.0, 0.0], ""ak_t"": 3, ""ak_o"": 1.0,
                ""e"": [ {}, {}, {}, {} ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());
            var shape = result.Level.Game.Objects.Values.OfType<ShapeObject>().Single();

            Assert.AreEqual(ShapeId.Circle.Fill, shape.ShapeId);
            Assert.IsEmpty(result.Level.Resources.CompositeShapes);
        }

        // Freehand notes are authored, unlike the rest of the editor block, so losing them is worth
        // a line in the report.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Import_Annotations_AreReported()
        {
            const string json = @"{ ""objects"": [], ""annotations"": [
                { ""id"": ""x"", ""m"": ""y"", ""t"": 12.5, ""p"": [ { ""x"": 1.0, ""y"": 2.0 } ] } ] }";

            var result = ABLevelImporter.ImportJson(json, null, Options());

            CollectionAssert.Contains(result.Report.Issues.Select(i => i.Code).ToArray(), "annotations");
        }

        private static float Degrees(BH.SDK.Models.Keyframes.AngleKey key)
            => ((BH.SDK.Models.Values.FloatValue)key.Angle).Value * ABValueMap.RadiansToDegrees;
    }
}
