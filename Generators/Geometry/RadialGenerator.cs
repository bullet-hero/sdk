using BH.SDK.Generators.Spawn;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Geometry
{
    /// <summary>
    /// Objects spread around a circle or an arc of one, optionally rotated to face the centre.
    /// </summary>
    public class RadialGenerator : BaseSpawnGenerator<RadialGenerator.Parameters>
    {
        /// <summary> <c>"gen_geometry_radial"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_geometry_radial";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Count), nameof(Parameters.Radius))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.StartAngle), nameof(Parameters.Arc),
                nameof(Parameters.CenterX), nameof(Parameters.CenterY), nameof(Parameters.FaceCenter))
            .Range(nameof(Parameters.Count), 1, 1024)
            .Range(nameof(Parameters.Radius), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.StartAngle), -3600f, 3600f)
            .Range(nameof(Parameters.Arc), -3600f, 3600f)
            .Unit(nameof(Parameters.StartAngle), "deg")
            .Unit(nameof(Parameters.Arc), "deg")
            .Range(nameof(Parameters.CenterX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.CenterY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);

            // A full 360 arc must not place two objects on top of each other at 0 and 360, so the
            // last step is dropped there; a partial arc keeps both ends, which is what "spread these
            // across a 90 degree fan" means.
            var full = IsFullCircle(parameters.Arc);
            var divisor = full ? count : (count > 1 ? count - 1 : 1);

            for (var i = 0; i < count; i++)
            {
                var angle = parameters.StartAngle + parameters.Arc * (i / (float)divisor);
                Direction(angle, out var dirX, out var dirY);

                var obj = Spawn(context, parameters, $"radial_{i}", context.Span);
                AddPosition(obj,
                    parameters.CenterX + dirX * parameters.Radius,
                    parameters.CenterY + dirY * parameters.Radius,
                    obj.Span.StartFrame);

                // "Facing the centre" is the outward direction turned around - the texture's own
                // forward is +X, so the angle IS the outward one and 180 flips it inward.
                if (parameters.FaceCenter) AddRotation(obj, angle + 180f, obj.Span.StartFrame);
            }
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var count = Count(parameters.Count);
            var keysPerObject = parameters.FaceCenter ? 4 : 3;
            return new GeneratorCost(count, count * keysPerObject);
        }

        private static bool IsFullCircle(float arc)
        {
            var absolute = arc < 0f ? -arc : arc;
            return absolute >= 359.999f;
        }

        private static int Count(int value) => value < 1 ? 1 : value;

        /// <summary> Which arc is covered, at what radius, and whether objects face the centre. Public mutable
        /// fields, like every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How many objects are placed. </summary>
            public int Count = 12;

            /// <summary> Distance from the centre. </summary>
            public float Radius = 5f;

            /// <summary> Angle the first object sits at. </summary>
            public float StartAngle;

            /// <summary> How much of the circle is covered - a full turn is a ring, less is an arc. </summary>
            public float Arc = 360f;

            /// <summary> Where the circle is centred. </summary>
            public float CenterX;
            /// <summary> Its vertical half. </summary>
            public float CenterY;

            /// <summary> Rotate each object to point at the centre. </summary>
            public bool FaceCenter;
        }
    }
}
