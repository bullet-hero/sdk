using System.Collections.Generic;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Modifiers
{
    /// <summary> What decides which object gets shifted first. </summary>
    public enum StaggerOrder : byte
    {
        /// <summary> The order the host handed the selection over in. </summary>
        Selection = 0,

        /// <summary> By draw order - the natural one for a stack of bars or lanes. </summary>
        Layer = 1,

        /// <summary> Left to right. </summary>
        PositionX = 2,

        /// <summary> Bottom to top. </summary>
        PositionY = 3,

        /// <summary> Outward from a point - a ripple rather than a sweep. </summary>
        Distance = 4,
    }

    // Turns a wall into a wave: identical objects placed at once, delayed one after another. That is
    // usually the second half of "generate a grid, then make it arrive in sequence", which is why it
    // pairs with every geometry generator rather than doing any placement itself.
    //
    // Both halves of the shift are optional and separately useful: moving an object's BOUNDS delays
    // when it exists, moving its KEYFRAMES delays what it does. Doing only the first makes a row
    // appear in sequence; doing only the second makes them all appear at once but animate in
    // sequence. Doing both is what most authors mean.

    /// <summary>
    /// Delays each selected object a little more than the last, in a chosen order.
    /// </summary>
    public class StaggerGenerator : BaseModifier<StaggerGenerator.Parameters>
    {
        public override string NameKey => "mod_span_stagger";

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.StepFrames), nameof(Parameters.Order))
            .Section(GeneratorSections.Additional, nameof(Parameters.Reverse),
                nameof(Parameters.ShiftBounds), nameof(Parameters.ShiftKeyframes),
                nameof(Parameters.Tracks), nameof(Parameters.OriginX), nameof(Parameters.OriginY))
            .Range(nameof(Parameters.StepFrames), -FrameRules.MaxFrameDuration, FrameRules.MaxFrameDuration)
            .Range(nameof(Parameters.OriginX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginY), ValueRules.MinPos, ValueRules.MaxPos)
            .Unit(nameof(Parameters.StepFrames), "frames")
            .VisibleWhen(nameof(Parameters.Tracks), p => ((Parameters)p).ShiftKeyframes)
            .VisibleWhen(nameof(Parameters.OriginX), p => ((Parameters)p).Order == StaggerOrder.Distance)
            .VisibleWhen(nameof(Parameters.OriginY), p => ((Parameters)p).Order == StaggerOrder.Distance)
            .Build();

        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            if (parameters.StepFrames == 0) return;
            if (!parameters.ShiftBounds && !parameters.ShiftKeyframes) return;

            var ordered = Order(context, parameters);
            var maxFrame = MaxFrame(context);

            for (var i = 0; i < ordered.Count; i++)
            {
                var index = parameters.Reverse ? ordered.Count - 1 - i : i;
                var delta = index * parameters.StepFrames;
                if (delta == 0) continue;

                var obj = context.Edit(ordered[i]);

                if (parameters.ShiftBounds)
                    obj.Span = ShiftBounded(obj.Span, delta, maxFrame);

                if (!parameters.ShiftKeyframes) continue;

                // A uniform shift can never collide two keys of the same track, so no
                // occupied-frame bookkeeping is needed here - unlike quantizing, where that is the
                // whole problem.
                foreach (var track in ObjectTracks.Of(obj, parameters.Tracks))
                    for (var key = 0; key < track.Count; key++)
                        track.SetFrameAt(key, Clamp(track.FrameAt(key) + delta, maxFrame));
            }
        }

        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
            => GeneratorCost.Zero;

        private static List<ObjectId> Order(GeneratorContext context, Parameters parameters)
        {
            var ids = new List<ObjectId>();
            if (context.Selection.Count > 0)
            {
                foreach (var id in context.Selection)
                    if (context.Objects.ContainsKey(id))
                        ids.Add(id);
            }
            else
            {
                ids.AddRange(context.Objects.Keys);
            }

            if (parameters.Order == StaggerOrder.Selection) return ids;

            // A stable sort on a key computed once per object: List.Sort is unstable, so equal keys
            // would otherwise shuffle between runs and break "same inputs, same level".
            var keys = new Dictionary<ObjectId, float>(ids.Count);
            foreach (var id in ids) keys[id] = SortKey(context.Objects[id], parameters);

            ids.Sort((a, b) =>
            {
                var compared = keys[a].CompareTo(keys[b]);
                return compared != 0 ? compared : a.value.CompareTo(b.value);
            });
            return ids;
        }

        private static float SortKey(RectObject obj, Parameters parameters)
        {
            switch (parameters.Order)
            {
                case StaggerOrder.Layer:
                    return obj.Layer;
                case StaggerOrder.PositionX:
                    return PositionOf(obj, out var x, out _) ? x : 0f;
                case StaggerOrder.PositionY:
                    return PositionOf(obj, out _, out var y) ? y : 0f;
                case StaggerOrder.Distance:
                    if (!PositionOf(obj, out var px, out var py)) return 0f;
                    var dx = px - parameters.OriginX;
                    var dy = py - parameters.OriginY;
                    return dx * dx + dy * dy; // squared - ordering only, never displayed
                default:
                    return 0f;
            }
        }

        // An empty Positions track is valid data, not missing data (see the project's note on empty
        // keyframe collections): it means the object sits at the engine default, which for ordering
        // purposes is the origin.
        private static bool PositionOf(RectObject obj, out float x, out float y)
        {
            x = 0f;
            y = 0f;
            if (obj.Positions.Count == 0) return false;
            if (obj.Positions[0].Pos is not Vector2Value literal) return false;

            x = literal.X;
            y = literal.Y;
            return true;
        }

        private static int MaxFrame(GeneratorContext context)
        {
            var length = context?.Settings?.FrameDuration ?? FrameRules.MinFrameDuration;
            return length - 1; // FrameDuration is a count - see RuleLevelFrame
        }

        private static int Clamp(int frame, int maxFrame)
            => frame < FrameRules.MinFrame ? FrameRules.MinFrame : frame > maxFrame ? maxFrame : frame;

        // The whole lifetime moves as one. Clamping the two edges separately - which is what the
        // old StartFrame/EndFrame pair invited - squashed an object into a single frame as soon as
        // the run reached the end of the timeline, silently deleting its animation.
        private static FrameSpan ShiftBounded(in FrameSpan span, int delta, int maxFrame)
        {
            var lastStart = maxFrame + 1 - span.FrameDuration;
            var start = span.StartFrame + delta;
            if (lastStart >= FrameRules.MinFrame && start > lastStart) start = lastStart;
            return span.WithStart(start);
        }

        public class Parameters
        {
            public int StepFrames = 4;
            public StaggerOrder Order = StaggerOrder.Selection;
            public bool Reverse;

            /// <summary> Delays when the object exists. Because a keyframe's Frame is LOCAL to its
            /// object, this already carries the whole animation with it - which is why it, and not
            /// ShiftKeyframes, is the one that is on by default. </summary>
            public bool ShiftBounds = true;

            /// <summary> Delays what the object does WITHIN its own lifetime, on top of any bounds
            /// shift. Off by default: with bounds shifting too, this delays the motion twice. </summary>
            public bool ShiftKeyframes;

            public ObjectTrackMask Tracks = ObjectTrackMask.All;
            public float OriginX;
            public float OriginY;
        }
    }
}
