using BH.SDK.Models.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Models.Interfaces
{
    // Carries no [RuleXxx] attribute on purpose. FrameSpan clamps into its own invariants in its
    // constructor, so "Start >= 0, Duration >= 1" cannot be violated by a value that exists, and a
    // property rule would be dead code. The one frame invariant a rule still has to police is
    // relational - a child staying inside its parent's span - and that needs the whole object graph,
    // so it lives in Validations/Graph/LevelGraphAnalyzer instead.
    //
    // Deliberately NOT bounded by the level's own FrameDuration either: a root object is allowed to
    // extend past the end of the level.

    /// <summary> Implemented by whatever owns a lifetime on a timeline: objects and audio tracks. </summary>
    public interface IFrameBounds
    {
        /// <summary> The frames it covers, half-open: <c>[Start, Start + Duration)</c>. </summary>
        [JsonProperty(Names.SpanShort)]
        public FrameSpan Span { get; set; }
    }
}
