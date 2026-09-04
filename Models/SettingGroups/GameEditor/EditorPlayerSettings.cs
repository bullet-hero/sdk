using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The preview player's toggle is also what decides who owns the viewport's touches, and it starts
    // OFF on every platform: someone opening the editor is there to edit, and on touch the avatar
    // takes the whole viewport the moment it exists. It used to be seeded per platform into a fresh
    // settings file instead by the host's platform rules (desktop on, mobile off) - one
    // behaviour everywhere is worth more here than a free preview on a mouse, so that seeding is gone
    // and the field is the author's own from the first launch.
    //
    // The toggle now also answers a second question - who STEERS that player - and BotControl is
    // where that lives for the same reason: which of the two the author wants is how they work, not
    // what they happen to be looking at, so it is remembered rather than being session state (the
    // same split the viewport grid's size and its visibility already have).
    //
    // IT IS A BOOL RATHER THAN A CHOICE OF BOT, and that is a statement about this screen rather
    // than a simplification. A bot here is a way of watching the level you are editing play itself,
    // so the only one that fits is the one that needs no preparation and survives a scrub - the
    // reflex bot. A baked bot has nothing to replay while the level is being changed under it.
    // Offering the choice here would be offering an option with one correct answer, so the editor
    // picks the bot (EditorAvatarService) and the author picks whether there is one. A RUN is where
    // the choice belongs, and LevelPlayerInfo.Bot is where it lives.

    /// <summary>
    /// How the editor's preview player behaves when it is switched on.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorPlayerSettings : IModel<EditorPlayerSettings>, IMoveable<EditorPlayerSettings>
    {
        /// <summary> Whether the editor's preview player starts switched on. </summary>
        [JsonProperty(Names.ActiveDefault)]
        public bool ActiveDefault { get; set; }

        /// <summary> Whether switching the preview player on drops the gizmo mode back to None. </summary>
        [JsonProperty(Names.ResetGizmos)]
        public bool ResetGizmos { get; set; }

        /// <summary> Whether the preview player is driven by the bot instead of by the author. Off by
        /// default - a preview player that steers itself is a surprise for someone who reached for
        /// the toggle to test a jump by hand. </summary>
        [JsonProperty(Names.Bot)]
        public bool BotControl { get; set; }

        // The master switch is OFF and every part of it is ON, which is not an inconsistency: opening
        // the master is meant to show the whole picture at once, and the sub-switches are there to
        // take pieces of it away again. Defaulting them off would make the master do nothing.

        /// <summary> Whether any of the bot's debug overlays are drawn at all. </summary>
        [JsonProperty(Names.BotDebug)]
        public bool BotDebug { get; set; }

        /// <summary> Draw the bot's clearance grid. </summary>
        [JsonProperty(Names.BotDebugGrid)]
        public bool BotDebugGrid { get; set; }

        /// <summary> Draw the point the bot is currently heading for. </summary>
        [JsonProperty(Names.BotDebugTarget)]
        public bool BotDebugTarget { get; set; }

        /// <summary> Draw how far the bot believes it can reach this frame. </summary>
        [JsonProperty(Names.BotDebugReach)]
        public bool BotDebugReach { get; set; }

        public EditorPlayerSettings()
        {
            ResetOwn();
        }

        public EditorPlayerSettings(bool activeDefault, bool resetGizmos, bool botControl,
            bool botDebug, bool botDebugGrid, bool botDebugTarget, bool botDebugReach)
        {
            ActiveDefault = activeDefault;
            ResetGizmos = resetGizmos;
            BotControl = botControl;
            BotDebug = botDebug;
            BotDebugGrid = botDebugGrid;
            BotDebugTarget = botDebugTarget;
            BotDebugReach = botDebugReach;
        }

        private void ResetOwn()
        {
            ActiveDefault = false;
            ResetGizmos = true;
            BotControl = false;
            BotDebug = false;
            BotDebugGrid = true;
            BotDebugTarget = true;
            BotDebugReach = true;
        }
    }
}