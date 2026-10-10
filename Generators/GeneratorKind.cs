namespace BH.SDK.Generators
{
    /// <summary> What a generator may touch, and so where a host offers it. Content and Modifier share
    /// one entry point (IScopeGenerator); this enum lets a UI group and filter them anyway. </summary>
    public enum GeneratorKind : byte
    {
        /// <summary> Builds a brand-new Level + LevelMeta from nothing; runs before any level is
        /// open, so it gets no GeneratorContext. See ILevelGenerator. </summary>
        Level = 0,

        /// <summary> Adds new objects/resources to whichever scope is currently being edited. </summary>
        Content = 1,

        /// <summary> Edits objects that already exist, usually the host's current selection. </summary>
        Modifier = 2,
    }
}
