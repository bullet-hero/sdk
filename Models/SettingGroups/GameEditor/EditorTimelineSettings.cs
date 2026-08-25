using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.GameEditor
{
    // Only four numbers out of the timeline's several dozen are here, and the line between them and
    // the rest is one question: does a wrong value make the screen WRONG, or merely uncomfortable?
    // Lane heights, pixels-per-frame, overscans and warmup counts all fail the first way and stay in
    // the project's asset. These four fail the second way.

    /// <summary>
    /// How the editor's timelines respond to a pointer, and whether playback wraps.
    /// </summary>
    [RuleContainer]
    public class EditorTimelineSettings : IModel<EditorTimelineSettings>, IMoveable<EditorTimelineSettings>
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

        public EditorTimelineSettings()
        {
            ResetOwn();
        }
        public EditorTimelineSettings(float snapThresholdPx, float edgeHandlePx, bool globalLoop, bool localLoop)
        {
            SnapThresholdPx = snapThresholdPx;
            EdgeHandlePx = edgeHandlePx;
            GlobalLoop = globalLoop;
            LocalLoop = localLoop;
        }
        public void Reset() => ResetOwn();
        private void ResetOwn()
        {
            SnapThresholdPx = 10f;
            EdgeHandlePx = 8f;
            GlobalLoop = true;
            LocalLoop = true;
        }

        public object Clone() => Copy();
        public EditorTimelineSettings Copy() => new(SnapThresholdPx, EdgeHandlePx, GlobalLoop, LocalLoop);

        public void Pull(EditorTimelineSettings source)
        {
            SnapThresholdPx = source.SnapThresholdPx;
            EdgeHandlePx = source.EdgeHandlePx;
            GlobalLoop = source.GlobalLoop;
            LocalLoop = source.LocalLoop;
        }

        public void Update(EditorTimelineSettings src)
        {
            SnapThresholdPx = src.SnapThresholdPx;
            EdgeHandlePx = src.EdgeHandlePx;
            GlobalLoop = src.GlobalLoop;
            LocalLoop = src.LocalLoop;
        }

        public override int GetHashCode() =>
            HashCode.Combine(SnapThresholdPx, EdgeHandlePx, GlobalLoop, LocalLoop);
        public override bool Equals(object obj) => obj is EditorTimelineSettings value && Equals(value);

        public bool Equals(EditorTimelineSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return SnapThresholdPx.Equals(other.SnapThresholdPx)
                   && EdgeHandlePx.Equals(other.EdgeHandlePx)
                   && GlobalLoop == other.GlobalLoop
                   && LocalLoop == other.LocalLoop;
        }
    }
}
