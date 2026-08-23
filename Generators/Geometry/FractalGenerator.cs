using System;
using BH.SDK.Generators.Spawn;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Geometry
{
    /// <summary> Which self-similar figure <see cref="FractalGenerator"/> draws. </summary>
    public enum FractalType : byte
    {
        /// <summary> Koch snowflake - three Koch curves on the sides of a triangle. </summary>
        Koch = 0,

        /// <summary> Sierpinski triangle - one object per surviving sub-triangle. </summary>
        Sierpinski = 1,

        /// <summary> Binary branching tree. </summary>
        Tree = 2,
    }

    // Depth is capped at 6 rather than left open because these grow by a constant factor per level:
    // Koch alone is 3*4^depth, so depth 8 is 196 608 objects - within shouting distance of
    // LevelRules.MaxObjects (262 144) from a single click. The cap keeps the estimate meaningful
    // instead of making it a warning nobody can act on.

    /// <summary>
    /// Self-similar figures built by recursion: a Koch snowflake, a Sierpinski triangle, or a
    /// branching tree.
    /// </summary>
    public class FractalGenerator : BaseSpawnGenerator<FractalGenerator.Parameters>
    {
        public override string NameKey => "gen_geometry_fractal";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Type), nameof(Parameters.Depth),
                nameof(Parameters.Scale))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.Thickness), nameof(Parameters.Rotation),
                nameof(Parameters.CenterX), nameof(Parameters.CenterY),
                nameof(Parameters.BranchAngle), nameof(Parameters.BranchScale))
            .Range(nameof(Parameters.Depth), 0, MaxDepth)
            .Range(nameof(Parameters.Scale), 0.01f, 1000f)
            .Range(nameof(Parameters.Thickness), 0.01f, 100f)
            .Range(nameof(Parameters.BranchAngle), 0f, 180f)
            .Range(nameof(Parameters.BranchScale), 0.1f, 1f)
            .Unit(nameof(Parameters.Rotation), "deg")
            .Unit(nameof(Parameters.BranchAngle), "deg")
            .VisibleWhen(nameof(Parameters.BranchAngle), p => ((Parameters)p).Type == FractalType.Tree)
            .VisibleWhen(nameof(Parameters.BranchScale), p => ((Parameters)p).Type == FractalType.Tree)
            .VisibleWhen(nameof(Parameters.Thickness), p => ((Parameters)p).Type != FractalType.Sierpinski)
            .Range(nameof(Parameters.Rotation), -3600f, 3600f)
            .Range(nameof(Parameters.CenterX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.CenterY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var depth = Depth(parameters.Depth);
            _index = 0;

            switch (parameters.Type)
            {
                case FractalType.Koch:
                    GenerateKoch(context, parameters, depth);
                    break;
                case FractalType.Sierpinski:
                    GenerateSierpinski(context, parameters, depth);
                    break;
                default:
                    GenerateTree(context, parameters, depth);
                    break;
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var depth = Depth(parameters.Depth);
            var objects = parameters.Type switch
            {
                FractalType.Koch => 3 * Power(4, depth),
                FractalType.Sierpinski => Power(3, depth),
                _ => Power(2, depth + 1) - 1,
            };
            var keysPerObject = parameters.Type == FractalType.Sierpinski ? PointKeys : SegmentKeys;
            return new GeneratorCost(objects, objects * keysPerObject);
        }

        // Object naming has to stay unique across the whole run, and recursion has no natural index
        // to hand out - a per-run counter is simpler than threading a path down every call.
        private int _index;

        #region Koch

        private void GenerateKoch(GeneratorContext context, Parameters parameters, int depth)
        {
            // Equilateral triangle around the centre, vertices at 90/210/330 degrees so it points up.
            CornerOf(parameters, 90f, out var ax, out var ay);
            CornerOf(parameters, 210f, out var bx, out var by);
            CornerOf(parameters, 330f, out var cx, out var cy);

            KochSide(context, parameters, depth, ax, ay, bx, by);
            KochSide(context, parameters, depth, bx, by, cx, cy);
            KochSide(context, parameters, depth, cx, cy, ax, ay);
        }

        private void KochSide(GeneratorContext context, Parameters parameters, int depth,
            float ax, float ay, float bx, float by)
        {
            if (depth <= 0)
            {
                SpawnSegment(context, parameters, ax, ay, bx, by);
                return;
            }

            var dx = (bx - ax) / 3f;
            var dy = (by - ay) / 3f;
            var p1X = ax + dx;
            var p1Y = ay + dy;
            var p3X = ax + dx * 2f;
            var p3Y = ay + dy * 2f;

            // The peak sits on the outward normal of the middle third, at the height of an
            // equilateral triangle built on it (sqrt(3)/2 of its length).
            var midX = (p1X + p3X) * 0.5f;
            var midY = (p1Y + p3Y) * 0.5f;
            const float height = 0.8660254f;
            var p2X = midX - dy * height;
            var p2Y = midY + dx * height;

            KochSide(context, parameters, depth - 1, ax, ay, p1X, p1Y);
            KochSide(context, parameters, depth - 1, p1X, p1Y, p2X, p2Y);
            KochSide(context, parameters, depth - 1, p2X, p2Y, p3X, p3Y);
            KochSide(context, parameters, depth - 1, p3X, p3Y, bx, by);
        }

        #endregion

        #region Sierpinski

        private void GenerateSierpinski(GeneratorContext context, Parameters parameters, int depth)
        {
            CornerOf(parameters, 90f, out var ax, out var ay);
            CornerOf(parameters, 210f, out var bx, out var by);
            CornerOf(parameters, 330f, out var cx, out var cy);
            SierpinskiStep(context, parameters, depth, ax, ay, bx, by, cx, cy);
        }

        private void SierpinskiStep(GeneratorContext context, Parameters parameters, int depth,
            float ax, float ay, float bx, float by, float cx, float cy)
        {
            if (depth <= 0)
            {
                var obj = Spawn(context, parameters, $"fractal_{_index++}", context.Span);
                AddPosition(obj, (ax + bx + cx) / 3f, (ay + by + cy) / 3f, obj.Span.StartFrame);
                return;
            }

            var abX = (ax + bx) * 0.5f;
            var abY = (ay + by) * 0.5f;
            var bcX = (bx + cx) * 0.5f;
            var bcY = (by + cy) * 0.5f;
            var caX = (cx + ax) * 0.5f;
            var caY = (cy + ay) * 0.5f;

            SierpinskiStep(context, parameters, depth - 1, ax, ay, abX, abY, caX, caY);
            SierpinskiStep(context, parameters, depth - 1, abX, abY, bx, by, bcX, bcY);
            SierpinskiStep(context, parameters, depth - 1, caX, caY, bcX, bcY, cx, cy);
        }

        #endregion

        #region Tree

        private void GenerateTree(GeneratorContext context, Parameters parameters, int depth)
        {
            TreeBranch(context, parameters, depth,
                parameters.CenterX, parameters.CenterY, 90f + parameters.Rotation, parameters.Scale);
        }

        private void TreeBranch(GeneratorContext context, Parameters parameters, int depth,
            float x, float y, float angle, float length)
        {
            Direction(angle, out var dirX, out var dirY);
            var endX = x + dirX * length;
            var endY = y + dirY * length;
            SpawnSegment(context, parameters, x, y, endX, endY);

            if (depth <= 0) return;

            var childLength = length * parameters.BranchScale;
            TreeBranch(context, parameters, depth - 1, endX, endY, angle - parameters.BranchAngle, childLength);
            TreeBranch(context, parameters, depth - 1, endX, endY, angle + parameters.BranchAngle, childLength);
        }

        #endregion

        /// <summary> One object stretched along a segment: positioned at its midpoint, rotated to
        /// its direction, and sized to its length - the template Size is replaced, not appended to. </summary>
        private void SpawnSegment(GeneratorContext context, Parameters parameters,
            float ax, float ay, float bx, float by)
        {
            var dx = bx - ax;
            var dy = by - ay;
            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            var angle = (float)(Math.Atan2(dy, dx) * (180.0 / Math.PI));

            var obj = Spawn(context, parameters, $"fractal_{_index++}", context.Span);
            AddPosition(obj, (ax + bx) * 0.5f, (ay + by) * 0.5f, obj.Span.StartFrame);
            AddRotation(obj, angle, obj.Span.StartFrame);
            SetSize(obj, length, parameters.Thickness);
        }

        private static void CornerOf(Parameters parameters, float angle, out float x, out float y)
        {
            Direction(angle + parameters.Rotation, out var dirX, out var dirY);
            x = parameters.CenterX + dirX * parameters.Scale;
            y = parameters.CenterY + dirY * parameters.Scale;
        }

        private const int MaxDepth = 6;
        private const int SegmentKeys = 4; // position + rotation + size + colour
        private const int PointKeys = 3;   // position + size + colour

        private static int Depth(int value) => value < 0 ? 0 : value > MaxDepth ? MaxDepth : value;

        private static int Power(int value, int exponent)
        {
            var result = 1;
            for (var i = 0; i < exponent; i++) result *= value;
            return result;
        }

        public class Parameters : SpawnParameters
        {
            public FractalType Type = FractalType.Koch;
            public int Depth = 3;
            public float Scale = 6f;
            public float Thickness = 0.2f;
            public float Rotation;
            public float CenterX;
            public float CenterY;
            public float BranchAngle = 30f;
            public float BranchScale = 0.7f;
        }
    }
}
