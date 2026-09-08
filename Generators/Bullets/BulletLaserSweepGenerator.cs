using BH.SDK.Generators.Spawn;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Bullets
{
    // Two objects, not one, and that is the whole design decision here. A laser is a telegraph
    // followed by a hit, and ColliderId is a static field - it cannot be animated from "harmless"
    // to "lethal" partway through an object's life. So the warning beam is its own collider-less
    // object that dies exactly when the firing beam is born.

    /// <summary>
    /// A sweeping laser: a thin harmless warning beam, then a full-width beam that carries the
    /// collider and rotates from one angle to another.
    /// </summary>
    public class BulletLaserSweepGenerator : BaseSpawnGenerator<BulletLaserSweepGenerator.Parameters>
    {
        /// <summary> <c>"gen_bullet_laser_sweep"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_bullet_laser_sweep";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Length), nameof(Parameters.Width),
                nameof(Parameters.WarnFrames), nameof(Parameters.FireFrames))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.WarnWidth),
                nameof(Parameters.StartAngle), nameof(Parameters.EndAngle),
                nameof(Parameters.OriginX), nameof(Parameters.OriginY),
                nameof(Parameters.WarnAlpha), nameof(Parameters.Ease))
            .Range(nameof(Parameters.Length), 0.01f, ValueRules.MaxSca)
            .Range(nameof(Parameters.Width), 0.01f, ValueRules.MaxSca)
            .Range(nameof(Parameters.WarnWidth), 0.01f, ValueRules.MaxSca)
            .Range(nameof(Parameters.StartAngle), -3600f, 3600f)
            .Range(nameof(Parameters.EndAngle), -3600f, 3600f)
            .Range(nameof(Parameters.WarnFrames), 0, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.FireFrames), 1, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.WarnAlpha), 0f, 1f)
            .Unit(nameof(Parameters.StartAngle), "deg")
            .Unit(nameof(Parameters.EndAngle), "deg")
            .Unit(nameof(Parameters.WarnFrames), "frames")
            .Unit(nameof(Parameters.FireFrames), "frames")
            .Range(nameof(Parameters.OriginX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var warnFrames = Frames(parameters.WarnFrames, 0);
            var fireFrames = Frames(parameters.FireFrames, 1);

            var warnStart = context.Span.StartFrame;
            var fireStart = warnStart + warnFrames;

            if (warnFrames > 0)
            {
                // The telegraph ends exactly where the beam begins and they never share a frame -
                // which is what a half-open span buys: warn covers [warnStart, fireStart).
                var warn = Spawn(context, parameters, "laser_warn", new FrameSpan(warnStart, warnFrames));
                warn.ColliderId = ShapeId.Null; // telegraph only - never collides, whatever the template says
                SetSize(warn, parameters.Length, parameters.WarnWidth);
                PlaceBeam(warn, parameters, parameters.StartAngle, warn.Span.StartFrame);
                RecolorFaded(warn, parameters, warn.Span.StartFrame);
            }

            var fire = Spawn(context, parameters, "laser_fire", new FrameSpan(fireStart, fireFrames));
            SetSize(fire, parameters.Length, parameters.Width);
            PlaceBeam(fire, parameters, parameters.StartAngle, fire.Span.StartFrame);

            // Position and rotation animate together: the beam pivots around its origin, so its
            // midpoint travels along an arc rather than staying put.
            if (CanAnimate(fire.Span))
                PlaceBeam(fire, parameters, parameters.EndAngle, fire.Span.LastFrame, parameters.Ease);
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var warnFrames = Frames(parameters.WarnFrames, 0);
            var fireFrames = Frames(parameters.FireFrames, 1);

            var fireStart = ClampFrame(context, context.Span.StartFrame + warnFrames);
            var fireSpan = ClampSpan(context, new FrameSpan(fireStart, fireFrames));

            var objects = warnFrames > 0 ? 2 : 1;
            var keys = warnFrames > 0 ? WarnKeys : 0;
            keys += 3 + (CanAnimate(fireSpan) ? 2 : 0); // position + rotation + size (+ pair)
            keys += 1; // colour
            return new GeneratorCost(objects, keys);
        }

        /// <summary> Places the beam so that it extends OUT of the origin: the rect is centred on
        /// its own midpoint, which sits half a length along the beam direction. </summary>
        private static void PlaceBeam(ShapeObject obj, Parameters parameters, float angle,
            int frame, EaseType ease = FrameRules.DefaultEase)
        {
            Direction(angle, out var dirX, out var dirY);
            var half = parameters.Length * 0.5f;
            AddPosition(obj, parameters.OriginX + dirX * half, parameters.OriginY + dirY * half, frame, ease);
            AddRotation(obj, angle, frame, ease);
        }

        /// <summary> Replaces the template colour with a faded copy - the warning beam has to read
        /// as "not yet dangerous" without the author configuring a second colour for it. </summary>
        private static void RecolorFaded(ShapeObject obj, Parameters parameters, int frame)
        {
            obj.Colors.Clear();
            AddColor(obj, parameters.Color, parameters.WarnAlpha, frame);
        }

        private const int WarnKeys = 4; // position + rotation + size + colour

        private static int Frames(int value, int minimum) => value < minimum ? minimum : value;

        /// <summary> How long the warning lasts and which arc the beam sweeps through. Public mutable fields, like
        /// every parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How far the beam reaches from its origin. </summary>
            public float Length = 40f;

            /// <summary> Width of the firing beam, which is the one carrying the collider. </summary>
            public float Width = 2f;

            /// <summary> Width of the warning beam, which is harmless. </summary>
            public float WarnWidth = 0.3f;

            /// <summary> Angle the sweep begins at. </summary>
            public float StartAngle;

            /// <summary> Angle it ends at. </summary>
            public float EndAngle = 90f;

            /// <summary> The point the beam pivots around. </summary>
            public float OriginX;
            /// <summary> Its vertical half. </summary>
            public float OriginY;

            /// <summary> How long the warning is shown before the beam fires. </summary>
            public int WarnFrames = 45;

            /// <summary> How long the firing beam lasts. </summary>
            public int FireFrames = 60;

            /// <summary> How visible the warning beam is. </summary>
            public float WarnAlpha = 0.35f;

            /// <summary> Curve the sweep follows between the two angles. </summary>
            public EaseType Ease = EaseType.Linear;
        }
    }
}
