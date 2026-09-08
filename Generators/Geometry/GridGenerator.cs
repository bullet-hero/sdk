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
        /// <summary> <c>"gen_geometry_grid"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_geometry_grid";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
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

        /// <summary> Writes this run's objects into the scope. </summary>
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

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var objects = Count(parameters.Columns) * Count(parameters.Rows);
            return new GeneratorCost(objects, objects * KeysPerObject);
        }

        // Position + size + colour, written by Spawn/AddPosition for every object.
        private const int KeysPerObject = 3;

        private static int Count(int value) => value < 1 ? 1 : value;

        /// <summary> The grid's extent and spacing. Public mutable fields, like every parameters class here - a
        /// form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How many columns the grid has. </summary>
            public int Columns = 4;

            /// <summary> How many rows. </summary>
            public int Rows = 4;

            /// <summary> Gap between neighbouring cells. </summary>
            public float SpacingX = 2f;
            /// <summary> The vertical gap. </summary>
            public float SpacingY = 2f;

            /// <summary> Where the grid is placed. </summary>
            public float OriginX;
            /// <summary> Its vertical half. </summary>
            public float OriginY;

            /// <summary> Treat the origin as the grid's centre rather than its first cell. </summary>
            public bool Centered = true;
        }
    }
}
