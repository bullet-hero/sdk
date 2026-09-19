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

        // A template's timeline is bounded exactly like a level's - same frames, same timeline UI.
        // Nothing VALIDATES against this any more: the length is a FrameSpan's duration now
        // (Prefab.Root.Span), and no illegal FrameSpan is representable. It stays as the bound the
        // editor's own length field clamps to.

        /// <summary> Upper bound of a template's own timeline, Prefab.Root.Span's duration. </summary>
        public const int MaxFrameDuration = FrameRules.MaxFrameDuration;

        // A template is just another object scope, so it inherits the level's own object budget
        // rather than getting a separate (and inevitably drifting) number.

        /// <summary> Upper bound of Prefab.Objects. </summary>
        public const int MaxObjects = LevelRules.MaxObjects;

        // And it inherits the id BUDGET the same way, which is a different number for the reason
        // spelled out over LevelRules.MaxObjectIds: a template's counter measures ids ever minted,
        // not objects held. A template is the scope a prefab-editing session spends ids in fastest,
        // since every create/undo/branch inside Prefab Mode consumes from here rather than from the
        // level's own counter.

        /// <summary> Upper bound of Prefab.ObjectIdCounter. </summary>
        public const int MaxObjectIds = LevelRules.MaxObjectIds;

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
