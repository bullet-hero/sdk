using System.Collections.Generic;

namespace BH.SDK.Agent
{
    /// <summary> The key of a hint attached to a result; the server sends each once per repeat window.
    /// </summary>
    public static class AgentHintKey
    {
        /// <summary> A drawable object has no size keyframe, so it has no extent. </summary>
        public const string ZeroSizeGroup = "zero-size-group";

        /// <summary> A child's span reaches outside its parent's; it simply does not play there. </summary>
        public const string SpanOutsideParent = "span-outside-parent";

        /// <summary> Two objects share a layer and a start frame and carry random values. </summary>
        public const string SameLayerSameStartRandom = "same-layer-same-start-random";

        /// <summary> Keyframe frames are global for the AI and local to the span in the file. </summary>
        public const string LocalKeyframes = "local-keyframes";

        /// <summary> A layer is the effective one, the sum up the parent chain. </summary>
        public const string EffectiveLayer = "effective-layer";

        /// <summary> A write was refused because the read it was based on is out of date. </summary>
        public const string StaleRead = "stale-read";

        /// <summary> The editor is in Prefab Mode. </summary>
        public const string PrefabMode = "prefab-mode";

        /// <summary> The level has no context file yet; the AI may create one. </summary>
        public const string ContextMissing = "context-missing";

        /// <summary> A context file is written straight to disk: no history step, no undo. </summary>
        public const string ContextNotUndoable = "context-not-undoable";

        /// <summary> Every key, for a test that each one is unique. </summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            ZeroSizeGroup, SpanOutsideParent, SameLayerSameStartRandom, LocalKeyframes, EffectiveLayer,
            StaleRead, PrefabMode, ContextMissing, ContextNotUndoable,
        };
    }
}
