using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Which wire format the editor WRITES with, split by what is being written rather than kept as
    // one switch: a level is the thing an author hands to somebody else, a library resource is
    // reused across levels, and a clipboard payload leaves the process entirely - three different
    // trade-offs between size and being readable by hand. None of the three describes how anything
    // is READ, which is always resolved from the file itself (PathUtils.FindDataFile), so changing
    // one of these can never make existing content unreadable.

    /// <summary>
    /// Which wire format the editor writes each kind of file with.
    /// </summary>
    [RuleContainer]
    public class EditorSerializationSettings : IModel<EditorSerializationSettings>,
        IMoveable<EditorSerializationSettings>
    {
        /// <summary> Format new levels are created with - level.* and metadata.* alike. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Level)]
        public SerializationType LevelMode { get; set; }

        /// <summary> Format every resource exported to the device library is written with. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Resources)]
        public SerializationType ResourcesMode { get; set; }

        /// <summary> Format a copied selection is serialized with for the clipboard. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Copy)]
        public SerializationType CopyMode { get; set; }

        public EditorSerializationSettings()
        {
            ResetOwn();
        }
        public EditorSerializationSettings(SerializationType levelMode,
            SerializationType resourcesMode, SerializationType copyMode)
        {
            LevelMode = levelMode;
            ResourcesMode = resourcesMode;
            CopyMode = copyMode;
        }
        public void Reset() => ResetOwn();
        private void ResetOwn()
        {
            LevelMode = SerializationType.Json;
            ResourcesMode = SerializationType.Json;
            CopyMode = SerializationType.Json;
        }

        public object Clone() => Copy();
        public EditorSerializationSettings Copy() => new(LevelMode, ResourcesMode, CopyMode);

        public void Pull(EditorSerializationSettings source)
        {
            LevelMode = source.LevelMode;
            ResourcesMode = source.ResourcesMode;
            CopyMode = source.CopyMode;
        }

        public void Update(EditorSerializationSettings src)
        {
            LevelMode = src.LevelMode;
            ResourcesMode = src.ResourcesMode;
            CopyMode = src.CopyMode;
        }

        public override int GetHashCode() => HashCode.Combine(LevelMode, ResourcesMode, CopyMode);
        public override bool Equals(object obj) => obj is EditorSerializationSettings value && Equals(value);

        public bool Equals(EditorSerializationSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LevelMode == other.LevelMode
                   && ResourcesMode == other.ResourcesMode
                   && CopyMode == other.CopyMode;
        }
    }
}
