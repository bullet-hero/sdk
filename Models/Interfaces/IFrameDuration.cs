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
    // [0, N), so the last playable frame is N - 1 and N itself is the end boundary, not a frame.
    public interface IFrameDuration
    {
        [RuleMinValue(FrameRules.MinFrameDuration)]
        [JsonProperty(Names.FrameDurationShort)]
        public int FrameDuration { get; set; }
    }
}
