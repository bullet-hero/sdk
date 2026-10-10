using BH.SDK.Models.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Models.Interfaces
{
    // No [RuleXxx] on purpose: FrameSpan clamps into its own invariants, so a property rule would be dead
    // code. Containment in the parent is relational (LevelGraphAnalyzer); a root may overrun the level.

    /// <summary> Implemented by whatever owns a lifetime on a timeline: objects and audio tracks. </summary>
    public interface IFrameBounds
    {
        /// <summary> The frames it covers, half-open: <c>[Start, Start + Duration)</c>. </summary>
        [JsonProperty(Names.SpanShort)]
        public FrameSpan Span { get; set; }
    }
}
