using BH.SDK.Models;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The three serialize modes are what the editor WRITES with, and all three default to Json for a
    // reason worth pinning: Json is 0, so a settings file written before they existed deserializes
    // into the same value a fresh one is born with, and no migration was needed. The Copy/Pull/Equals
    // boilerplate here is hand-written eleven fields over, which is the mistake this file exists for.

    /// <summary> GameEditorSettings' serialize-mode preferences: defaults, boilerplate, round trip. </summary>
    public class GameEditorSettingsTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SerializeModes_DefaultToJson()
        {
            var settings = new GameEditorSettings();

            Assert.AreEqual(SerializationType.Json, settings.LevelSerializeMode);
            Assert.AreEqual(SerializationType.Json, settings.ResourcesSerializeMode);
            Assert.AreEqual(SerializationType.Json, settings.CopySerializeMode);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_RestoresSerializeModes()
        {
            var settings = new GameEditorSettings
            {
                LevelSerializeMode = SerializationType.Bson,
                ResourcesSerializeMode = SerializationType.JsonPretty,
                CopySerializeMode = SerializationType.Bson,
            };

            settings.Reset();

            Assert.AreEqual(SerializationType.Json, settings.LevelSerializeMode);
            Assert.AreEqual(SerializationType.Json, settings.ResourcesSerializeMode);
            Assert.AreEqual(SerializationType.Json, settings.CopySerializeMode);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CopyAndPull_CarrySerializeModes()
        {
            var source = new GameEditorSettings
            {
                LevelSerializeMode = SerializationType.JsonPretty,
                ResourcesSerializeMode = SerializationType.Bson,
                CopySerializeMode = SerializationType.JsonPretty,
            };

            var copy = source.Copy();
            Assert.IsTrue(source.Equals(copy));

            var pulled = new GameEditorSettings();
            pulled.Pull(source);
            Assert.IsTrue(source.Equals(pulled));
            Assert.AreEqual(source.GetHashCode(), pulled.GetHashCode());
        }

        // Each mode is its own field, so a Copy/Pull that folded two of them together would still pass
        // the test above - this is what tells them apart.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Equals_SeesEachSerializeModeIndependently()
        {
            var a = new GameEditorSettings();

            var b = a.Copy();
            b.LevelSerializeMode = SerializationType.Bson;
            Assert.IsFalse(a.Equals(b));

            var c = a.Copy();
            c.ResourcesSerializeMode = SerializationType.Bson;
            Assert.IsFalse(a.Equals(c));

            var d = a.Copy();
            d.CopySerializeMode = SerializationType.Bson;
            Assert.IsFalse(a.Equals(d));
        }

        // The editor's viewport grid: only the CELL SIZE is remembered, never whether the lines are
        // currently drawn - that is the session's business (GridModeService), the same split the
        // active gizmo already has.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GridSize_DefaultsToOneUnit()
        {
            var settings = new GameEditorSettings();
            Assert.AreEqual(1f, settings.GridSize);

            settings.GridSize = 0.25f;
            settings.Reset();
            Assert.AreEqual(1f, settings.GridSize);
        }

        // The one preference here that defaults to OFF. The overlay it gates covers the object the
        // author has just selected, so it is in the way far more often than it answers a question -
        // and a default of true would also make it appear for every existing settings file, since
        // false is what a missing JSON key deserializes to either way.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void PreviewColliderOnSelect_DefaultsToOff()
        {
            var settings = new GameEditorSettings();
            Assert.IsFalse(settings.PreviewColliderOnSelect);

            settings.PreviewColliderOnSelect = true;
            settings.Reset();
            Assert.IsFalse(settings.PreviewColliderOnSelect);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PreviewColliderOnSelect_SurvivesCopyPullAndEquality()
        {
            var source = new GameEditorSettings { PreviewColliderOnSelect = true };

            var copy = source.Copy();
            Assert.IsTrue(copy.PreviewColliderOnSelect);
            Assert.IsTrue(source.Equals(copy));

            var pulled = new GameEditorSettings();
            pulled.Pull(source);
            Assert.IsTrue(pulled.PreviewColliderOnSelect);
            Assert.AreEqual(source.GetHashCode(), pulled.GetHashCode());

            var other = source.Copy();
            other.PreviewColliderOnSelect = false;
            Assert.IsFalse(source.Equals(other));
        }

        // The grid's opacity is the ONLY part of its colour anyone authors - the hue is the inverse
        // of the camera background of the current frame, resolved live by the editor.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GridOpacity_DefaultsToAQuarter()
        {
            var settings = new GameEditorSettings();
            Assert.AreEqual(0.25f, settings.GridOpacity);

            settings.GridOpacity = 1f;
            settings.Reset();
            Assert.AreEqual(0.25f, settings.GridOpacity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GridSize_SurvivesCopyPullAndEquality()
        {
            var source = new GameEditorSettings { GridSize = 0.5f, GridOpacity = 0.8f };

            var copy = source.Copy();
            Assert.AreEqual(0.5f, copy.GridSize);
            Assert.AreEqual(0.8f, copy.GridOpacity);
            Assert.IsTrue(source.Equals(copy));

            var pulled = new GameEditorSettings();
            pulled.Pull(source);
            Assert.AreEqual(0.5f, pulled.GridSize);
            Assert.AreEqual(0.8f, pulled.GridOpacity);
            Assert.AreEqual(source.GetHashCode(), pulled.GetHashCode());

            // Two fields, seen independently - a Copy/Pull folding them together would pass the
            // asserts above and still lose one of them.
            var other = source.Copy();
            other.GridSize = 2f;
            Assert.IsFalse(source.Equals(other));

            var dimmer = source.Copy();
            dimmer.GridOpacity = 0.1f;
            Assert.IsFalse(source.Equals(dimmer));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Grid_SurvivesARoundTrip()
        {
            var service = new SerializationService(new SerializationSettings());

            var settings = new UserSettings();
            settings.GameEditor.GridSize = 0.125f;
            settings.GameEditor.GridOpacity = 0.6f;

            var restored = service.DeserializeData<UserSettings>(service.SerializeData(settings));

            Assert.AreEqual(0.125f, restored.GameEditor.GridSize);
            Assert.AreEqual(0.6f, restored.GameEditor.GridOpacity);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void SerializeModes_SurviveARoundTrip()
        {
            var service = new SerializationService(new SerializationSettings());

            var settings = new UserSettings();
            settings.GameEditor.LevelSerializeMode = SerializationType.JsonPretty;
            settings.GameEditor.ResourcesSerializeMode = SerializationType.Bson;
            settings.GameEditor.CopySerializeMode = SerializationType.JsonPretty;

            var json = service.SerializeData(settings);
            var restored = service.DeserializeData<UserSettings>(json);

            Assert.AreEqual(SerializationType.JsonPretty, restored.GameEditor.LevelSerializeMode);
            Assert.AreEqual(SerializationType.Bson, restored.GameEditor.ResourcesSerializeMode);
            Assert.AreEqual(SerializationType.JsonPretty, restored.GameEditor.CopySerializeMode);
        }
    }
}
