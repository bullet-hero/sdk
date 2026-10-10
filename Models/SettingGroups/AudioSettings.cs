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

        // The editor's own fader for level audio: a level sounds the same in both places and is listened to
        // differently. Full by default, the value it replaces. Docs/SETTING_GROUPS.md

        /// <summary> Volume of the level audio the in-game editor plays, separate from
        /// <see cref="Game"/> so an author can work quietly without turning the game down. </summary>
        [JsonProperty(Names.EditorGame)]
        [RuleInRange(0f, 1f)]
        public float EditorGame { get; set; }

        // HALF where the others are full: interface sounds are feedback, the music is what they came for.

        /// <summary> Volume of interface sounds, so menu clicks can be muted without losing the
        /// music. </summary>
        [JsonProperty(Names.UI)]
        [RuleInRange(0f, 1f)]
        public float UI { get; set; }

        // The editor is its own category: it sounds OUTCOMES only, never the ordinary press.
        // Docs/SETTING_GROUPS.md

        /// <summary> Volume of the in-game level editor's own interface sounds, separate from
        /// <see cref="UI"/> so the shell stays audible while the editor is silenced. </summary>
        [JsonProperty(Names.EditorUI)]
        [RuleInRange(0f, 1f)]
        public float EditorUI { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AudioSettings()
        {
            Volume = 1f;
            Game = 1f;
            EditorGame = 1f;
            UI = 0.5f;
            EditorUI = 0.5f;
        }

        /// <summary> Built from its volume, game, ui, editor ui and editor game. </summary>
        public AudioSettings(float volume, float game, float editorGame, float ui, float editorUI)
        {
            Volume = volume;
            Game = game;
            EditorGame = editorGame;
            UI = ui;
            EditorUI = editorUI;
        }
    }
}