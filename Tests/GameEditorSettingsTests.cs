using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // GameEditorSettings is the one UserSettings group that has been RESTRUCTURED rather than added
    // to - sixteen flat properties became nine nested groups, which is what took the domain to (2,0).
    // Two kinds of assert live here as a result: the ordinary boilerplate ones every group needs
    // (defaults, Reset, Copy/Pull, Equals seeing each field independently), and the ones that pin the
    // NESTING itself - a Copy that shared a group instance with its source, or an Equals that only
    // compared references, would pass every flat test ever written for this class.

    /// <summary> GameEditorSettings' nine groups: defaults, boilerplate, isolation, round trip. </summary>
    public class GameEditorSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Groups_AreNeverNull()
        {
            var settings = new GameEditorSettings();

            Assert.NotNull(settings.Savings);
            Assert.NotNull(settings.Camera);
            Assert.NotNull(settings.Player);
            Assert.NotNull(settings.Grid);
            Assert.NotNull(settings.Selection);
            Assert.NotNull(settings.Gizmos);
            Assert.NotNull(settings.Timeline);
            Assert.NotNull(settings.Interface);
            Assert.NotNull(settings.Serialization);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SerializeModes_DefaultToJson()
        {
            var settings = new GameEditorSettings();

            Assert.AreEqual(SerializationType.Json, settings.Serialization.LevelMode);
            Assert.AreEqual(SerializationType.Json, settings.Serialization.ResourcesMode);
        }

        // The one default here that used to differ by platform: the host seeded it on for a desktop
        // and off for a phone, so the editor opened differently depending on where it was opened.
        // Off everywhere is the answer now, and the SDK owns it alone - nothing outside re-seeds it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PreviewPlayer_DefaultsToOff()
        {
            var settings = new GameEditorSettings();

            Assert.IsFalse(settings.Player.ActiveDefault);
        }

        // Reset on the root has to reach every group. It delegates rather than reassigning, so a
        // group somebody forgot to list would keep whatever the author had left in it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reset_ReachesEveryGroup()
        {
            var settings = new GameEditorSettings();

            settings.Savings.Autosave = false;
            settings.Savings.HistoryLength = 64;
            settings.Camera.ZoomToMouse = false;
            settings.Player.ActiveDefault = true;
            settings.Grid.Size = 0.25f;
            settings.Selection.PickInvisibleAABB = true;
            settings.Gizmos.Scale = 4f;
            settings.Timeline.GlobalLoop = false;
            settings.Interface.RenderInframes = true;
            settings.Serialization.LevelMode = SerializationType.Blob;

            settings.Reset();

            Assert.IsTrue(settings.Savings.Autosave);
            Assert.AreEqual(512, settings.Savings.HistoryLength);
            Assert.IsTrue(settings.Camera.ZoomToMouse);
            Assert.IsFalse(settings.Player.ActiveDefault);
            Assert.AreEqual(1f, settings.Grid.Size);
            Assert.IsFalse(settings.Selection.PickInvisibleAABB);
            Assert.AreEqual(1f, settings.Gizmos.Scale);
            Assert.IsTrue(settings.Timeline.GlobalLoop);
            Assert.IsFalse(settings.Interface.RenderInframes);
            Assert.AreEqual(SerializationType.Json, settings.Serialization.LevelMode);
        }

        // The nesting's own failure mode, and the reason this test exists at all: Copy() must build
        // NEW group instances. Sharing them would make a copy an alias, so editing the copy would
        // silently edit the original - and every equality assert would still pass.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Copy_DoesNotShareGroupInstances()
        {
            var source = new GameEditorSettings();
            var copy = source.Copy();

            Assert.AreNotSame(source.Savings, copy.Savings);
            Assert.AreNotSame(source.Camera, copy.Camera);
            Assert.AreNotSame(source.Player, copy.Player);
            Assert.AreNotSame(source.Grid, copy.Grid);
            Assert.AreNotSame(source.Selection, copy.Selection);
            Assert.AreNotSame(source.Gizmos, copy.Gizmos);
            Assert.AreNotSame(source.Timeline, copy.Timeline);
            Assert.AreNotSame(source.Interface, copy.Interface);
            Assert.AreNotSame(source.Serialization, copy.Serialization);

            copy.Grid.Size = 8f;
            Assert.AreEqual(1f, source.Grid.Size);
        }

        // Pull is the one that must NOT reassign: the device hands its sub-groups out one at a time,
        // so whoever is holding source.Grid has to keep seeing the value that lands in it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Pull_KeepsEveryGroupInstance()
        {
            var source = new GameEditorSettings();
            source.Grid.Size = 0.5f;
            source.Gizmos.Scale = 2f;

            var target = new GameEditorSettings();
            var heldGrid = target.Grid;
            var heldGizmos = target.Gizmos;

            target.Pull(source);

            Assert.AreSame(heldGrid, target.Grid);
            Assert.AreSame(heldGizmos, target.Gizmos);
            Assert.AreEqual(0.5f, heldGrid.Size);
            Assert.AreEqual(2f, heldGizmos.Scale);
            Assert.IsTrue(source.Equals(target));
            Assert.AreEqual(source.GetHashCode(), target.GetHashCode());
        }

        // Nine groups fold into eight hash slots, so the last two share one - and Equals has to see
        // a change in ANY of them. A group left out of either would make two different settings
        // compare equal.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_SeesEveryGroupIndependently()
        {
            var a = new GameEditorSettings();

            AssertDiffers(a, s => s.Savings.HistoryLength = 64);
            AssertDiffers(a, s => s.Camera.WheelMultiplier = 0.5f);
            AssertDiffers(a, s => s.Player.ResetGizmos = false);
            AssertDiffers(a, s => s.Player.BotControl = true);
            AssertDiffers(a, s => s.Player.BotDebug = true);
            AssertDiffers(a, s => s.Player.BotDebugGrid = false);
            AssertDiffers(a, s => s.Player.BotDebugTarget = false);
            AssertDiffers(a, s => s.Player.BotDebugReach = false);
            AssertDiffers(a, s => s.Grid.Opacity = 0.9f);
            AssertDiffers(a, s => s.Selection.LongPressDelay = 1.5f);
            AssertDiffers(a, s => s.Gizmos.Scale = 3f);
            AssertDiffers(a, s => s.Timeline.EdgeHandlePx = 24f);
            AssertDiffers(a, s => s.Interface.LogValueClamps = false);
            AssertDiffers(a, s => s.Interface.LinkColliderToShape = true);
            AssertDiffers(a, s => s.Serialization.ResourcesMode = SerializationType.Blob);
        }

        private static void AssertDiffers(GameEditorSettings source, System.Action<GameEditorSettings> edit)
        {
            var other = source.Copy();
            edit(other);
            Assert.IsFalse(source.Equals(other));
        }

        // The three preferences that default to OFF, which is the opposite call the rest of this
        // model makes. Each covers or widens something the author has just worked on, so it is in the
        // way more often than it answers a question.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ThreeDiagnosticToggles_DefaultToOff()
        {
            var settings = new GameEditorSettings();

            Assert.IsFalse(settings.Selection.PreviewColliderOnSelect);
            Assert.IsFalse(settings.Selection.PickInvisibleAABB);
            Assert.IsFalse(settings.Interface.RenderInframes);
        }

        // Off for the same reason those three are, and for one more: it CHANGES what an ordinary
        // edit writes. A mode that silently edits a second field has to be asked for.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void LinkColliderToShape_DefaultsToOff()
        {
            var settings = new GameEditorSettings();

            Assert.IsFalse(settings.Interface.LinkColliderToShape);
        }

        // The bot's own two defaults, and they point opposite ways on purpose. BotControl is off
        // because a preview player that steers itself is a surprise for an author who reached for the
        // toggle to try a jump by hand. The debug MASTER is off for the same reason, but every part
        // of it is on: opening the master is meant to show the whole picture at once, and the parts
        // exist to take pieces of it away again - defaulting them off would make the master do
        // nothing at all.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Bot_IsOffAndItsDebugMasterIsOff_ButEveryPartOfItIsOn()
        {
            var settings = new GameEditorSettings();

            Assert.IsFalse(settings.Player.BotControl);
            Assert.IsFalse(settings.Player.BotDebug);

            Assert.IsTrue(settings.Player.BotDebugGrid);
            Assert.IsTrue(settings.Player.BotDebugTarget);
            Assert.IsTrue(settings.Player.BotDebugReach);
        }

        // BOTKIND.NONE MUST BE ZERO, and that is a contract rather than a style rule. Nothing in
        // UserSettings stores a BotKind - the editor's preview player is a bool above, since only the
        // reflex bot fits that screen - so what depends on the zero is every OTHER default: a
        // LevelPlayerInfo that was never filled in, and a launch that says nothing about a bot. Both
        // have to read as "the player steers", and both get there through default(BotKind).
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void BotKind_NoneIsZero()
        {
            Assert.AreEqual(0, (byte)BotKind.None);
            Assert.AreEqual(BotKind.None, default(BotKind));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Bot_SurvivesCopyPullAndReset()
        {
            var source = new GameEditorSettings();
            source.Player.BotControl = true;
            source.Player.BotDebug = true;
            source.Player.BotDebugGrid = false;

            var copy = source.Copy();
            Assert.IsTrue(copy.Player.BotControl);
            Assert.IsTrue(copy.Player.BotDebug);
            Assert.IsFalse(copy.Player.BotDebugGrid);

            var target = new GameEditorSettings();
            var player = target.Player;
            target.Pull(source);

            Assert.AreSame(player, target.Player, "Pull keeps the instance the device handed out");
            Assert.IsTrue(target.Player.BotControl);
            Assert.IsFalse(target.Player.BotDebugGrid);

            target.Reset();
            Assert.IsFalse(target.Player.BotControl);
            Assert.IsTrue(target.Player.BotDebugGrid);
        }

        // The grid's opacity is the ONLY part of its colour anyone authors - the hue is the inverse
        // of the camera background of the current frame, resolved live by the editor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Grid_DefaultsToOneUnitAtAQuarterOpacity()
        {
            var settings = new GameEditorSettings();

            Assert.AreEqual(1f, settings.Grid.Size);
            Assert.AreEqual(0.25f, settings.Grid.Opacity);
        }

        // Rotation is stored in RADIANS everywhere; this decides only what a field shows. Degrees is
        // the default because it is the unit an author thinks a rotation in.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void RotationDisplayUnit_DefaultsToDegrees()
        {
            var settings = new GameEditorSettings();
            Assert.AreEqual(AngleDisplayUnit.Degrees, settings.Interface.RotationDisplayUnit);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryGroup_SurvivesARoundTrip()
        {
            var service = new SerializationService(new SerializationSettings());

            var settings = new UserSettings();
            settings.GameEditor.Savings.HistoryLength = 96;
            settings.GameEditor.Camera.MinSize = 0.5f;
            settings.GameEditor.Camera.Invert = false;
            settings.GameEditor.Camera.MoveSensitivityX = 2.5f;
            settings.GameEditor.Player.ResetGizmos = false;
            settings.GameEditor.Player.BotControl = true;
            settings.GameEditor.Player.BotDebugTarget = false;
            settings.GameEditor.Grid.Size = 0.125f;
            settings.GameEditor.Grid.Opacity = 0.6f;
            settings.GameEditor.Selection.LongPressDelay = 1.25f;
            settings.GameEditor.Selection.ColliderOpacityView = 0.75f;
            settings.GameEditor.Gizmos.Scale = 1.75f;
            settings.GameEditor.Timeline.SnapThresholdPx = 20f;
            settings.GameEditor.Timeline.LocalLoop = false;
            settings.GameEditor.Interface.DirtyFieldDelay = 0.3f;
            settings.GameEditor.Interface.RotationDisplayUnit = AngleDisplayUnit.Radians;
            settings.GameEditor.Serialization.LevelMode = SerializationType.Blob;

            var restored = service.DeserializeData<UserSettings>(service.SerializeData(settings));
            var editor = restored.GameEditor;

            Assert.AreEqual(96, editor.Savings.HistoryLength);
            Assert.AreEqual(0.5f, editor.Camera.MinSize);
            Assert.IsFalse(editor.Camera.Invert);
            Assert.AreEqual(2.5f, editor.Camera.MoveSensitivityX);
            Assert.IsFalse(editor.Player.ResetGizmos);
            Assert.IsTrue(editor.Player.BotControl);
            Assert.IsFalse(editor.Player.BotDebugTarget);
            Assert.AreEqual(0.125f, editor.Grid.Size);
            Assert.AreEqual(0.6f, editor.Grid.Opacity);
            Assert.AreEqual(1.25f, editor.Selection.LongPressDelay);
            Assert.AreEqual(0.75f, editor.Selection.ColliderOpacityView);
            Assert.AreEqual(1.75f, editor.Gizmos.Scale);
            Assert.AreEqual(20f, editor.Timeline.SnapThresholdPx);
            Assert.IsFalse(editor.Timeline.LocalLoop);
            Assert.AreEqual(0.3f, editor.Interface.DirtyFieldDelay);
            Assert.AreEqual(AngleDisplayUnit.Radians, editor.Interface.RotationDisplayUnit);
            Assert.AreEqual(SerializationType.Blob, editor.Serialization.LevelMode);
        }

        // The two nested "iface" keys - UserSettings.Interface and GameEditor.Interface - reuse one
        // name across models that can never co-occur, which is what Names' own header allows. This is
        // the assert that keeps that reuse honest: if the serializer ever confused them, one group
        // would read the other's values back.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void TheTwoInterfaceGroups_DoNotCollide()
        {
            var service = new SerializationService(new SerializationSettings());

            var settings = new UserSettings();
            settings.Interface.StatsActive = true;
            settings.GameEditor.Interface.RenderInframes = true;
            settings.GameEditor.Interface.LogValueClamps = false;

            var restored = service.DeserializeData<UserSettings>(service.SerializeData(settings));

            Assert.IsTrue(restored.Interface.StatsActive);
            Assert.IsTrue(restored.GameEditor.Interface.RenderInframes);
            Assert.IsFalse(restored.GameEditor.Interface.LogValueClamps);
        }
    }
}