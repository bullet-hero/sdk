using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Interop.AfterBeat.Import
{
    // Reads Afterbeat's objects into this format's. Four conversions in here are the ones that go
    // wrong invisibly, so they are named up front:
    //
    // 1. A keyframe's time is relative to its object's start in BOTH formats, so it crosses
    //    unchanged. It looks like the sort of thing that needs converting, and converting it is
    //    what produces objects that spawn correctly and then never move.
    // 2. Rotation is degrees and RELATIVE per keyframe there, radians and ABSOLUTE here. Both
    //    halves are done by ABValueMap; either one missed produces a level that spins.
    // 3. Depth is absolute there and Layer is parent-relative here, so a child's layer is its own
    //    effective layer minus its parent's. Reading obj.d straight into Layer draws a whole
    //    hierarchy at the wrong depth as soon as anything is parented.
    // 4. Afterbeat's "scale" is a width/height in world units, which is this format's SIZE, not its
    //    Scale - except on TEXT, where it is the only thing sizing the glyphs and therefore crosses
    //    as Scale instead, the Size being the block the glyphs lay out in. See ApplyTextSize.
    //
    // Import is TWO passes over the object list on purpose - see ABImportContext.

    /// <summary> One Afterbeat object into one <see cref="RectObject"/>. </summary>
    public static class ABObjectImporter
    {
        /// <summary> Mints ids and effective layers for every source object, then fills them in.
        /// Both first, because a child may be written before its parent - and because two of the
        /// four layer modes cannot answer for one object without having seen the whole list. </summary>
        public static void ImportAll(IReadOnlyList<VgdObject> sources, ABImportContext context,
            string pathPrefix)
        {
            if (sources == null || context?.Scope?.Objects == null) return;

            var layers = ABLayerMap.Resolve(sources, context.Options, context.Report, pathPrefix,
                context.LayerPlan);

            // A packed scope registered its own range when it was packed; the depth plan's range
            // would only widen it with rows nothing occupies.
            if (context.PackedLayers == null) context.RegisterContentLayers(layers.Lowest, layers.Highest);

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                context.Mint(source.Id);
                context.SetEffectiveLayer(source.Id, layers.Layers[i]);
            }

            var byId = IndexById(sources);
            ResolveScaleTargets(sources, byId, context, pathPrefix);
            ResolveShearFits(sources, byId, context);

            for (var i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                var path = $"{pathPrefix}[{i}]";
                var imported = Import(source, context, layers.Layers[i], path);
                if (imported == null) continue;

                ApplyParentOrigin(source, imported, byId);
                ReportShear(source, byId, context.Report, path);
                context.Scope.Objects[imported.ObjectId] = imported;

                // After the object is in the scope, because it may add collider children beside it -
                // see ABOpacityHitGate for the rule (an object below full opacity cannot hurt the
                // player over there) and for why it cannot be expressed on the object itself.
                ABOpacityHitGate.Apply(source, imported, context, path);
            }
        }

        // A QUARTER TURN UNDER A SQUASHED PARENT IS NOT SHEAR, and reading it as one is how the
        // worst objects in a converted level got their worst numbers. Afterbeat composes matrices,
        // so a child's linear map is R(rp)·S(sp)·R(rc)·S(sc), and S does not commute with R unless
        // the scale is uniform. At exactly a quarter turn, though, it ALMOST does:
        //
        //     S(x, y)·R(90) == R(90)·S(y, x)
        //
        // The product is still a plain rotation and scale - the parent's two scale components have
        // simply traded places. Nothing is skewed and nothing is unrepresentable; there is an exact
        // answer, and composing in this format's own order (rotate by the sum, scale by the product)
        // reaches the wrong one. On a parent squashed 35:1 that is not a subtle error: measured on a
        // real level, one object came out 1710 by 3.18 where it should have been 47.7 by 114.
        //
        // EVERY OTHER ANGLE IS GENUINELY SKEWED, AND THAT IS STILL NOT A REASON TO LEAVE IT ALONE.
        // The quarter turns used to be the only case handled, because they are the only case with
        // an EXACT answer; but a parallelogram this format cannot hold still has a nearest
        // rectangle, and reaching for it is strictly better than composing the wrong one. ABLinearFit
        // solves that in closed form and reduces to the axis trade above wherever the trade applies,
        // so this pass no longer has an angle table at all - it hands the fit two numbers and writes
        // down what comes back.
        //
        // WHAT THE FIT IS ALLOWED TO CHANGE depends on what else reads it. The scale half is always
        // safe: it reaches the object's own extent, and its children's through a Scales track that
        // Afterbeat's own parenting propagates identically. The ANGLE half is not - this format
        // rotates a child's offset by its parent's rotation, so moving a parent's angle swings the
        // whole subtree, and a non-centred pivot swings the object's own mesh around itself. So the
        // angle is only fitted on an object that is nobody's parent and whose pivot is centred;
        // everything else keeps its rotation and takes the scale.
        //
        // Both halves need the correction to be a CONSTANT, which is why an animated parent scale or
        // an animated child rotation falls through to the report instead. A child whose own scale is
        // animated keeps the angle it was authored at as well: the scale-only fit does not read the
        // child's scale, but the free one does, and one angle cannot serve a changing aspect ratio.
        private static void ResolveShearFits(IReadOnlyList<VgdObject> sources,
            IReadOnlyDictionary<string, VgdObject> byId, ABImportContext context)
        {
            var parents = CollectParentIds(sources);

            foreach (var source in sources)
            {
                if (source == null || string.IsNullOrEmpty(source.Id)) continue;
                if (string.IsNullOrEmpty(source.ParentId)) continue;
                if (!InheritsScale(source)) continue;
                if (!byId.TryGetValue(source.ParentId, out var parent) || parent == null) continue;

                if (!TryGetConstantScale(parent, out var x, out var y)) continue;
                if (Math.Abs(x) < MinCompensableScale || Math.Abs(y) < MinCompensableScale) continue;
                if (Math.Abs(x - y) <= ShearEpsilon) continue;
                if (!TryGetConstantRotation(source, out var degrees)) continue;

                var rotation = Radians(degrees);
                var fit = CanFitRotation(source, parents)
                          && TryGetConstantScale(source, out var childX, out var childY)
                    ? ABLinearFit.Free(x, y, rotation, childX, childY)
                    : ABLinearFit.KeepingRotation(x, y, rotation);

                if (!fit.IsIdentity)
                    context.ShearScales[source.Id] = (fit.ScaleX, fit.ScaleY);

                var offset = fit.Rotation - rotation;
                if (Math.Abs(offset) > float.Epsilon)
                    context.ShearRotations[source.Id] = offset;
            }
        }

        /// <summary> Whether the fit may move this object's own angle: only when nothing hangs off
        /// it to be swung, and nothing but its centre to swing around. </summary>
        private static bool CanFitRotation(VgdObject source, ICollection<string> parents)
        {
            if (parents.Contains(source.Id)) return false;
            if (Math.Abs(source.Origin?.X ?? 0f) > float.Epsilon) return false;
            if (Math.Abs(source.Origin?.Y ?? 0f) > float.Epsilon) return false;

            return Math.Abs(ABShapeMap.GetPivotOffsetY(source.Shape, source.ShapeOption))
                   <= float.Epsilon;
        }

        private static HashSet<string> CollectParentIds(IReadOnlyList<VgdObject> sources)
        {
            var parents = new HashSet<string>();
            foreach (var source in sources)
                if (!string.IsNullOrEmpty(source?.ParentId))
                    parents.Add(source.ParentId);
            return parents;
        }

        private static float Radians(float degrees) => (float)(degrees * Math.PI / 180.0);

        // Afterbeat's rotation keyframes are DELTAS, so a constant rotation is one whose deltas
        // after the first are all zero - not one with a single keyframe.
        private static bool TryGetConstantRotation(VgdObject source, out float degrees)
        {
            degrees = 0f;
            var keyframes = source.Rotate?.Keyframes;
            if (keyframes == null || keyframes.Count == 0) return false;

            for (var i = 0; i < keyframes.Count; i++)
            {
                var value = keyframes[i]?.GetValue(0) ?? 0f;
                if (i == 0) degrees = value;
                else if (Math.Abs(value) > ShearEpsilon) return false;
            }

            return true;
        }

        private static bool TryGetConstantScale(VgdObject source, out float x, out float y)
        {
            x = 1f;
            y = 1f;

            var keyframes = source.Scale?.Keyframes;
            if (keyframes == null || keyframes.Count == 0) return false;

            x = keyframes[0]?.GetValue(0) ?? 1f;
            y = keyframes[0]?.GetValue(1) ?? 1f;

            foreach (var key in keyframes)
            {
                if (Math.Abs((key?.GetValue(0) ?? 0f) - x) > ShearEpsilon) return false;
                if (Math.Abs((key?.GetValue(1) ?? 0f) - y) > ShearEpsilon) return false;
            }

            return true;
        }

        // WHAT IS LEFT AFTER THE FIT IS GENUINE SHEAR. Afterbeat parents with plain Unity
        // transforms, and a non-uniform scale above a rotation SHEARS what is under it - a square
        // under a parent scaled (3, 1) and turned 30 degrees comes out a parallelogram. This format
        // composes a rotation and a per-axis scale without a matrix between them, so it has no shear
        // to give: the same object comes out the nearest rectangle to that parallelogram.
        //
        // The fit above closes the gap as far as it closes; what is reported is the RESIDUE, which
        // is why this asks ABLinearFit rather than counting angles. An exactly representable
        // composition - every quarter turn, every straight angle, every uniform parent - leaves no
        // residue and says nothing, and a hop whose residue is below a hundredth of the map it came
        // from is a rectangle staying a rectangle. Both used to be reported, which buried the
        // objects that really are skewed among the ones that were never wrong.
        //
        // A correction that could not be computed at all (an animated parent scale, an animated
        // child rotation) is reported whatever its angle: nothing was fitted there.
        private static void ReportShear(VgdObject source, IReadOnlyDictionary<string, VgdObject> byId,
            InteropReport report, string path)
        {
            if (string.IsNullOrEmpty(source.ParentId) || !InheritsScale(source)) return;
            if (!byId.TryGetValue(source.ParentId, out var parent) || parent == null) return;
            if (!IsNonUniformlyScaled(parent) || !IsRotated(source)) return;

            if (TryGetConstantScale(parent, out var x, out var y)
                && TryGetConstantRotation(source, out var degrees)
                && ABLinearFit.Shear(x, y, Radians(degrees)) <= ABLinearFit.Epsilon)
                return;

            report.Approximated("parent_scale_shear",
                "Afterbeat skews a rotated object sitting under a non-uniformly scaled parent; this format has no skew to give, so the object is fitted to the nearest rotation and scale it can hold. The wider apart the parent's two scales are, the more of the skew is lost.",
                path);
        }

        private static bool IsNonUniformlyScaled(VgdObject source)
        {
            var keyframes = source.Scale?.Keyframes;
            if (keyframes == null) return false;

            foreach (var key in keyframes)
            {
                if (key == null) continue;
                if (Math.Abs(key.GetValue(0) - key.GetValue(1)) > ShearEpsilon) return true;
            }

            return false;
        }

        private static bool IsRotated(VgdObject source)
        {
            var keyframes = source.Rotate?.Keyframes;
            if (keyframes == null) return false;

            foreach (var key in keyframes)
            {
                if (key == null) continue;
                if (Math.Abs(key.GetValue(0)) > ShearEpsilon) return true;
            }

            return false;
        }

        /// <summary> Below this a scale counts as uniform and a rotation as none - a hundredth of a
        /// degree, or of a world unit, shears nothing anybody can see. </summary>
        public const float ShearEpsilon = 0.01f;

        private static Dictionary<string, VgdObject> IndexById(IReadOnlyList<VgdObject> sources)
        {
            var byId = new Dictionary<string, VgdObject>(sources.Count);
            foreach (var source in sources)
                if (source != null && !string.IsNullOrEmpty(source.Id))
                    byId[source.Id] = source;
            return byId;
        }

        #region Scale inheritance

        // The whole of ABImportContext.ScaleTarget's header applies here; this is the pass that
        // fills it. Two things are decided per object:
        //
        //   which FIELD its own scale track goes into, read off its CHILDREN's masks - the mask is
        //   a property of the child in the source format, so an object with no children has no
        //   opinion and keeps the plain Size;
        //
        //   and, when its children disagree with each other, which of them has to be COMPENSATED,
        //   since one field cannot serve both. The majority side keeps the free, exact, animation-
        //   proof mapping and the minority is divided or multiplied by the parent's own scale.
        //   That bake is exact only while the parent's scale is CONSTANT, which is what 40 of the
        //   44 mixed parents in the levels this was measured against are; the rest are reported,
        //   because there is no per-frame compensation this format can hold without resampling
        //   both objects' tracks onto each other.
        private static void ResolveScaleTargets(IReadOnlyList<VgdObject> sources,
            IReadOnlyDictionary<string, VgdObject> byId, ABImportContext context, string path)
        {
            var inheriting = new Dictionary<string, int>();
            var total = new Dictionary<string, int>();

            foreach (var source in sources)
            {
                if (source?.ParentId == null || string.IsNullOrEmpty(source.ParentId)) continue;
                if (!byId.ContainsKey(source.ParentId)) continue;

                total.TryGetValue(source.ParentId, out var count);
                total[source.ParentId] = count + 1;

                if (!InheritsScale(source)) continue;
                inheriting.TryGetValue(source.ParentId, out var inherited);
                inheriting[source.ParentId] = inherited + 1;
            }

            foreach (var pair in total)
            {
                inheriting.TryGetValue(pair.Key, out var inherited);
                var target = inherited * 2 >= pair.Value
                    ? ABImportContext.ScaleTarget.Scale
                    : ABImportContext.ScaleTarget.Size;

                context.ScaleTargets[pair.Key] = target;

                var minority = target == ABImportContext.ScaleTarget.Scale
                    ? pair.Value - inherited
                    : inherited;
                if (minority > 0)
                    RecordCompensation(pair.Key, byId, sources, target, context, path);
            }
        }

        private static void RecordCompensation(string parentId,
            IReadOnlyDictionary<string, VgdObject> byId, IReadOnlyList<VgdObject> sources,
            ABImportContext.ScaleTarget target, ABImportContext context, string path)
        {
            if (!byId.TryGetValue(parentId, out var parent)) return;

            var scaleTrack = parent.Scale;
            var animated = scaleTrack?.Keyframes is { Count: > 1 };

            if (animated)
            {
                context.Report.Approximated("scale_inheritance_mixed_animated",
                    "Some objects have children that disagree about inheriting their scale, and their own scale is animated; this format cannot express both at once, so the disagreeing children follow the majority.",
                    path);
                return;
            }

            var first = scaleTrack?.Keyframes is { Count: > 0 } ? scaleTrack.Keyframes[0] : null;
            var x = first?.GetValue(0) ?? 1f;
            var y = first?.GetValue(1) ?? 1f;
            if (Math.Abs(x) < MinCompensableScale || Math.Abs(y) < MinCompensableScale) return;

            // Divide when the parent's scale now reaches the child and should not; multiply when it
            // no longer reaches one that expected it.
            var factorX = target == ABImportContext.ScaleTarget.Scale ? 1f / x : x;
            var factorY = target == ABImportContext.ScaleTarget.Scale ? 1f / y : y;
            if (Math.Abs(factorX - 1f) < float.Epsilon && Math.Abs(factorY - 1f) < float.Epsilon)
                return;

            foreach (var child in sources)
            {
                if (child?.ParentId != parentId) continue;
                var inherits = InheritsScale(child);
                var isMinority = target == ABImportContext.ScaleTarget.Scale ? !inherits : inherits;
                if (isMinority && !string.IsNullOrEmpty(child.Id))
                    context.ScaleCompensations[child.Id] = (factorX, factorY);
            }
        }

        /// <summary> Below this a parent's scale cannot be divided out of a child without
        /// producing an infinity - a zero-scaled parent draws nothing anyway. </summary>
        public const float MinCompensableScale = 1e-4f;

        /// <summary> Positional meaning of the three characters of p_t. </summary>
        public static class ParentTypeIndex
        {
            /// <summary> Character of the parent-type string that says position is inherited. </summary>
            public const int Position = 0;

            /// <summary> The one that says scale is. </summary>
            public const int Scale = 1;

            /// <summary> The one that says rotation is. </summary>
            public const int Rotation = 2;
        }

        /// <summary> Whether one source object inherits its parent's scale. The mask lives on the
        /// CHILD, and a document that wrote none means the format's own default - which does not
        /// inherit it. </summary>
        public static bool InheritsScale(VgdObject source) => HasParentBit(source, ParentTypeIndex.Scale);

        private static bool HasParentBit(VgdObject source, int index)
        {
            var mask = source?.ParentType;
            if (string.IsNullOrEmpty(mask)) mask = VgdObject.DefaultParentType;
            return index < mask.Length && mask[index] == '1';
        }

        #endregion

        /// <summary> One object, at an effective layer <see cref="ABLayerMap"/> already
        /// resolved for the whole list. Returns null only when the source is null. </summary>
        public static RectObject Import(VgdObject source, ABImportContext context,
            int effectiveLayer, string path)
        {
            if (source == null) return null;

            var report = context.Report;
            var framerate = context.Options.Framerate;

            // Resolved BEFORE the target is built, because an emitter's definition carries the
            // frame its emission stops on and that frame is this span's own duration.
            var span = ABTimeMap.ResolveSpan(source, framerate, report, path,
                context.AbsoluteTimeBase);

            var target = CreateTarget(source, span, context, path);
            target.ObjectId = context.Mint(source.Id);
            target.Name = context.Options.KeepObjectNames
                ? (string.IsNullOrEmpty(source.Name) ? source.Id ?? string.Empty : source.Name)
                : string.Empty;
            target.Active = true;
            target.Span = ResolveEmitterSpan(source, target, span, framerate);

            // A mask of "000" inherits nothing at all, which this format cannot say of a child -
            // but CAN say of a root, exactly and with nothing baked. So such an object is imported
            // as one, keeping its own keyframes untouched. It only loses the parent's own lifetime
            // bounding it, which is bookkeeping rather than motion.
            target.ParentObjectId = InheritsNothing(source)
                ? ObjectId.Null
                : context.ResolveParent(source.ParentId, path);

            ApplyLayer(source, target, context, effectiveLayer);
            ApplyPivot(source, target, report, path);
            ReportParenting(source, report, path);

            ImportPositions(source, target, framerate, context, path);
            ImportScales(source, target, framerate, context, path);
            ApplyTextSize(target, report, path);
            ImportRotations(source, target, framerate, context, path);
            ImportColors(source, target, context, path);

            return target;
        }

        /// <summary> Whether a source object's mask inherits none of the three channels, which is
        /// the one mask this format has an exact answer for. </summary>
        public static bool InheritsNothing(VgdObject source)
            => !string.IsNullOrEmpty(source?.ParentId)
               && !HasParentBit(source, ParentTypeIndex.Position)
               && !HasParentBit(source, ParentTypeIndex.Scale)
               && !HasParentBit(source, ParentTypeIndex.Rotation);

        #region Type

        // Hit and No Hit are the same object with and without a collider, which is exactly how this
        // format expresses the distinction. Empty carries no geometry at all and becomes the base
        // type - it is a transform other objects hang off, and giving it a shape would draw
        // something the source level never drew.
        private static RectObject CreateTarget(VgdObject source, FrameSpan span,
            ABImportContext context, string path)
        {
            // Before the text branch, exactly as the source game orders it: InitVisual answers
            // IsParticles first and returns, so an emitter carrying a text shape is still an
            // emitter over there.
            if (ABParticleMap.IsEmitter(source))
                return CreateEffect(source, span, context, path);

            if (ABShapeMap.IsText(source.Shape))
                return CreateText(source, context, path);

            if (IsEmpty(source.ObjectType))
                return new RectObject();

            var shapeId = ABShapeMap.Import(source.Shape, source.ShapeOption,
                context.Shapes, context.Report, path, source);

            return new ShapeObject
            {
                ShapeId = shapeId,
                ColliderId = IsHit(source.ObjectType, context.Report, path) ? shapeId : ShapeId.Null,
                ShaderType = ShaderType.Auto,
            };
        }

        // An unknown type is read as Hit rather than as No Hit, which is the direction that fails
        // safely: an object that should not have hurt the player is a level that is too hard to
        // beat, while a missing collider is a level that cannot be lost - and the second one is
        // silent, since nothing on screen looks any different.
        private static bool IsHit(int objectType, InteropReport report, string path)
        {
            switch ((ABObjectType)objectType)
            {
                case ABObjectType.Normal:
                case ABObjectType.Hit:
                    return true;
                case ABObjectType.Helper:
                case ABObjectType.Decoration:
                case ABObjectType.NoHit:
                    return false;
                // Unreachable from CreateTarget, which answers an emitter before it gets here -
                // kept so that reaching it some other way still cannot hand a collider to
                // something the source game never let hit the player.
                case ABObjectType.Particles:
                    return false;
                default:
                    report.Approximated("object_type_unknown",
                        $"Object type {objectType} is not one this converter knows; those objects were imported as ordinary hitting objects.",
                        path);
                    return true;
            }
        }

        private static bool IsEmpty(int objectType)
            => (ABObjectType)objectType is ABObjectType.Empty or ABObjectType.AlphaEmpty;

        #endregion

        #region Particles

        // AN AFTERBEAT EMITTER DRAWS NO SHAPE OF ITS OWN. InitVisual spawns the particle prefab and
        // returns, and the object's (shape, shapeOption) is resolved into the PARTICLE RENDERER's
        // mesh rather than into a quad of its own. So this replaces the ShapeObject rather than
        // standing beside one - keeping a shape would keep drawing something the source level never
        // drew - and the collider goes with it, which an EffectObject has no field for anyway.

        /// <summary> One source emitter as an effect placement, its definition landing in the
        /// level's own effect resources. </summary>
        private static RectObject CreateEffect(VgdObject source, FrameSpan span,
            ABImportContext context, string path)
        {
            var settings = ABParticleMap.TryRead(source).Value;
            var report = context.Report;

            report.Approximated("object_type_particles",
                "Afterbeat particle emitters were imported as effects: the emission rate, the particle mesh, its lifetime and the emitter volume cross, while world-space emission, the per-particle velocity curve and an animated emitter volume have no counterpart here.",
                path);

            ReportParticleLosses(source, settings, report, path);

            var shapeId = ABShapeMap.Import(source.Shape, source.ShapeOption,
                context.Shapes, report, path, source);

            var data = BuildEffectData(source, settings, span, shapeId, context, path);
            var effectId = ABIdMap.ToEffectId(BuildEffectSignature(settings, data, shapeId));
            data.EffectId = effectId;

            // First writer wins, exactly as a synthesized shape does: two emitters authored the same
            // way describe the same effect, so the second one adds nothing.
            if (context.Effects != null && !context.Effects.ContainsKey(effectId))
                context.Effects.Add(effectId, data);

            return new EffectObject { EffectId = effectId };
        }

        private static EffectData BuildEffectData(VgdObject source, ABParticleSettings settings,
            FrameSpan span, ShapeId shapeId, ABImportContext context, string path)
        {
            var data = new EffectData
            {
                Name = ResolveEffectName(source),
            };

            // THE STOP FRAME IS PART OF THE DEFINITION, not of the placement - EffectData is what
            // carries it, and EffectData is shared. So two emitters agreeing on every parameter but
            // living for different lengths are NOT the same effect, which is why the signature ends
            // up naming this too.
            if (!settings.DespawnOnEnd)
            {
                data.HasStopLocalFrame = true;
                data.StopLocalFrame = Math.Clamp(span.FrameDuration,
                    EffectRules.StopLocalFrame_Min, EffectRules.StopLocalFrame_Max);
            }

            // Loop is what gates the graph's constant-rate spawner; without it the definition is a
            // single burst, and ev[4] is a RATE.
            data.Core.Loop = true;
            data.Core.ParticleCount = ToParticleCount(settings.SpawnRatePerSecond);
            data.Core.LifetimeBounds = ToLifetimeBounds(settings.TimelineLength);
            data.Core.ParticleShapeId = shapeId;
            data.Shape = BuildEmitterShape(source, settings);
            data.Forces.StartVelocityMin = BuildStartVelocity(source, settings);
            data.Forces.StartVelocityMax = BuildStartVelocity(source, settings);
            data.Scale = BuildParticleScale(source, settings, context, path);
            data.Angle = BuildParticleAngle(source, settings, context, path);
            data.Color = BuildParticleColor(source, settings, context, path);

            return data;
        }

        /// <summary> Particles per second, as the constant-rate spawner reads it. </summary>
        private static uint ToParticleCount(float spawnRatePerSecond)
        {
            if (spawnRatePerSecond <= 0f) return EffectRules.Core.ParticleCount_Min;

            var rounded = Math.Round(spawnRatePerSecond, MidpointRounding.AwayFromZero);
            if (rounded >= EffectRules.Core.ParticleCount_Max) return EffectRules.Core.ParticleCount_Max;

            return (uint)rounded;
        }

        /// <summary> Every particle lives exactly as long as the object's own animation - Afterbeat
        /// assigns one startLifetime with no spread at all, so both ends are the same number. </summary>
        private static IVector2 ToLifetimeBounds(float timelineLength)
        {
            var lifetime = Math.Clamp(timelineLength,
                EffectRules.Core.LifetimeBounds_Min, EffectRules.Core.LifetimeBounds_Max);

            return new Vector2Value(lifetime, lifetime);
        }

        private static string ResolveEffectName(VgdObject source)
        {
            var name = string.IsNullOrEmpty(source.Name) ? source.Id ?? string.Empty : source.Name;
            return name.Length > ValueRules.MaxEditorName
                ? name[..ValueRules.MaxEditorName]
                : name;
        }

        // The scale track's FIRST keyframe only. EffectShape* fields are values rather than tracks,
        // so an animated emitter volume cannot cross whole - that loss is reported in its own right
        // rather than approximated silently here.

        /// <summary> The volume particles spawn inside, out of ev[8..10] and the scale track. </summary>
        private static IEffectShape BuildEmitterShape(VgdObject source, ABParticleSettings settings)
        {
            var (x, y) = ReadEmitterVolume(source);

            if (settings.EmitterShape == ABParticleEmitterShapeType.Rectangle)
                return new EffectShapeRectangle { Size = new Vector2Value(x, y) };

            var (radius, aspect) = ToEllipse(x, y);

            return new EffectShapeCircle
            {
                Radius = new FloatValue(radius),
                Aspect = new FloatValue(aspect),
                Arc = new FloatValue(ToArcRadians(settings.EmitterArc)),
                Thickness = new FloatValue(settings.EmitterRadiusThickness),
            };
        }

        /// <summary> The two authored extents as this format's radius-plus-ratio pair. A horizontal
        /// extent of nothing has no ratio to describe, so it falls back to a circle. </summary>
        private static (float Radius, float Aspect) ToEllipse(float x, float y)
        {
            if (x < ABParticleMap.MinEmitterExtent)
                return (Math.Max(x, y), EffectRules.Shape.CircleAspect_Default);

            return (x, Math.Clamp(y / x,
                EffectRules.Shape.CircleAspect_Min, EffectRules.Shape.CircleAspect_Max));
        }

        // AN EMITTER'S FOUR TRACKS DO TWO JOBS AT ONCE. Values 0/1 keep their ordinary meaning and
        // animate the EMITTER; values 2/3 are a second, hidden channel describing ONE PARTICLE over
        // its own life. That is the whole feature, and it is why an emitter's tracks are read twice -
        // once by the ordinary track import above, once here.
        //
        // The angle channel crosses DIRECTLY rather than as a derivative. Over there the authored
        // numbers are fed to rotationOverLifetime, whose parameter is an angular VELOCITY, so the
        // source game differentiates them first; EffectAngleCurvesOverLife is an angle over
        // normalized lifetime, which is what the author wrote in the first place.

        /// <summary> Size over a particle's life, out of the scale track's hidden channel. </summary>
        private static IEffectScale BuildParticleScale(VgdObject source, ABParticleSettings settings,
            ABImportContext context, string path)
        {
            var track = source.Scale;
            if (!HasChannel(track, ABParticleMap.ParticleScaleXIndex) &&
                !HasChannel(track, ABParticleMap.ParticleScaleYIndex))
                return new EffectScaleValue();

            return new EffectScaleCurvesOverLife
            {
                CurveX = ABCurveMap.Import(track, ABParticleMap.ParticleScaleXIndex, ABParticleMap.ParticleScaleDefault,
                    settings.TimelineLength, null, context.Report, path),
                CurveY = ABCurveMap.Import(track, ABParticleMap.ParticleScaleYIndex, ABParticleMap.ParticleScaleDefault,
                    settings.TimelineLength, null, context.Report, path),
            };
        }

        /// <summary> Rotation over a particle's life, out of the rotation track's hidden channel -
        /// or the one constant angle it is born with, when nothing animates it. </summary>
        private static IEffectAngle BuildParticleAngle(VgdObject source, ABParticleSettings settings,
            ABImportContext context, string path)
        {
            var track = source.Rotate;
            if (!HasChannel(track, ABParticleMap.ParticleAngleIndex)) return new EffectAngleValue();

            if (track.Keyframes.Count < 2)
                return new EffectAngleValue
                {
                    Angle = new FloatValue(ToRadians(
                        ReadChannel(track.Keyframes[0], ABParticleMap.ParticleAngleIndex,
                            ABParticleMap.ParticleAngleDefault))),
                };

            return new EffectAngleCurvesOverLife
            {
                Curve = ABCurveMap.Import(track, ABParticleMap.ParticleAngleIndex, ABParticleMap.ParticleAngleDefault,
                    settings.TimelineLength, ToRadians, context.Report, path),
            };
        }

        /// <summary> Tint over a particle's life. The object's whole colour timeline is a
        /// per-particle ramp over there, not the emitter's own colour over time. </summary>
        private static IEffectColor BuildParticleColor(VgdObject source, ABParticleSettings settings,
            ABImportContext context, string path)
        {
            var track = source.Color;
            if (track?.Keyframes == null || track.Keyframes.Count == 0) return new EffectColorValue();

            context.Report.Approximated("particle_color_theme_lost",
                "A gradient stop here holds a literal colour rather than a theme slot, so imported particle colours were resolved against the level's own theme once and no longer follow a theme change.",
                path);

            return new EffectColorGradientOverLife
            {
                Gradient = ABCurveMap.ImportGradient(track, settings.TimelineLength,
                    context.ReferenceTheme, context.Report, path),
            };
        }

        // THE VELOCITY CHANNEL IS A POSITION, NOT A SPEED. The authored numbers describe where a
        // particle has travelled by a given point of its life, and the source game feeds their
        // DERIVATIVE to velocityOverLifetime - which is what makes the first value almost always
        // zero, since a particle starts where it was born. Reading the raw value at t = 0 would
        // therefore give every emitter a start velocity of nothing.
        //
        // What crosses is the average velocity over the whole life, which reproduces the net
        // displacement exactly whenever the channel is linear - two keyframes and Linear easing,
        // which is what an ordinary emitter authors. The shape of the travel after that is
        // flattened, and that is the largest single approximation in this whole conversion.

        /// <summary> The one velocity a particle is born with, out of the position channel it was
        /// authored as. </summary>
        private static IVector2 BuildStartVelocity(VgdObject source, ABParticleSettings settings)
        {
            var track = source.Move;
            if (!UsesVelocityChannel(source)) return new Vector2Value(0f, 0f);

            var keyframes = track.Keyframes;
            var first = keyframes[0];
            var last = keyframes[^1];

            var life = Math.Max(settings.TimelineLength, ABParticleMap.MinTimelineLength);
            var x = ReadChannel(last, ABParticleMap.ParticleVelocityXIndex, 0f)
                    - ReadChannel(first, ABParticleMap.ParticleVelocityXIndex, 0f);
            var y = ReadChannel(last, ABParticleMap.ParticleVelocityYIndex, 0f)
                    - ReadChannel(first, ABParticleMap.ParticleVelocityYIndex, 0f);

            return new Vector2Value(x / life, y / life);
        }

        // PRESENCE IS NOT USE for this one channel, and the difference is the whole report. Every
        // parameter an emitter authors lives past it in the same array (ev[4] onwards), so an
        // emitter that sets a spawn rate necessarily carries indices 2 and 3 as zeros - and a
        // channel of zeros describes a particle that does not travel. Asking whether the index
        // EXISTS would therefore report the largest approximation in this converter on every single
        // emitter, which is exactly the noise the named codes exist to avoid.

        /// <summary> Whether this emitter actually sends its particles anywhere. </summary>
        private static bool UsesVelocityChannel(VgdObject source)
        {
            var keyframes = source.Move?.Keyframes;
            if (keyframes == null) return false;

            foreach (var keyframe in keyframes)
                if (!BHSDKMath.Approximately(ReadChannel(keyframe, ABParticleMap.ParticleVelocityXIndex, 0f), 0f)
                    || !BHSDKMath.Approximately(ReadChannel(keyframe, ABParticleMap.ParticleVelocityYIndex, 0f), 0f))
                    return true;

            return false;
        }

        // NAMED ONE BY ONE, and each one firing only when the source actually used the thing. A
        // report that says "some particle details were lost" on every emitter is what this
        // converter's report exists to avoid: an author cannot act on it, and it hides the one
        // emitter that lost something that mattered.

        /// <summary> Everything about this emitter that has no counterpart here. </summary>
        private static void ReportParticleLosses(VgdObject source, ABParticleSettings settings,
            InteropReport report, string path)
        {
            if (settings.SpawnRatePerUnit > 0f)
                report.Dropped("particle_spawn_per_unit",
                    "Afterbeat can emit a particle every so many units travelled; there is no distance-based emission here, so those emitters spawn on their time rate alone.",
                    path);

            if (settings.WorldSpace)
                report.Approximated("particle_world_space",
                    "A world-space Afterbeat emitter leaves its particles behind as it travels; effects here always simulate in their own space, so those particles are dragged along with the emitter instead.",
                    path);

            if (!BHSDKMath.Approximately(settings.StartSpeed, ABParticleMap.StartSpeedDefault))
                report.Dropped("particle_start_speed",
                    "Afterbeat pushes a particle along its emitter shape's normal at birth; there is no radial-outward force here, so that push is lost.",
                    path);

            if (UsesVelocityChannel(source))
                report.Approximated("particle_velocity_curve",
                    "Afterbeat animates where a particle travels over its whole life; only one start velocity crosses here, so that travel is flattened to its average.",
                    path);

            if (HasAnimatedEmitterVolume(source))
                report.Approximated("particle_emitter_volume_animated",
                    "Afterbeat can animate the volume particles spawn inside; an emitter shape here is a value rather than a track, so only its first keyframe crosses.",
                    path);

            if (source.GradientType != 0)
                report.Dropped("particle_gradient_material",
                    "Afterbeat can draw an emitter's particles with a gradient material; particles here take one colour ramp over their life and nothing else.",
                    path);
        }

        /// <summary> Whether the emitter volume moves after its first keyframe - values 0/1 of the
        /// scale track, which is the emitter half rather than the particle half. </summary>
        private static bool HasAnimatedEmitterVolume(VgdObject source)
        {
            var keyframes = source.Scale?.Keyframes;
            if (keyframes == null || keyframes.Count < 2) return false;

            var x = ReadChannel(keyframes[0], 0, ABParticleMap.DefaultEmitterExtent);
            var y = ReadChannel(keyframes[0], 1, ABParticleMap.DefaultEmitterExtent);

            for (var i = 1; i < keyframes.Count; i++)
                if (!BHSDKMath.Approximately(ReadChannel(keyframes[i], 0, ABParticleMap.DefaultEmitterExtent), x)
                    || !BHSDKMath.Approximately(ReadChannel(keyframes[i], 1, ABParticleMap.DefaultEmitterExtent), y))
                    return true;

            return false;
        }

        /// <summary> Whether any keyframe of a track actually wrote that value index - the hidden
        /// channels sit past the arity an ordinary track carries, so their absence is ordinary. </summary>
        private static bool HasChannel(VgdTrack track, int index)
        {
            if (track?.Keyframes == null) return false;

            foreach (var keyframe in track.Keyframes)
                if (keyframe?.Values != null && index < keyframe.Values.Count)
                    return true;

            return false;
        }

        private static float ReadChannel(VgdKeyframe keyframe, int index, float fallback)
        {
            var values = keyframe?.Values;
            return values != null && index >= 0 && index < values.Count ? values[index] : fallback;
        }

        private static float ToRadians(float degrees) => degrees * (float)(Math.PI / 180d);

        // Absolute, not clamped at zero: a negative scale MIRRORS over there, and a spawn volume is
        // symmetric, so the mirrored one is the same volume. Clamping instead would collapse it.
        private static (float X, float Y) ReadEmitterVolume(VgdObject source)
        {
            var keyframes = source?.Scale?.Keyframes;
            if (keyframes == null || keyframes.Count == 0)
                return (ABParticleMap.DefaultEmitterExtent, ABParticleMap.DefaultEmitterExtent);

            var keyframe = keyframes[0];
            return (Math.Abs(keyframe.GetValue(0)), Math.Abs(keyframe.GetValue(1)));
        }

        private static float ToArcRadians(float degrees)
            => Math.Clamp(degrees / ABParticleMap.MaxEmitterArc * EffectRules.Shape.Arc_Max,
                EffectRules.Shape.Arc_Min, EffectRules.Shape.Arc_Max);

        // ONE RESOURCE PER DEFINITION, and the definition is what the emitter IS - so the signature
        // has to name every parameter that reaches the EffectData and nothing that does not. A
        // number is written invariantly, since a signature read differently under another culture
        // would hand the same emitter two ids on two machines.

        /// <summary> A canonical description of everything this emitter's definition is built out
        /// of. Two emitters agreeing on it describe one effect. </summary>
        private static string BuildEffectSignature(ABParticleSettings settings, EffectData data,
            ShapeId shapeId)
        {
            var culture = CultureInfo.InvariantCulture;

            return string.Join("|",
                "v2",
                shapeId.value.ToString("N", culture),
                settings.SpawnRatePerSecond.ToString("R", culture),
                settings.TimelineLength.ToString("R", culture),
                Describe(data.Shape, culture),
                data.HasStopLocalFrame ? "stop" : "run",
                data.StopLocalFrame.ToString(culture),
                Describe(data.Forces, culture),
                Describe(data.Scale, culture),
                Describe(data.Angle, culture),
                Describe(data.Color, culture));
        }

        // THE EMITTER VOLUME BELONGS IN THE SIGNATURE, and it used to be missing: the emitter's own
        // parameters were named one by one while the size particles spawn inside - the thing an
        // author most obviously changes between two otherwise identical emitters - was not, so two
        // of them collapsed onto one shared EffectData.

        private static string Describe(IEffectShape shape, CultureInfo culture) => shape switch
        {
            EffectShapeRectangle rectangle when rectangle.Size is Vector2Value size
                => $"sh:r,{size.X.ToString("R", culture)},{size.Y.ToString("R", culture)}",
            EffectShapeCircle circle
                => $"sh:c,{Read(circle.Radius).ToString("R", culture)},{Read(circle.Aspect).ToString("R", culture)},"
                   + $"{Read(circle.Thickness).ToString("R", culture)},{Read(circle.Arc).ToString("R", culture)}",
            _ => "sh:-",
        };

        private static float Read(IFloat value) => value is FloatValue literal ? literal.Value : 0f;

        private static string Describe(EffectObjectForces forces, CultureInfo culture)
        {
            var velocity = (Vector2Value)forces.StartVelocityMin;
            return $"v:{velocity.X.ToString("R", culture)},{velocity.Y.ToString("R", culture)}";
        }

        private static string Describe(IEffectScale scale, CultureInfo culture)
            => scale is EffectScaleCurvesOverLife curves
                ? $"s:{Describe(curves.CurveX, culture)};{Describe(curves.CurveY, culture)}"
                : "s:-";

        private static string Describe(IEffectAngle angle, CultureInfo culture) => angle switch
        {
            EffectAngleCurvesOverLife curves => $"a:{Describe(curves.Curve, culture)}",
            EffectAngleValue value => $"a={((FloatValue)value.Angle).Value.ToString("R", culture)}",
            _ => "a:-",
        };

        private static string Describe(IEffectColor color, CultureInfo culture)
        {
            if (color is not EffectColorGradientOverLife gradient) return "c:-";

            var parts = new List<string>();
            foreach (var stop in gradient.Gradient.ColorKeys)
                parts.Add(string.Concat(
                    stop.Time.ToString("R", culture), ",",
                    stop.Color4.R.ToString("R", culture), ",",
                    stop.Color4.G.ToString("R", culture), ",",
                    stop.Color4.B.ToString("R", culture)));

            foreach (var stop in gradient.Gradient.AlphaKeys)
                parts.Add(string.Concat(
                    stop.Time.ToString("R", culture), ",", stop.Alpha.ToString("R", culture)));

            return "c:" + string.Join(";", parts);
        }

        private static string Describe(CurveValue curve, CultureInfo culture)
        {
            var parts = new List<string>(curve.KeyFrames.Count);
            foreach (var key in curve.KeyFrames)
                parts.Add(string.Concat(
                    key.Time.ToString("R", culture), ",", key.Value.ToString("R", culture)));

            return string.Join("/", parts);
        }

        // A NON-DESPAWNING EMITTER OUTLIVES ITS OWN EMISSION. Over there the object's length becomes
        // logicalLength + particleMaxLifetime, so the last particles finish their life after the
        // emitter has stopped spawning; here the span is extended by the same amount and the stop
        // frame above is what ends the emission where the object used to end. An emitter that DOES
        // despawn keeps its span exactly, because over there it is killed with its particles.
        //
        // Running past the end of the level is legal authored data - a root object is not bounded by
        // the level's own length - so nothing clamps against FrameDuration here.

        /// <summary> The span an emitter actually occupies, once the tail its particles need has
        /// been added. Anything that is not a non-despawning emitter keeps the span it was given. </summary>
        private static FrameSpan ResolveEmitterSpan(VgdObject source, RectObject target,
            FrameSpan span, int framerate)
        {
            if (target is not EffectObject) return span;

            var settings = ABParticleMap.TryRead(source);
            if (settings == null || settings.Value.DespawnOnEnd) return span;

            var tail = ABTimeMap.ToFrameCount(settings.Value.TimelineLength, framerate);
            return ABTimeMap.FromFrames(span.StartFrame, span.StartFrame + span.FrameDuration + tail);
        }

        #endregion

        #region Text

        // Afterbeat text has NO bounds of its own - it lays out from its origin and runs as far as
        // it needs to. This format lays text out inside the object's Size, so an imported text has
        // to be given one, and there is nothing in the source document to compute it from.
        //
        // So it is ESTIMATED, on the crudest rule that cannot clip: one character wide per
        // character of the longest line, one line tall per line. That over-reserves for most
        // typefaces (glyphs are narrower than they are tall, and a proportional font much more so)
        // and the block is a rectangle nothing else reads, so over-reserving costs nothing while
        // under-reserving would cut the text off. Written at the object's own first frame, since
        // the string it measures does not change over the object's life.
        private static void ApplyTextSize(RectObject target, InteropReport report, string path)
        {
            if (target is not TextObject text) return;

            report.Approximated("text_bounds_estimated",
                "Afterbeat text has no bounds; imported text was given a block one character wide per character and one line tall per line, which fits any typeface rather than matching the source exactly.",
                path);

            if (text.Sizes.Count > 0) text.Sizes.Clear();

            var (columns, lines) = MeasureText((text.Text as StringValue)?.Value);
            text.Sizes.Add(new ScaKey(
                new Vector2Value(columns * TextColumnWidth, lines * TextLineHeight),
                FrameRules.MinFrame, EaseType.Linear));
        }

        /// <summary> World units one character of an imported text is given. </summary>
        public const float TextColumnWidth = 1f;

        /// <summary> World units one line of an imported text is given. </summary>
        public const float TextLineHeight = 1f;

        // Inline formatting tags are not text and must not be measured - a line carrying a colour
        // tag is not sixty characters wide because the tag spelled it that way. Everything else is
        // counted as written, including whitespace.
        private static (int Columns, int Lines) MeasureText(string value)
        {
            if (string.IsNullOrEmpty(value)) return (1, 1);

            var lines = 1;
            var longest = 0;
            var current = 0;
            var inTag = false;

            foreach (var character in value)
            {
                switch (character)
                {
                    case '<':
                        inTag = true;
                        continue;
                    case '>' when inTag:
                        inTag = false;
                        continue;
                    case '\n':
                        if (current > longest) longest = current;
                        current = 0;
                        lines++;
                        continue;
                }

                if (inTag) continue;
                if (character != '\r') current++;
            }

            if (current > longest) longest = current;
            return (Math.Max(1, longest), Math.Max(1, lines));
        }

        private static RectObject CreateText(VgdObject source, ABImportContext context, string path)
        {
            var fonts = new ABFontMap.Selector();
            var text = StripUnplayableTags(source.Text ?? string.Empty, fonts, out var dropped);

            if (dropped)
                context.Report.Dropped("text_rotate_tag",
                    "Afterbeat rotates individual glyphs with TextMeshPro's <rotate> tag, which has no counterpart here; the tags were removed and the glyphs stand upright.",
                    path);

            var fontResourceId = fonts.Resolve(out var mixed);

            if (fonts.Recognized)
            {
                if (mixed)
                    context.Report.Dropped("text_font_mixed",
                        "Afterbeat can switch typeface inside one string; a font here is a property of the object, so those objects were given the closest of this game's own typefaces to the one covering most of their text, and the rest of the string changed face.",
                        path);
                else
                    context.Report.Approximated("text_font_tag",
                        "Afterbeat sets a text's typeface inline with TextMeshPro's <font> tag; a font here is a property of the object, so the tags were removed and the object was given the closest of this game's own typefaces.",
                        path);
            }

            if (text.IndexOf('<') >= 0)
                context.Report.Approximated("text_inline_tags",
                    "Afterbeat text carries inline formatting tags; the ones this format shares with TextMeshPro are interpreted, the rest are drawn as literal text.",
                    path);

            // Afterbeat's string is unbounded - real levels carry blocks of ten thousand characters
            // and more - while this one's is the FIXED SLOT LENGTH of the player's per-frame text
            // buffers (ValueRules.MaxGameString), so a longer string is not a validation nicety
            // here but a number the runtime cannot address. Truncating is the only import that
            // produces a playable level, and it is reported as dropped because it loses content.
            if (text.Length > ValueRules.MaxGameString)
            {
                text = TrimToLength(text, ValueRules.MaxGameString);
                context.Report.Dropped("text_over_cap",
                    $"Some text objects carry more than {ValueRules.MaxGameString} characters, which is the fixed length this format's text buffers hold; those strings were cut to fit.",
                    path);
            }

            return new TextObject
            {
                Text = new StringValue(text),
                FontResourceId = fontResourceId,
            };
        }

        // Afterbeat draws a text object by assigning the authored string to a TextMeshPro component
        // verbatim, so its markup vocabulary is TMP's own - nothing custom is added and nothing is
        // stripped over there. Most of that vocabulary crosses, because this format's renderer
        // parses the same tag names; two cannot. <rotate> has no counterpart at all, and <font>
        // has one that is not a tag: a typeface is a property of the object here, so the tag is
        // removed and what it named is written onto the object (ABFontMap).
        //
        // A tag nothing parses is not inert - it is DRAWN, as its own literal characters, inside a
        // block whose width was measured without them, and it also shifts every character index the
        // per-character fill and appearing tracks address. Removing it is therefore the readable
        // outcome rather than a lossy shortcut, and it is reported as dropped either way.
        //
        // <noparse> is honoured while scanning: inside it TMP treats everything as literal text, so
        // a <rotate> written there is content the author wanted shown and must survive untouched -
        // and a <font> written there is content too, which is why the selector is fed the literal
        // run as ordinary characters rather than being pushed by it.

        /// <summary> Removes every TextMeshPro tag nothing here can play from an imported string,
        /// charging what it draws to <paramref name="fonts"/> and reporting through
        /// <paramref name="dropped"/> whether a <c>rotate</c> was there. </summary>
        private static string StripUnplayableTags(string value, ABFontMap.Selector fonts, out bool dropped)
        {
            dropped = false;
            if (string.IsNullOrEmpty(value)) return value;

            if (value.IndexOf('<') < 0)
            {
                fonts.Count(value.Length);
                return value;
            }

            var builder = new StringBuilder(value.Length);
            var literal = false;
            var index = 0;

            while (index < value.Length)
            {
                var open = value.IndexOf('<', index);
                var close = open < 0 ? -1 : value.IndexOf('>', open + 1);

                if (close < 0)
                {
                    fonts.Count(value.Length - index);
                    builder.Append(value, index, value.Length - index);
                    break;
                }

                fonts.Count(open - index);
                builder.Append(value, index, open - index);
                index = close + 1;

                var closing = open + 1 < close && value[open + 1] == '/';
                var name = ReadTagName(value, closing ? open + 2 : open + 1, close);

                if (literal)
                {
                    // Only the closer of the literal run is a tag in here; everything else is text.
                    if (closing && IsTag(name, NoparseTag)) literal = false;
                    else fonts.Count(close - open + 1);
                    builder.Append(value, open, close - open + 1);
                    continue;
                }

                if (IsTag(name, RotateTag))
                {
                    dropped = true;
                    continue;
                }

                if (IsTag(name, FontTag))
                {
                    if (closing) fonts.Pop();
                    else fonts.Push(ReadFontName(value, open, close));
                    continue;
                }

                if (!closing && IsTag(name, NoparseTag)) literal = true;
                builder.Append(value, open, close - open + 1);
            }

            return builder.ToString();
        }

        /// <summary> Reads a tag's name out of the span between its brackets, stopping at whatever
        /// ends the name - a parameter, whitespace, or the closing bracket itself. </summary>
        private static string ReadTagName(string value, int start, int close)
        {
            var end = start;
            while (end < close)
            {
                var character = value[end];
                if (!char.IsLetterOrDigit(character) && character != '-') break;
                end++;
            }

            return value.Substring(start, end - start);
        }

        // A quoted value is read to its closing quote and an unquoted one to the end of the tag,
        // rather than to the first space: the spelling levels written before the source game's own
        // fonts migration carry is "<font=LiberationSans SDF>", a name with a space in it and no
        // quotes at all, and cutting at the space would leave "liberationsans" resolving by luck
        // and "electronic highway sign" resolving to nothing.

        /// <summary> Reads what a <c>font</c> tag names, empty when it names nothing. </summary>
        private static string ReadFontName(string value, int open, int close)
        {
            var assign = value.IndexOf('=', open + 1);
            if (assign < 0 || assign > close) return string.Empty;

            var start = assign + 1;
            while (start < close && char.IsWhiteSpace(value[start])) start++;
            if (start >= close) return string.Empty;

            var quote = value[start];
            if (quote != '"' && quote != '\'') return value[start..close].Trim();

            var end = value.IndexOf(quote, start + 1);
            return end < 0 || end > close ? value[(start + 1)..close].Trim() : value[(start + 1)..end];
        }

        private static bool IsTag(string name, string tag) =>
            string.Equals(name, tag, StringComparison.OrdinalIgnoreCase);

        /// <summary> TextMeshPro's per-glyph rotation tag, the one tag of theirs nothing here can play. </summary>
        private const string RotateTag = "rotate";

        /// <summary> TextMeshPro's literal-text tag, inside which no other tag is markup. </summary>
        private const string NoparseTag = "noparse";

        /// <summary> TextMeshPro's typeface tag, the one this format answers with a field rather
        /// than with markup of its own. </summary>
        private const string FontTag = "font";

        #endregion

        #region Layer and pivot

        // Draw order itself is ABLayerMap's; only the conversion into what this format stores
        // happens here, and it is a subtraction. Layer is parent-relative here and the resolved
        // layer is absolute, so a child stores the difference.
        //
        // The parent's effective layer comes from the table the first pass filled, never from
        // whatever this pass happens to have seen: an object list is in no particular order, so
        // computing it as it goes gives every child written before its parent a parent layer of
        // zero and draws that whole branch at the wrong depth.
        private static void ApplyLayer(VgdObject source, RectObject target,
            ABImportContext context, int effectiveLayer)
        {
            if (context.TryGetPackedLayer(source.Id, out var packed))
            {
                target.Layer = Math.Clamp(packed, ValueRules.MinLayer, ValueRules.MaxLayer);
                return;
            }

            var parentEffective = context.GetParentEffectiveLayer(source.ParentId);
            var relative = effectiveLayer - parentEffective;

            target.Layer = Math.Clamp(relative, ValueRules.MinLayer, ValueRules.MaxLayer);
        }

        // Afterbeat's origin is an OFFSET of the reference point from the object's centre; this
        // format's pivot is a normalized point inside the object's own box, with 0.5,0.5 at the
        // centre. Moving the reference point one way moves the pivot the other, hence the
        // subtraction. An origin of zero is the ordinary case and converts exactly, which is why it
        // is not reported.
        //
        // TEXT IS THE OPPOSITE SIGN, and it is not a special case invented here - it is a different
        // mechanism over there. A shape's origin moves the MESH by +origin under an unmoved
        // transform, so the transform sits at 0.5 - origin of the shape. A text's origin instead
        // picks one of TextMeshPro's three alignments, and an origin of +0.5 selects Right - the
        // glyph run pushed against the right edge of the block, extending LEFT. Subtracting there
        // anchors the text at the left edge and runs it the other way, i.e. the reading direction
        // of every off-centre text in a converted level was mirrored.
        private static void ApplyPivot(VgdObject source, RectObject target,
            InteropReport report, string path)
        {
            var originX = source.Origin?.X ?? 0f;
            var originY = source.Origin?.Y ?? 0f;
            var authored = originX != 0f || originY != 0f;

            if (target is TextObject)
            {
                if (!authored) return;

                report.Approximated("origin_text_alignment",
                    "Afterbeat lays text out by one of three alignments rather than by a pivot; an origin between them is centred there and is placed proportionally here.",
                    path);

                target.Pivots.Add(new AlignmentKey(
                    new Vector2Value(DefaultPivot + originX, DefaultPivot + originY),
                    FrameRules.MinFrame));
                return;
            }

            // A shape whose geometry is offset inside its own box carries that offset as a pivot
            // too, and the two ADD: an author can move the reference point of a Triangle Bottom
            // exactly as they can any other shape's, and reading either alone puts it in the wrong
            // place. It is NOT reported, unlike an authored origin - nothing was approximated,
            // this is simply where that shape sits.
            originY += ABShapeMap.GetPivotOffsetY(source.Shape, source.ShapeOption);

            if (originX == 0f && originY == 0f) return;

            if (authored)
                report.Approximated("origin_pivot",
                    "Afterbeat's object origin was converted into this format's pivot; the two measure the same idea from opposite sides, so check objects whose origin was not centred.",
                    path);

            target.Pivots.Add(new AlignmentKey(
                new Vector2Value(DefaultPivot - originX, DefaultPivot - originY), FrameRules.MinFrame));
        }

        /// <summary> Centre of an object's own box, in this format's normalized pivot space. </summary>
        public const float DefaultPivot = 0.5f;

        // A PARENT'S ORIGIN MUST NOT REACH ITS CHILDREN, and here it does unless this runs. Over
        // there the origin lives on a leaf mesh transform hanging below the visual object, so the
        // parent chain never sees it; here it is the parent's Pivot, and RectTransform2D.Apply
        // computes a child's frame from a centre point that the pivot moves - so every child of an
        // off-centre parent is displaced by it, by more the larger the parent is.
        //
        // The cancellation is structural rather than baked, which is what makes it survive an
        // animated parent with no keyframes added: Apply's anchor term is
        // Rot(r_p) * ((lerp(anchorMin, anchorMax, pivot) - 0.5) * parentFullSize), and with the two
        // anchors EQUAL the pivot drops out of the lerp entirely, leaving a term that cancels the
        // centre-point shift for any parent size and any parent rotation. Equal anchors also leave
        // the `size += parent.size * (max - min)` term at zero, so nothing else moves.
        private static void ApplyParentOrigin(VgdObject source, RectObject target,
            IReadOnlyDictionary<string, VgdObject> byId)
        {
            if (string.IsNullOrEmpty(source.ParentId)) return;
            if (!byId.TryGetValue(source.ParentId, out var parent) || parent == null) return;

            // Whatever moved the PARENT's pivot has to be cancelled, whether the author moved it or
            // the parent's own shape sits off-centre in its box - both end up on the same field, so
            // both leak the same way. Text is excluded because its pivot went the other way.
            var originX = parent.Origin?.X ?? 0f;
            var originY = parent.Origin?.Y ?? 0f;

            if (!ABShapeMap.IsText(parent.Shape))
                originY += ABShapeMap.GetPivotOffsetY(parent.Shape, parent.ShapeOption);
            else
                (originX, originY) = (-originX, -originY);

            if (originX == 0f && originY == 0f) return;

            var anchor = new Vector2Value(DefaultPivot - originX, DefaultPivot - originY);
            target.AnchorsMin.Add(new AlignmentKey(anchor, FrameRules.MinFrame));
            target.AnchorsMax.Add(new AlignmentKey(anchor, FrameRules.MinFrame));
        }

        // What is reported here is what is LOST, and the condition used to be the exact inverse of
        // that. It fired on a mask that was not the format's own default - i.e. on "111", the one
        // mask this format expresses perfectly - and stayed silent on the default "101", which is
        // both the most common mask in real levels and a genuine loss. So every level came back
        // with a finding on the objects that were fine and nothing on the objects that were not.
        //
        // Two of the three bits are answered elsewhere rather than reported: the SCALE bit crosses
        // exactly through the choice of Size vs Scale (ResolveScaleTargets), and a mask of "000"
        // crosses exactly by importing the object as a root. What is left is a mask that drops the
        // parent's position or rotation while keeping something else - which needs the parent's
        // value at each sample time to cancel, and is therefore not something this format can hold.
        private static void ReportParenting(VgdObject source, InteropReport report, string path)
        {
            if (string.IsNullOrEmpty(source.ParentId)) return;
            if (InheritsNothing(source)) return;

            if (!HasParentBit(source, ParentTypeIndex.Position))
                report.Dropped("parent_position_not_inherited",
                    "Afterbeat can stop a child following its parent's position while it still follows the rest; this format inherits the whole transform, so those objects move with their parent here.",
                    path);

            if (!HasParentBit(source, ParentTypeIndex.Rotation))
                report.Dropped("parent_rotation_not_inherited",
                    "Afterbeat can stop a child following its parent's rotation while it still follows the rest; this format inherits the whole transform, so those objects turn with their parent here.",
                    path);

            if (source.ParentOffsets == null) return;
            foreach (var offset in source.ParentOffsets)
            {
                if (offset == 0f) continue;
                report.Dropped("parent_time_offset",
                    "Afterbeat can delay a child's inheritance from its parent in time; this format has no equivalent, so those delays are not imported.",
                    path);
                return;
            }
        }

        #endregion

        #region Tracks

        private static void ImportPositions(VgdObject source, RectObject target, int framerate,
            ABImportContext context, string path)
        {
            var track = source.Move;
            if (track?.Keyframes == null) return;

            var report = context.Report;
            var compensation = GetCompensation(source, context);

            foreach (var key in Take(track.Keyframes, LevelRules.MaxObjectKeys, report, path))
            {
                var value = ABValueMap.ImportVector(
                    key.GetValue(0) * compensation.X, key.GetValue(1) * compensation.Y,
                    key, report, path);
                target.Positions.Add(new PosKey(value, LocalFrame(key, framerate),
                    ABEaseMap.Import(key.Ease, report, path)));
            }

            ABTimeMap.DeduplicateByFrame(target.Positions, k => k.Frame, report, path);
        }

        // WHICH FIELD the source scale lands in decides whether this object's children inherit it,
        // and getting that wrong is what pulls a whole parented hierarchy apart - see
        // ABImportContext.ScaleTarget. Everything below the text exception follows from the table
        // ResolveScaleTargets built.
        //
        // Text is the one object whose field is fixed regardless, and the reason is on the other
        // side: an Afterbeat text object has a scale and NO font size, so its scale is the only
        // thing sizing the glyphs, while here Size is the block the glyphs are laid out in and
        // Scale is the multiplier on top of it. Writing the source scale into Size gave every
        // imported text a one-by-one block - a whole line of text inside a single cell - which is
        // what ApplyTextSize fixes. It also happens to be the propagating field, so a text object
        // parenting something behaves as p_t[1] == '1' whatever its children asked for; a text
        // object with children is rare enough to leave at that.
        private static void ImportScales(VgdObject source, RectObject target, int framerate,
            ABImportContext context, string path)
        {
            // AN EMITTER'S SCALE TRACK IS NOT ITS TRANSFORM. Over there it drives shape.scale - the
            // volume particles spawn inside - while the ordinary branch below drives what the object
            // itself measures. They are different quantities that happen to be authored on the same
            // track, so the emitter's went into EffectShape* when the effect was built
            // (CreateEffect), and writing it here as well would scale the whole system on top of it.
            if (target is EffectObject) return;

            var track = source.Scale;
            if (track?.Keyframes == null)
            {
                SeedSize(source, target);
                return;
            }

            var report = context.Report;
            var toScale = target is TextObject
                          || context.GetScaleTarget(source.Id) == ABImportContext.ScaleTarget.Scale;
            var into = toScale ? target.Scales : target.Sizes;
            var compensation = GetCompensation(source, context);

            // A custom polygon whose geometry had to be shrunk to fit this format's own box is drawn
            // back up here - see ABShapeMap.GetCustomSizeCompensation. It multiplies into the SIZE
            // and never into the scale, since the object grew and its children did not.
            var shapeFit = ABShapeMap.GetCustomSizeCompensation(source);
            var sizeFactor = toScale ? 1f : shapeFit;

            // The shear fit's scale half belongs nowhere near the position - see ResolveShearFits.
            var shear = GetShearScale(source, context);

            foreach (var key in Take(track.Keyframes, LevelRules.MaxObjectKeys, report, path))
            {
                var value = ABValueMap.ImportVector(
                    key.GetValue(0) * compensation.X * sizeFactor * shear.X,
                    key.GetValue(1) * compensation.Y * sizeFactor * shear.Y,
                    key, report, path);
                into.Add(new ScaKey(value, LocalFrame(key, framerate),
                    ABEaseMap.Import(key.Ease, report, path)));
            }

            ABTimeMap.DeduplicateByFrame(into, k => k.Frame, report, path);

            // Two ways to end up with no extent of its own, and both are ordinary: the scale went to
            // Scales because this object's children inherit it, or the source carried no scale
            // KEYFRAME at all - the track itself is always present, empty or not.
            if (toScale || target.Sizes.Count == 0) SeedSize(source, target);
        }

        // THE UNIT SQUARE AN OBJECT USED TO GET FOR FREE, written out because it no longer comes for
        // free: an empty Sizes track reads as ZERO now (BH.Shared.defaults.size) - a group node with
        // no extent of its own - and two paths through ImportScales leave it empty. One is an object
        // whose scale went to Scales because its children inherit it, which is 39% of the parents in
        // the measured corpus (see AFTERBEAT_ISSUE.md), the other an object whose scale track carries
        // no keyframe. Both are ordinary drawn shapes over there and would otherwise arrive invisible -
        // and a child anchor-stretched over such a parent (ABOpacityHitGate's collider) would come
        // out with no rect either, since RectTransform2D.Apply adds `parent.size * (max - min)`.
        //
        // It carries the custom-polygon shrink where there is one: that factor multiplies into the
        // SIZE and never into the scale, since the object grew and its children did not - see
        // ABShapeMap.GetCustomSizeCompensation.
        //
        // Text is not seeded here because ApplyTextSize measures its own block and clears whatever is
        // on the track first, and an emitter never reaches this method at all.
        private static void SeedSize(VgdObject source, RectObject target)
        {
            if (target is TextObject) return;

            var shapeFit = ABShapeMap.GetCustomSizeCompensation(source);
            target.Sizes.Add(new ScaKey(
                new Vector2Value(ValueRules.DefaultScaX * shapeFit, ValueRules.DefaultScaY * shapeFit),
                FrameRules.MinFrame));
        }

        // A child whose own mask disagreed with the field its parent's scale had to go into. Both
        // of its own tracks are scaled by the same factor, since the parent's scale reaches the
        // child's OFFSET as well as its extent.
        private static (float X, float Y) GetCompensation(VgdObject source, ABImportContext context)
            => source?.Id != null
               && context.ScaleCompensations.TryGetValue(source.Id, out var factor)
                ? factor
                : (1f, 1f);

        private static (float X, float Y) GetShearScale(VgdObject source, ABImportContext context)
            => source?.Id != null
               && context.ShearScales.TryGetValue(source.Id, out var factor)
                ? factor
                : (1f, 1f);

        private static float GetShearRotation(VgdObject source, ABImportContext context)
            => source?.Id != null
               && context.ShearRotations.TryGetValue(source.Id, out var offset)
                ? offset
                : 0f;

        // The one track that cannot be converted keyframe by keyframe: each source value is a delta
        // from the one before it, so the whole track has to be walked in order while a running total
        // is kept.
        //
        // The shear fit's angle half lands here, and only ever as a CONSTANT offset - the fit is
        // only computed for a constant rotation in the first place, so adding it to every key adds
        // it to the one angle the track actually holds. See ResolveShearFits for who is allowed one.
        private static void ImportRotations(VgdObject source, RectObject target, int framerate,
            ABImportContext context, string path)
        {
            var track = source.Rotate;
            if (track?.Keyframes == null) return;

            var report = context.Report;
            var offset = GetShearRotation(source, context);

            var accumulated = 0f;
            var randomKeys = 0;
            foreach (var key in Take(track.Keyframes, LevelRules.MaxObjectKeys, report, path))
            {
                var angle = ABValueMap.AccumulateRotationValue(key.GetValue(0), key,
                    ref accumulated, offset, report, path);
                if (angle is not FloatValue) randomKeys++;
                target.Rotations.Add(new AngleKey(angle, LocalFrame(key, framerate),
                    ABEaseMap.Import(key.Ease, report, path)));
            }

            // Only worth saying once a track holds MORE than one random keyframe - a single one
            // crosses exactly, and it is the compounding between them that this format cannot say.
            if (randomKeys > 1)
                report?.Approximated("rotation_random_not_compounded",
                    "Afterbeat randomizes a rotation as a delta from the keyframe before it, so its spread grows along a track; this format rolls every keyframe against the authored total, so those tracks vary by the right amount at each keyframe but no longer drift apart along it.",
                    path);

            ABTimeMap.DeduplicateByFrame(target.Rotations, k => k.Frame, report, path);
        }

        private static void ImportColors(VgdObject source, RectObject target,
            ABImportContext context, string path)
        {
            var track = source.Color;
            if (track?.Keyframes == null) return;

            var report = context.Report;
            var framerate = context.Options.Framerate;

            switch (target)
            {
                case ShapeObject shape:
                {
                    foreach (var key in Take(track.Keyframes, LevelRules.MaxObjectKeys, report, path))
                        shape.Colors.Add(BuildShapeColor(key, source, context, path));
                    ABTimeMap.DeduplicateByFrame(shape.Colors, k => k.Frame, report, path);
                    break;
                }
                case TextObject text:
                {
                    foreach (var key in Take(track.Keyframes, LevelRules.MaxObjectKeys, report, path))
                        text.Colors.Add(new Color4Key(ReadStartColor(key, context, path),
                            LocalFrame(key, framerate), ABEaseMap.Import(key.Ease, report, path)));
                    ABTimeMap.DeduplicateByFrame(text.Colors, k => k.Frame, report, path);
                    break;
                }
                default:
                    // An Empty object draws nothing, so its colour track describes nothing. Not a
                    // loss worth reporting - the source level did not draw it either.
                    break;
            }
        }

        // The ramp's own three numbers live on the OBJECT over there and the two colours it runs
        // between live in the keyframe, so this reads both: gt/gr/gs off the source object, the
        // pair of theme slots off the key. ABGradientMap owns everything after that - which of
        // the four-corner keyframes the ramp samples into, and what it costs.
        private static IColor4X4Key BuildShapeColor(VgdKeyframe key, VgdObject source,
            ABImportContext context, string path)
        {
            var report = context.Report;
            var frame = LocalFrame(key, context.Options.Framerate);
            var ease = ABEaseMap.Import(key.Ease, report, path);
            var start = ReadStartColor(key, context, path);
            var gradient = (ABGradientType)source.GradientType;

            // A non-gradient object still carries a third colour component in every key, and the
            // source game ignores it. Reading it here would give an object a second colour its
            // own level never drew.
            if (gradient == ABGradientType.None)
                return new Color4Key(start, frame, ease);

            return ABGradientMap.Build(gradient, source.GradientRotation, source.GradientScale,
                start, ReadEndColor(key, context, path), frame, ease, context.ReferenceTheme,
                context.Options.BakeGradientCorners, report, path);
        }

        // Cutting a C# string at a fixed length cuts UTF-16 CODE UNITS, and two of those can be one
        // character: an emoji or any other non-BMP glyph is a surrogate PAIR, so a cut landing
        // between them leaves a lone half - a string that is not valid UTF-16 at all, which a JSON
        // writer and a text renderer are each entitled to handle differently. The second hazard is
        // a rich-text tag the cut opens and never closes, which draws the tag's own source instead
        // of formatting anything. Both are cheap to avoid and neither announces itself.

        /// <summary> The longest prefix of <paramref name="text"/> that fits and is still a whole
        /// string - never splitting a surrogate pair, never leaving a tag open. </summary>
        private static string TrimToLength(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) return text;

            var end = maxLength;
            if (char.IsHighSurrogate(text[end - 1])) end--;

            // An unclosed '<' after the cut would swallow whatever the renderer reads next, so the
            // cut moves back to before it. Only when the tag really is unterminated: a '<' with its
            // own '>' still inside the kept text is a finished tag and stays.
            var open = text.LastIndexOf('<', end - 1);
            if (open >= 0 && text.IndexOf('>', open) >= end) end = open;

            return end <= 0 ? string.Empty : text[..end];
        }

        private static IColor4 ReadStartColor(VgdKeyframe key, ABImportContext context, string path)
            => ABColorMap.Import(ABColorMap.ToIndex(key.GetValue(0)), OpacityOf(key), ABPalette.Objects,
                context.ReferenceTheme, context.Report, path);

        private static IColor4 ReadEndColor(VgdKeyframe key, ABImportContext context, string path)
            => ABColorMap.Import(ABColorMap.ToIndex(key.GetValue(2)), OpacityOf(key), ABPalette.Objects,
                context.ReferenceTheme, context.Report, path);

        // Afterbeat writes opacity as a PERCENTAGE, 0 to 100, and this format stores alpha as 0 to
        // 1. Read straight across, every fade an author wrote clamped to fully opaque, so a level
        // arrived with every one of its fades missing - the single most visible colour bug in the
        // converter, and invisible in a round trip because both directions had it.
        //
        // A keyframe that carries only its index is fully opaque: the format's own default for a
        // missing component is 0, which here would mean invisible, and the source game's own reader
        // fills a missing opacity with 100 rather than with 0.

        /// <summary> The source writes opacity as a percentage; this format wants it as a fraction. </summary>
        public const float OpacityScale = 100f;

        private static float OpacityOf(VgdKeyframe key)
            => key.Values != null && key.Values.Count > 1 ? key.GetValue(1) / OpacityScale : 1f;

        #endregion

        #region Shared

        /// <summary> A keyframe's frame, local to its object exactly as it was local to its object
        /// in the source. </summary>
        private static int LocalFrame(VgdKeyframe key, int framerate)
            => ABTimeMap.ToFrame(key.Time, framerate);

        // This format caps a track at LevelRules.MaxObjectKeys and Afterbeat does not, so a long
        // track is truncated rather than thinned: dropping every other key changes the motion
        // everywhere, while cutting the tail leaves everything before it exactly as authored.
        //
        // Ordering is this method's job because two readers below depend on it and neither can
        // check: rotation accumulates each delta onto the one before it, so an out-of-order track
        // integrates to a different animation, and "the tail" is only the tail if the keys are in
        // time order. The format guarantees keyframe times are unique, never that they are sorted.
        private static IEnumerable<VgdKeyframe> Take(List<VgdKeyframe> keyframes, int max,
            InteropReport report, string path)
        {
            var sorted = new List<VgdKeyframe>(keyframes);
            sorted.Sort(CompareByTime);

            var taken = 0;
            foreach (var key in sorted)
            {
                if (key == null) continue;
                if (taken >= max)
                {
                    report.Dropped("keys_over_cap",
                        $"Some tracks carry more than {max} keyframes, which is this format's limit; the extra ones were dropped.",
                        path);
                    yield break;
                }

                taken++;
                yield return key;
            }
        }

        private static int CompareByTime(VgdKeyframe left, VgdKeyframe right)
        {
            if (left == null) return right == null ? 0 : 1;
            if (right == null) return -1;
            return left.Time.CompareTo(right.Time);
        }

        #endregion
    }
}