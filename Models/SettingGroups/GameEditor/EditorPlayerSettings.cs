using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The preview player's toggle is also what decides who owns the viewport's touches, so its
    // starting state is a real preference rather than a constant: a desktop author wants the player
    // there from the first frame (a mouse loses nothing to it), a phone author does not, since the
    // whole viewport goes to the avatar the moment it exists. The platform only picks the value a
    // FRESH settings file is born with - after that it is the author's own.
    //
    // The toggle now also answers a second question - who STEERS that player - and BotControl is
    // where that lives for the same reason: which of the two the author wants is how they work, not
    // what they happen to be looking at, so it is remembered rather than being session state (the
    // same split the viewport grid's size and its visibility already have).

    /// <summary>
    /// How the editor's preview player behaves when it is switched on.
    /// </summary>
    [RuleContainer]
    public class EditorPlayerSettings : IModel<EditorPlayerSettings>, IMoveable<EditorPlayerSettings>
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

        public void Reset() => ResetOwn();

        private void ResetOwn()
        {
            ActiveDefault = true;
            ResetGizmos = true;
            BotControl = false;
            BotDebug = false;
            BotDebugGrid = true;
            BotDebugTarget = true;
            BotDebugReach = true;
        }

        public object Clone() => Copy();

        public EditorPlayerSettings Copy() => new(ActiveDefault, ResetGizmos, BotControl, BotDebug,
            BotDebugGrid, BotDebugTarget, BotDebugReach);

        public void Pull(EditorPlayerSettings source)
        {
            ActiveDefault = source.ActiveDefault;
            ResetGizmos = source.ResetGizmos;
            BotControl = source.BotControl;
            BotDebug = source.BotDebug;
            BotDebugGrid = source.BotDebugGrid;
            BotDebugTarget = source.BotDebugTarget;
            BotDebugReach = source.BotDebugReach;
        }

        public void Update(EditorPlayerSettings src)
        {
            ActiveDefault = src.ActiveDefault;
            ResetGizmos = src.ResetGizmos;
            BotControl = src.BotControl;
            BotDebug = src.BotDebug;
            BotDebugGrid = src.BotDebugGrid;
            BotDebugTarget = src.BotDebugTarget;
            BotDebugReach = src.BotDebugReach;
        }

        public override int GetHashCode() => HashCode.Combine(ActiveDefault, ResetGizmos, BotControl,
            BotDebug, BotDebugGrid, BotDebugTarget, BotDebugReach);

        public override bool Equals(object obj) => obj is EditorPlayerSettings value && Equals(value);

        public bool Equals(EditorPlayerSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ActiveDefault == other.ActiveDefault && ResetGizmos == other.ResetGizmos
                                                        && BotControl == other.BotControl
                                                        && BotDebug == other.BotDebug
                                                        && BotDebugGrid == other.BotDebugGrid
                                                        && BotDebugTarget == other.BotDebugTarget
                                                        && BotDebugReach == other.BotDebugReach;
        }
    }
}