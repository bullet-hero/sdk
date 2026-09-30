using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of GameEditorSettingsV1, frozen because
    // generation 2 added the editor level browser's starting layout.

    /// <summary> The editor's interface group as generation 1 (Release) of the user-settings domain
    /// wrote it - before <c>levels_layout</c>. A frozen snapshot - never edit it to match today's
    /// shape. </summary>
    [GenerateModel]
    public sealed partial class EditorInterfaceSettingsV1 : IModel<EditorInterfaceSettingsV1>
    {
        /// <summary> Quiet time after a keystroke before an inspector field commits its edit. </summary>
        [JsonProperty("dirty_field_dly")]
        public float DirtyFieldDelay { get; set; }

        /// <summary> Which unit the editor's rotation fields are read and typed in. </summary>
        [JsonProperty("rot_display_unit")]
        public AngleDisplayUnit RotationDisplayUnit { get; set; }

        /// <summary> Whether a value clamped back into its rule is reported to the editor console. </summary>
        [JsonProperty("log_value_clamps")]
        public bool LogValueClamps { get; set; }

        /// <summary> Whether the frame hierarchy lists the objects effects spawn at runtime. </summary>
        [JsonProperty("render_inframes")]
        public bool RenderInframes { get; set; }

        /// <summary> Whether picking a shape also writes it as that object's collider. </summary>
        [JsonProperty("link_collider_to_shp")]
        public bool LinkColliderToShape { get; set; }

        /// <summary> Whether selecting something opens the editor's right panel by itself. </summary>
        [JsonProperty("auto_open")]
        public bool SelectionAutoOpenActive { get; set; }

        /// <summary> Whether folding in the hierarchy and in the timelines is tied together. </summary>
        [JsonProperty("sync_timeline_expansion")]
        public bool SyncTimelineExpansion { get; set; }

        /// <summary> Whether an explicit save reports how many level rules the saved level breaks. </summary>
        [JsonProperty("log_rules")]
        public bool LogRuleFindings { get; set; }

        /// <summary> Which object kinds stay unfolded in the partial expansion mode. </summary>
        [JsonProperty("expansion_mask")]
        public ObjectTypeMask ExpansionMask { get; set; }

        /// <summary> The rung the frame hierarchy opens at. </summary>
        [JsonProperty("hierarchy_expansion")]
        public ExpansionMode HierarchyExpansion { get; set; }

        /// <summary> The rung the object timelines open at. </summary>
        [JsonProperty("timeline_expansion")]
        public ExpansionMode TimelineExpansion { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public EditorInterfaceSettingsV1()
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
        }
    }
}
