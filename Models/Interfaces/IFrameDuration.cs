using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Interfaces
{
    // Implemented by whichever model owns its own independent timeline length - LevelSettings for a
    // level's own timeline, Prefab for a template's own local timeline (see PrefabRules.
    // DefaultFrameDuration / Prefab.FrameDuration's own comment). Lets timeline-bound editor code
    // (the in-game editor's Prefab Timeline) size itself generically without caring which kind of
    // scope it's bounding, same reasoning as IObjectScope/IObjectIdCounter's own split.
    //
    // FrameDuration is a COUNT, matching FrameSpan.FrameDuration: a timeline of N holds frames
    // 1..N, so the last playable frame IS N and N + 1 is the end boundary rather than a frame.
    // FrameRules.LastFrameOf/EndBoundaryOf spell both, so nothing has to write the relationship out.

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
