using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Serialization.Serializers;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Which wire format the editor WRITES with, split by what is being written rather than kept as
    // one switch: a level is the thing an author hands to somebody else, while a library resource is
    // reused across levels - two different trade-offs between speed and being readable by hand.
    // Neither describes how anything is READ, which is always resolved from the file itself
    // (PathUtils.FindDataFile), so changing one of these can never make existing content unreadable.
    //
    // THERE WAS A THIRD, FOR THE CLIPBOARD, AND IT COULD NOT MEAN ANYTHING. A clipboard payload is
    // TEXT - it leaves the process through the operating system's own buffer - so the binary format
    // has no form to take there, and once the indented JSON mode was retired the only remaining
    // choice was between JSON and JSON. It was removed rather than left as a dropdown that changes
    // nothing; `copy` is retired as a key and never reissued.

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

        public EditorSerializationSettings()
        {
            ResetOwn();
        }
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
