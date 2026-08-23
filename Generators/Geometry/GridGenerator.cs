using BH.SDK.Generators.Spawn;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Geometry
{
    /// <summary>
    /// A rectangular grid of objects - the simplest useful generator, and the reference for what a
    /// concrete one looks like: placement math plus hints, nothing else.
    /// </summary>
    public class GridGenerator : BaseSpawnGenerator<GridGenerator.Parameters>
    {
        public override string NameKey => "gen_geometry_grid";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Columns), nameof(Parameters.Rows),
                nameof(Parameters.SpacingX), nameof(Parameters.SpacingY))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.OriginX), nameof(Parameters.OriginY),
                nameof(Parameters.Centered))
            .Range(nameof(Parameters.Columns), 1, 256)
            .Range(nameof(Parameters.Rows), 1, 256)
            .Range(nameof(Parameters.SpacingX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.SpacingY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var columns = Count(parameters.Columns);
            var rows = Count(parameters.Rows);

            // Centering shifts by half the SPAN (gaps between cells), not half the cell count -
            // off-by-one here puts a 2x2 grid's centre on a cell instead of between cells.
            var offsetX = parameters.Centered ? -(columns - 1) * parameters.SpacingX * 0.5f : 0f;
            var offsetY = parameters.Centered ? -(rows - 1) * parameters.SpacingY * 0.5f : 0f;

            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
            {
                var obj = Spawn(context, parameters, $"grid_{column}_{row}",
                    context.Span);
                AddPosition(obj,
                    parameters.OriginX + offsetX + column * parameters.SpacingX,
                    parameters.OriginY + offsetY + row * parameters.SpacingY,
                    obj.Span.StartFrame);
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var objects = Count(parameters.Columns) * Count(parameters.Rows);
            return new GeneratorCost(objects, objects * KeysPerObject);
        }

        // Position + size + colour, written by Spawn/AddPosition for every object.
        private const int KeysPerObject = 3;

        private static int Count(int value) => value < 1 ? 1 : value;

        public class Parameters : SpawnParameters
        {
            public int Columns = 4;
            public int Rows = 4;
            public float SpacingX = 2f;
            public float SpacingY = 2f;
            public float OriginX;
            public float OriginY;
            public bool Centered = true;
        }
    }
}
