using BH.SDK.Models;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using BH.SDK.Versions.V1_0;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // UserSettings is at (2,0) because GameEditorSettings' sixteen flat properties became nine nested
    // groups - the first change to this domain that an additive default could not cover. These tests
    // go through the ENVELOPE rather than calling the migrator directly, because the envelope is what
    // a settings.json on a player's disk actually is: the version tag it carries is what picks
    // UserSettingsV1_0 as the deserialization target, and getting that wiring wrong is silent - every
    // old key simply reads back as a default and the file looks merely stale rather than misread.

    /// <summary> The (1,0) to (2,0) UserSettings migration: every old key lands, nothing is invented. </summary>
    public class UserSettingsMigrationTests
    {
        private static SerializationService Service() => new(new SerializationSettings());

        private static UserSettingsV1_0 OldSettings() => new()
        {
            General = new GeneralSettings(),
            Controls = new ControlsSettings(),
            Audio = new AudioSettings(),
            Graphics = new GraphicsSettings(),
            Interface = new InterfaceSettings(),
            Keybindings = new KeybindingsSettings(),
            GameEditor = new GameEditorSettingsV1_0
            {
                Autosave = false,
                AutosaveRate = 15f,
                MaxAutosaveFiles = 7,
                CameraMinSize = 0.25f,
                CameraMaxSize = 40f,
                PlayerActiveDefault = false,
                GizmosResetOnPlayer = false,
                MultiSelectRequiresHold = false,
                PreviewColliderOnSelect = true,
                PickInvisibleAABB = true,
                RenderInframes = true,
                GridSize = 0.5f,
                GridOpacity = 0.8f,
                LevelSerializeMode = SerializationType.Blob,
                ResourcesSerializeMode = SerializationType.Json,

                // Carried by the v1.0 file and DROPPED by the migration - the clipboard is text, so
                // the setting this fed could never mean anything. Set to something conspicuous here
                // precisely so the assert below can say it went nowhere.
                CopySerializeMode = SerializationType.Blob,
            },
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void CurrentVersion_IsTwoZero()
        {
            var latest = VersionedTypeRegistry.GetLatestAttribute(DataDomains.UserSettings);

            Assert.AreEqual(2, latest.Major);
            Assert.AreEqual(0, latest.Minor);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheOneZeroSnapshot_IsStillResolvable()
        {
            var type = VersionedTypeRegistry.Resolve(DataDomains.UserSettings, 1, 0);
            Assert.AreEqual(typeof(UserSettingsV1_0), type);
        }

        // Every one of the sixteen old keys, checked in the group it landed in. A migrator that
        // dropped one would leave a default there, which reads as "the author never set it".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryOldKey_SurvivesTheEnvelope()
        {
            var service = Service();
            var bytes = service.SerializeEnvelope(OldSettings(), SerializationType.Json);

            var restored = service.DeserializeEnvelope<UserSettings>(bytes, SerializationType.Json);
            var editor = restored.GameEditor;

            Assert.IsFalse(editor.Savings.Autosave);
            Assert.AreEqual(15f, editor.Savings.AutosaveRate);
            Assert.AreEqual(7, editor.Savings.MaxAutosaveFiles);

            Assert.AreEqual(0.25f, editor.Camera.MinSize);
            Assert.AreEqual(40f, editor.Camera.MaxSize);

            Assert.IsFalse(editor.Player.ActiveDefault);
            Assert.IsFalse(editor.Player.ResetGizmos);

            Assert.AreEqual(0.5f, editor.Grid.Size);
            Assert.AreEqual(0.8f, editor.Grid.Opacity);

            Assert.IsFalse(editor.Selection.MultiRequiresHold);
            Assert.IsTrue(editor.Selection.PreviewColliderOnSelect);
            Assert.IsTrue(editor.Selection.PickInvisibleAABB);

            Assert.IsTrue(editor.Interface.RenderInframes);

            Assert.AreEqual(SerializationType.Blob, editor.Serialization.LevelMode);
            Assert.AreEqual(SerializationType.Json, editor.Serialization.ResourcesMode);
        }

        // The properties the restructure ADDED have no old key to come from, so they must arrive at
        // their shipped defaults - not at zero, which is what an uninitialised group would give and
        // what would read on screen as "gizmos are invisible and the timeline never snaps".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void NewProperties_ArriveAtTheirDefaults()
        {
            var service = Service();
            var bytes = service.SerializeEnvelope(OldSettings(), SerializationType.Json);

            var editor = service.DeserializeEnvelope<UserSettings>(bytes, SerializationType.Json).GameEditor;
            var fresh = new GameEditorSettings();

            Assert.AreEqual(fresh.Savings.HistoryLength, editor.Savings.HistoryLength);
            Assert.AreEqual(fresh.Camera.Invert, editor.Camera.Invert);
            Assert.AreEqual(fresh.Camera.MoveSensitivityX, editor.Camera.MoveSensitivityX);
            Assert.AreEqual(fresh.Camera.MoveSensitivityY, editor.Camera.MoveSensitivityY);
            Assert.AreEqual(fresh.Camera.WheelMultiplier, editor.Camera.WheelMultiplier);
            Assert.AreEqual(fresh.Camera.ZoomToMouse, editor.Camera.ZoomToMouse);
            Assert.AreEqual(fresh.Selection.LongPressDelay, editor.Selection.LongPressDelay);
            Assert.AreEqual(fresh.Selection.LongPressMoveThreshold, editor.Selection.LongPressMoveThreshold);
            Assert.AreEqual(fresh.Selection.ColliderOpacitySelection, editor.Selection.ColliderOpacitySelection);
            Assert.AreEqual(fresh.Selection.ColliderOpacityView, editor.Selection.ColliderOpacityView);
            Assert.AreEqual(fresh.Gizmos.Scale, editor.Gizmos.Scale);
            Assert.AreEqual(fresh.Timeline.SnapThresholdPx, editor.Timeline.SnapThresholdPx);
            Assert.AreEqual(fresh.Timeline.EdgeHandlePx, editor.Timeline.EdgeHandlePx);
            Assert.AreEqual(fresh.Timeline.GlobalLoop, editor.Timeline.GlobalLoop);
            Assert.AreEqual(fresh.Timeline.LocalLoop, editor.Timeline.LocalLoop);
            Assert.AreEqual(fresh.Interface.DirtyFieldDelay, editor.Interface.DirtyFieldDelay);
            Assert.AreEqual(AngleDisplayUnit.Degrees, editor.Interface.RotationDisplayUnit);
            Assert.IsTrue(editor.Interface.LogValueClamps);
        }

        // The six groups that did NOT change shape are typed with their current classes inside the
        // snapshot, so they have to survive untouched - a migrator reaching for `new()` instead of
        // pulling would quietly reset a player's whole audio mix on upgrade.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void TheUnchangedGroups_AreCarriedOver()
        {
            var old = OldSettings();
            old.Audio.Volume = 0.42f;
            old.General.ResourceParallelLoadCount = 5;
            old.Interface.StatsActive = true;
            old.Graphics.FixedFramerate = 144;

            var service = Service();
            var bytes = service.SerializeEnvelope(old, SerializationType.Json);
            var restored = service.DeserializeEnvelope<UserSettings>(bytes, SerializationType.Json);

            Assert.AreEqual(0.42f, restored.Audio.Volume);
            Assert.AreEqual(5, restored.General.ResourceParallelLoadCount);
            Assert.IsTrue(restored.Interface.StatsActive);
            Assert.AreEqual(144, restored.Graphics.FixedFramerate);
        }

        // A (2,0) file must not go near the migrator at all - it is already current. Worth its own
        // assert because the failure would only show up on the SECOND launch after an upgrade.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ACurrentFile_RoundTripsUnchanged()
        {
            var service = Service();

            var settings = new UserSettings();
            settings.GameEditor.Gizmos.Scale = 2.5f;
            settings.GameEditor.Timeline.GlobalLoop = false;

            var bytes = service.SerializeEnvelope(settings, SerializationType.Json);
            var restored = service.DeserializeEnvelope<UserSettings>(bytes, SerializationType.Json);

            Assert.AreEqual(2.5f, restored.GameEditor.Gizmos.Scale);
            Assert.IsFalse(restored.GameEditor.Timeline.GlobalLoop);
            Assert.IsTrue(settings.Equals(restored));
        }
    }
}
