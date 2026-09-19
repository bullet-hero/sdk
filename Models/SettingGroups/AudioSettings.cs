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

        // THE LEVEL SOUNDS THE SAME IN BOTH PLACES AND IS LISTENED TO DIFFERENTLY, which is the
        // whole of why this is a second fader rather than a reuse of Game. A player hears a level
        // once through; an author hears the same eight bars a hundred times while dragging a clip
        // across them, and turns it down without wanting the game turned down. It covers the level
        // audio the editor PLAYS - the music and its tracks - and nothing else: auditioning a
        // resource in Level Settings is a preview, and the editor's own clicks are EditorUI.
        //
        // FULL, like Game, because that is the value it replaces while the editor is open: adding
        // it changes nothing until an author moves it.

        /// <summary> Volume of the level audio the in-game editor plays, separate from
        /// <see cref="Game"/> so an author can work quietly without turning the game down. </summary>
        [JsonProperty(Names.EditorGame)]
        [RuleInRange(0f, 1f)]
        public float EditorGame { get; set; }

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