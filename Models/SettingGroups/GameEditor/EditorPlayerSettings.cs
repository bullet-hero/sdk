using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Starts OFF everywhere (on touch the avatar takes the whole viewport). BotControl is a bool: the only
    // fitting bot is the reflex bot; a run's choice is LevelPlayerInfo.Bot. Docs/SETTING_GROUPS.md

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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorPlayerSettings()
        {
            ResetOwn();
        }

        /// <summary> Every member at once, in declaration order. </summary>
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