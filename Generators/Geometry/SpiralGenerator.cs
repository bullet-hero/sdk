using BH.SDK.Generators.Spawn;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Geometry
{
    /// <summary>
    /// Objects along an Archimedean spiral - radius and angle both advance evenly across the count,
    /// so Turns controls how tightly it winds and RadiusStart/RadiusEnd how far it travels.
    /// </summary>
    public class SpiralGenerator : BaseSpawnGenerator<SpiralGenerator.Parameters>
    {
        /// <summary> <c>"gen_geometry_spiral"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_geometry_spiral";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Count), nameof(Parameters.RadiusStart),
                nameof(Parameters.RadiusEnd), nameof(Parameters.Turns))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.StartAngle),
                nameof(Parameters.CenterX), nameof(Parameters.CenterY), nameof(Parameters.FaceOutward))
            .Range(nameof(Parameters.Count), 1, 1024)
            .Range(nameof(Parameters.RadiusStart), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.RadiusEnd), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.Turns), -32f, 32f)
            .Unit(nameof(Parameters.StartAngle), "deg")
            .Range(nameof(Parameters.StartAngle), -3600f, 3600f)
            .Range(nameof(Parameters.CenterX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.CenterY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);

            for (var i = 0; i < count; i++)
            {
                var t = Ratio(i, count);
                var angle = parameters.StartAngle + parameters.Turns * 360f * t;
                var radius = Lerp(parameters.RadiusStart, parameters.RadiusEnd, t);
                Direction(angle, out var dirX, out var dirY);

                var obj = Spawn(context, parameters, $"spiral_{i}", context.Span);
                AddPosition(obj,
                    parameters.CenterX + dirX * radius,
                    parameters.CenterY + dirY * radius,
                    obj.Span.StartFrame);

                if (parameters.FaceOutward) AddRotation(obj, angle, obj.Span.StartFrame);
            }
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);
            var keysPerObject = parameters.FaceOutward ? 4 : 3;
            return new GeneratorCost(count, count * keysPerObject);
        }

        private static int Count(int value) => value < 1 ? 1 : value;

        /// <summary> How tightly the spiral winds and how far it travels. Public mutable fields, like every
        /// parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How many objects are placed along the spiral. </summary>
            public int Count = 32;

            /// <summary> Distance from the centre the spiral begins at. </summary>
            public float RadiusStart = 1f;

            /// <summary> Distance it ends at. </summary>
            public float RadiusEnd = 8f;

            /// <summary> How many full turns it makes on the way - how tightly it winds. </summary>
            public float Turns = 2f;

            /// <summary> Angle the spiral begins at. </summary>
            public float StartAngle;

            /// <summary> Where it is centred. </summary>
            public float CenterX;
            /// <summary> Its vertical half. </summary>
            public float CenterY;

            /// <summary> Rotate each object to point away from the centre. </summary>
            public bool FaceOutward;
        }
    }
}
