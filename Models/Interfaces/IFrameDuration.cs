using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Interfaces
{
    // Implemented by whichever model owns its own timeline length (LevelSettings, Prefab), so timeline code
    // sizes itself generically. A COUNT: a timeline of N holds frames 1..N, N + 1 is the end boundary
    // (FrameRules.LastFrameOf / EndBoundaryOf).

    /// <summary> Owns a timeline of its own, and knows how long it is. </summary>
    public interface IFrameDuration
    {
        /// <summary> How many frames the timeline holds - a count, and since the timeline counts
        /// frames from one, also the number of its last playable frame. </summary>
        [RuleMinValue(FrameRules.MinFrameDuration)]
        [JsonProperty(Names.FrameDurationShort)]
        public int FrameDuration { get; set; }
    }
}
