using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Only five of the timeline's several dozen settings are here, and the line between them and the
    // rest is one question: does a wrong value make the screen WRONG, or merely uncomfortable? Lane
    // heights, pixels-per-frame, overscans and warmup counts all fail the first way and stay in the
    // project's asset. These five fail the second way - the ruler spelling a frame in a language the
    // author does not think in is uncomfortable, never incorrect, since it addresses the same frame
    // either way.

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

        // Whether the playhead wraps at the end is a working habit, not a property of the level: an
        // author polishing one passage wants the loop, an author watching the whole thing through
        // does not. The two timelines keep separate answers because they are separate questions - a
        // local loop repeats one object's own span, a global one repeats the level.

        /// <summary> Whether the level timeline's playhead wraps at the end instead of stopping. </summary>
        [JsonProperty(Names.LoopGlobal)]
        public bool GlobalLoop { get; set; }

        /// <summary> Whether the local (per-object) timeline's playhead wraps at the end of the
        /// object's own span. </summary>
        [JsonProperty(Names.LoopLocal)]
        public bool LocalLoop { get; set; }

        // One answer for every ruler AND for the playhead readout, which is the whole point of it
        // being one setting: the two used to speak different languages on the same screen (the ruler
        // counted frames while the readout printed MM:SS.FF), and an author reading a number off one
        // to type into the other had to convert it in their head.

        /// <summary> How a ruler label and the playhead readout spell a frame. Default
        /// <see cref="TimelineTimeFormat.Frames"/>, which is what the ruler always printed. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.TimeFormat)]
        public TimelineTimeFormat TimeFormat { get; set; }

        public EditorTimelineSettings()
        {
            ResetOwn();
        }

        public EditorTimelineSettings(float snapThresholdPx, float edgeHandlePx, bool globalLoop,
            bool localLoop, TimelineTimeFormat timeFormat)
        {
            SnapThresholdPx = snapThresholdPx;
            EdgeHandlePx = edgeHandlePx;
            GlobalLoop = globalLoop;
            LocalLoop = localLoop;
            TimeFormat = timeFormat;
        }

        private void ResetOwn()
        {
            SnapThresholdPx = 10f;
            EdgeHandlePx = 8f;
            GlobalLoop = true;
            LocalLoop = true;
            TimeFormat = TimelineTimeFormat.Frames;
        }
    }
}