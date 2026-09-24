using System;
using System.Collections.Generic;
using BH.SDK.Interop.AfterBeat.Models;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Events;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.PostProcessing;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Interop.AfterBeat.Export
{
    // Exporting is lossy by construction and there is no way to make it otherwise: this format grew
    // past the one it is writing to. What the exporter owes the author is therefore not fidelity
    // but HONESTY - every one of these ends up in the report rather than being quietly skipped:
    //
    //   particle effects, audio tracks beyond the first, level-authored geometry, anchors, pivots
    //   past the first keyframe, per-corner colours, per-character text effects, random values,
    //   every beat segment after the first, checkpoint spaces other than World, and post-processing
    //   with no Afterbeat counterpart.
    //
    // The single biggest structural gap is AUDIO. An Afterbeat level is one song in a folder, with
    // no track list, no offsets and no effects; this format has a whole mixer. Nothing about that
    // can cross, so the export says so once and moves on.

    /// <summary> A <see cref="Level"/> and its <see cref="LevelMeta"/> back into Afterbeat's
    /// documents. </summary>
    public static class ABLevelExporter
    {
        /// <summary> Everything one export produced. Each document is a separate file on disk, the
        /// same way this project keeps level and metadata apart. </summary>
        public readonly struct Result
        {
            /// <summary> The .vgd document that came out. </summary>
            public VgdLevel Level { get; }

            /// <summary> The .vgm document beside it. </summary>
            public VgmMeta Meta { get; }

            /// <summary> What the export had to say about itself. </summary>
            public InteropReport Report { get; }

            /// <summary> All three at once; nothing mutates them afterwards. </summary>
            public Result(VgdLevel level, VgmMeta meta, InteropReport report)
            {
                Level = level;
                Meta = meta;
                Report = report;
            }
        }

        /// <summary> A whole level and its metadata as the source's two documents. </summary>
        public static Result Export(Level level, LevelMeta meta,
            ABOptions options = null, InteropReport report = null)
        {
            report ??= new InteropReport();

            if (level == null)
            {
                report.Failed("level_missing", "There was no level to export.", "level");
                return new Result(null, null, report);
            }

            options = (options ?? new ABOptions(level.Settings.Fps)).Sanitized();

            // The export's framerate is the LEVEL's, not the caller's preference: frames are being
            // turned back into seconds, and reading them at any other rate retimes the whole level.
            options.Framerate = level.Settings.Fps;

            // Afterbeat rounds every time it stores onto a 10 ms grid (ABTimeMap.SourceTimeStep), so
            // a level running finer than 100 fps has frames it cannot tell apart - and a track over
            // there holds one keyframe per time, so the neighbours merge rather than crowd.
            if (options.Framerate > ABTimeMap.MaxLosslessFramerate)
                report.Approximated("framerate_finer_than_source_grid",
                    $"Afterbeat stores times in hundredths of a second; this level runs at {options.Framerate} fps, so keyframes less than {ABTimeMap.SourceTimeStep} seconds apart land on the same time there and only the last of them survives.",
                    "level");

            var target = new VgdLevel();
            var context = new ABExportContext(options, report, level.Game)
            {
                Effects = level.Resources.Effects,
            };

            ExportThemes(level, target, context);

            // Which placements cross as placements is decided before any object is written: their
            // copies must stay out of objects[], and their layers belong in the draw-order map.
            var placements = ABPlacementExporter.Decide(level, context);
            foreach (var skipped in placements.Skipped) context.Skipped.Add(skipped);

            var layers = ABObjectExporter.CollectLayers(context);
            foreach (var placement in placements.Expressed) layers.Add(context.GetEffectiveLayer(placement));
            context.DrawOrder = ABDrawOrderMap.Build(layers);
            ReportDrawOrder(context.DrawOrder, layers, context.Report);

            target.Objects = ABObjectExporter.ExportAll(context, "objects");
            ExportPrefabs(level, target, context, placements);
            ExportPlacements(target, context, placements);
            ExportEvents(level, target, context);
            ExportBpm(level, target, context);
            ReportUnsupported(level, report);

            return new Result(target, ABMetaExporter.Export(meta, level, report), report);
        }

        #region Themes and prefabs

        private static void ExportThemes(Level level, VgdLevel target, ABExportContext context)
        {
            foreach (var pair in level.Resources.Themes)
            {
                var theme = pair.Value;
                if (theme == null) continue;

                context.ReferenceTheme ??= theme;
                target.Themes.Add(ABThemeMap.Export(theme, ToThemeSourceId(pair.Key),
                    context.Report, "themes"));
            }
        }

        // Afterbeat names a theme with an arbitrary string, so a Guid's own text is a perfectly good
        // one and is stable across exports of the same level - which is what the theme keyframes
        // referencing it need.
        private static string ToThemeSourceId(ThemeId id) => id.value.ToString("N");

        // A TEMPLATE'S ROOT IS WRITTEN ONLY WHEN IT CARRIES SOMETHING. Prefab.Root is this format's
        // own object - Afterbeat has no such thing, a template there is a flat list - and everything
        // in a template hangs off it whether it says so or not. An identity root adds nothing, so
        // its children become the template's roots, which is exactly what an imported template
        // started as; a root that moves, scales, rotates or sizes is written as an empty the rest
        // hangs off, since that is the only way its transform can reach them over there. Either way
        // nothing is left parented to an id Afterbeat cannot name.
        //
        // A nested placement inside a template is written as what it materialized into - the
        // format being written to is the one this converter's own import never nests, so the export
        // keeps to the same rule.
        private static void ExportPrefabs(Level level, VgdLevel target, ABExportContext context,
            ABPlacementExporter.Plan placements)
        {
            var bands = CollectTemplateBands(level, context, placements);
            var nested = 0;

            foreach (var pair in level.Resources.Prefabs)
            {
                var prefab = pair.Value;
                if (prefab == null) continue;

                var prefabContext = new ABExportContext(context.Options, context.Report, prefab)
                {
                    ReferenceTheme = context.ReferenceTheme,
                    Effects = context.Effects,
                };

                foreach (var obj in prefab.Objects.Values)
                    if (obj is PrefabObject) nested++;

                var objects = new List<VgdObject>();
                var root = prefab.Root;
                if (root != null && !IsIdentityRoot(root))
                {
                    prefabContext.PrefabRootSourceId = ABExportContext.ToSourceId(ObjectId.PrefabRoot);
                    var node = root.Copy();
                    node.Active = true;
                    node.ParentObjectId = ObjectId.Null;
                    node.Layer = 0;

                    var holder = ABObjectExporter.Export(node, prefabContext, "prefab.root");
                    if (holder != null)
                    {
                        holder.ParentId = string.Empty;
                        objects.Add(holder);
                    }
                }

                var layers = ABObjectExporter.CollectLayers(prefabContext);
                bands.TryGetValue(pair.Key, out var band);
                prefabContext.DrawOrder = ABDrawOrderMap.Build(layers, IsPackedTemplate(layers) ? band : null);

                objects.AddRange(ABObjectExporter.ExportAll(prefabContext, "prefab.objs"));

                target.Prefabs.Add(new VgpPrefab
                {
                    Id = ABPlacementExporter.ToPrefabSourceId(pair.Key),
                    Name = root?.Name ?? string.Empty,
                    Type = (int)ABPrefabType.Misc1,
                    Objects = objects,
                });
            }

            if (nested > 0)
                context.Report.Approximated("nested_prefab_flattened",
                    $"{nested} prefab placements inside prefab templates were written as the objects they materialize into; this converter does not write nested prefabs.",
                    "prefabs");
        }

        /// <summary> Whether a template's root carries nothing its children would inherit. </summary>
        private static bool IsIdentityRoot(RectObject root)
        {
            foreach (var key in root.Positions)
                if (key.Pos is not Vector2Value { X: 0f, Y: 0f }) return false;
            foreach (var key in root.Scales)
                if (key.Scale is not Vector2Value { X: 1f, Y: 1f }) return false;
            foreach (var key in root.Rotations)
                if (key.Angle is not FloatValue { Value: 0f }) return false;

            return root.Sizes.Count == 0 && root.AnchorsMin.Count == 0 && root.AnchorsMax.Count == 0
                   && root.Pivots.Count == 0;
        }

        // A PACKED template's layers are relative to its placement - rows 1 and up above
        // Prefab.Root - so what band they draw in is the placement's, not theirs. A template whose
        // content reaches 0 or below was laid out in the level's own layer space (every legacy
        // import mode does that, with the placement on 0) and maps like the level does.
        private static bool IsPackedTemplate(List<int> layers)
        {
            if (layers.Count == 0) return false;
            foreach (var layer in layers)
                if (layer < ValueRules.FirstLayerAbovePlayer)
                    return false;
            return true;
        }

        /// <summary> The band each template's placements sit in - the first expressed placement's,
        /// then any placement's; AbovePlayer when its block starts above the player line. </summary>
        private static Dictionary<PrefabId, ABRenderLayer> CollectTemplateBands(Level level,
            ABExportContext context, ABPlacementExporter.Plan placements)
        {
            var bands = new Dictionary<PrefabId, ABRenderLayer>();

            foreach (var placement in placements.Expressed) Add(placement);
            foreach (var pair in level.Game.Objects)
                if (pair.Value is PrefabObject placement && !bands.ContainsKey(placement.PrefabId))
                    Add(placement);

            return bands;

            void Add(PrefabObject placement)
            {
                if (bands.ContainsKey(placement.PrefabId)) return;
                bands[placement.PrefabId] = context.GetEffectiveLayer(placement) >= ValueRules.FirstLayerAbovePlayer
                    ? ABRenderLayer.AbovePlayer
                    : ABRenderLayer.Default;
            }
        }

        // A PLACEMENT CROSSES AS A PLACEMENT where Afterbeat can say the same thing and as its copies
        // where it cannot - ABPlacementExporter decides which, before any object is written, and a
        // flattened one keeps the old shape: its copies as objects, and the placement itself as the
        // empty they hang off (ABObjectExporter's ApplyShape), because it carries the position,
        // scale and rotation the whole subtree sits at.
        private static void ExportPlacements(VgdLevel target, ABExportContext context,
            ABPlacementExporter.Plan placements)
        {
            var map = context.DrawOrder;
            for (var i = 0; i < placements.Expressed.Count; i++)
            {
                var placement = placements.Expressed[i];
                var exported = ABPlacementExporter.Export(placement, context, $"prefab_objects[{i}]");
                var layer = context.GetEffectiveLayer(placement);
                ABObjectExporter.ApplyEditorRow(exported.Editor,
                    map?.RowOf(layer) ?? ABDrawOrderMap.MapLinear(layer).Depth);
                target.PrefabPlacements.Add(exported);
            }

            if (placements.Expressed.Count > 0)
                context.Report.Info("placements_exported",
                    $"{placements.Expressed.Count} prefab placements were written as Afterbeat placements of their prefab.",
                    "prefab_objects");

            var unmaterialized = 0;
            foreach (var pair in context.Scope.Objects)
                if (pair.Value is PrefabObject placement && !context.Skipped.Contains(pair.Key)
                                                         && placement.ObjectIds is not { Count: > 0 })
                    unmaterialized++;

            foreach (var pair in placements.Flattened)
                context.Report.Info(ABPlacementExporter.ToCode(pair.Key),
                    ABPlacementExporter.ToMessage(pair.Key, pair.Value), "prefab_objects");

            if (placements.Flattened.Count > 0)
                context.Report.Info("prefabs_flattened",
                    "Some prefab placements were exported as the objects they materialize into rather than as placements, so Afterbeat does not draw their content a second time.",
                    "prefab_objects");

            if (unmaterialized > 0)
                context.Report.Dropped("placement_not_materialized",
                    "Some prefab placements have never been materialized, so they own no objects to export and their content is missing from the exported level.",
                    "prefab_objects");
        }

        private static void ReportDrawOrder(ABDrawOrderMap map, List<int> layers, InteropReport report)
        {
            if (!map.IsLinear)
                report.Info("draw_order_ranked",
                    "This level uses more layers than Afterbeat has depths, so its draw order was written by rank: the order survives, the exact depths do not, and every layer from 0 down is exported as ordinary content rather than background.",
                    "objects");
            else if (map.ReadsBackground(layers))
                report.Info("background_band_inferred",
                    "Layers below -60 are read as Afterbeat's background band - that is where they came from if this level was imported with the OnlyDepth or Auto layer mode. A level packed on import that reaches that far is ordinary content drawn with the background there.",
                    "objects");
        }

        #endregion

        #region Events

        private static void ExportEvents(Level level, VgdLevel target, ABExportContext context)
        {
            var framerate = context.Options.Framerate;
            var camera = level.Game.CameraEvents;
            var post = level.Game.PostProcessingEvents;
            var events = level.Game.Events;

            target.SetEvents(ABEventTrack.CameraPosition, Map(camera.Positions, framerate, context,
                key =>
                {
                    var (x, y) = ABValueMap.ExportVector(key.Pos, context.Report, "camera");
                    return new List<float> { x, y };
                }));

            // Halved on the way out for the same reason the import doubles it: over there the zoom
            // IS the camera's orthographic size, here it is the whole visible height.
            target.SetEvents(ABEventTrack.CameraZoom, Map(camera.Zooms, framerate, context,
                key => new List<float>
                {
                    Import.ABEventsImporter.ExportZoomValue(
                        ABValueMap.ExportFloat(key.Zoom, context.Report, "camera")),
                }));

            target.SetEvents(ABEventTrack.CameraRotation, Map(camera.Rotations, framerate, context,
                key => new List<float>
                {
                    ABValueMap.ExportFloat(key.Angle, context.Report, "camera")
                    * ABValueMap.RadiansToDegrees,
                }));

            target.SetEvents(ABEventTrack.CameraShake, Map(camera.Shakes, framerate, context,
                Import.ABEventsImporter.ExportShakeValues));

            target.SetEvents(ABEventTrack.Theme, MapThemes(events.Themes, framerate, context));

            // Each of these undoes exactly what the import did - see ABPostProcessingMap.
            target.SetEvents(ABEventTrack.Chromatic, Map(post.Chromatics, framerate, context,
                key => new List<float>
                {
                    ABPostProcessingMap.ExportChromatic(key.Intensity), 0f,
                }));

            target.SetEvents(ABEventTrack.Bloom, Map(post.Blooms, framerate, context,
                key => new List<float>
                {
                    ABPostProcessingMap.ExportBloomIntensity(key.Intensity),
                    ABPostProcessingMap.ExportBloomScatter(key.Scatter),
                    EffectIndex(key.Color4, ABColorMap.EffectColorWhite, context),
                }));

            target.SetEvents(ABEventTrack.Vignette, Map(post.Vignettes, framerate, context,
                key =>
                {
                    var (centerX, centerY) = ABValueMap.ExportVector(key.Center, context.Report, "vignette");
                    return new List<float>
                    {
                        ABPostProcessingMap.ExportVignetteIntensity(key.Intensity),
                        ABPostProcessingMap.ExportVignetteSmoothness(key.Smoothness),
                        key.Rounded ? 1f : 0f, 0f,
                        centerX, centerY,
                        EffectIndex(key.Color3, ABColorMap.EffectColorBlack, context),
                    };
                }));

            target.SetEvents(ABEventTrack.LensDistortion, Map(post.Lenses, framerate, context,
                key =>
                {
                    var (centerX, centerY) = ABValueMap.ExportVector(key.Center, context.Report, "lens");
                    return new List<float>
                    {
                        ABPostProcessingMap.ExportLensIntensity(key.Intensity),
                        ABPostProcessingMap.ExportLensCenter(centerX),
                        ABPostProcessingMap.ExportLensCenter(centerY),
                    };
                }));

            // ev is [Intensity, unused, TYPE, Response] - slot 2 is not a size, it is the preset
            // index the source game casts to URP's own FilmGrainLookup, which is what this format's
            // FilmGrainType is too (see ABPostProcessingMap's own note and the import beside it).
            // Writing a constant there sent every exported grain over as the same texture, whatever
            // the author picked. Only the luminance response has no field here.
            target.SetEvents(ABEventTrack.Grain, Map(post.Grains, framerate, context,
                key => new List<float>
                {
                    ABPostProcessingMap.ExportGrainIntensity(key.Intensity), 0f,
                    ABPostProcessingMap.ExportGrainType(key.Type), DefaultExportedGrainResponse,
                }));

            // ev is [Intensity, Width, Speed]. The last two have no counterpart here and no reader
            // there either (Afterbeat leaves both unimplemented), but a keyframe shaped like every
            // other one is what its parser expects.
            target.SetEvents(ABEventTrack.Glitch, Map(post.DigitalGlitches, framerate, context,
                key => new List<float>
                {
                    ABPostProcessingMap.ExportGlitchIntensity(key.Intensity),
                    DefaultExportedGlitchWidth, DefaultExportedGlitchSpeed,
                }));

            // Only Hue vs Hue crosses, and only as far as one number: Afterbeat's track is a single
            // rotation, so a curve that is not flat loses its shape and the other seven curves have
            // nowhere to go at all. Both are reported rather than silently flattened.
            var hueReshaped = false;
            target.SetEvents(ABEventTrack.Hue, Map(post.ColorCurves, framerate, context,
                key =>
                {
                    var hue = ABPostProcessingMap.ExportHueCurve(key.HueVsHue, out var isFlat);
                    if (!isFlat) hueReshaped = true;
                    if (HasOtherColorCurves(key)) hueReshaped = true;

                    return new List<float> { hue, 0f, 0f };
                }));
            if (hueReshaped)
            {
                context.Report.Dropped("color_curves_shape",
                    "Afterbeat's hue track is one rotation per keyframe, so a colour curve that is not flat - and every curve other than Hue vs Hue - has nothing to convert into.",
                    "game.post_processing.color_curves");
            }

            // Afterbeat's force track is a flat direction, so the directional half crosses and the
            // radial one (VelocityPoints) has nowhere to go.
            target.SetEvents(ABEventTrack.PlayerForce, Map(level.Game.PlayerEvents.Velocities,
                framerate, context, key =>
                {
                    var (x, y) = ABValueMap.ExportVector(key.Force, context.Report, "player_force");
                    return new List<float> { x, y, 0f };
                }));

            if (level.Game.PlayerEvents.VelocityPoints is { Count: > 0 })
                context.Report.Dropped("velocity_points",
                    "Afterbeat pushes the player in one flat direction; the points that push or pull from a place have no equivalent and are not exported.",
                    "player_events");

            ExportMarkersAndCheckpoints(level, target, context);
        }

        /// <summary> Luminance response Afterbeat is given when this format has none to give - its
        /// own neutral, since a response of zero is a grain that ignores the image under it. </summary>
        public const float DefaultExportedGrainResponse = 1f;

        /// <summary> Glitch width and speed Afterbeat is given when this format has neither. </summary>
        public const float DefaultExportedGlitchWidth = 1f;

        /// <summary> The source's glitch carries a speed this format has no counterpart for, so one is written. </summary>
        public const float DefaultExportedGlitchSpeed = 1f;

        // "NO THEME COLOUR" IS A VALUE, and it is the one every real level carries on the effects
        // its author never opened - index 9, one past the palette, which the source game reads as
        // white for a bloom and black for a vignette (ABEventsImporter's EffectColorNone). Matching
        // those two literals to the nearest palette slot instead hands an untouched bloom a theme
        // colour it never had, and makes it follow every theme switch afterwards.
        private static float EffectIndex(IColor4 color, Color4Value none, ABExportContext context)
        {
            if (color is Color4Value literal && IsSameColor(literal, none))
                return Import.ABEventsImporter.EffectColorNone;

            var (index, _) = ABColorMap.Export(color, ABPalette.Effects,
                context.ReferenceTheme, context.Report, "events");
            return index;
        }

        // The three-channel twin, for the post-processing colours that stopped carrying a fourth
        // component - see LiftGammaGainKey's header. "None" is still stated as a Color4Value because
        // that is what the palette constants are; only the comparison drops the alpha.
        private static float EffectIndex(IColor3 color, Color4Value none, ABExportContext context)
        {
            if (color is Color3Value literal
                && IsSameColor(new Color4Value(literal.R, literal.G, literal.B, none.A), none))
                return Import.ABEventsImporter.EffectColorNone;

            var (index, _) = ABColorMap.Export(color, ABPalette.Effects,
                context.ReferenceTheme, context.Report, "events");
            return index;
        }

        private static bool IsSameColor(Color4Value left, Color4Value right)
            => BHSDKMath.Approximately(left.R, right.R)
               && BHSDKMath.Approximately(left.G, right.G)
               && BHSDKMath.Approximately(left.B, right.B)
               && BHSDKMath.Approximately(left.A, right.A);

        private static List<VgdEventKeyframe> Map<T>(List<T> keys, int framerate,
            ABExportContext context, Func<T, List<float>> read) where T : IKeyframe
        {
            var exported = new List<VgdEventKeyframe>();
            if (keys == null) return exported;

            var sorted = new List<T>(keys);
            sorted.Sort((a, b) => a.Frame.CompareTo(b.Frame));

            foreach (var key in sorted)
            {
                var values = read(key);
                if (values == null) continue;

                exported.Add(new VgdEventKeyframe
                {
                    Time = ABTimeMap.ToSeconds(key.Frame, framerate),
                    Ease = ABEaseMap.Export(key.Ease, context.Report, "events"),
                    Values = JArray.FromObject(values),
                });
            }

            return exported;
        }

        private static List<VgdEventKeyframe> MapThemes(List<ThemeKeyframe> keys,
            int framerate, ABExportContext context)
        {
            var exported = new List<VgdEventKeyframe>();
            if (keys == null) return exported;

            var sorted = new List<ThemeKeyframe>(keys);
            sorted.Sort((a, b) => a.Frame.CompareTo(b.Frame));

            foreach (var key in sorted)
            {
                var exportedKey = new VgdEventKeyframe
                {
                    Time = ABTimeMap.ToSeconds(key.Frame, framerate),
                    Ease = ABEaseMap.Export(key.Ease, context.Report, "events"),
                };
                exportedKey.SetString(ToThemeSourceId(key.ThemeId));
                exported.Add(exportedKey);
            }

            return exported;
        }

        private static void ExportMarkersAndCheckpoints(Level level, VgdLevel target,
            ABExportContext context)
        {
            var framerate = context.Options.Framerate;
            var events = level.Game.Events;

            foreach (var marker in events.Markers)
                target.Markers.Add(new VgdMarker
                {
                    Id = marker.Frame.ToString(),
                    Name = marker.Name ?? string.Empty,
                    Description = marker.Description ?? string.Empty,
                    // Afterbeat stores a choice among seven editor colours, this format stores a
                    // colour, so the round trip snaps to the nearest - exact for anything that came
                    // from there, approximate for a colour authored here.
                    Color = ABEditorColors.Export(marker.Color4),
                    Time = ABTimeMap.ToSeconds(marker.Frame, framerate),
                });

            var spacedCheckpoint = false;
            foreach (var checkpoint in events.Checkpoints)
            {
                if (checkpoint.Space != CheckpointSpace.World) spacedCheckpoint = true;

                var (x, y) = ABValueMap.ExportVector(checkpoint.Position, context.Report, "checkpoints");
                target.Checkpoints.Add(new VgdCheckpoint
                {
                    Id = checkpoint.Frame.ToString(),
                    Name = checkpoint.Name ?? string.Empty,
                    Time = ABTimeMap.ToSeconds(checkpoint.Frame, framerate),
                    Position = new VgdVector2(x, y),
                });
            }

            if (spacedCheckpoint)
                context.Report.Approximated("checkpoint_space",
                    "Afterbeat respawn positions are always in world space; checkpoints using a camera-relative space export as if they were world-space.",
                    "checkpoints");
        }

        // Afterbeat carries ONE tempo for the whole level, in its editor block. The first segment is
        // the one exported because it is the one an author hears first; every other segment is a
        // tempo the exported file cannot change to.
        private static void ExportBpm(Level level, VgdLevel target, ABExportContext context)
        {
            var beats = level.Game.Events.Beats;
            if (beats == null || beats.Count == 0) return;

            var first = beats[0];
            target.Editor.Bpm.Value = first.BPM;
            target.Editor.Bpm.ValueDuplicate = first.BPM;
            target.Editor.Bpm.Offset = ABTimeMap.ToSeconds(first.Span.StartFrame, context.Options.Framerate)
                                       + first.Offset / context.Options.Framerate;

            if (beats.Count > 1)
                context.Report.Dropped("beats_single_tempo",
                    "Afterbeat stores one tempo per level; only the first beat segment was exported.",
                    "beats");
        }

        #endregion

        /// <summary> Whether every key of a screen-limit track pins the level to exactly the aspect
        /// Afterbeat itself runs at, which is the one limit that costs nothing to leave out. </summary>
        private static bool IsSourceAspectOnly(IEnumerable<ScreenLimitKey> keys)
        {
            foreach (var key in keys)
            {
                if (key?.ScreenLimit is not ScreenLimitFixed fixedLimit) return false;
                if (fixedLimit.Aspect == null) return false;
                if (fixedLimit.Aspect.Width != Import.ABEventsImporter.SourceAspectWidth
                    || fixedLimit.Aspect.Height != Import.ABEventsImporter.SourceAspectHeight)
                    return false;
            }

            return true;
        }

        private static void ReportUnsupported(Level level, InteropReport report)
        {
            if (level.Audio?.Tracks is { Count: > 0 })
                report.Dropped("audio_tracks",
                    "An Afterbeat level is one song file in its folder - it has no track list, offsets, speeds or audio effects, so none of this level's audio setup is exported. Put the song in the exported folder by hand.",
                    "audio");

            if (CountUnreferencedEffects(level) > 0)
                report.Dropped("effect_resources",
                    "Effect definitions nothing places are not written on their own - Afterbeat has no effect resources, only emitter objects, so an unused definition has nowhere to go.",
                    "resources.effects");

            if (level.Resources.CompositeShapes is { Count: > 0 })
                report.Dropped("shape_resources",
                    "Afterbeat has a fixed shape library; level-authored geometry is not exported and those objects fall back to a square.",
                    "resources.shapes");

            if (level.Resources.Textures is { Count: > 0 })
                report.Dropped("texture_resources",
                    "Afterbeat objects carry no texture; images used by this level are not exported.",
                    "resources.textures");

            ReportPostProcessing(level, report);
            ReportSilentDrops(level, report);
        }

        // Everything below has no Afterbeat counterpart AND no conversion that could have been got
        // wrong, which is exactly why none of it used to be reported: an export that says nothing
        // reads as an export that lost nothing. Each of these is content an author authored.
        private static void ReportSilentDrops(Level level, InteropReport report)
        {
            var events = level.Game.Events;
            var player = level.Game.PlayerEvents;

            // A screen limit has no field over there and is never written - but "not written" and
            // "lost" are not the same thing here, and saying the second when the first is true puts
            // a finding on every level that came FROM Afterbeat (the import pins one at the source
            // game's own 16:9, see ABEventsImporter.ImportScreenLimit). A limit that is exactly
            // that aspect describes the frame the target format already runs at, so dropping it
            // changes nothing about how the level plays; any other limit genuinely goes.
            if (events.ScreenLimits is { Count: > 0 })
            {
                if (IsSourceAspectOnly(events.ScreenLimits))
                    report.Info("screen_limit_matches_source",
                        "This level is limited to the aspect Afterbeat runs at anyway, so its screen limit is not written and nothing about the framing changes.",
                        "events");
                else
                    report.Dropped("screen_limits",
                        "Afterbeat has no playable-area limits and always runs at 16:9; this level's screen limit track is not exported, and a level built for another aspect will be framed differently there.",
                        "events");
            }

            if (events.Backgrounds is { Count: > 0 })
                report.Dropped("backgrounds",
                    "Afterbeat takes its background colour from the active theme rather than from a track of its own, so this level's background track is not exported.",
                    "events");

            // Velocities are NOT on this list - they have Afterbeat's own force track to go to.
            if (player.Visibilities is { Count: > 0 } || player.Controls is { Count: > 0 }
                                                      || player.Collisions is { Count: > 0 })
                report.Dropped("player_events",
                    "Afterbeat levels cannot hide the player, take control away or turn collision off; those tracks are not exported.",
                    "player_events");

            if (level.Settings.Seed != LevelRules.NullSeed)
                report.Dropped("seed",
                    "Afterbeat resolves randomness as it plays rather than from a seed; this level's seed is not exported and its random values were resolved to their midpoints.",
                    "settings");

            if (HasColouredMarkerOrCheckpoint(level))
                report.Dropped("event_colors",
                    "Afterbeat picks a marker's colour from its own editor palette and gives a checkpoint none at all, so the colours set here are not exported.",
                    "events");

            foreach (var checkpoint in events.Checkpoints)
            {
                if (checkpoint == null || checkpoint.Active) continue;
                report.Dropped("checkpoint_inactive",
                    "Afterbeat checkpoints are always active; a checkpoint turned off here exports as an ordinary one.",
                    "checkpoints");
                break;
            }

            if (level.Resources.Prefabs is { Count: > 0 })
                report.Info("prefab_type",
                    "Afterbeat sorts prefabs into categories this format does not have; every exported prefab is written as Misc 1.",
                    "prefabs");
        }

        // Level.Hints is advisory by definition - a cache of what the level needs, rebuilt on load -
        // so it is the one thing on this list that is not worth a line in a report an author reads.
        private static bool HasColouredMarkerOrCheckpoint(Level level)
        {
            foreach (var marker in level.Game.Events.Markers)
                if (marker != null && IsColoured(marker.Color4))
                    return true;

            foreach (var checkpoint in level.Game.Events.Checkpoints)
                if (checkpoint != null && IsColoured(checkpoint.Color4))
                    return true;

            return false;
        }

        // Anything that is not a plain white literal carries a decision the export cannot take with
        // it - including a theme reference, which has no colour of its own to compare.
        private static bool IsColoured(IColor4 color)
            => color is not Color4Value literal || !literal.Equals(Color4Value.white);

        private static void ReportPostProcessing(Level level, InteropReport report)
        {
            var post = level.Game.PostProcessingEvents;
            var unsupported =
                post.MotionBlurs is { Count: > 0 } ||
                post.LiftGammaGains is { Count: > 0 } ||
                post.ShadowsMidtonesHighlights is { Count: > 0 } ||
                post.WhiteBalances is { Count: > 0 };

            if (unsupported)
                report.Dropped("postprocessing_unsupported",
                    "Motion blur, lift/gamma/gain, shadows/midtones/highlights and white balance have no Afterbeat equivalent and are not exported.",
                    "postprocessing");

            // Colour curves DO have a destination - Afterbeat's hue track - but only half of one:
            // it rotates hue and knows nothing about saturation.
            if (post.ColorCurves is { Count: > 0 })
                report.Approximated("color_curves_hue_only",
                    "Afterbeat has a hue rotation but no saturation curve; only the hue half of this level's colour curves was exported.",
                    "postprocessing");

            if (post.AnalogGlitches is { Count: > 0 })
                report.Approximated("glitch_merged",
                    "Afterbeat has one glitch effect; the analog and digital tracks were merged into it, keeping the digital one's intensity.",
                    "postprocessing");
        }
        // An effect nobody places has nothing to become: over there an emitter IS the object, so a
        // definition only travels as the objects referencing it. One that is referenced is not a
        // loss and must not be reported as one.

        /// <summary> Effect definitions no object in the level points at. </summary>
        private static int CountUnreferencedEffects(Level level)
        {
            if (level.Resources?.Effects == null || level.Resources.Effects.Count == 0) return 0;

            var referenced = new HashSet<EffectId>();
            foreach (var value in level.Game.Objects.Values)
                if (value is EffectObject effect)
                    referenced.Add(effect.EffectId);

            var count = 0;
            foreach (var pair in level.Resources.Effects)
                if (!referenced.Contains(pair.Key))
                    count++;

            return count;
        }

        /// <summary> Whether a ColorCurves key authored anything Afterbeat has no track for - i.e.
        /// any curve but Hue vs Hue. </summary>
        private static bool HasOtherColorCurves(ColorCurvesKey key)
            => key.Master != null || key.Red != null || key.Green != null || key.Blue != null
               || key.HueVsSat != null || key.SatVsSat != null || key.LumVsSat != null;
    }
}