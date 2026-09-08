using BH.SDK.Models;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators
{
    /// <summary>
    /// A blank level: nothing but the timeline shape the author asked for. The reference
    /// implementation of the contract as much as it is a useful preset - if something here needs a
    /// special case, the contract is wrong.
    /// </summary>
    public class EmptyLevelGenerator : BaseLevelGenerator<EmptyLevelGenerator.Parameters>
    {
        /// <summary> <c>"gen_level_empty"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_level_empty";

        // First in the list: the level everything else is a shortcut to.

        /// <summary> Where this sits in a host's list; lower comes first. </summary>
        public override int ListOrder => -20;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints => HintsValue;

        private static readonly GeneratorHints HintsValue = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.LevelName), nameof(Parameters.Framerate),
                nameof(Parameters.FrameDuration))
            .Section(GeneratorSections.Additional, nameof(Parameters.LevelDescription))
            .Range(nameof(Parameters.Framerate), FrameRules.MinFramerate, FrameRules.MaxFramerate)
            .Range(nameof(Parameters.FrameDuration), FrameRules.MinFrameDuration, FrameRules.MaxFrameDuration)
            .Unit(nameof(Parameters.FrameDuration), "frames")
            .Unit(nameof(Parameters.Framerate), "fps")
            .Build();

        /// <summary> Builds the level and its metadata together, so the two cannot disagree. </summary>
        protected override GeneratedLevel CreateTyped(Parameters parameters)
        {
            var level = new Level();
            level.Settings.Fps = parameters.Framerate;
            level.Settings.FrameDuration = parameters.FrameDuration;

            var meta = new LevelMeta
            {
                LevelName = parameters.LevelName.Copy(),
                LevelDescription = parameters.LevelDescription.Copy(),
            };

            return new GeneratedLevel(level, meta);
        }

        /// <summary>
        /// Public mutable fields, deliberately - a form binds to them by reflection and a preset
        /// serializes from them. This is the shape every parameters class takes, and the one place
        /// in the SDK where it is intended rather than an oversight.
        /// </summary>
        public class Parameters
        {
            /// <summary> Name the new level is created under. </summary>
            public IString LevelName = new StringValue();
            /// <summary> Its description. </summary>
            public IString LevelDescription = new StringValue();
            /// <summary> Frames per second the timeline is measured in. </summary>
            public int Framerate = DefaultFramerate;
            /// <summary> How many frames long it is. </summary>
            public int FrameDuration = DefaultFramerate * DefaultSeconds;

            private const int DefaultFramerate = 60;
            private const int DefaultSeconds = 10;
        }
    }
}
