namespace BH.SDK.Versions
{
    // What a client must support to read a level whole, answered without reading it. The domains are
    // LISTED, not taken from ModelGenerations.Current, and LevelMeta is excluded on purpose:
    // Docs/VERSIONING.md ("LevelGenerations.Required()") carries the reasoning.

    /// <summary> The generations a level file's own domains are at. </summary>
    public static class LevelGenerations
    {
        /// <summary> Every domain a level.json can contain, directly or nested. </summary>
        public static readonly string[] Domains =
        {
            ModelDomains.Level,
            ModelDomains.LevelSettings,
            ModelDomains.GameLevel,
            ModelDomains.AudioLevel,
            ModelDomains.LevelResources,
            ModelDomains.LevelHints,
            ModelDomains.GameEvents,
            ModelDomains.CameraEvents,
            ModelDomains.PostProcessingEvents,
            ModelDomains.PlayerEvents,
            ModelDomains.Prefab,
            ModelDomains.EffectData,
            ModelDomains.ThemeData,
            ModelDomains.CompositeShape
        };

        // Computed once: the answer comes from [ModelGeneration] attributes, which cannot change at
        // runtime, and it is asked per level card (LevelMetaInfo.NewerThanThisBuild).

        private static readonly int Highest = Compute();

        /// <summary> The highest generation any of them is at - what this build stamps on a level it saves,
        /// and what it compares a level's own claim against. </summary>
        public static int Required() => Highest;

        private static int Compute()
        {
            var highest = ModelGenerations.Invalid;

            foreach (var domain in Domains)
            {
                var generation = VersionedTypeRegistry.GetLatestAttribute(domain).Generation;
                if (generation > highest) highest = generation;
            }

            return highest;
        }
    }
}