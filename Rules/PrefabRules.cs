namespace BH.SDK.Rules
{
    /// <summary> What a prefab template may be: its own timeline length, and how deep placements may nest. </summary>
    public static class PrefabRules
    {
        // A Prefab template has no Framerate of its own (unlike LevelSettings) to scale a "10
        // seconds" default by, so this is a flat frame count instead - matches LevelSettings'
        // own default (60fps * 10s) at a nominal 60fps.

        /// <summary> The frame duration used when nothing says otherwise, read by Prefab. </summary>
        public const int DefaultFrameDuration = 600;

        // A template's timeline is bounded exactly like a level's - same frames, same timeline UI.

        /// <summary> Upper bound of Prefab.FrameDuration. </summary>
        public const int MaxFrameDuration = FrameRules.MaxFrameDuration;

        // A template is just another object scope, so it inherits the level's own object budget
        // rather than getting a separate (and inevitably drifting) number.

        /// <summary> Upper bound of Prefab.ObjectIdCounter, Prefab.Objects. </summary>
        public const int MaxObjects = LevelRules.MaxObjects;

        // How deep placements may nest before the format calls it absurd. This is a property of the
        // FORMAT, not of one device: a file nesting deeper cannot be materialized correctly by any
        // consumer, so it belongs here rather than in a per-project settings asset - the Unity
        // side's ResourceSettings.Prefabs_MaxInheritanceLevel now defaults from this constant
        // instead of carrying its own number. Cycles are a separate, graph-level check; this bounds
        // nesting that is legitimate but unreasonable.

        /// <summary> Highest inheritance level allowed, read by GraphRule, LevelGraphAnalyzer. </summary>
        public const int MaxInheritanceLevel = 8;

        // Per-instance overrides on one placement. High enough that overriding every field of a
        // sizeable template stays possible, low enough that a hostile file can't ship a dictionary
        // the editor has to resolve path-by-path through reflection.

        /// <summary> Upper bound of PrefabObject.Modifications. </summary>
        public const int MaxModifications = 4096;

        // template-inner id -> this placement's materialized outer id. Bounded by the template's own
        // object budget: a placement can't remap more objects than a template can hold.

        /// <summary> Upper bound of PrefabObject.ObjectIds. </summary>
        public const int MaxObjectIdRemaps = MaxObjects;
    }
}
