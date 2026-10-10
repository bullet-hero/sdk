namespace BH.SDK.Rules
{
    /// <summary> What a prefab template may be: its own timeline length, and how deep placements may nest. </summary>
    public static class PrefabRules
    {
        // A Prefab template has no Framerate of its own (unlike LevelSettings) to scale a "10
        // seconds" default by, so this is a flat frame count instead - matches LevelSettings'
        // own default (60fps * 10s) at a nominal 60fps.

        /// <summary> The frame duration used when nothing says otherwise - the length Prefab's own
        /// constructor gives Root.Span. </summary>
        public const int DefaultFrameDuration = 600;

        // A template's timeline is bounded like a level's. Nothing validates against this (the length is
        // a FrameSpan duration); it is the bound the editor's length field clamps to.

        /// <summary> Upper bound of a template's own timeline, Prefab.Root.Span's duration. </summary>
        public const int MaxFrameDuration = FrameRules.MaxFrameDuration;

        // A template is just another object scope, so it inherits the level's own object budget
        // rather than getting a separate (and inevitably drifting) number.

        /// <summary> Upper bound of Prefab.Objects. </summary>
        public const int MaxObjects = LevelRules.MaxObjects;

        // Inherits the id BUDGET too, for the reason over LevelRules.MaxObjectIds. Prefab Mode spends it
        // fastest: every create/undo/branch inside it consumes the template's own counter.

        /// <summary> Upper bound of Prefab.ObjectIdCounter. </summary>
        public const int MaxObjectIds = LevelRules.MaxObjectIds;

        // A property of the FORMAT: no consumer can materialize deeper, so ResourceSettings'
        // Prefabs_MaxInheritanceLevel defaults from this. Cycles are a separate, graph-level check.

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
