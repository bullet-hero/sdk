using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Only settings whose wrong value is uncomfortable, not WRONG; lane heights, pixels-per-frame and
    // overscans stay in the project asset.

    /// <summary>
    /// How the editor's timelines respond to a pointer, whether playback wraps, and what
    /// language they count in.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorTimelineSettings : IModel<EditorTimelineSettings>,
        IMoveable<EditorTimelineSettings>
    {
        // Both are ON-SCREEN distances, so they mean the same thing at every zoom - and both are the
        // finger-versus-mouse trade the long-press pair makes: a grab zone sized for a cursor is
        // smaller than a fingertip, and an author working on a phone needs to widen both.

        /// <summary> How close, on screen, a dragged edge has to land for snapping to pull it onto a
        /// candidate frame. </summary>
        [RuleInRange(0f, 100f)]
        [JsonProperty(Names.SnapThreshold)]
        public float SnapThresholdPx { get; set; }

        /// <summary> Width of a clip's grab zone for Trim's edge-resize; the rest of the clip slips. </summary>
        [RuleInRange(1f, 100f)]
        [JsonProperty(Names.EdgeHandle)]
        public float EdgeHandlePx { get; set; }

        // A working habit, not a property of the level; the two timelines keep separate answers.

        /// <summary> Whether the level timeline's playhead wraps at the end instead of stopping. </summary>
        [JsonProperty(Names.LoopGlobal)]
        public bool GlobalLoop { get; set; }

        /// <summary> Whether the local (per-object) timeline's playhead wraps at the end of the
        /// object's own span. </summary>
        [JsonProperty(Names.LoopLocal)]
        public bool LocalLoop { get; set; }

        // One answer for every ruler AND the playhead readout.

        /// <summary> How a ruler label and the playhead readout spell a frame. Default
        /// <see cref="TimelineTimeFormat.Frames"/>, which is what the ruler always printed. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.TimeFormat)]
        public TimelineTimeFormat TimeFormat { get; set; }

        // The tool is per-tab; on, a tab returns to Selection, the one tool that cannot destroy anything
        // when pressed by accident.

        /// <summary> Whether switching timeline tabs puts the tab arrived at back on the Selection
        /// tool instead of the one it was last left on. </summary>
        [JsonProperty(Names.ToolReset)]
        public bool ToolResetOnTabChange { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorTimelineSettings()
        {
            ResetOwn();
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public EditorTimelineSettings(float snapThresholdPx, float edgeHandlePx, bool globalLoop,
            bool localLoop, TimelineTimeFormat timeFormat, bool toolResetOnTabChange)
        {
            SnapThresholdPx = snapThresholdPx;
            EdgeHandlePx = edgeHandlePx;
            GlobalLoop = globalLoop;
            LocalLoop = localLoop;
            TimeFormat = timeFormat;
            ToolResetOnTabChange = toolResetOnTabChange;
        }

        private void ResetOwn()
        {
            SnapThresholdPx = 10f;
            EdgeHandlePx = 8f;
            GlobalLoop = true;
            LocalLoop = true;
            TimeFormat = TimelineTimeFormat.Frames;
            ToolResetOnTabChange = true;
        }
    }
}