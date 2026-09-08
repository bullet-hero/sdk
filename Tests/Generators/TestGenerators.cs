using BH.SDK.Generators;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;

namespace BH.SDK.Tests.Generators
{
    // Fixtures, not tests. Real generators (gen_radial and friends) arrive in later stages; these
    // exist so the contract itself - context, journal, estimate, requirements - can be exercised
    // now, and so each one is deliberately the SMALLEST generator that reaches one part of it.
    //
    // They deliberately live in the test assembly rather than in Generators/: GeneratorRegistry
    // scans only the SDK assembly, so keeping them here is what stops test fixtures from showing up
    // in a real host's generator list.

    /// <summary> Creates N plain objects. Exercises Create + Estimate. </summary>
    internal class SpawnTestGenerator : BaseContentGenerator<SpawnTestGenerator.Parameters>
    {
        public override string NameKey => "gen_test_spawn";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Order(nameof(Parameters.Count))
            .Range(nameof(Parameters.Count), 0, 1024)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            for (var i = 0; i < parameters.Count; i++)
            {
                var obj = context.Create<RectObject>();
                obj.ParentObjectId = context.Parent;
                obj.Name = $"spawn_{i}";
                obj.Span = context.Span;
                obj.Layer = context.Layer;
                obj.Positions.Add(new PosKey(new Vector2Value(i, 0f), context.Span.StartFrame));
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => new(parameters.Count, parameters.Count);

        /// <summary> How many objects to spawn. </summary>
        internal class Parameters
        {
            public int Count = 4;
        }
    }

    /// <summary> Renames and re-layers everything selected. Exercises Edit + Selection. </summary>
    internal class RenameTestModifier : BaseModifier<RenameTestModifier.Parameters>
    {
        public override string NameKey => "mod_test_rename";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Order(nameof(Parameters.Prefix), nameof(Parameters.Layer))
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var index = 0;
            foreach (var id in context.Selection)
            {
                var obj = context.Edit(id);
                obj.Name = $"{parameters.Prefix}_{index++}";
                obj.Layer = parameters.Layer;
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        /// <summary> The name prefix and the layer everything selected is moved to. </summary>
        internal class Parameters
        {
            public string Prefix = "renamed";
            public int Layer = 5;
        }
    }

    /// <summary> Writes camera zoom keys, optionally wiping the range first. Exercises LevelScope,
    /// AddLevelKey and - the point of it - RemoveLevelKeys being undoable. </summary>
    internal class CameraFlashTestGenerator : BaseContentGenerator<CameraFlashTestGenerator.Parameters>
    {
        public override string NameKey => "gen_test_camera_flash";

        public override GeneratorRequirements Requirements => GeneratorRequirements.LevelScope;

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Order(nameof(Parameters.Frames), nameof(Parameters.Zoom), nameof(Parameters.ClearRange))
            .Hidden(nameof(Parameters.Frames))
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var zooms = context.Game.CameraEvents.Zooms;

            if (parameters.ClearRange)
            {
                var start = context.Span.StartFrame;
                var end = context.Span.EndFrame;
                context.RemoveLevelKeys(zooms, key => key.Frame >= start && key.Frame <= end);
            }

            foreach (var frame in parameters.Frames)
                context.AddLevelKey(zooms, new ZoomKey(new FloatValue(parameters.Zoom), frame));
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => new(0, parameters.Frames.Length);

        /// <summary> The zoom keys to write, and whether the range is wiped first. </summary>
        internal class Parameters
        {
            public int[] Frames = System.Array.Empty<int>();
            public float Zoom = 2f;
            public bool ClearRange;
        }
    }

    /// <summary> Scatters objects using the context seed. Exercises determinism. </summary>
    internal class ScatterTestGenerator : BaseContentGenerator<ScatterTestGenerator.Parameters>
    {
        public override string NameKey => "gen_test_scatter";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Order(nameof(Parameters.Count))
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var random = context.CreateRandom();
            for (var i = 0; i < parameters.Count; i++)
            {
                var obj = context.Create<RectObject>();
                obj.Span = context.Span;
                obj.Positions.Add(new PosKey(
                    new Vector2Value(random.NextFloat(-100f, 100f), random.NextFloat(-100f, 100f)),
                    context.Span.StartFrame));
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => new(parameters.Count, parameters.Count);

        /// <summary> How many objects to scatter, and over what area. </summary>
        internal class Parameters
        {
            public int Count = 8;
        }
    }
}
