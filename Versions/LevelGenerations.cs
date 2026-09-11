namespace BH.SDK.Versions
{
    // WHAT A CLIENT MUST SUPPORT TO READ A LEVEL WHOLE, answered without reading the level. It is
    // written into metadata.json at save (LevelMeta.MinGeneration) so a browser can warn before it
    // opens anything - see that member's own header for why that is where the key lives.
    //
    // THE DOMAINS ARE LISTED RATHER THAN TAKEN FROM ModelGenerations.Current, and the difference
    // only shows once generations diverge. Current is the maximum across EVERY domain, including
    // UserSettings, ClipboardData and the statistics files, none of which a level contains; a level
    // whose own domains never moved would inherit a warning from settings.json. The list below is
    // exactly what level.json can hold, LevelMeta itself excluded - metadata is the half that has to
    // stay readable, so a file that hid its own claim inside the thing it describes could never be
    // asked about.

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

        // COMPUTED ONCE, AND THAT IS A STATEMENT RATHER THAN AN OPTIMISATION: the answer comes from
        // [ModelGeneration] attributes on types in a loaded assembly, so it cannot change while the
        // process runs. Caching it is also what makes it usable on the READING side - LevelMetaInfo
        // .NewerThanThisBuild asks this per level card, and a fourteen-domain sweep per card would
        // have pushed that comparison onto ModelGenerations.Current instead, which is the number
        // this whole type exists to say is the wrong one.

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