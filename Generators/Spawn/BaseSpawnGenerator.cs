using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Spawn
{
    // What every geometry/bullet generator has in common is not the shape - it is the bookkeeping
    // around each spawned object: mint it, parent it, bound it to the context's frame range, give it
    // the template's texture/size/colour/collider, and only then place it. Concentrating that here
    // leaves a concrete generator as pure placement math, which is the part worth reading.
    //
    // Placement math is written in DEGREES here because that is what the maths reads like, but the
    // format STORES RADIANS: every hand-authored rotation in a real level is a multiple of PI (a
    // full turn is 6.2831855, not 360), and the Unity project converts to degrees only at its
    // inspector boundary. AddRotation is the single place that conversion happens - a generator
    // writing an AngleKey by hand must convert too, or a 45 comes out as 45 RADIANS, i.e. ~2578
    // degrees, and the object spins wildly.
    //
    // Frames come into these helpers ABSOLUTE (a generator thinks in the context's window) and are
    // stored LOCAL, because that is what a keyframe's Frame means in this format: the runtime reads
    // it back as obj.Span.StartFrame + key.Frame (Unity side: FrameMath/FrameUtils.ToGlobalFrame). Storing
    // an absolute frame is not a visible error - the object simply never reaches its own keys, so it
    // spawns correctly and then never moves. Every Add*/SetSize below converts; a generator writing
    // obj.Positions.Add itself has to do the same, and the sweep test checks the result.

    /// <summary>
    /// Base for content generators that spawn objects from a shared template.
    /// </summary>
    public abstract class BaseSpawnGenerator<TParams> : BaseContentGenerator<TParams>
        where TParams : SpawnParameters, new()
    {
        /// <summary> Creates one templated object, already parented, framed and given its size and
        /// colour on its first frame. Placement (position/rotation) is the caller's job. </summary>
        protected static ShapeObject Spawn(GeneratorContext context, SpawnParameters parameters,
            string name, in FrameSpan span)
        {
            var obj = context.Create<ShapeObject>();
            obj.ParentObjectId = context.Parent;
            obj.Name = name;
            obj.Layer = ClampLayer(context.LocalLayer);
            obj.Span = ClampSpan(context, span);
            obj.ShapeId = parameters.Shape;
            obj.TextureResourceId = parameters.Texture;
            obj.ColliderId = parameters.Collider;

            AddSize(obj, parameters.Size, obj.Span.StartFrame);
            AddColor(obj, parameters.Color, obj.Span.StartFrame);
            return obj;
        }

        /// <summary> Adds a position keyframe, converting the ABSOLUTE frame the caller thinks in into the local one the format stores. </summary>
        protected static void AddPosition(ShapeObject obj, float x, float y, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            obj.Positions.Add(new PosKey(new Vector2Value(ClampPos(x), ClampPos(y)), LocalFrame(obj, frame), ease));
        }

        /// <summary> Takes DEGREES and stores the radians the format actually holds. </summary>
        protected static void AddRotation(ShapeObject obj, float degrees, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            obj.Rotations.Add(new AngleKey(new FloatValue(ToRadians(degrees)), LocalFrame(obj, frame), ease));
        }

        /// <summary> Degrees as radians. Placement maths reads better in degrees; the format stores radians, and this is where that crossing happens. </summary>
        protected static float ToRadians(float degrees) => (float)(degrees * (Math.PI / 180.0));

        /// <summary> Adds a size keyframe, on the same frame terms. </summary>
        protected static void AddSize(ShapeObject obj, IVector2 size, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            obj.Sizes.Add(new ScaKey(size?.Copy() ?? new Vector2Value(1f, 1f), LocalFrame(obj, frame), ease));
        }

        /// <summary> The same from two plain numbers. </summary>
        protected static void AddSize(ShapeObject obj, float width, float height, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            obj.Sizes.Add(new ScaKey(new Vector2Value(ClampSize(width), ClampSize(height)),
                LocalFrame(obj, frame), ease));
        }

        /// <summary> Replaces the template size Spawn already wrote, for generators whose objects are
        /// individually sized (a line segment's length, a shrinking bullet). Replaces rather than
        /// appends because Frame must stay unique within a track - see RuleCollectionUnique. </summary>
        protected static void SetSize(ShapeObject obj, float width, float height)
        {
            obj.Sizes.Clear();
            AddSize(obj, width, height, obj.Span.StartFrame);
        }

        /// <summary> Adds a colour keyframe, on the same frame terms. </summary>
        protected static void AddColor(ShapeObject obj, IColor4 color, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            obj.Colors.Add(new Color4Key(color?.Copy() ?? new Color4Value(1f, 1f, 1f, 1f),
                LocalFrame(obj, frame), ease));
        }

        /// <summary> The same with the opacity given separately. </summary>
        protected static void AddColor(ShapeObject obj, IColor4 color, float alpha, int frame,
            EaseType ease = FrameRules.DefaultEase)
        {
            var faded = color?.Copy() ?? new Color4Value(1f, 1f, 1f, 1f);
            if (faded is Color4Value literal) literal.A = Clamp01(alpha);
            obj.Colors.Add(new Color4Key(faded, LocalFrame(obj, frame), ease));
        }

        /// <summary> Absolute frame -> the object-relative frame a keyframe actually stores. Clamped
        /// to the object's own lifetime: a key before its start or after its end is unreachable, and
        /// the format bounds a keyframe's Frame at <see cref="FrameRules.MinFrame"/> anyway. </summary>
        protected static int LocalFrame(RectObject obj, int frame)
        {
            var local = obj.Span.ToLocalFrame(frame);
            if (local < FrameRules.MinFrame) return FrameRules.MinFrame;

            var lastLocal = FrameRules.LastFrameOf(obj.Span.FrameDuration);
            return local > lastLocal ? lastLocal : local;
        }

        // Degrees in, unit vector out. System.Math is the only trigonometry available here - the
        // core SDK asmdef has noEngineReferences, so there is no Mathf.

        /// <summary> A heading in degrees as a unit vector. </summary>
        protected static void Direction(float degrees, out float x, out float y)
        {
            var radians = degrees * (Math.PI / 180.0);
            x = (float)Math.Cos(radians);
            y = (float)Math.Sin(radians);
        }

        /// <summary> Straight interpolation, so placement maths needs no engine. </summary>
        protected static float Lerp(float from, float to, float t) => from + (to - from) * t;

        /// <summary> Even split of [0,1] across count steps: 0 for a single item (which then sits at
        /// `from` rather than jumping to `to`), i/(count-1) otherwise. </summary>
        protected static float Ratio(int index, int count) => count <= 1 ? 0f : index / (float)(count - 1);

        private static float ClampPos(float value) => Clamp(value, ValueRules.MinPos, ValueRules.MaxPos);
        private static float ClampSize(float value) => Clamp(value, ValueRules.MinSca, ValueRules.MaxSca);

        private static int ClampLayer(int value)
            => value < ValueRules.MinLayer ? ValueRules.MinLayer
                : value > ValueRules.MaxLayer ? ValueRules.MaxLayer : value;

        // Frames are clamped to the context's own window, not to the level's: a generator asked to
        // fill [100, 200] must not write outside it even when its own math overshoots, or an author
        // who scoped a run to one section finds objects in another.
        //
        // protected because an animated generator has to apply the SAME clamp inside its Estimate:
        // a bullet whose lifetime got truncated to a single frame carries one position key instead
        // of two, and an estimate that ignored that would drift from reality by exactly the amount
        // the clamp removed.

        /// <summary> A frame held inside the run's own window. </summary>
        protected static int ClampFrame(GeneratorContext context, int frame)
            => frame < context.Span.StartFrame ? context.Span.StartFrame
                : frame > context.Span.LastFrame ? context.Span.LastFrame : frame;

        /// <summary> A whole lifetime cut down to the context's window, exactly the way Spawn does
        /// it - an Estimate measuring the same object has to apply this too. </summary>
        protected static FrameSpan ClampSpan(GeneratorContext context, in FrameSpan span)
            => span.ClampedInto(context.Span);

        /// <summary> True when an object's clamped lifetime still spans more than one frame, i.e.
        /// there is room for a second keyframe on any of its tracks. A collapsed lifetime must get
        /// ONE key per track - two on the same frame violates RuleCollectionUnique. </summary>
        protected static bool CanAnimate(in FrameSpan span) => span.FrameDuration > FrameRules.MinFrameDuration;

        /// <summary> Whether a MOVING object that would start at this frame belongs in the run at
        /// all. A staggered generator asked for more objects than its window has room for used to
        /// clamp the overflow onto the last frame, which spawned a pile of one-frame ghosts flashing
        /// after the pattern was over - not spawning them is what the author meant. Strictly before
        /// the end, because a bullet with no frame to travel in is one of those ghosts. Estimate
        /// must apply the same check, or it counts objects the run never creates. </summary>
        protected static bool CanSpawn(GeneratorContext context, int startFrame)
            => startFrame < context.Span.EndFrame;

        private static float Clamp(float value, float min, float max)
            => value < min ? min : value > max ? max : value;

        private static float Clamp01(float value) => Clamp(value, 0f, 1f);
    }
}