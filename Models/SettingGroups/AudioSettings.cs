using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// The player's own volume mix, stored per device in UserSettings - unrelated to a level's
    /// LevelTrackEffects, which is authored content. Category sliders multiply with the master one.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioSettings : IModel<AudioSettings>, IMoveable<AudioSettings>
    {
        /// <summary> Master volume, applied on top of every category below. </summary>
        [JsonProperty(Names.Volume)]
        [RuleInRange(0f, 1f)]
        public float Volume { get; set; }

        /// <summary> Volume of level audio - the music and its effects. </summary>
        [JsonProperty(Names.Game)]
        [RuleInRange(0f, 1f)]
        public float Game { get; set; }

        /// <summary> Volume of interface sounds, so menu clicks can be muted without losing the
        /// music. </summary>
        [JsonProperty(Names.UI)]
        [RuleInRange(0f, 1f)]
        public float UI { get; set; }

        public AudioSettings()
        {
            Volume = 1f;
            Game = 1f;
            UI = 1f;
        }
        public AudioSettings(float volume, float game, float ui)
        {
            Volume = volume;
            Game = game;
            UI = ui;
        }
    }
}