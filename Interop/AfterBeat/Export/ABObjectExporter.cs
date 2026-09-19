using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Interop.AfterBeat.Export
{
    // Every conversion the importer does, backwards, plus the ones that only exist going out:
    //
    //   Rotation is differentiated back into per-keyframe degree deltas, which needs the track in
    //   order and a running previous value - the exact mirror of accumulating it on the way in.
    //   A track that was reordered in between would export a different animation, so it is sorted
    //   first: the format does not guarantee keyframe order and this direction depends on it.
    //
    //   Effects, anchors and per-corner colours have nowhere to go. Each is reported once rather
    //   than per object, because a level using effects uses a lot of them.
    //
    //   Afterbeat has no "active" flag on an object, so an inactive object would silently become a
    //   visible one. It is skipped instead and reported - a level that plays differently is worse
    //   than a level missing a decoration the author had already turned off.

    /// <summary> One <see cref="RectObject"/> into one Afterbeat object. </summary>
    public static class ABObjectExporter
    {
        /// <summary> Exports every object of a scope, skipping the ones with no representation. </summary>
        public static List<VgdObject> ExportAll(ABExportContext context, string pathPrefix)
        {
            var exported = new List<VgdObject>();
            if (context?.Scope?.Objects == null) return exported;

            foreach (var pair in context.Scope.Objects)
            {
                var source = pair.Value;
                if (source == null) continue;

                var path = $"{pathPrefix}[{pair.Key.value}]";
                var target = Export(source, context, path);
                if (target != null) exported.Add(target);
            }

            return exported;
        }

        /// <summary> One object, or null when it has no Afterbeat equivalent at all. </summary>
        public static VgdObject Export(RectObject source, ABExportContext context, string path)
        {
            if (source == null) return null;

            var report = context.Report;

            switch (source)
            {
                case EffectObject effect when ResolveEffect(effect, context, path) == null:
                    return null;
            }

            // Bookkeeping this converter created rather than content the author did, and the source
            // game rebuilds it itself - see ABExportContext.CameraScaleRootId. Its children are
            // written parented to the camera, which is where they were before the import.
            if (source.ObjectId == context.CameraScaleRootId) return null;

            if (!source.Active)
            {
                report.Dropped("inactive_objects",
                    "Afterbeat objects have no active switch; objects turned off here are not exported.", path);
                return null;
            }

            var framerate = context.Options.Framerate;
            var target = new VgdObject
            {
                Id = ABExportContext.ToSourceId(source.ObjectId),
                Name = source.Name ?? string.Empty,
                ParentId = context.ToParentId(source.ParentObjectId),
                ParentType = FullParentType,
            };

            ApplyDrawOrder(context.GetEffectiveLayer(source), target);

            ABTimeMap.ExportSpan(source.Span, framerate, target);
            ApplyShape(source, target, context, path);
            ApplyOrigin(source, target);

            ExportPositions(source, target, framerate, context, path);
            ExportSizes(source, target, framerate, context, path);
            ExportRotations(source, target, framerate, context, path);
            ExportColors(source, target, context, path);
            ExportEmitter(source, target, context, path);
            ReportUnsupported(source, report, path);

            return target;
        }

        #region Particles

        // THE EMITTER IS WRITTEN OVER THE ORDINARY TRACKS, not instead of them. Values 0/1 of each
        // track keep the meaning every other object gives them - where the emitter is, how big it
        // is, which way it faces - and the particle's own life is laid on top as values 2/3, with
        // the eight parameters appended to the first position keyframe. So the ordinary exporters
        // above run first and this only adds.
        //
        // A round trip is deliberately NOT lossless, and the losses are named one by one below
        // rather than summarised: the whole Forces group past the start velocity, every shape that
        // is not a box or a circle, every Random and BySpeed variant, the particle texture and
        // pivot. An effect authored here rather than imported will lose most of what it is.

        /// <summary> The definition an effect placement points at, or null when it cannot be
        /// resolved - in which case the object has already been reported and is not exported. </summary>
        private static EffectData ResolveEffect(EffectObject source, ABExportContext context,
            string path)
        {
            if (context.Effects != null
                && context.Effects.TryGetValue(source.EffectId, out var data) && data != null)
                return data;

            context.Report.Dropped("effect_unresolved",
                "Some effect objects point at a definition the level does not hold; those objects are not exported.",
                path);
            return null;
        }

        private static void ExportEmitter(RectObject source, VgdObject target,
            ABExportContext context, string path)
        {
            if (source is not EffectObject effect) return;

            var data = ResolveEffect(effect, context, path);
            if (data == null) return;

            var report = context.Report;
            var framerate = context.Options.Framerate;

            target.ObjectType = (int)ABObjectType.Particles;

            var (shape, option) = ABShapeMap.Export(data.Core.ParticleShapeId, report, path);
            target.Shape = shape;
            target.ShapeOption = option;

            var life = ResolveExportedLifetime(data, report, path);

            ApplyEmitterParameters(data, target, report, path);
            ApplyEmitterVolume(data, target, report, path);
            ApplyParticleScale(data, target, life, framerate);
            ApplyParticleAngle(data, target, life, framerate);
            ApplyParticleColor(data, target, life, framerate, context, path);
            ReportEmitterLosses(data, report, path);
        }

        // A lifetime SPREAD has no representation at all - over there every particle lives exactly
        // as long as the object's own animation, one number with no range - so a spread collapses to
        // its lower bound and says so.
        private static float ResolveExportedLifetime(EffectData data, InteropReport report, string path)
        {
            var bounds = data.Core.LifetimeBounds as Vector2Value;
            if (bounds == null) return ABParticleMap.MinTimelineLength;

            if (!BHSDKMath.Approximately(bounds.X, bounds.Y))
                report.Approximated("particle_lifetime_spread",
                    "Afterbeat gives every particle of an emitter the same lifetime; a lifetime range exports as its lower bound.",
                    path);

            return Math.Max(bounds.X, ABParticleMap.MinTimelineLength);
        }

        private static void ApplyEmitterParameters(EffectData data, VgdObject target,
            InteropReport report, string path)
        {
            var keyframe = FirstKeyframe(target, VgdObject.TrackIndex.Move);
            if (keyframe == null) return;

            var circle = data.Shape as EffectShapeCircle;

            Write(keyframe, ABParticleMap.SpawnRatePerSecondIndex, data.Core.ParticleCount);
            Write(keyframe, ABParticleMap.SpawnRatePerUnitIndex, ABParticleMap.SpawnRatePerUnitDefault);

            // Effects here always simulate in their own space, which is Afterbeat's local emitter.
            Write(keyframe, ABParticleMap.WorldSpaceIndex, 0f);

            // Emission stopping early is what a stop frame IS, and an emitter with none runs until
            // it dies with its particles.
            Write(keyframe, ABParticleMap.DespawnOnEndIndex, data.HasStopLocalFrame ? 0f : 1f);

            Write(keyframe, ABParticleMap.EmitterShapeIndex,
                circle != null ? (int)ABParticleEmitterShapeType.Circle : (int)ABParticleEmitterShapeType.Rectangle);
            Write(keyframe, ABParticleMap.EmitterArcIndex,
                circle != null
                    ? ToDegrees(Read(circle.Arc, EffectRules.Shape.Arc_Max))
                    : ABParticleMap.EmitterArcDefault);
            Write(keyframe, ABParticleMap.EmitterRadiusThicknessIndex,
                circle != null ? Read(circle.Thickness, 1f) : ABParticleMap.EmitterRadiusThicknessDefault);
            Write(keyframe, ABParticleMap.StartSpeedIndex, ABParticleMap.StartSpeedDefault);

            if (!BHSDKMath.Approximately(VelocityOf(data), 0f))
                report.Approximated("particle_start_velocity",
                    "Afterbeat describes a particle's travel as a position over its life rather than as one start velocity; the velocity exports as the straight line it draws over that life.",
                    path);
        }

        private static void ApplyEmitterVolume(EffectData data, VgdObject target,
            InteropReport report, string path)
        {
            var (x, y) = ResolveExportedVolume(data);

            var track = target.GetTrack(VgdObject.TrackIndex.Scale);
            if (track == null) return;

            if (track.Keyframes.Count == 0)
                track.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float> { x, y } });

            foreach (var keyframe in track.Keyframes)
            {
                Write(keyframe, 0, x);
                Write(keyframe, 1, y);
            }
        }

        private static void ApplyParticleScale(EffectData data, VgdObject target, float life,
            int framerate)
        {
            if (data.Scale is not EffectScaleCurvesOverLife curves) return;

            var track = target.GetTrack(VgdObject.TrackIndex.Scale);
            if (track == null) return;

            foreach (var time in CurveTimes(curves.CurveX, curves.CurveY))
            {
                var keyframe = KeyframeAt(track, time * life);
                Write(keyframe, ABParticleMap.ParticleScaleXIndex, Sample(curves.CurveX, time));
                Write(keyframe, ABParticleMap.ParticleScaleYIndex, Sample(curves.CurveY, time));
            }
        }

        private static void ApplyParticleAngle(EffectData data, VgdObject target, float life,
            int framerate)
        {
            var track = target.GetTrack(VgdObject.TrackIndex.Rotate);
            if (track == null) return;

            switch (data.Angle)
            {
                case EffectAngleValue value:
                    Write(KeyframeAt(track, 0f), ABParticleMap.ParticleAngleIndex,
                        ToDegrees(Read(value.Angle, 0f)));
                    return;

                case EffectAngleCurvesOverLife curves:
                    foreach (var time in CurveTimes(curves.Curve, curves.Curve))
                        Write(KeyframeAt(track, time * life), ABParticleMap.ParticleAngleIndex,
                            ToDegrees(Sample(curves.Curve, time)));
                    return;
            }
        }

        private static void ApplyParticleColor(EffectData data, VgdObject target, float life,
            int framerate, ABExportContext context, string path)
        {
            if (data.Color is not EffectColorGradientOverLife gradient) return;

            var track = target.GetTrack(VgdObject.TrackIndex.Color);
            if (track == null) return;

            foreach (var stop in gradient.Gradient.ColorKeys)
            {
                var keyframe = KeyframeAt(track, stop.Time * life);
                var (index, opacity) = ABColorMap.Export(stop.Color4, ABPalette.Objects,
                    context.ReferenceTheme, context.Report, path);

                Write(keyframe, ABCurveMap.ColorSlotIndex, index);
                Write(keyframe, ABCurveMap.ColorOpacityIndex,
                    AlphaAt(gradient, stop.Time) * ABCurveMap.ColorOpacityScale);
            }
        }

        /// <summary> Every target of an effect that Afterbeat has no field for at all, named one by
        /// one - a category would tell an author nothing about what to rebuild. </summary>
        private static void ReportEmitterLosses(EffectData data, InteropReport report, string path)
        {
            if (data.Shape is not (EffectShapeRectangle or EffectShapeCircle))
                report.Dropped("particle_shape_unsupported",
                    "Afterbeat emitters are a box or a circle; Point, Line, Cone and Torus emitter shapes have no equivalent and export as a box.",
                    path);

            if (data.Shape is EffectShapeCircle { Spread: not null })
                report.Dropped("particle_shape_spread",
                    "Afterbeat walks a rim at random and has no spread setting; Random, Loop, PingPong and Sine spreads are not exported.",
                    path);

            if (data.Scale is not (EffectScaleValue or EffectScaleCurvesOverLife))
                report.Dropped("particle_scale_variant",
                    "Afterbeat sizes a particle by one curve over its life; the RandomUniform, RandomPerComponent and CurvesBySpeed variants are not exported.",
                    path);

            if (data.Angle is not (EffectAngleValue or EffectAngleCurvesOverLife))
                report.Dropped("particle_angle_variant",
                    "Afterbeat turns a particle by one curve over its life; the RandomUniform, RandomPerComponent and CurvesBySpeed variants are not exported.",
                    path);

            if (data.Color is not (EffectColorValue or EffectColorGradientOverLife))
                report.Dropped("particle_color_variant",
                    "Afterbeat tints a particle by one ramp over its life; the RandomUniform, RandomPerComponent, GradientBySpeed and GradientRandom variants are not exported.",
                    path);

            if (HasForcesPastVelocity(data))
                report.Dropped("particle_forces",
                    "Afterbeat has no gravity, angular velocity, orbital or linear force on a particle; those are not exported.",
                    path);

            if (data.Core.TextureResourceId.value != TextureResourceId.Null.value)
                report.Dropped("particle_texture",
                    "Afterbeat draws a particle with the emitter's own shape and no image of its own; a particle texture is not exported.",
                    path);

            if (!data.Core.Render)
                report.Dropped("particle_render_off",
                    "Afterbeat has no way to simulate an emitter without drawing it; an effect with rendering turned off exports as a visible one.",
                    path);

            if (!data.Core.Loop)
                report.Dropped("particle_burst",
                    "Afterbeat clears an emitter's burst list unconditionally and only emits at a rate; a one-shot effect exports as a continuous one.",
                    path);
        }

        private static bool HasForcesPastVelocity(EffectData data)
        {
            var forces = data.Forces;

            return !IsZero(forces.StartGravityMin) || !IsZero(forces.StartGravityMax)
                                                   || !IsZero(forces.StartAngularVelocityMin) ||
                                                   !IsZero(forces.StartAngularVelocityMax)
                                                   || !IsZeroVector(forces.LinearVelocity) ||
                                                   !IsZeroVector(forces.LinearForce);
        }

        private static bool IsZero(IFloat value)
            => value is not FloatValue literal || BHSDKMath.Approximately(literal.Value, 0f);

        private static bool IsZeroVector(IVector2 value)
            => value is not Vector2Value literal
               || (BHSDKMath.Approximately(literal.X, 0f) && BHSDKMath.Approximately(literal.Y, 0f));

        private static float VelocityOf(EffectData data)
            => data.Forces.StartVelocityMin is Vector2Value velocity
                ? Math.Abs(velocity.X) + Math.Abs(velocity.Y)
                : 0f;

        private static (float X, float Y) ResolveExportedVolume(EffectData data)
        {
            switch (data.Shape)
            {
                case EffectShapeRectangle rectangle when rectangle.Size is Vector2Value size:
                    return (size.X, size.Y);
                // The two semi-axes ARE the scale over there - see ABParticleMap's note on why
                // nothing is doubled here.
                case EffectShapeCircle circle:
                    var radius = Read(circle.Radius, EffectRules.Shape.CircleRadius_Default);
                    return (radius, radius * Read(circle.Aspect, EffectRules.Shape.CircleAspect_Default));
                default:
                    return (ABParticleMap.DefaultEmitterExtent, ABParticleMap.DefaultEmitterExtent);
            }
        }

        private static float Read(IFloat value, float fallback)
            => value is FloatValue literal ? literal.Value : fallback;

        private static float ToDegrees(float radians) => radians * (float)(180d / Math.PI);

        /// <summary> Every distinct time two curves put a key on, in order. </summary>
        private static List<float> CurveTimes(CurveValue first, CurveValue second)
        {
            var times = new List<float>();

            foreach (var curve in new[] { first, second })
            foreach (var key in curve.KeyFrames)
                if (!times.Contains(key.Time))
                    times.Add(key.Time);

            times.Sort();
            return times;
        }

        // Linear between the two keys around it, deliberately ignoring their tangents: Afterbeat
        // stores an easing NAME per keyframe and has no way to say "this bend", so a sampled point
        // is the only honest thing to write.
        private static float Sample(CurveValue curve, float time)
        {
            var keys = curve.KeyFrames;
            if (keys.Count == 0) return 0f;
            if (time <= keys[0].Time) return keys[0].Value;

            for (var i = 1; i < keys.Count; i++)
            {
                if (time > keys[i].Time) continue;

                var span = keys[i].Time - keys[i - 1].Time;
                if (span <= 0f) return keys[i].Value;

                var t = (time - keys[i - 1].Time) / span;
                return keys[i - 1].Value + (keys[i].Value - keys[i - 1].Value) * t;
            }

            return keys[^1].Value;
        }

        private static float AlphaAt(EffectColorGradientOverLife gradient, float time)
        {
            var keys = gradient.Gradient.AlphaKeys;
            if (keys == null || keys.Count == 0) return ValueRules.MaxColor;

            var closest = keys[0];
            foreach (var key in keys)
                if (Math.Abs(key.Time - time) < Math.Abs(closest.Time - time))
                    closest = key;

            return closest.Alpha;
        }

        /// <summary> The keyframe standing at that time, created if the track has none there. </summary>
        private static VgdKeyframe KeyframeAt(VgdTrack track, float time)
        {
            foreach (var keyframe in track.Keyframes)
                if (BHSDKMath.Approximately(keyframe.Time, time))
                    return keyframe;

            var created = new VgdKeyframe { Time = time, Values = new List<float>() };
            track.Keyframes.Add(created);
            track.Keyframes.Sort((left, right) => left.Time.CompareTo(right.Time));
            return created;
        }

        private static VgdKeyframe FirstKeyframe(VgdObject target, int index)
        {
            var track = target.GetTrack(index);
            if (track == null) return null;

            if (track.Keyframes.Count == 0)
                track.Keyframes.Add(new VgdKeyframe { Time = 0f, Values = new List<float>() });

            return track.Keyframes[0];
        }

        private static void Write(VgdKeyframe keyframe, int index, float value)
        {
            keyframe.Values ??= new List<float>();
            while (keyframe.Values.Count <= index) keyframe.Values.Add(0f);
            keyframe.Values[index] = value;
        }

        #endregion

        // p_t has to be WRITTEN, and writing nothing is not the same as writing the common value.
        // Afterbeat's own default for the key is "101" - position and rotation inherited, SCALE NOT
        // - while this format inherits all three together and has no way to say otherwise. So an
        // export that omits it hands every parented object over with its scale inheritance switched
        // off, and a hierarchy authored here comes apart the moment any parent in it is not at
        // scale 1.

        /// <summary> Position, scale and rotation all inherited - what a parent means here. </summary>
        public const string FullParentType = "111";

        // The inverse of the importer's OnlyDepth mapping - the one of the four layer modes that is
        // a bijection - so a level that came from Afterbeat under it goes back unchanged, and one
        // authored here lands where the same layer would have drawn.
        //
        // Which BAND a layer belongs to is decided the same way the import decided it, and the
        // player line is what splits them: this format's avatar occupies (0, 1), the source game
        // draws its own in front of every Default object and behind every AbovePlayer one, so layer
        // 1 and up is AbovePlayer, 0 down to -60 is the Default band with depth 0 at its top, and
        // everything under that is Background. Clamped at both ends, since this format has 2001
        // layers to spend and Afterbeat has 183 - a level using the whole range loses ordering at
        // the extremes rather than everywhere.
        private static void ApplyDrawOrder(int effectiveLayer, VgdObject target)
        {
            const int span = ABLayerMap.DepthSpan;

            var band = effectiveLayer >= ValueRules.FirstLayerAbovePlayer ? ABRenderLayer.AbovePlayer
                : effectiveLayer >= ValueRules.LastLayerBehindPlayer - span + 1 ? ABRenderLayer.Default
                : ABRenderLayer.Background;

            // The layer depth 0 of this band sits on; every deeper depth steps one further down.
            var frontmost = band switch
            {
                ABRenderLayer.AbovePlayer => VgdObject.MaxDepth + ValueRules.FirstLayerAbovePlayer,
                ABRenderLayer.Default => ValueRules.LastLayerBehindPlayer,
                _ => ValueRules.LastLayerBehindPlayer - span,
            };

            target.RenderLayer = (int)band;
            target.Depth = Math.Clamp(frontmost - effectiveLayer,
                VgdObject.MinDepth, VgdObject.MaxDepth);

            ApplyEditorRow(target);
        }

        // Afterbeat's editor layer and bin are the ROW an object gets on its timeline, and an
        // exported level used to write neither - so every object in it landed on row zero of layer
        // zero, i.e. a few thousand clips stacked into one line that cannot be worked with. They
        // are bookkeeping in the sense that nothing about playback reads them, and the exact
        // opposite of bookkeeping in the sense that they decide whether the file can be edited at
        // all once it is over there.
        //
        // The row is derived from DEPTH rather than from anything of our own, for two reasons: it
        // is the number that survived the conversion (this format's Layer did not - it was just
        // spent on the depth), and it makes the import's OnlyEditor mode the inverse of this
        // export, exactly as OnlyDepth already is. Depth runs 0-60 and the source editor allows six
        // layers of fifteen bins, which is 90 rows for 61 depths - so every depth gets a row of its
        // own with room to spare, rather than several sharing one.
        // COUNTED FROM LAYER 1, NOT FROM 0, and that is what makes the inverse exact rather than
        // nearly exact. ABLayerMap.ToEditorIndex reads an editor layer of 0 as 1 - an object nobody
        // sorted belongs with the ones nobody sorted - so rows written on layer 0 and on layer 1
        // decode to the same index, and the first thirty depths came back in fifteen rows.
        private static void ApplyEditorRow(VgdObject target)
        {
            var row = Math.Clamp(target.Depth, VgdObject.MinDepth, VgdObject.MaxDepth);

            target.Editor.Layer = Math.Clamp(row / EditorBinsPerLayer + FirstEditorLayer,
                FirstEditorLayer, MaxEditorLayer);
            target.Editor.Bin = Math.Clamp(row % EditorBinsPerLayer, MinEditorBin, MaxEditorBin);
        }

        /// <summary> Rows the source editor shows per layer - its own BeatmapObject.EditorData.Bin
        /// clamps to 0-14. </summary>
        public const int EditorBinsPerLayer = 15;

        /// <summary> First timeline bin the source editor offers. </summary>
        public const int MinEditorBin = 0;

        /// <summary> Last one - a row written past it is not reachable in that editor. </summary>
        public const int MaxEditorBin = 14;

        /// <summary> The source editor's own layer clamp, 0-5. </summary>
        public const int MinEditorLayer = 0;

        /// <summary> Last editor layer it offers. </summary>
        public const int MaxEditorLayer = 5;

        /// <summary> The lowest layer an export writes - see <see cref="ApplyEditorRow"/> for why it
        /// is not <see cref="MinEditorLayer"/>. </summary>
        public const int FirstEditorLayer = 1;

        private static void ApplyShape(RectObject source, VgdObject target,
            ABExportContext context, string path)
        {
            switch (source)
            {
                case TextObject text:
                    target.Shape = (int)ABShape.Text;
                    target.ObjectType = (int)ABObjectType.NoHit;
                    target.Text = ReadText(text, context, path);
                    return;

                case ShapeObject shape:
                {
                    // An object that draws nothing is an empty over there, and an empty cannot hit
                    // the player. That pair is unrepresentable rather than approximable: an
                    // invisible hitbox in that engine would have to be a fully transparent object,
                    // and a transparent object is exactly what its damage check refuses (see
                    // ABOpacityHitGate). Exporting one as a Square instead would put a visible
                    // square in a level that never had one.
                    if (!shape.ShapeId.IsEnabled())
                    {
                        target.ObjectType = (int)ABObjectType.AlphaEmpty;
                        if (shape.ColliderId.IsEnabled())
                            context.Report.Dropped("collider_invisible",
                                "Afterbeat cannot be hit by an object it does not draw; invisible hitboxes exported as empty objects and no longer hurt the player.",
                                path);
                        return;
                    }

                    // A preset pair first, a CUSTOM POLYGON second, and only then a Square. The
                    // middle branch is what makes the round trip whole: the built-in library is a
                    // packed set of parameters, so anything without a preset name over there can
                    // still be written as the five numbers that mean the same thing - and the source
                    // game reads csp in preference to its own shape table, so the family the pair
                    // names is irrelevant. Only a level's OWN geometry has nothing to write.
                    var (main, option) = ABShapeMap.Export(shape.ShapeId);
                    if (ABShapeMap.ReverseTableHas(shape.ShapeId))
                    {
                        target.Shape = main;
                        target.ShapeOption = option;
                    }
                    else if (ABShapeMap.TryExportCustom(shape.ShapeId, out var customMain,
                                 out var customOption, out var customShape))
                    {
                        target.Shape = customMain;
                        target.ShapeOption = customOption;
                        target.CustomShape = customShape;

                        if (ABShapeMap.IsSecondQuarter(shape.ShapeId))
                            context.Report.Approximated("shape_quarter_variant",
                                "Afterbeat cuts a quarter one way only; lower-right quarters export as upper-right ones and need turning by hand.",
                                path);
                    }
                    else
                    {
                        target.Shape = 0;
                        target.ShapeOption = 0;
                        context.Report.Approximated("shape_not_representable",
                            "Afterbeat has no name for level-authored geometry; those objects export as a Square.",
                            path);
                    }

                    // Normal rather than Hit: a real level writes 0 for its hitting objects, and
                    // the documented 4 is Solid in the numbering those files use - an object that
                    // pushes the player rather than one that merely hurts them.
                    target.ObjectType = shape.ColliderId.IsEnabled()
                        ? (int)ABObjectType.Normal
                        : (int)ABObjectType.NoHit;

                    if (shape.ColliderId.IsEnabled() && shape.ColliderId != shape.ShapeId)
                        context.Report.Approximated("collider_differs",
                            "Afterbeat hits objects with the shape they are drawn as; objects whose collider differs from their shape export with the drawn one.",
                            path);
                    return;
                }

                default:
                    // AlphaEmpty (6) rather than Empty (3): both mean "no geometry" over there, and
                    // 6 is the one real levels are written with - 3 appears in no file measured.
                    //
                    // A PREFAB PLACEMENT LANDS HERE TOO, and as an empty rather than as nothing at
                    // all. Its content is exported as the objects it materialized into (see the
                    // level exporter's ExportPlacements - writing prefab_objects as well would hand
                    // Afterbeat a second copy to expand), but those copies hang OFF the placement
                    // and it carries the position, scale and rotation the whole subtree sits at.
                    // Dropping the object while its children still name it as their parent left
                    // that reference dangling, which the source game reads as a root - so every
                    // placed prefab in an exported level snapped back to the origin.
                    target.ObjectType = (int)ABObjectType.AlphaEmpty;
                    return;
            }
        }

        private static string ReadText(TextObject source, ABExportContext context, string path)
            => ApplyFontTag(source, ReadTextValue(source, context, path));

        private static string ReadTextValue(TextObject source, ABExportContext context, string path)
        {
            switch (source.Text)
            {
                case StringValue literal:
                    return literal.Value ?? string.Empty;
                case null:
                    return string.Empty;
                default:
                    context.Report.Approximated("text_localized",
                        "Afterbeat text is a single string; localized text exports as its default language.",
                        path);
                    return string.Empty;
            }
        }

        // The typeface is a FIELD here and a tag inside the string there, so the way back is to
        // write the tag - the same one ABObjectImporter strips, naming whichever of Afterbeat's own
        // ten fonts is nearest (ABFontMap.TryExport). Only the object's own default is written
        // silently and without a tag: it pairs with LiberationSans, which is what the source game
        // draws an untagged string in anyway, so tagging it would put markup into every exported
        // text to say what was already true.
        //
        // The tag opens and never closes, which is what makes it the whole string's: it is prepended
        // rather than wrapped, so nothing inside - a <noparse> run included - has to be understood
        // to place it correctly.

        /// <summary> Prepends the <c>font</c> tag naming what this text is set in, unchanged when
        /// there is nothing to name or nothing to name it on. </summary>
        private static string ApplyFontTag(TextObject source, string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (source.FontResourceId == FontResourceId.Default) return value;

            return ABFontMap.TryExport(source.FontResourceId, out var name)
                ? $"<font=\"{name}\">{value}"
                : value;
        }

        // Only the pivot's FIRST keyframe can cross - Afterbeat's origin is a static field, not a
        // track. Anything animating its pivot loses the animation, which is reported.
        //
        // Both of the corrections below are inverses of something ABObjectImporter.ApplyPivot does,
        // and writing a plain `0.5 - pivot` for everything got both of them wrong in the same
        // direction it once got the import wrong:
        //
        //   TEXT MEASURES IT THE OTHER WAY. A shape's origin moves its mesh by +origin, so its
        //   pivot is 0.5 - origin; a text's origin picks a TextMeshPro alignment and its pivot is
        //   0.5 + origin. Subtracting for both mirrored every off-centre text on the way out.
        //
        //   A SHAPE'S OWN GEOMETRY OFFSET IS NOT AN ORIGIN. The import folds it into the pivot
        //   because this format has nowhere else to put it, but over there it is intrinsic to the
        //   shape - so writing it back into the origin applies it twice, and a Triangle imported
        //   with no origin at all came back displaced by its own centroid.
        private static void ApplyOrigin(RectObject source, VgdObject target)
        {
            if (source.Pivots == null || source.Pivots.Count == 0) return;

            var pivot = source.Pivots[0]?.Value;
            if (pivot is not Vector2Value literal) return;

            if (source is TextObject)
            {
                target.Origin = new VgdVector2(
                    literal.X - Import.ABObjectImporter.DefaultPivot,
                    literal.Y - Import.ABObjectImporter.DefaultPivot);
                return;
            }

            // ApplyShape has already run, so this is the pair actually being written rather than
            // whatever the object came in as.
            var shapeOffsetY = ABShapeMap.GetPivotOffsetY(target.Shape, target.ShapeOption);

            target.Origin = new VgdVector2(
                Import.ABObjectImporter.DefaultPivot - literal.X,
                Import.ABObjectImporter.DefaultPivot - literal.Y - shapeOffsetY);
        }

        #region Tracks

        private static void ExportPositions(RectObject source, VgdObject target, int framerate,
            ABExportContext context, string path)
        {
            var track = target.GetTrack(VgdObject.TrackIndex.Move);
            if (track == null || source.Positions == null) return;

            foreach (var key in Sorted(source.Positions))
            {
                var (x, y) = ABValueMap.ExportVector(key.Pos, context.Report, path);
                track.Keyframes.Add(NewKeyframe(key.Frame, key.Ease, framerate, x, y));
            }
        }

        private static void ExportSizes(RectObject source, VgdObject target, int framerate,
            ABExportContext context, string path)
        {
            var track = target.GetTrack(VgdObject.TrackIndex.Scale);
            if (track == null) return;

            // Text goes the way it came in: over there a text object's scale is the only thing
            // sizing its glyphs, while here that is Scale and Size is only the block they lay out
            // in - a block this converter estimated on the way in and that means nothing on the
            // far side. See ABParticleMap.ApplyTextSize.
            //
            // A PREFAB PLACEMENT reads the same way for a different reason: it has no extent of its
            // own at all, and its scale is the multiplier its whole materialized subtree sits
            // inside - which over there is exactly what a parent's scale track is.
            var fromScales = source is TextObject or PrefabObject;
            var exported = fromScales ? source.Scales : source.Sizes;
            var dropped = fromScales ? source.Sizes : source.Scales;
            if (exported == null) return;

            foreach (var key in Sorted(exported))
            {
                var (x, y) = ABValueMap.ExportVector(key.Scale, context.Report, path);
                track.Keyframes.Add(NewKeyframe(key.Frame, key.Ease, framerate, x, y));
            }

            // AN EMPTY SIZE TRACK IS A VALUE, NOT AN ABSENCE, and writing nothing would hand the far
            // side its own default instead. Over there an object without a scale keyframe is one unit
            // square; here it is a group node with no extent at all (BH.Shared.defaults.size), which
            // is what a bare rect and a prefab placement are. So it goes out as the zero it means,
            // and the object draws nothing on either side. Text and placements are exported from
            // Scales, whose empty-track value is still one, so they are left alone.
            if (!fromScales && exported.Count == 0)
                track.Keyframes.Add(NewKeyframe(FrameRules.MinFrame, EaseType.Linear, framerate, 0f, 0f));

            if (!fromScales && dropped is { Count: > 0 })
                context.Report.Approximated("scale_track_dropped",
                    "Afterbeat has one size per object; this format's separate scale multiplier has no field there and is not exported.",
                    path);
        }

        // The one track that cannot be exported keyframe by keyframe - see this file's header.
        private static void ExportRotations(RectObject source, VgdObject target, int framerate,
            ABExportContext context, string path)
        {
            var track = target.GetTrack(VgdObject.TrackIndex.Rotate);
            if (track == null || source.Rotations == null) return;

            var previous = 0f;
            foreach (var key in Sorted(source.Rotations))
            {
                var radians = ABValueMap.ExportFloat(key.Angle, context.Report, path);
                var delta = ABValueMap.DifferentiateRotation(radians, ref previous);
                track.Keyframes.Add(NewKeyframe(key.Frame, key.Ease, framerate, delta));
            }
        }

        private static void ExportColors(RectObject source, VgdObject target,
            ABExportContext context, string path)
        {
            var track = target.GetTrack(VgdObject.TrackIndex.Color);
            if (track == null) return;

            var framerate = context.Options.Framerate;

            switch (source)
            {
                case ShapeObject shape when shape.Colors != null:
                {
                    var gradient = false;
                    var rotation = ABGradientMap.HorizontalRotation;
                    foreach (var key in SortedKeys(shape.Colors))
                    {
                        var exported = ABColorMap.ExportKey(key, ABPalette.Objects,
                            context.ReferenceTheme, context.Report, path);
                        if (exported.IsGradient && !gradient)
                            rotation = ABColorMap.ExportRotation(key);
                        gradient |= exported.IsGradient;
                        track.Keyframes.Add(NewKeyframe(key.Frame, key.Ease, framerate,
                            exported.StartIndex, ToSourceOpacity(exported.Opacity), exported.EndIndex));
                    }

                    // The type lives on the object, so it is decided once the whole track has been
                    // read - a gradient anywhere in it means the object has one. So does the axis,
                    // which is why it comes from the FIRST key that carried one: a track whose keys
                    // disagree about direction is a thing this format can hold and that one cannot.
                    if (!gradient) return;
                    target.GradientType = (int)ABGradientType.Linear;
                    target.GradientRotation = rotation;
                    target.GradientScale = ABGradientMap.NeutralScale;
                    return;
                }

                case TextObject text when text.Colors != null:
                    foreach (var key in Sorted(text.Colors))
                    {
                        var (index, opacity) = ABColorMap.Export(key.Value,
                            ABPalette.Objects, context.ReferenceTheme, context.Report, path);
                        track.Keyframes.Add(NewKeyframe(key.Frame, key.Ease, framerate,
                            index, ToSourceOpacity(opacity), index));
                    }

                    return;
            }
        }

        /// <summary> Alpha back to the percentage Afterbeat stores - see the importer's own
        /// OpacityScale, which is the half of this pair that actually bit. </summary>
        private static float ToSourceOpacity(float alpha)
            => alpha * Import.ABObjectImporter.OpacityScale;

        #endregion

        #region Shared

        private static VgdKeyframe NewKeyframe(int localFrame, EaseType ease,
            int framerate, params float[] values)
            => new()
            {
                Time = ABTimeMap.ToSeconds(localFrame, framerate),
                Ease = ABEaseMap.Export(ease),
                Values = new List<float>(values),
            };

        // The format guarantees keyframe frames are UNIQUE, not sorted, and both the rotation
        // differentiation and Afterbeat's own reader assume order. Sorting a copy leaves the model
        // alone - an export must not reorder the level it is exporting.
        private static IEnumerable<T> Sorted<T>(List<T> keyframes) where T : Keyframe
        {
            var copy = new List<T>(keyframes);
            copy.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            return copy;
        }

        private static IEnumerable<IColor4X4Key> SortedKeys(List<IColor4X4Key> keyframes)
        {
            var copy = new List<IColor4X4Key>(keyframes);
            copy.Sort((a, b) => a.Frame.CompareTo(b.Frame));
            return copy;
        }

        private static void ReportUnsupported(RectObject source, InteropReport report, string path)
        {
            if (source.AnchorsMin is { Count: > 0 } || source.AnchorsMax is { Count: > 0 })
                report.Dropped("anchors",
                    "Afterbeat objects have no anchors; anchor tracks are not exported.", path);

            if (source.Pivots is { Count: > 1 })
                report.Approximated("pivot_animated",
                    "Afterbeat's object origin is static; an animated pivot exports as its first keyframe.",
                    path);

            if (source is TextObject text)
            {
                if (text.Fillments is { Count: > 0 } || text.Appearings is { Count: > 0 })
                    report.Dropped("text_effects",
                        "Afterbeat has no per-character fill or appearing effects; those tracks are not exported.",
                        path);

                ReportTextSetup(text, report, path);
            }

            if (source is ShapeObject shape && shape.ShaderType != ShaderType.Auto)
                report.Dropped("shader_type",
                    "Afterbeat decides how a shape is drawn on its own; an explicitly opaque or transparent shape exports without that choice.",
                    path);
        }

        // An Afterbeat text object is a string and a colour. Everything this format wraps around one
        // - how big it is, how it is aligned, whether it wraps - has no field there at all. None of
        // it is a conversion that could be got wrong, which is exactly why it used to leave no
        // trace: an author sent a level away and learned about it by looking.
        //
        // THE FONT IS THE ONE THAT CROSSES, because over there it lives inside the string rather
        // than beside it (ApplyFontTag). It still reports, since the tag names the nearest of that
        // game's own ten fonts and never the one this level is actually set in.
        private static void ReportTextSetup(TextObject source, InteropReport report, string path)
        {
            if (source.FontResourceId.IsUserDefined())
                report.Dropped("text_font",
                    "Afterbeat can only name the fonts it ships itself; text set in a font this level brings along exports in the source game's default one.",
                    path);
            else if (source.FontResourceId != FontResourceId.Default
                     && ABFontMap.TryExport(source.FontResourceId, out _))
                report.Approximated("text_font_tag",
                    "Afterbeat has no font field - the typeface is a <font> tag inside the string - so the text was prefixed with one naming the nearest of the fonts that game ships.",
                    path);
            else if (source.FontResourceId != FontResourceId.Default)
                report.Dropped("text_font",
                    "Afterbeat ships nothing resembling the font this text is set in; it exports in that game's default one.",
                    path);

            if (source.FontSizes is { Count: > 0 })
                report.Dropped("text_font_size",
                    "Afterbeat text has no font size of its own; this level's size track is not exported.", path);

            if (!string.IsNullOrEmpty(source.AppearingMask))
                report.Dropped("text_appearing_mask",
                    "Afterbeat has no appearing mask on text; it is not exported.", path);

            if (source.WordWrap != TextRules.WordWrap_Default
                || source.HorizontalAlignment != TextRules.HorizontalAlignment_Default
                || source.VerticalAlignment != TextRules.VerticalAlignment_Default)
                report.Dropped("text_layout",
                    "Afterbeat text is centred and never wraps; this level's alignment and word wrap are not exported.",
                    path);
        }

        #endregion
    }
}