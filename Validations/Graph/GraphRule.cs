namespace BH.SDK.Validations.Graph
{
    /// <summary>
    /// Invariants that span more than one object, and so cannot be expressed as a property or even
    /// an object rule - those only ever see a single value or a single instance.
    /// </summary>
    public enum GraphRule : byte
    {
        None = 0,

        /// <summary> Two objects in one scope claim the same ObjectId. </summary>
        DuplicateObjectId = 1,

        /// <summary> ParentObjectId names an object that does not exist in the same scope. </summary>
        MissingParent = 2,

        /// <summary> A parent chain loops back on itself. </summary>
        ParentCycle = 3,

        /// <summary> A parent chain is longer than LevelRules.MaxObjectDepth. </summary>
        ParentTooDeep = 4,

        /// <summary> A prefab template transitively places itself. </summary>
        PrefabCycle = 5,

        /// <summary> Prefab nesting exceeds PrefabRules.MaxInheritanceLevel. </summary>
        PrefabTooDeep = 6,

        /// <summary> A placement's id remap names a template object or an outer object that is gone. </summary>
        PrefabRemapBroken = 7,

        /// <summary> A per-instance override targets a template object that no longer exists. </summary>
        ModificationTargetMissing = 8,

        /// <summary> An id counter is at or below an id already in use, so the next object created
        /// would collide with an existing one. </summary>
        IdCounterBehind = 9,

        /// <summary> A Guid reference resolves in neither the level's resources nor - as far as the
        /// SDK can tell - anywhere else. </summary>
        UnresolvedReference = 10,

        /// <summary> Two beat segments cover the same frame, so what the grid is there has two
        /// answers. </summary>
        BeatSegmentsOverlap = 11,

        /// <summary> An id counter is far enough into the id space that the scope will eventually
        /// stop being able to create objects. A warning, and the only one here that is about the
        /// FUTURE rather than about something already wrong. </summary>
        IdCounterNearExhaustion = 12,
    }
}
