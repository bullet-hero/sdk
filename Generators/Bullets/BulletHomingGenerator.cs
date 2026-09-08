using System;
using BH.SDK.Generators.Spawn;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Bullets
{
    // Homing is a runtime behaviour, and this format has no runtime behaviours - a level is data, so
    // the curve has to be BAKED into position keys at author time. That is why Steps exists and why
    // it is capped.
    //
    // The cap is this generator's OWN number and must stay that way. It was LevelRules.MaxObjectKeys
    // minus two, which read as the same thing while that cap was 32; it is not. The format ceiling
    // says how many keyframes a track may STORE, while this says how many a single bullet should
    // COST - a burst multiplies it by every bullet in it - and the two answers only ever coincided
    // by accident. Raising the ceiling to 512 would otherwise have made every homing bullet an
    // order of magnitude heavier with nothing said about it.

    /// <summary>
    /// A burst of bullets that curve toward a target, their pursuit curve baked into position
    /// keyframes.
    /// </summary>
    public class BulletHomingGenerator : BaseSpawnGenerator<BulletHomingGenerator.Parameters>
    {
        /// <summary> <c>"gen_bullet_homing"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_bullet_homing";

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.BurstCount), nameof(Parameters.Speed),
                nameof(Parameters.TravelFrames))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.Spread), nameof(Parameters.LaunchAngle),
                nameof(Parameters.OriginX), nameof(Parameters.OriginY),
                nameof(Parameters.TargetX), nameof(Parameters.TargetY), nameof(Parameters.TurnRate),
                nameof(Parameters.StaggerFrames), nameof(Parameters.Steps), nameof(Parameters.FaceTravel))
            .Range(nameof(Parameters.BurstCount), 1, 128)
            .Range(nameof(Parameters.Spread), 0f, 360f)
            .Range(nameof(Parameters.LaunchAngle), -3600f, 3600f)
            .Range(nameof(Parameters.Speed), 0.01f, 100f)
            .Range(nameof(Parameters.TurnRate), 0f, 180f)
            .Range(nameof(Parameters.TravelFrames), 1, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.StaggerFrames), 0, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.Steps), MinSteps, MaxSteps)
            .Unit(nameof(Parameters.Spread), "deg")
            .Unit(nameof(Parameters.LaunchAngle), "deg")
            .Unit(nameof(Parameters.TurnRate), "deg/step")
            .Unit(nameof(Parameters.TravelFrames), "frames")
            .Unit(nameof(Parameters.StaggerFrames), "frames")
            .Range(nameof(Parameters.OriginX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.TargetX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.TargetY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var burst = Burst(parameters.BurstCount);
            var steps = Steps(parameters.Steps);
            var travel = Travel(parameters.TravelFrames);
            var stagger = Stagger(parameters.StaggerFrames);

            for (var i = 0; i < burst; i++)
            {
                var launchAngle = parameters.LaunchAngle + Offset(i, burst, parameters.Spread);
                var spawnFrame = context.Span.StartFrame + i * stagger;
                if (!CanSpawn(context, spawnFrame)) break; // stagger ran past the window - no ghost on its last frame
                var obj = Spawn(context, parameters, $"homing_{i}", new FrameSpan(spawnFrame, travel));

                // Frames of MOVEMENT, one less than the lifetime: the first frame is the spawn
                // position, so a bullet alive for N frames has N-1 frames left to travel in.
                var travelFrames = obj.Span.FrameDuration - 1;
                var x = parameters.OriginX;
                var y = parameters.OriginY;
                var angle = launchAngle;

                AddPosition(obj, x, y, obj.Span.StartFrame);
                if (parameters.FaceTravel) AddRotation(obj, angle, obj.Span.StartFrame);
                if (!CanAnimate(obj.Span)) continue;

                // One simulation step per keyframe: turn toward the target by at most TurnRate, then
                // advance. Steps are spread evenly over the lifetime, so Speed is per step rather
                // than per frame - fewer steps means each one covers more ground.
                for (var step = 1; step <= steps; step++)
                {
                    var toTarget = (float)(Math.Atan2(parameters.TargetY - y, parameters.TargetX - x)
                                           * (180.0 / Math.PI));
                    angle = TurnToward(angle, toTarget, parameters.TurnRate);
                    Direction(angle, out var dirX, out var dirY);
                    x += dirX * parameters.Speed;
                    y += dirY * parameters.Speed;

                    var frame = obj.Span.StartFrame + (int)Math.Round(travelFrames * (step / (double)steps));
                    if (frame <= obj.Span.StartFrame) continue; // collapsed onto the spawn frame - skip
                    if (frame > obj.Span.LastFrame) frame = obj.Span.LastFrame;

                    // Rounding can land two consecutive steps on the same frame when the lifetime is
                    // shorter than the step count; Frame must stay unique within a track. The stored
                    // frame is object-LOCAL (see BaseSpawnGenerator's header), so the comparison has
                    // to happen in that same space rather than against the absolute one.
                    if (obj.Positions[obj.Positions.Count - 1].Frame == LocalFrame(obj, frame)) continue;

                    AddPosition(obj, x, y, frame);
                    if (parameters.FaceTravel) AddRotation(obj, angle, frame);
                }
            }
        }

        // Walks the same simulation as Generate, minus the writes: the number of baked keys depends
        // on frame rounding and on the duplicate-frame skips above, so counting it any other way
        // would be a guess.

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var burst = Burst(parameters.BurstCount);
            var steps = Steps(parameters.Steps);
            var travel = Travel(parameters.TravelFrames);
            var stagger = Stagger(parameters.StaggerFrames);
            var keys = 0;
            var objects = 0;

            for (var i = 0; i < burst; i++)
            {
                if (!CanSpawn(context, context.Span.StartFrame + i * stagger)) break;
                objects++;

                var spawnFrame = ClampFrame(context, context.Span.StartFrame + i * stagger);
                var span = ClampSpan(context, new FrameSpan(spawnFrame, travel));
                var travelFrames = span.FrameDuration - 1;

                var perKey = parameters.FaceTravel ? 2 : 1;
                keys += 2 + perKey; // size + colour + spawn position (+ rotation)
                if (!CanAnimate(span)) continue;

                var lastFrame = spawnFrame;
                for (var step = 1; step <= steps; step++)
                {
                    var frame = spawnFrame + (int)Math.Round(travelFrames * (step / (double)steps));
                    if (frame <= spawnFrame) continue;
                    if (frame > span.LastFrame) frame = span.LastFrame;
                    if (frame == lastFrame) continue;

                    lastFrame = frame;
                    keys += perKey;
                }
            }

            return new GeneratorCost(objects, keys);
        }

        /// <summary> Even fan around the launch angle: a single bullet fires straight ahead, N
        /// bullets share the spread symmetrically. </summary>
        private static float Offset(int index, int count, float spread)
            => count <= 1 ? 0f : (index / (float)(count - 1) - 0.5f) * spread;

        /// <summary> Rotates `from` toward `to` by at most maxDelta degrees, taking the short way
        /// around so a bullet never spins the long way to reach a target just behind it. </summary>
        private static float TurnToward(float from, float to, float maxDelta)
        {
            var delta = (to - from) % 360f;
            if (delta > 180f) delta -= 360f;
            if (delta < -180f) delta += 360f;
            if (delta > maxDelta) delta = maxDelta;
            if (delta < -maxDelta) delta = -maxDelta;
            return from + delta;
        }

        private const int MinSteps = 2;
        private const int MaxSteps = 30; // what one bullet should cost, not what a track may hold

        private static int Burst(int value) => value < 1 ? 1 : value;
        private static int Travel(int value) => value < 1 ? 1 : value;
        private static int Stagger(int value) => value < 0 ? 0 : value;

        private static int Steps(int value)
            => value < MinSteps ? MinSteps : value > MaxSteps ? MaxSteps : value;

        /// <summary> What the burst chases and how tightly it curves. Public mutable fields, like every parameters
        /// class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters
        {
            /// <summary> How many bullets the burst holds. </summary>
            public int BurstCount = 6;

            /// <summary> Angle the burst fans out over, centred on the launch angle. </summary>
            public float Spread = 90f;

            /// <summary> Direction the burst leaves the origin in, in degrees. </summary>
            public float LaunchAngle = 90f;

            /// <summary> Where the burst starts. </summary>
            public float OriginX;

            /// <summary> Its vertical half. </summary>
            public float OriginY = 6f;

            /// <summary> The point the bullets curve towards. </summary>
            public float TargetX;

            /// <summary> Its vertical half. </summary>
            public float TargetY = -6f;

            /// <summary> How fast a bullet travels along its pursuit curve. </summary>
            public float Speed = 1.2f;

            /// <summary> How sharply a bullet may turn toward the target - low values overshoot and curve back. </summary>
            public float TurnRate = 12f;

            /// <summary> How long each bullet lives. </summary>
            public int TravelFrames = 90;

            /// <summary> Delay between one bullet leaving and the next. </summary>
            public int StaggerFrames = 0;

            /// <summary> How many position keyframes the curve is baked into - the pursuit is sampled, not simulated. </summary>
            public int Steps = 12;

            /// <summary> Rotate each bullet to point along its own path. </summary>
            public bool FaceTravel = true;
        }
    }
}