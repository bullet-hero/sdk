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

        // HALF, WHERE THE OTHER TWO ARE FULL, and the asymmetry is the point: interface sounds are
        // feedback on a press the player already made, while the music is the thing they came for.
        // At parity every click competes with the track it plays over, so the mix a player is first
        // handed is one where the menu sits under the level rather than beside it.

        /// <summary> Volume of interface sounds, so menu clicks can be muted without losing the
        /// music. </summary>
        [JsonProperty(Names.UI)]
        [RuleInRange(0f, 1f)]
        public float UI { get; set; }

        // THE EDITOR IS ITS OWN CATEGORY BECAUSE ITS SOUNDS ARE EARNED DIFFERENTLY, not because it is
        // quieter - it starts at the same half the shell does. A player meets a menu button a few
        // times a session and the click is feedback; an author meets the editor for hours, so it
        // sounds its OUTCOMES only - a save, a refusal, an undo, a deletion - and never the ordinary
        // press. Those are different enough to want their own fader: an author who works in silence
        // still wants the menu audible, and one slider covering both could only ever be set to
        // whichever of the two was wrong.

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
            UI = 0.5f;
            EditorUI = 0.5f;
        }

        /// <summary> Built from its volume, game, ui and editor ui. </summary>
        public AudioSettings(float volume, float game, float ui, float editorUI)
        {
            Volume = volume;
            Game = game;
            UI = ui;
            EditorUI = editorUI;
        }
    }
}