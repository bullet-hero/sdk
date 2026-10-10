using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // The EDITOR's interface, unrelated to UserSettings.Interface: both use the "iface" key, never together.

    /// <summary>
    /// How the editor's own panels read and behave: how eagerly a field commits, which unit an angle
    /// is shown in, and how much the console is told.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorInterfaceSettings : IModel<EditorInterfaceSettings>,
        IMoveable<EditorInterfaceSettings>
    {
        // The debounce every inspector field commits through; zero commits on every keystroke. Read when a
        // view is CONSTRUCTED, so a change applies on the next open.

        /// <summary> Quiet time after a keystroke before an inspector field commits its edit. </summary>
        [RuleInRange(0f, 5f)]
        [JsonProperty(Names.DirtyFieldDelay)]
        public float DirtyFieldDelay { get; set; }

        /// <summary> Which unit the editor's rotation fields are read and typed in. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.RotationUnit)]
        public AngleDisplayUnit RotationDisplayUnit { get; set; }

        // The clamp itself is not optional; only the telling is. A value pushed back into its rule is
        // a thing the author asked for and did not get, so the default is to say so - but an author
        // dragging a slider against its own ceiling does not need to be told thirty times.

        /// <summary> Whether a value clamped back into its rule is reported to the editor console. </summary>
        [JsonProperty(Names.LogClamps)]
        public bool LogValueClamps { get; set; }

        // Off: inframes are engine-owned rows an effect spawns, a diagnostic view.

        /// <summary> Whether the editor's frame hierarchy lists the objects effects spawn at runtime. </summary>
        [JsonProperty(Names.RenderInframes)]
        public bool RenderInframes { get; set; }

        // Off. Modifies one action: picking a shape also writes its id into ColliderId. None never reaches
        // the collider (a Null ShapeId beside a real ColliderId is an invisible hitbox).

        /// <summary> Whether picking a shape also writes it as that object's collider. </summary>
        [JsonProperty(Names.LinkColliderShape)]
        public bool LinkColliderToShape { get; set; }

        /// <summary> Whether selecting something opens the editor's right panel by itself. </summary>
        [JsonProperty(Names.AutoOpen)]
        public bool SelectionAutoOpenActive { get; set; }

        // Off: both surfaces fold by the same rule but keep separate answers (a fold closes a row in a
        // tree and removes one from a timeline).

        /// <summary> Whether folding a row in the frame hierarchy also folds that object's subtree
        /// out of the object timelines, and the other way round. </summary>
        [JsonProperty(Names.SyncTimelineExpansion)]
        public bool SyncTimelineExpansion { get; set; }

        // Off, the one reporting switch whose default is silence: findings are advisory, and one UserLogger
        // entry is a row, a notification and a sound at once.

        /// <summary> Whether an explicit save reports how many level rules the saved level breaks. </summary>
        [JsonProperty(Names.LogRules)]
        public bool LogRuleFindings { get; set; }

        // ONE mask for "Partial". A bit PRESENT means UNFOLDED (the inverse of a filter mask); the default
        // is everything but prefabs.

        /// <summary> Which object kinds stay unfolded in the frame hierarchy and the object timelines
        /// while a surface is in its partial expansion mode. </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.ExpansionMask)]
        public ObjectTypeMask ExpansionMask { get; set; }

        // The STARTING rung of each surface; the rung it is on now is the consumer's session state.

        /// <summary> The rung the frame hierarchy opens at. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.HierarchyExpansion)]
        public ExpansionMode HierarchyExpansion { get; set; }

        /// <summary> The rung the object timelines open at. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.TimelineExpansion)]
        public ExpansionMode TimelineExpansion { get; set; }

        // Where the EDITOR's level browser starts, on the list rather than the menu's grid: an author
        // finds a level by its name and description and scrolls through far more of them than a
        // player does. Joined the unshipped generation V2_SimplifyEntrance with its menu twin.

        /// <summary> How the editor's level browser lays its results out when it opens. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.LevelsLayout)]
        public LevelBrowserLayout LevelsLayout { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorInterfaceSettings()
        {
            ResetOwn();
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public EditorInterfaceSettings(float dirtyFieldDelay, AngleDisplayUnit rotationDisplayUnit,
            bool logValueClamps, bool renderInframes, bool linkColliderToShape,
            bool selectionAutoOpenActive, bool syncTimelineExpansion, bool logRuleFindings,
            ObjectTypeMask expansionMask, ExpansionMode hierarchyExpansion,
            ExpansionMode timelineExpansion)
        {
            DirtyFieldDelay = dirtyFieldDelay;
            RotationDisplayUnit = rotationDisplayUnit;
            LogValueClamps = logValueClamps;
            RenderInframes = renderInframes;
            LinkColliderToShape = linkColliderToShape;
            SelectionAutoOpenActive = selectionAutoOpenActive;
            SyncTimelineExpansion = syncTimelineExpansion;
            LogRuleFindings = logRuleFindings;
            ExpansionMask = expansionMask;
            HierarchyExpansion = hierarchyExpansion;
            TimelineExpansion = timelineExpansion;
            LevelsLayout = LevelBrowserLayout.List;
        }

        private void ResetOwn()
        {
            DirtyFieldDelay = 0.05f;
            RotationDisplayUnit = AngleDisplayUnit.Degrees;
            LogValueClamps = true;
            RenderInframes = false;
            LinkColliderToShape = false;
            SelectionAutoOpenActive = false;
            SyncTimelineExpansion = false;
            LogRuleFindings = false;
            ExpansionMask = ObjectTypeMask.All & ~ObjectTypeMask.PrefabObject;
            HierarchyExpansion = ExpansionMode.Partial;
            TimelineExpansion = ExpansionMode.Partial;
            LevelsLayout = LevelBrowserLayout.List;
        }
    }
}