using BH.SDK.Generators.Spawn;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Bullets
{
    /// <summary>
    /// Bullets fired outward from a point, each rotated one step further than the last, so a
    /// staggered burst reads as a rotating spray rather than a ring.
    /// </summary>
    public class BulletSpiralGenerator : BaseSpawnGenerator<BulletSpiralGenerator.Parameters>
    {
        /// <summary> <c>"gen_bullet_spiral"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_bullet_spiral";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Count), nameof(Parameters.AngularStep),
                nameof(Parameters.RadiusStart), nameof(Parameters.RadiusEnd),
                nameof(Parameters.TravelFrames))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.StartAngle),
                nameof(Parameters.CenterX), nameof(Parameters.CenterY),
                nameof(Parameters.StaggerFrames), nameof(Parameters.Ease), nameof(Parameters.FaceOutward))
            .Range(nameof(Parameters.Count), 1, 512)
            .Range(nameof(Parameters.AngularStep), -360f, 360f)
            .Range(nameof(Parameters.RadiusStart), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.RadiusEnd), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.TravelFrames), 1, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.StaggerFrames), 0, FrameRules.MaxFrameDuration)
            .Unit(nameof(Parameters.AngularStep), "deg")
            .Unit(nameof(Parameters.StartAngle), "deg")
            .Unit(nameof(Parameters.TravelFrames), "frames")
            .Unit(nameof(Parameters.StaggerFrames), "frames")
            .Range(nameof(Parameters.StartAngle), -3600f, 3600f)
            .Range(nameof(Parameters.CenterX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.CenterY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);
            var travel = Travel(parameters.TravelFrames);
            var stagger = Stagger(parameters.StaggerFrames);

            for (var i = 0; i < count; i++)
            {
                var angle = parameters.StartAngle + parameters.AngularStep * i;
                Direction(angle, out var dirX, out var dirY);
                var spawnFrame = context.Span.StartFrame + i * stagger;
                if (!CanSpawn(context, spawnFrame)) break; // stagger ran past the window - no ghost on its last frame

                var obj = Spawn(context, parameters, $"spiral_{i}", new FrameSpan(spawnFrame, travel));
                AddPosition(obj,
                    parameters.CenterX + dirX * parameters.RadiusStart,
                    parameters.CenterY + dirY * parameters.RadiusStart,
                    obj.Span.StartFrame);

                if (CanAnimate(obj.Span))
                    AddPosition(obj,
                        parameters.CenterX + dirX * parameters.RadiusEnd,
                        parameters.CenterY + dirY * parameters.RadiusEnd,
                        obj.Span.LastFrame, parameters.Ease);

                if (parameters.FaceOutward) AddRotation(obj, angle, obj.Span.StartFrame);
            }
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);
            var travel = Travel(parameters.TravelFrames);
            var stagger = Stagger(parameters.StaggerFrames);
            var perObject = 2 + (parameters.FaceOutward ? 1 : 0);

            var keys = 0;

            var objects = 0;
            for (var i = 0; i < count; i++)
            {
                if (!CanSpawn(context, context.Span.StartFrame + i * stagger)) break;
                objects++;

                var span = ClampSpan(context, new FrameSpan(ClampFrame(context, context.Span.StartFrame + i * stagger), travel));
                keys += perObject + (CanAnimate(span) ? 2 : 1);
            }
            return new GeneratorCost(objects, keys);
        }

        private static int Count(int value) => value < 1 ? 1 : value;
        private static int Travel(int value) => value < 1 ? 1 : value;
        private static int Stagger(int value) => value < 0 ? 0 : value;

        /// <summary> Where the spray starts and how far each shot is turned from the last. Public mutable fields,
        /// like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How many bullets are fired. </summary>
            public int Count = 24;

            /// <summary> How far each shot is turned from the one before it - what makes the spray read as rotating. </summary>
            public float AngularStep = 17f;

            /// <summary> Angle the first shot leaves at. </summary>
            public float StartAngle;

            /// <summary> Distance from the centre a bullet starts at. </summary>
            public float RadiusStart = 0.5f;

            /// <summary> Distance it travels out to. </summary>
            public float RadiusEnd = 12f;

            /// <summary> The point everything is fired from. </summary>
            public float CenterX;
            /// <summary> Its vertical half. </summary>
            public float CenterY;

            /// <summary> How long one bullet lives. </summary>
            public int TravelFrames = 90;

            /// <summary> Delay between one shot and the next. </summary>
            public int StaggerFrames = 3;

            /// <summary> Curve a bullet travels along. </summary>
            public EaseType Ease = EaseType.Linear;

            /// <summary> Rotate each bullet to point away from the centre. </summary>
            public bool FaceOutward = true;
        }
    }
}
