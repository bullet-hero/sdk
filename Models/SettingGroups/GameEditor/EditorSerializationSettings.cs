using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The format the editor WRITES, split by what is written; reading is always resolved from the file
    // (PathUtils.FindDataFile). No clipboard key: `copy` is retired and never reissued.

    /// <summary>
    /// Which wire format the editor writes each kind of file with.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorSerializationSettings : IModel<EditorSerializationSettings>,
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorSerializationSettings()
        {
            ResetOwn();
        }
        /// <summary> Built from its mode and mode. </summary>
        public EditorSerializationSettings(SerializationType levelMode, SerializationType resourcesMode)
        {
            LevelMode = levelMode;
            ResourcesMode = resourcesMode;
        }
        private void ResetOwn()
        {
            LevelMode = SerializationType.Json;
            ResourcesMode = SerializationType.Json;
        }
    }
}
