using BH.SDK.Generators.Spawn;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Geometry
{
    // Two different shapes share one generator because they share one input set: "N points on a
    // circle" and "N edges between those points" differ only in what gets placed between the
    // vertices. Splitting them would duplicate Sides/Radius/Rotation three times over.

    /// <summary>
    /// A regular polygon - either one object per vertex, or objects spread along its edges to draw
    /// the outline itself.
    /// </summary>
    public class PolygonGenerator : BaseSpawnGenerator<PolygonGenerator.Parameters>
    {
        public override string NameKey => "gen_geometry_polygon";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Sides), nameof(Parameters.Radius),
                nameof(Parameters.AsOutline))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.Rotation),
                nameof(Parameters.CenterX), nameof(Parameters.CenterY), nameof(Parameters.PointsPerEdge))
            .Range(nameof(Parameters.Sides), MinSides, 64)
            .Range(nameof(Parameters.Radius), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.Rotation), -3600f, 3600f)
            .Range(nameof(Parameters.PointsPerEdge), 1, 64)
            .Unit(nameof(Parameters.Rotation), "deg")
            .VisibleWhen(nameof(Parameters.PointsPerEdge), p => ((Parameters)p).AsOutline)
            .Range(nameof(Parameters.CenterX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.CenterY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var sides = Sides(parameters.Sides);

            if (!parameters.AsOutline)
            {
                for (var i = 0; i < sides; i++)
                {
                    VertexAt(parameters, sides, i, out var x, out var y);
                    var obj = Spawn(context, parameters, $"polygon_{i}", context.Span);
                    AddPosition(obj, x, y, obj.Span.StartFrame);
                }
                return;
            }

            var perEdge = PointsPerEdge(parameters.PointsPerEdge);
            for (var edge = 0; edge < sides; edge++)
            {
                VertexAt(parameters, sides, edge, out var fromX, out var fromY);
                VertexAt(parameters, sides, edge + 1, out var toX, out var toY);

                // The end vertex is skipped on every edge - it is the next edge's start, and placing
                // both would double up an object on every corner.
                for (var step = 0; step < perEdge; step++)
                {
                    var t = step / (float)perEdge;
                    var obj = Spawn(context, parameters, $"polygon_{edge}_{step}",
                        context.Span);
                    AddPosition(obj, Lerp(fromX, toX, t), Lerp(fromY, toY, t), obj.Span.StartFrame);
                }
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var sides = Sides(parameters.Sides);
            var objects = parameters.AsOutline ? sides * PointsPerEdge(parameters.PointsPerEdge) : sides;
            return new GeneratorCost(objects, objects * KeysPerObject);
        }

        private void VertexAt(Parameters parameters, int sides, int index, out float x, out float y)
        {
            var angle = parameters.Rotation + 360f * (index % sides) / sides;
            Direction(angle, out var dirX, out var dirY);
            x = parameters.CenterX + dirX * parameters.Radius;
            y = parameters.CenterY + dirY * parameters.Radius;
        }

        private const int KeysPerObject = 3;
        private const int MinSides = 3;

        private static int Sides(int value) => value < MinSides ? MinSides : value;
        private static int PointsPerEdge(int value) => value < 1 ? 1 : value;

        public class Parameters : SpawnParameters
        {
            public int Sides = 6;
            public float Radius = 5f;
            public float Rotation;
            public float CenterX;
            public float CenterY;
            public bool AsOutline;
            public int PointsPerEdge = 4;
        }
    }
}
