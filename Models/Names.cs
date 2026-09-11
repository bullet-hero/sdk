namespace BH.SDK.Models
{
    /// <summary>
    /// Every JSON key used by the format, in one place. Deliberately short and abbreviated to keep
    /// saved levels small; a key is only ever reused across models that can never co-occur.
    /// Changing a value here changes the on-disk format - historical snapshots keep their own copies
    /// precisely so that stays safe.
    /// </summary>
    public static class Names
    {
        // ---------------------------------------------------------------------------------------------
        // Single names
        // ---------------------------------------------------------------------------------------------

        /// <summary> BestRun.Frame, CheckpointDeaths.Frame. </summary>
        public const string Frame = "frame";

        /// <summary> Word fragment, built into DoubleClickTime, DoubleTapTime, MaxScrubTime, ResyncJumpTime and 4 more. </summary>
        public const string Time = "time";

        /// <summary> BoolKey.Frame, Checkpoint.Frame, Keyframe.Frame, Marker.Frame and 1 more. </summary>
        public const string FrameShort = "f";

        /// <summary> CurveKeyframeValue.Time, GradientAlphaKeyValue.Time, GradientColorKeyValue.Time. </summary>
        public const string TimeShort = "t";

        /// <summary> Keyframe.Ease, PostProcessingKeyframe.Ease. </summary>
        public const string Ease = "e";

        /// <summary> AutoFontSizeKey.MinValue, FloatMinMax.Min, FloatMinMaxStep.Min, IntMinMax.Min and 1 more. </summary>
        public const string Min = "min";

        /// <summary> AutoFontSizeKey.MaxValue, FloatMinMax.Max, FloatMinMaxStep.Max, IntMinMax.Max and 1 more. </summary>
        public const string Max = "max";

        /// <summary> FloatMinMaxStep.Step, IntMinMaxStep.Step, Vector2RectStep.Step, Vector3RectStep.Step and 1 more. </summary>
        public const string Step = "step";

        /// <summary> Word fragment, built into FrameStepBudget, ReplayStepBudget. </summary>
        public const string Budget = "budget";

        /// <summary> Word fragment, built into ReplayStepBudget. </summary>
        public const string Replay = "replay";

        /// <summary> Word fragment, built into CalibrateOnStart. </summary>
        public const string Start = "start";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string End = "end";

        /// <summary> Word fragment, built into PrevObjectId. </summary>
        public const string Prev = "prev";

        /// <summary> Word fragment, built into NextObjectId. </summary>
        public const string Next = "next";

        /// <summary> EffectShapeLine.Start. </summary>
        public const string StartShort = "s";

        /// <summary> EffectShapeLine.End. </summary>
        public const string EndShort = "e";

        /// <summary> AntiAliasingGraphicsSettings.Type, AudioResource.Type, BytesResource.Type, FontResource.Type and 3 more. </summary>
        public const string Type = "type";

        /// <summary> TextureResource.Kind. </summary>
        public const string Kind = "kind";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Value = "v";

        /// <summary> The versioned envelope's own generation key - the OTHER half of {g, v}. Not
        /// Version below, which is the author's own version of a level: one file would then carry
        /// "vrs" meaning two different things at two depths. The letter is reused by ChannelG and
        /// GlobalShort, which is fine - keys are local to their object, and an envelope holds
        /// nothing but these two. </summary>
        public const string Generation = "g";

        /// <summary> The word "value", for a settings key that spells it out - Value
        /// itself is the ENVELOPE's payload key and is one character. </summary>
        public const string ValueWord = "value";

        /// <summary> Word fragment, built into MaxDataFileBytes. </summary>
        public const string Data = "data";

        /// <summary> FilmGrainKey.Type. </summary>
        public const string TypeShort = "tp";

        /// <summary> Alignment.Value, CurveKeyframeValue.Value, FloatValue.Value, IntValue.Value and 4 more. </summary>
        public const string ValueShort = "v";

        /// <summary> EditorSerializationSettings.LevelMode. </summary>
        public const string Level = "level";

        /// <summary> AudioDistortion.Level. </summary>
        public const string LevelShort = "lvl";

        /// <summary> BestRun.LevelVersion, LevelMeta.LevelVersion, LevelStatistics.LevelVersion.
        /// The AUTHOR's version of a level, a System.Version written as a string - never the
        /// envelope's, which is Generation above. </summary>
        public const string Version = "vrs";

        /// <summary> Word fragment, built into RequireResourceMeta, ResourcesMeta. </summary>
        public const string Meta = "meta";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Track = "track";

        /// <summary> AudioLevel.Tracks, LimitHints.Tracks. </summary>
        public const string Tracks = "tracks";

        /// <summary> Level.Settings. </summary>
        public const string Settings = "settings";

        /// <summary> UserSettings.General. </summary>
        public const string General = "general";

        /// <summary> UserSettings.Graphics. </summary>
        public const string Graphics = "graphics";

        /// <summary> GameEditorSettings.Interface, UserSettings.Interface. </summary>
        public const string Interface = "iface";

        /// <summary> Word fragment, built into StatsActive, StatsAlignmentX, StatsAlignmentY and 3 more. </summary>
        public const string Stats = "stats";

        /// <summary> Word fragment, built into StatsMemory. </summary>
        public const string Memory = "memory";

        /// <summary> Word fragment, built into ShowAllFoundContent, ShowGameInterface, ShowGamePause, ShowGameProgress. </summary>
        public const string Show = "show";

        /// <summary> Word fragment, built into ShowAllFoundContent. </summary>
        public const string All = "all";

        /// <summary> BestRun.Progress. </summary>
        public const string Progress = "progress";

        /// <summary> Word fragment, built into ShowGamePause. </summary>
        public const string Pause = "pause";

        /// <summary> AudioSettings.Game, Level.Game. </summary>
        public const string Game = "game";

        /// <summary> AudioSettings.UI. </summary>
        public const string UI = "ui";

        /// <summary> GraphicsSettings.Audio, Level.Audio, UserSettings.Audio. </summary>
        public const string Audio = "audio";

        /// <summary> LevelResources.Audios. </summary>
        public const string Audios = "audios";

        /// <summary> Word fragment, built into AudioId. </summary>
        public const string AudioShort = "a";

        /// <summary> EditorSerializationSettings.ResourcesMode, Level.Resources. </summary>
        public const string Resources = "resources";

        /// <summary> Word fragment, built into BotDebugTarget, FpsTarget, TargetDeltaTime. </summary>
        public const string Target = "target";

        /// <summary> Word fragment, built into FpsFixed. </summary>
        public const string Fixed = "fixed";

        /// <summary> BaseGraphicsSettings.Render. </summary>
        public const string Render = "render";

        /// <summary> EffectObjectCore.Render. </summary>
        public const string RenderShort = "r";

        /// <summary> Word fragment, built into ResourceParallelLoadCount. </summary>
        public const string Load = "load";

        /// <summary> Word fragment, built into ResourceParallelLoadCount. </summary>
        public const string Parallel = "parallel";

        /// <summary> Word fragment, built into ResourceWebTimeout. </summary>
        public const string Web = "web";

        /// <summary> Word fragment, built into ResourceWebTimeout. </summary>
        public const string Timeout = "timeout";

        /// <summary> Word fragment, built into TargetDeltaTime. </summary>
        public const string Delta = "delta";

        /// <summary> AudioGraphicsSettings.UseScrub. </summary>
        public const string Scrub = "scrub";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Diff = "diff";

        /// <summary> Word fragment, built into ResyncJumpTime. </summary>
        public const string Resync = "resync";

        /// <summary> Word fragment, built into DeadZone, SyncDeadZone. </summary>
        public const string Dead = "dead";

        /// <summary> Word fragment, built into DeadZone, SyncDeadZone. </summary>
        public const string Zone = "zone";

        /// <summary> Word fragment, built into PitchCorrection. </summary>
        public const string Correction = "correction";

        /// <summary> Word fragment, built into IsLocal. </summary>
        public const string Is = "is";

        /// <summary> Author.Name, BeatSegment.Name, Checkpoint.Name, CustomLicense.LicenseName and 9 more. </summary>
        public const string Name = "name";

        /// <summary> ResourceMeta.ResourceTitle, TrustedSource.Title. </summary>
        public const string Title = "title";

        /// <summary> LevelMeta.LevelDescription, Marker.Description, ResourceMeta.ResourceDescription. </summary>
        public const string Description = "desc";

        /// <summary> BaseDeviceControlsSettings.Active. </summary>
        public const string Active = "active";

        /// <summary> Checkpoint.Active, LevelTrackEffects.Active, PostProcessingEvents.Active, PostProcessingKeyframe.Active and 1 more. </summary>
        public const string ActiveShort = "a";

        // EVERY KEYFRAME PAYLOAD IS ONE KEY. A keyframe's concrete type comes from the static
        // type of the track holding it, and the polymorphic value inside carries its own
        // positional tag - nothing ever inferred a type from the property name, so spelling the
        // type out cost 3 characters on 176 383 of volcano's keys and bought nothing.

        /// <summary> <c>"v"</c> - BoolKey.Value. </summary>
        public const string Bool = ValueShort;

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Byte = "byte";

        /// <summary> <c>"v"</c> - IntKey.Value, LayerKey.Layer. </summary>
        public const string Int = ValueShort;

        /// <summary> <c>"v"</c> - AngleKey.Angle, AppearingKey.Value, FillmentKey.Value, FloatKey.Value and 2 more. </summary>
        public const string Float = ValueShort;

        /// <summary> <c>"v"</c> - AlignmentKey.Value, PosKey.Pos, ScaKey.Scale, Vector2Key.Value. </summary>
        public const string Vector2 = ValueShort;

        /// <summary> <c>"v"</c> - Vector3Key.Value. </summary>
        public const string Vector3 = ValueShort;

        /// <summary> <c>"v"</c> - Vector4Key.Value. </summary>
        public const string Vector4 = ValueShort;

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string String = "string";

        /// <summary> StringLocalized.Strings. </summary>
        public const string Strings = "strs";

        /// <summary> GeneralSettings.Language, StringLanguage.LanguageCode. </summary>
        public const string Language = "lang";

        /// <summary> Word fragment, built into PrefabIndex. </summary>
        public const string Index = "idx";

        /// <summary> Word fragment, built into AudioId, ColliderId, LevelId, NextObjectId and 4 more. </summary>
        public const string Id = "id";

        /// <summary> Word fragment, built into ObjectIds. </summary>
        public const string Ids = "ids";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Guid = "guid";

        /// <summary> CameraEvents.Positions, Checkpoint.Position, RectObject.Positions. </summary>
        public const string Position = "pos";

        /// <summary> CameraEvents.Rotations, RectObject.Rotations. </summary>
        public const string Rotation = "rot";

        /// <summary> EditorGizmosSettings.Scale, EffectData.Scale, EffectScaleValue.Scale, LensDistortionKey.Scale and 1 more. </summary>
        public const string Scale = "sca";

        /// <summary> CameraEvents.Zooms. </summary>
        public const string Zoom = "zoom";

        /// <summary> CameraEvents.Shakes. </summary>
        public const string Shake = "shake";

        /// <summary> EditorGridSettings.Size, EffectShapeRectangle.Size, PlayerEvents.Sizes, RectObject.Sizes. </summary>
        public const string Size = "sz";

        /// <summary> EffectAngleValue.Angle, EffectData.Angle. </summary>
        public const string Angle = "ang";

        /// <summary> EffectAngleCurvesBySpeed.Curve, EffectAngleCurvesOverLife.Curve. </summary>
        public const string Curve = "crv";

        /// <summary> EffectColorGradientBySpeed.Gradient, EffectColorGradientOverLife.Gradient, EffectColorGradientRandom.Gradient. </summary>
        public const string Gradient = "grd";

        /// <summary> BeatSegment.Color4, BloomKey.Color4, Checkpoint.Color4, Color3Key.Value and 8 more. </summary>
        public const string Color = "clr";

        /// <summary> TextureResource.Alpha. </summary>
        public const string Alpha = "alpha";

        /// <summary> GradientAlphaKeyValue.Alpha. </summary>
        public const string AlphaShort = "a";

        /// <summary> Color3Value.R, Color4Value.R. </summary>
        public const string ChannelR = "r";

        /// <summary> Color3Value.G, Color4Value.G. </summary>
        public const string ChannelG = "g";

        /// <summary> Color3Value.B, Color4Value.B. </summary>
        public const string ChannelB = "b";

        /// <summary> Color4Value.A. </summary>
        public const string ChannelA = "a";

        /// <summary> ShakeKey.IntensityX, Vector2Circle.X, Vector2Value.X, Vector3Circle.X and 3 more. </summary>
        public const string CoordX = "x";

        /// <summary> ShakeKey.IntensityY, Vector2Circle.Y, Vector2Value.Y, Vector3Circle.Y and 3 more. </summary>
        public const string CoordY = "y";

        /// <summary> Vector3Circle.Z, Vector3Value.Z, Vector4Circle.Z, Vector4Value.Z. </summary>
        public const string CoordZ = "z";

        /// <summary> Vector4Circle.W, Vector4Value.W. </summary>
        public const string CoordW = "w";

        /// <summary> Word fragment, built into AngleA, ColorA, ScaleA. </summary>
        public const string ValueA = "a";

        /// <summary> Word fragment, built into AngleB, ColorB, ScaleB. </summary>
        public const string ValueB = "b";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string ValueC = "c";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string ValueD = "d";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num0 = "0";

        /// <summary> Word fragment, built into Point1, WetMixTap1. </summary>
        public const string Num1 = "1";

        /// <summary> Word fragment, built into Point2, WetMixTap2. </summary>
        public const string Num2 = "2";

        /// <summary> Word fragment, built into Point3, WetMixTap3. </summary>
        public const string Num3 = "3";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num4 = "4";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num5 = "5";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num6 = "6";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num7 = "7";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num8 = "8";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Num9 = "9";

        /// <summary> Word fragment, built into ColorBottom. </summary>
        public const string AlignmentB = "b"; // bottom

        /// <summary> Word fragment, built into ColorTop. </summary>
        public const string AlignmentT = "t"; // top

        /// <summary> Word fragment, built into ColorLeft. </summary>
        public const string AlignmentL = "l"; // left

        /// <summary> Word fragment, built into ColorRight. </summary>
        public const string AlignmentR = "r"; // right

        /// <summary> Word fragment, built into ColorBL. </summary>
        public const string AlignmentBL = "bl"; // bottom-left

        /// <summary> Word fragment, built into ColorBM. </summary>
        public const string AlignmentBM = "bm"; // bottom-middle

        /// <summary> Word fragment, built into ColorBR. </summary>
        public const string AlignmentBR = "br"; // bottom-right

        /// <summary> Word fragment, built into ColorCL. </summary>
        public const string AlignmentCL = "cl"; // center-left

        /// <summary> Word fragment, built into ColorCM. </summary>
        public const string AlignmentCM = "cm"; // center-middle

        /// <summary> Word fragment, built into ColorCR. </summary>
        public const string AlignmentCR = "cr"; // center-right

        /// <summary> Word fragment, built into ColorTL. </summary>
        public const string AlignmentTL = "tl"; // top-left

        /// <summary> Word fragment, built into ColorTM. </summary>
        public const string AlignmentTM = "tm"; // top-middle

        /// <summary> Word fragment, built into ColorTR. </summary>
        public const string AlignmentTR = "tr"; // top-right

        /// <summary> EffectShapeSpreadLoop.Speed, EffectShapeSpreadPingPong.Speed, LevelTrack.Speed, PlayerEvents.Speeds and 1 more. </summary>
        public const string Speed = "spd";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Range = "range";

        /// <summary> EffectData.Core. </summary>
        public const string Core = "core";

        /// <summary> Velocity.Force, VelocityPoint.Force. </summary>
        public const string Force = "frc";

        /// <summary> EffectData.Forces. </summary>
        public const string Forces = "frcs";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Linear = "linear";

        /// <summary> PlayerEvents.Velocities. </summary>
        public const string Velocity = "velocity";

        /// <summary> Word fragment, built into VelocityPoint. </summary>
        public const string Point = "point";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Points = "points";

        /// <summary> Word fragment, built into Point1, Point2, Point3. </summary>
        public const string PointShort = "p";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Angular = "angular";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Orbital = "orbital";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Gravity = "gravity";

        /// <summary> BeatSegment.Offset, UVKey.Offset. </summary>
        public const string Offset = "off";

        /// <summary> LensDistortionKey.Center, VelocityPoint.Center, VignetteKey.Center. </summary>
        public const string Center = "cntr";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string CenterShort = "c";

        /// <summary> BloomKey.Intensity, ChromaticAberrationKey.Intensity, DigitalGlitchKey.Intensity, FilmGrainKey.Intensity and 4 more. </summary>
        public const string Intensity = "intns";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Collision = "collision";

        /// <summary> PlayerEvents.Collisions. </summary>
        public const string Collisions = "collisions";

        /// <summary> EffectShapeCircle.Radius, Vector2Circle.Radius, Vector3Circle.Radius, Vector4Circle.Radius. </summary>
        public const string Radius = "rad";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string RadiusShort = "r";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Major = "major";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Minor = "minor";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Top = "top";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Base = "base";

        /// <summary> EffectShapeCircle.Thickness. </summary>
        public const string Thickness = "thk";

        /// <summary> EffectShapeCircle.Arc, EffectShapeCone.Arc, EffectShapeTorus.Arc. </summary>
        public const string Arc = "arc";

        /// <summary> Word fragment, built into ResolutionWidth. </summary>
        public const string Width = "width";

        /// <summary> Word fragment, built into ResolutionHeight. </summary>
        public const string Height = "height";

        /// <summary> ScreenAspect.Width. </summary>
        public const string WidthShort = "w";

        /// <summary> EffectShapeCone.Height, ScreenAspect.Height. </summary>
        public const string HeightShort = "h";

        /// <summary> EffectShapeCircle.Spread, EffectShapeCone.Spread, EffectShapeLine.Spread, EffectShapeSpreadLoop.Spread and 3 more. </summary>
        public const string Spread = "spr";

        /// <summary> ThemeData.Matrix. </summary>
        public const string Matrix = "mtx";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Theme = "theme";

        /// <summary> GameEvents.Themes, LevelResources.Themes. </summary>
        public const string Themes = "themes";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Marker = "marker";

        /// <summary> GameEvents.Markers. </summary>
        public const string Markers = "markers";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Beat = "beat";

        /// <summary> GameEvents.Beats. </summary>
        public const string Beats = "beats";

        /// <summary> BeatSegment.BPM. </summary>
        public const string BPM = "bpm";

        /// <summary> BeatSegment.BeatsPerBar. </summary>
        public const string BeatsPerBar = "bpb";

        /// <summary> Word fragment, built into CheckpointRestarts, DeathsBeforeCheckpoint, DeathsByCheckpoint. </summary>
        public const string Checkpoint = "checkpoint";

        /// <summary> GameEvents.Checkpoints, RunProfile.UseCheckpoints. </summary>
        public const string Checkpoints = "checkpoints";

        /// <summary> Word fragment, built into MenuBackground. </summary>
        public const string Background = "background";

        /// <summary> GameEvents.Backgrounds. </summary>
        public const string Backgrounds = "backgrounds";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string BackgroundShort = "bg";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Object = "object";

        /// <summary> ClipboardData.Objects, GameLevel.Objects, Prefab.Objects. </summary>
        public const string Objects = "objs";

        /// <summary> Word fragment, built into StatsFrameObjects, StatsLevelObjects. The spelled-out
        /// twin of <see cref="Objects"/>, and the pair is the SINGLE/MULTIPLE split of docs/NAMING.md
        /// rather than an oversight: a level holds thousands of the abbreviated one, settings.json
        /// holds one of each of these and a person is the one reading it. </summary>
        public const string ObjectsFull = "objects";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Instance = "instance";

        /// <summary> LimitHints.Instances. </summary>
        public const string Instances = "instances";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Parent = "parent";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string ObjectShort = "obj";

        /// <summary> Word fragment, built into ParentObjectId. </summary>
        public const string ParentShort = "p";

        /// <summary> PostProcessingEvents.Blooms. </summary>
        public const string BloomShort = "blm";

        /// <summary> PostProcessingEvents.Chromatics. </summary>
        public const string ChromaticShort = "chr";

        /// <summary> PostProcessingEvents.Vignettes. </summary>
        public const string VignetteShort = "vgn";

        /// <summary> PostProcessingEvents.Lenses. </summary>
        public const string LensShort = "lns";

        /// <summary> PostProcessingEvents.Grains. </summary>
        public const string GrainShort = "grn";

        /// <summary> PostProcessingEvents.MotionBlurs. </summary>
        public const string MotionBlurShort = "mbr";

        /// <summary> PostProcessingEvents.ColorCurves. </summary>
        public const string ColorCurvesShort = "ccv";

        /// <summary> PostProcessingEvents.LiftGammaGains. </summary>
        public const string LiftGammaGainShort = "lgg";

        /// <summary> PostProcessingEvents.ShadowsMidtonesHighlights. </summary>
        public const string ShadowsMidtonesHighlightsShort = "smh";

        /// <summary> PostProcessingEvents.WhiteBalances. </summary>
        public const string WhiteBalanceShort = "wbl";

        /// <summary> PostProcessingEvents.AnalogGlitches. </summary>
        public const string AnalogGlitchShort = "agl";

        /// <summary> PostProcessingEvents.DigitalGlitches. </summary>
        public const string DigitalGlitchShort = "dgl";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Classic = "classic";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Control = "control";

        /// <summary> PlayerEvents.Controls, UserSettings.Controls. </summary>
        public const string Controls = "controls";

        /// <summary> PlayerEvents.Visibilities. </summary>
        public const string Visibles = "visibles";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Layer = "layer";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Layers = "layers";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Pivot = "pivot";

        /// <summary> Word fragment, built into DashButtonAnchor, JoystickAnchor. </summary>
        public const string Anchor = "anc";

        /// <summary> RectObject.Layer. </summary>
        public const string LayerShort = "l";

        /// <summary> CameraEvents.Pivots, RectObject.Pivots. </summary>
        public const string PivotShort = "pv";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string AnchorShort = "a";

        /// <summary> Word fragment, built into ColliderOpacity, ColliderOpacityView, LinkColliderShape, ParticleCollider and 1 more. </summary>
        public const string Collider = "collider";

        /// <summary> Word fragment, built into ColliderId. </summary>
        public const string ColliderShort = "c";

        /// <summary> Word fragment, built into Shader, ShapeId. </summary>
        public const string ShapeShort = "sh";

        /// <summary> CompositeShape.Vertices. </summary>
        public const string Vertices = "vts";

        /// <summary> CompositeShape.Indices. </summary>
        public const string Indices = "idxs";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Texture = "texture";

        /// <summary> GraphicsSettings.Textures, LevelResources.Textures. </summary>
        public const string Textures = "textures";

        /// <summary> CustomLicense.LicenseText, TextObject.Text. </summary>
        public const string Text = "text";

        /// <summary> LimitHints.Texts. </summary>
        public const string Texts = "texts";

        /// <summary> Word fragment, built into FontCharacters. </summary>
        public const string Font = "font";

        /// <summary> LevelResources.Fonts. </summary>
        public const string Fonts = "fonts";

        /// <summary> CachedFontText.Characters. </summary>
        public const string Chars = "chrs";

        /// <summary> Word fragment, built into FillDirection. </summary>
        public const string Fill = "fill";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Mask = "mask";

        /// <summary> FillmentKey.Direction. </summary>
        public const string Direction = "dir";

        /// <summary> Word fragment, built into MaxResourceBytes, RequireResourceMeta, RequireResourceUrl, ResourceId and 3 more. </summary>
        public const string Resource = "resource";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Res = "res";

        /// <summary> ShapeObject.UVs. </summary>
        public const string UV = "uv";

        /// <summary> UVKey.Tiling. </summary>
        public const string Tiling = "til";

        /// <summary> Word fragment, built into LoopGlobal, LoopLocal. </summary>
        public const string Loop = "loop";

        /// <summary> EffectObjectCore.Loop. </summary>
        public const string LoopShort = "l";

        /// <summary> Word fragment, built into ParticleCollider. </summary>
        public const string Particle = "particle";

        /// <summary> Word fragment, built into ResourceParallelLoadCount. </summary>
        public const string Count = "count";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Counter = "counter";

        /// <summary> EffectObjectCore.LifetimeBounds. </summary>
        public const string Lifetime = "lt";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Has = "has";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Stop = "stop";

        /// <summary> Word fragment, built into IsLocal, LocalFrame, LoopLocal. </summary>
        public const string Local = "local";

        /// <summary> Word fragment, built into LoopGlobal. </summary>
        public const string Global = "global";

        /// <summary> Word fragment, built into LocalFrameShort. </summary>
        public const string LocalShort = "l";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string GlobalShort = "g";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Effect = "effect";

        /// <summary> GameEditorSettings.Effects, GraphicsSettings.Effects, LevelResources.Effects, LevelTrack.Effects and 1 more. </summary>
        public const string Effects = "eff";

        /// <summary> Word fragment, built into EffAngle, EffColor, EffScale, EffShape. </summary>
        public const string Eff = "eff";

        /// <summary> EffectData.Shape. </summary>
        public const string Shape = "shp";

        /// <summary> LevelResources.CompositeShapes. </summary>
        public const string Shapes = "shapes";

        /// <summary> LimitHints.ShapesOpaque. </summary>
        public const string ShapesOpaque = "shapes_opaque";

        /// <summary> LimitHints.ShapesTransparent. </summary>
        public const string ShapesTransparent = "shapes_transparent";

        /// <summary> <c>"sh"</c> - ShapeObject.ShaderType. </summary>
        public const string Shader = ShapeShort;

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Triangle = "triangle";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Triangles = "triangles";

        /// <summary> Word fragment, built into PrefabIndex, PrefabObjects. </summary>
        public const string Prefab = "prefab";

        /// <summary> LevelResources.Prefabs. </summary>
        public const string Prefabs = "prefabs";

        /// <summary> PrefabObject.Modifications. </summary>
        public const string Mod = "mod";

        /// <summary> Modification.Key, TrustedSource.Key. </summary>
        public const string Key = "key";

        /// <summary> Pair.?. </summary>
        public const string KeyShort = "k";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Property = "property";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Path = "path";

        /// <summary> ModificationKey.Path. </summary>
        public const string PathShort = "p";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Order = "order";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Author = "author";

        /// <summary> LevelMeta.LevelAuthors, ResourceMeta.ResourceAuthors. </summary>
        public const string Authors = "authors";

        /// <summary> Author.Credit. </summary>
        public const string Credit = "credit";

        // ONE SPELLING FOR A SOURCE. Resource.Sources wrote src while ResourceMeta and
        // PublishProfile wrote sources, for the same concept.

        /// <summary> Resource.Sources. </summary>
        public const string Src = "src";

        /// <summary> <c>"src"</c> - NoSpecifiedLicense.Source. </summary>
        public const string Source = Src;

        /// <summary> <c>"src"</c> - PublishProfile.Sources, ResourceMeta.ResourceSources. </summary>
        public const string Sources = Src;

        /// <summary> Word fragment, built into LinkColliderShape. </summary>
        public const string Link = "link";

        /// <summary> ResourceKey.Uri. </summary>
        public const string Uri = "uri";

        /// <summary> Author.Url, CustomLicense.LicenseUrl, ResourceMeta.ResourceUrl, TrustedSource.Url. </summary>
        public const string Url = "url";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Vertical = "vertical";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Horizontal = "horizontal";

        /// <summary> Word fragment, built into StatsAlignmentX, StatsAlignmentY. </summary>
        public const string Alignment = "alignment";

        /// <summary> Word fragment, built into VerticalAlignment. </summary>
        public const string VerticalShort = "v";

        /// <summary> Word fragment, built into HorizontalAlignment. </summary>
        public const string HorizontalShort = "h";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string AlignmentShort = "align";

        /// <summary> Word fragment, built into HorizontalAlignment, VerticalAlignment. </summary>
        public const string AlignmentShortest = "a";

        /// <summary> Word fragment, built into OverEdge. </summary>
        public const string Over = "over";

        /// <summary> Word fragment, built into UnderEdge. </summary>
        public const string Under = "under";

        /// <summary> Word fragment, built into EdgeHandle, OverEdge, UnderEdge. </summary>
        public const string Edge = "edge";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Distrib = "distrib";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Scan = "scan";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Line = "line";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Jitter = "jitter";

        /// <summary> Word fragment, built into ResyncJumpTime. </summary>
        public const string Jump = "jump";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Jmp = "jmp";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Drift = "drift";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Hue = "hue";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Sat = "sat";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Vs = "vs";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Lum = "lum";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Master = "master";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Red = "red";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Green = "green";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Blue = "blue";

        /// <summary> BloomKey.Scatter. </summary>
        public const string Scatter = "sctr";

        /// <summary> LensDistortionKey.Multiplier. </summary>
        public const string Multiplier = "mult";

        /// <summary> Word fragment, built into RequiresHold. </summary>
        public const string Multi = "multi";

        /// <summary> LiftGammaGainKey.Lift. </summary>
        public const string Lift = "lift";

        /// <summary> LiftGammaGainKey.Gamma. </summary>
        public const string Gamma = "gamma";

        /// <summary> LiftGammaGainKey.Gain. </summary>
        public const string Gain = "gain";

        /// <summary> ShadowsMidtonesHighlightsKey.Shadows. </summary>
        public const string Shadow = "shw";

        /// <summary> ShadowsMidtonesHighlightsKey.Midtones. </summary>
        public const string Midtone = "mtn";

        /// <summary> ShadowsMidtonesHighlightsKey.Highlights. </summary>
        public const string Highlight = "hlt";

        /// <summary> Word fragment, built into SizeLimit. </summary>
        public const string Limit = "limit";

        /// <summary> LevelHints.Limits. </summary>
        public const string Limits = "limits";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string LimitShort = "lmt";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Hint = "hint";

        /// <summary> Level.Hints. </summary>
        public const string Hints = "hints";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string HintShort = "hnt";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Capacity = "capacity";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string CapacityShort = "cap";

        /// <summary> VignetteKey.Smoothness. </summary>
        public const string Smoothness = "smo";

        /// <summary> VignetteKey.Rounded. </summary>
        public const string Rounded = "rou";

        /// <summary> WhiteBalanceKey.Temperature. </summary>
        public const string Temperature = "tem";

        /// <summary> WhiteBalanceKey.Tint. </summary>
        public const string Tint = "tnt";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string In = "in";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Out = "out";

        /// <summary> Word fragment, built into InTangent, InWeight. </summary>
        public const string InShort = "i";

        /// <summary> Word fragment, built into OutTangent, OutWeight. </summary>
        public const string OutShort = "o";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Tangent = "tangent";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Weight = "weight";

        /// <summary> Word fragment, built into InTangent, OutTangent, TangentMode. </summary>
        public const string TangentShort = "t";

        /// <summary> Word fragment, built into InWeight, OutWeight, WeightedMode. </summary>
        public const string WeightShort = "w";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Weighted = "weighted";

        /// <summary> DeviceGyroControlsSettings.Mode, GamepadControlsSettings.Mode, GradientValue.Mode, KeyboardMouseControlsSettings.Mode and 1 more. </summary>
        public const string Mode = "mode";

        /// <summary> AppearingKey.Mode. </summary>
        public const string ModeShort = "m";

        /// <summary> CurveValue.KeyFrames, KeybindingsSettings.Overrides, UserSettings.Keybindings. </summary>
        public const string Keys = "keys";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Pre = "pre";

        /// <summary> Word fragment, built into PostProcessing, PostProcessingEvents, PostProcessingKeys. </summary>
        public const string Post = "post";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Word = "word";

        /// <summary> Word fragment, built into WrapU, WrapV; TextObject.WordWrap. </summary>
        public const string Wrap = "wrap";

        /// <summary> <c>"wrap_u"</c> - TextureResource.WrapU. </summary>
        public const string WrapU = Wrap + _ + "u";

        /// <summary> <c>"wrap_v"</c> - TextureResource.WrapV. </summary>
        public const string WrapV = Wrap + _ + "v";

        /// <summary> TextureResource.Sampling. </summary>
        public const string Sampling = "sampling";

        /// <summary> Checkpoint.Space. </summary>
        public const string Space = "spc";

        /// <summary> EffectShapeCircle.Aspect, ScreenLimitFixed.Aspect. </summary>
        public const string Aspect = "asp";

        /// <summary> BestRun.Seed, LevelSettings.Seed. </summary>
        public const string Seed = "seed";

        /// <summary> LevelSettings.Fps. </summary>
        public const string Fps = "fps";

        /// <summary> Word fragment, built into TimeFormat. </summary>
        public const string Format = "format";

        /// <summary> Word fragment, built into HistoryLength. </summary>
        public const string Length = "length";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string LengthShort = "len";

        /// <summary> LevelMeta.LevelDuration. </summary>
        public const string Duration = "duration";

        /// <summary> LevelMeta.LevelTags. </summary>
        public const string Tags = "tags";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string DurationShort = "dur";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Span = "span";

        /// <summary> BeatSegment.?, IFrameBounds.Span, LevelTrack.Span, RectObject.Span. </summary>
        public const string SpanShort = "sp";

        /// <summary> Word fragment, built into ScreenLimits, ScreenOrientation. </summary>
        public const string Screen = "screen";

        /// <summary> LevelSettings.Orientation. </summary>
        public const string Orientation = "orientation";

        /// <summary> Word fragment, built into WindowMode. </summary>
        public const string Window = "window";

        /// <summary> GraphicsSettings.Display. </summary>
        public const string Display = "display";

        /// <summary> Word fragment, built into ResolutionHeight, ResolutionWidth. </summary>
        public const string Resolution = "resolution";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Event = "event";

        /// <summary> GameLevel.Events. </summary>
        public const string Events = "events";

        /// <summary> GameEditorSettings.Camera. </summary>
        public const string Camera = "camera";

        /// <summary> Word fragment, built into PostProcessing, PostProcessingEvents, PostProcessingKeys. </summary>
        public const string Processing = "processing";

        /// <summary> GameEditorSettings.Player. </summary>
        public const string Player = "player";

        /// <summary> GameStatistics.Editor, LevelStatistics.Editor. </summary>
        public const string Editor = "editor";

        /// <summary> Word fragment, built into PitchCorrection. </summary>
        public const string Pitch = "pitch";

        /// <summary> AudioPitchShifter.Pitch. </summary>
        public const string PitchShort = "pth";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Stereo = "stereo";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Pan = "pan";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Mixer = "mixer";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Up = "up";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Down = "down";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Pass = "pass";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Low = "low";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string High = "high";

        /// <summary> LevelTrackEffects.Echo. </summary>
        public const string Echo = "echo";

        /// <summary> AudioReverb.Reverb, LevelTrackEffects.Reverb. </summary>
        public const string Reverb = "rvrb";

        /// <summary> LevelTrackEffects.Chorus. </summary>
        public const string Chorus = "chrs";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Shifter = "shifter";

        /// <summary> LevelTrackEffects.Distortion. </summary>
        public const string Distortion = "dist";

        /// <summary> LevelTrackEffects.Flange. </summary>
        public const string Flange = "flng";

        /// <summary> LevelTrackEffects.Compressor. </summary>
        public const string Compressor = "cmpr";

        /// <summary> LevelTrackEffects.Normalize. </summary>
        public const string Normalize = "nrml";

        /// <summary> LevelTrackEffects.ParamEQ. </summary>
        public const string ParamEQ = "pmeq";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Mix = "mix";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Dry = "dry";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Wet = "wet";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Cutoff = "cutoff";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Freq = "freq";

        /// <summary> AudioChorus.Delay, AudioEcho.Delay. </summary>
        public const string Delay = "dly";

        /// <summary> AudioEcho.Decay. </summary>
        public const string Decay = "dcy";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Ratio = "ratio";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string HF = "hf";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string LF = "lf";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Channels = "channels";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Tap1 = "tap1";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Tap2 = "tap2";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Tap3 = "tap3";

        /// <summary> AudioReverb.Room. </summary>
        public const string Room = "room";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Reflect = "rfl";

        /// <summary> AudioReverb.Reflections. </summary>
        public const string Reflections = "rfls";

        /// <summary> AudioReverb.Diffusion. </summary>
        public const string Diffusion = "dffs";

        /// <summary> AudioReverb.Density. </summary>
        public const string Density = "dnst";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Ref = "ref";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Reference = "reference";

        /// <summary> AudioChorus.Rate, AudioFlange.Rate, EditorSavingsSettings.AutosaveRate. </summary>
        public const string Rate = "rate";

        /// <summary> AudioChorus.Depth, AudioFlange.Depth. </summary>
        public const string Depth = "dpth";

        /// <summary> AudioChorus.Feedback. </summary>
        public const string Feedback = "fdbk";

        /// <summary> AudioPitchShifter.Overlap. </summary>
        public const string Overlap = "ovlp";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Fade = "fade";

        /// <summary> AudioSettings.Volume, LevelTrack.Volume, LevelTrackEffects.Volumes. </summary>
        public const string Volume = "vlm";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Lowest = "lowest";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Amp = "amp";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string FFT = "fft";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Octave = "octave";

        /// <summary> Word fragment, built into LongPressThreshold, SnapThreshold. </summary>
        public const string Threshold = "threshold";

        /// <summary> AudioCompressor.Threshold. </summary>
        public const string ThresholdShort = "thld";

        /// <summary> AudioCompressor.Attack. </summary>
        public const string Attack = "atk";

        /// <summary> AudioCompressor.Release. </summary>
        public const string Release = "rls";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Make = "make";

        /// <summary> EditorSavingsSettings.Autosave. </summary>
        public const string Autosave = "autosave";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string File = "file";

        /// <summary> Word fragment, built into MaxFiles. </summary>
        public const string Files = "files";

        /// <summary> LevelMeta.LevelLogo. </summary>
        public const string Logo = "logo";

        /// <summary> CustomLicense.Aggressive. </summary>
        public const string Aggressive = "aggressive";

        /// <summary> LevelMeta.LevelLicense, ResourceMeta.ResourceLicense. </summary>
        public const string License = "license";

        /// <summary> Word fragment, built into AllowsCommercialUse. </summary>
        public const string Use = "use";

        /// <summary> Word fragment, built into RequiresSameLicense. </summary>
        public const string Same = "same";

        /// <summary> Word fragment, built into AllowPermissionInstead, AllowUnknownLicense, AllowedLicenses, AllowedUriTypes. </summary>
        public const string Allow = "allow";

        /// <summary> Word fragment, built into AllowsCommercialUse, AllowsDistribution, AllowsModification. </summary>
        public const string Allows = "allows";

        /// <summary> Word fragment, built into RequireAgeRating, RequireAttribution, RequireHashes, RequireHold and 3 more. </summary>
        public const string Require = "require";

        /// <summary> Word fragment, built into RequiresAttribution, RequiresHold, RequiresSameLicense, RequiresSourceDisclosure. </summary>
        public const string Requires = "requires";

        /// <summary> Word fragment, built into AllowsDistribution. </summary>
        public const string Distribution = "distribution";

        /// <summary> Word fragment, built into AllowsModification. </summary>
        public const string Modification = "modification";

        /// <summary> Word fragment, built into AllowsCommercialUse. </summary>
        public const string Commercial = "commercial";

        /// <summary> Word fragment, built into RequireAttribution, RequiresAttribution. </summary>
        public const string Attribution = "attribution";

        /// <summary> Word fragment, built into RequiresSourceDisclosure. </summary>
        public const string Disclosure = "disclosure";

        /// <summary> Word fragment, built into AgeRating, RequireAgeRating. </summary>
        public const string Age = "age";

        /// <summary> Word fragment, built into AgeRating, RequireAgeRating. </summary>
        public const string Rating = "rating";

        /// <summary> ClipboardData.Content. </summary>
        public const string Content = "content";

        /// <summary> Word fragment, built into ShowAllFoundContent. </summary>
        public const string Found = "found";

        /// <summary> Word fragment, built into ContentDescriptors. </summary>
        public const string Descriptors = "descriptors";

        /// <summary> ResourceMeta.ResourceHashes. </summary>
        public const string Hashes = "hashes";

        /// <summary> PermissionGrant.Grantor. </summary>
        public const string Grantor = "grantor";

        /// <summary> Word fragment, built into AllowPermissionInstead, PermissionScope. </summary>
        public const string Permission = "permission";

        /// <summary> ResourceMeta.ResourcePermissions. </summary>
        public const string Permissions = "permissions";

        /// <summary> Word fragment, built into GrantedAt. </summary>
        public const string Granted = "granted";

        /// <summary> Word fragment, built into ExpiresAt. </summary>
        public const string Expires = "expires";

        /// <summary> Word fragment, built into ExpiresAt, GrantedAt. </summary>
        public const string At = "at";

        /// <summary> Word fragment, built into ProofText, ProofUrl. </summary>
        public const string Proof = "proof";

        /// <summary> Word fragment, built into PermissionScope. </summary>
        public const string Scope = "scope";

        /// <summary> TrustedSource.Trust. </summary>
        public const string Trust = "trust";

        /// <summary> TrustedSource.Domains. </summary>
        public const string Domains = "domains";

        /// <summary> TrustedSource.Note. </summary>
        public const string Note = "note";

        /// <summary> GameStatistics.Profile. </summary>
        public const string Profile = "profile";

        /// <summary> Word fragment, built into AllowUnknownLicense, UnknownSourceTrust. </summary>
        public const string Unknown = "unknown";

        /// <summary> TrustedSource.Licenses. </summary>
        public const string Licenses = "licenses";

        /// <summary> Word fragment, built into MaxDataFileBytes, MaxResourceBytes, MaxTotalBytes. </summary>
        public const string Bytes = "bytes";

        /// <summary> Word fragment, built into MaxTotalBytes, TotalDashes, TotalDistanceMoved, TotalResources. </summary>
        public const string Total = "total";

        /// <summary> ControlsSettings.Common. </summary>
        public const string Common = "common";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Shared = "shared";

        /// <summary> ControlsSettings.Priority. </summary>
        public const string Priority = "priority";

        /// <summary> CommonControlsSettings.Selection, GameEditorSettings.Selection. </summary>
        public const string Selection = "selection";

        /// <summary> Word fragment, built into PreviewCollider. </summary>
        public const string Preview = "preview";

        /// <summary> Word fragment, built into PickInvisibleAABB. </summary>
        public const string Pick = "pick";

        /// <summary> Word fragment, built into PickInvisibleAABB. </summary>
        public const string Invisible = "invisible";

        /// <summary> Word fragment, built into PickInvisibleAABB. </summary>
        public const string AABB = "aabb";

        /// <summary> Word fragment, built into ManualDevice. </summary>
        public const string Manual = "manual";

        /// <summary> Word fragment, built into DeviceGyro, ManualDevice. </summary>
        public const string Device = "device";

        /// <summary> Word fragment, built into KeyboardMouse. </summary>
        public const string Keyboard = "keyboard";

        /// <summary> Word fragment, built into KeyboardMouse, ZoomToMouse. </summary>
        public const string Mouse = "mouse";

        /// <summary> ControlsSettings.Touchscreen. </summary>
        public const string Touchscreen = "touchscreen";

        /// <summary> ControlsSettings.Gamepad. </summary>
        public const string Gamepad = "gamepad";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Touchpad = "touchpad";

        /// <summary> Word fragment, built into DeviceGyro, DeviceGyroSeconds. </summary>
        public const string Gyro = "gyro";

        /// <summary> BaseDeviceControlsSettings.Sensitivity. </summary>
        public const string Sensitivity = "sens";

        /// <summary> BaseDeviceControlsSettings.Smoothing. </summary>
        public const string Smoothing = "smoothing";

        /// <summary> EditorCameraSettings.Invert. </summary>
        public const string Invert = "invert";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Switch = "switch";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Cooldown = "cooldown";

        /// <summary> Word fragment, built into CursorHideAbsolute, CursorHideRelative, CursorRecenter, CursorReturn and 2 more. </summary>
        public const string Cursor = "cursor";

        /// <summary> Word fragment, built into CursorVisible. </summary>
        public const string Visible = "visible";

        /// <summary> Word fragment, built into CursorRecenter. </summary>
        public const string Recenter = "recenter";

        /// <summary> Word fragment, built into CursorReturn. </summary>
        public const string Return = "return";

        /// <summary> Word fragment, built into CursorHideAbsolute, CursorHideRelative. </summary>
        public const string Hide = "hide";

        /// <summary> Word fragment, built into CursorHideAbsolute. </summary>
        public const string Absolute = "abs";

        /// <summary> Word fragment, built into CursorHideRelative. </summary>
        public const string Relative = "rel";

        /// <summary> Word fragment, built into HoldButton, RequireHold, RequiresHold. </summary>
        public const string Hold = "hold";

        /// <summary> Word fragment, built into DashButtonAnchor, DashButtonIcon, DashButtonSize, HoldButton. </summary>
        public const string Button = "button";

        /// <summary> Word fragment, built into DashButtons. </summary>
        public const string Buttons = "buttons";

        /// <summary> Word fragment, built into DashButtonAnchor, DashButtonIcon, DashButtonSize, DashButtons and 5 more. </summary>
        public const string Dash = "dash";

        /// <summary> Word fragment, built into DashOnDoubleClick, DashOnDoubleTap, DoubleClickTime, DoubleTapTime. </summary>
        public const string Double = "double";

        /// <summary> Word fragment, built into DashOnDoubleClick, DoubleClickTime. </summary>
        public const string Click = "click";

        /// <summary> Word fragment, built into DashOnDoubleTap, DoubleTapTime, TapMaxTravel. </summary>
        public const string Tap = "tap";

        /// <summary> Word fragment, built into JoystickTravel, TapMaxTravel. </summary>
        public const string Travel = "travel";

        /// <summary> Word fragment, built into DashOnSecondFinger, FingerOffsetX, FingerOffsetY. </summary>
        public const string Finger = "finger";

        /// <summary> Word fragment, built into DashOnSecondFinger. </summary>
        public const string Second = "second";

        /// <summary> TouchscreenControlsSettings.Handedness. </summary>
        public const string Handedness = "handedness";

        /// <summary> Word fragment, built into JoystickAnchor, JoystickDynamicOrigin, JoystickSize, JoystickTravel. </summary>
        public const string Joystick = "joystick";

        /// <summary> Word fragment, built into JoystickDynamicOrigin. </summary>
        public const string Dynamic = "dynamic";

        /// <summary> Word fragment, built into JoystickDynamicOrigin. </summary>
        public const string Origin = "origin";

        /// <summary> Word fragment, built into DashButtonIcon. </summary>
        public const string Icon = "icon";

        /// <summary> Word fragment, built into MotionStick. </summary>
        public const string Motion = "motion";

        /// <summary> Word fragment, built into MotionStick. </summary>
        public const string Stick = "stick";

        /// <summary> Word fragment, built into ResponseCurve. </summary>
        public const string Response = "response";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Pad = "pad";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Area = "area";

        /// <summary> Word fragment, built into AxisMapping. </summary>
        public const string Axis = "axis";

        /// <summary> Word fragment, built into AxisMapping. </summary>
        public const string Mapping = "mapping";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Always = "always";

        /// <summary> Word fragment, built into CalibrateOnStart, DashOnDoubleClick, DashOnDoubleTap, DashOnSecondFinger and 3 more. </summary>
        public const string On = "on";

        /// <summary> Word fragment, built into AlertOnException. </summary>
        public const string Alert = "alert";

        /// <summary> Word fragment, built into AlertOnException. </summary>
        public const string Exception = "exception";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Activation = "activation";

        /// <summary> Word fragment, built into CalibrateOnStart. </summary>
        public const string Calibrate = "calibrate";

        /// <summary> Word fragment, built into MaxTiltAngle, TiltCenterX, TiltCenterY. </summary>
        public const string Tilt = "tilt";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Brand = "brand";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Glyph = "glyph";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Style = "style";

        /// <summary> Word fragment, built into ActiveDefault. </summary>
        public const string Default = "default";

        /// <summary> GameEditorSettings.Gizmos. </summary>
        public const string Gizmos = "gizmos";

        /// <summary> Word fragment, built into RenderInframes. </summary>
        public const string Inframes = "inframes";

        /// <summary> GameEditorSettings.Grid. </summary>
        public const string Grid = "grid";

        /// <summary> EditorGridSettings.Opacity. </summary>
        public const string Opacity = "opacity";

        /// <summary> Word fragment, built into ResetGizmos. </summary>
        public const string Reset = "reset";

        /// <summary> GameEditorSettings.Serialization. </summary>
        public const string Serialize = "serialize";

        /// <summary> EditorPlayerSettings.BotControl, RunProfile.Bot. </summary>
        public const string Bot = "bot";

        /// <summary> Word fragment, built into BotDebug, BotDebugGrid, BotDebugReach, BotDebugTarget. </summary>
        public const string Debug = "debug";

        /// <summary> Word fragment, built into BotDebugReach. </summary>
        public const string Reach = "reach";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Copy = "copy";

        /// <summary> GameEditorSettings.Savings. </summary>
        public const string Savings = "savings";

        /// <summary> Word fragment, built into HistoryLength. </summary>
        public const string History = "history";

        /// <summary> GameEditorSettings.Timeline. </summary>
        public const string Timeline = "timeline";

        /// <summary> Word fragment, built into WheelMultiplier. </summary>
        public const string Wheel = "wheel";

        /// <summary> Word fragment, built into LongPressThreshold, MoveSensitivityX, MoveSensitivityY. </summary>
        public const string Move = "move";

        /// <summary> Word fragment, built into LongPressDelay, LongPressThreshold. </summary>
        public const string Long = "long";

        /// <summary> Word fragment, built into LongPressDelay, LongPressThreshold. </summary>
        public const string Press = "press";

        /// <summary> Word fragment, built into SnapThreshold. </summary>
        public const string Snap = "snap";

        /// <summary> Word fragment, built into EdgeHandle. </summary>
        public const string Handle = "handle";

        /// <summary> Word fragment, built into ColliderOpacityView. </summary>
        public const string View = "view";

        /// <summary> Word fragment, built into DirtyFieldDelay. </summary>
        public const string Dirty = "dirty";

        /// <summary> Word fragment, built into DirtyFieldDelay. </summary>
        public const string Field = "field";

        /// <summary> Word fragment, built into LogClamps. </summary>
        public const string Clamps = "clamps";

        /// <summary> Word fragment, built into LogClamps. </summary>
        public const string Log = "log";

        /// <summary> Word fragment, built into RotationUnit. </summary>
        public const string Unit = "unit";

        /// <summary> Word fragment, built into AutoOpen, OpenMenuOnLose. </summary>
        public const string Open = "open";

        /// <summary> Word fragment, built into PreviewCollider. </summary>
        public const string Select = "select";

        /// <summary> Word fragment, built into LinkColliderShape, ZoomToMouse. </summary>
        public const string To = "to";

        /// <summary> Word fragment, built into MenuBackground, MenuSeconds, OpenMenuOnLose. </summary>
        public const string Menu = "menu";

        /// <summary> Word fragment, built into OpenMenuOnLose. </summary>
        public const string Lose = "lose";

        /// <summary> Word fragment, built into AutoOpen. </summary>
        public const string Auto = "auto";

        // Statistics

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Statistics = "stats";

        /// <summary> GameStatistics.Screens. </summary>
        public const string Screens = "screens";

        /// <summary> GameStatistics.Totals. </summary>
        public const string Totals = "totals";

        /// <summary> GameStatistics.Streaks. </summary>
        public const string Streaks = "streaks";

        /// <summary> GameStatistics.Devices. </summary>
        public const string Devices = "devices";

        /// <summary> LevelStatistics.Difficulty. </summary>
        public const string Difficulty = "difficulty";

        /// <summary> LevelStatistics.Records. </summary>
        public const string Records = "records";

        /// <summary> Word fragment, built into BestFrame, BestProgress. </summary>
        public const string Best = "best";

        /// <summary> Word fragment, built into BucketFrameDuration, DeathsByBucket, HitsByBucket. </summary>
        public const string Bucket = "bucket";

        /// <summary> LevelStatistics.Attempts, TotalsStatistics.TotalAttempts. </summary>
        public const string Attempts = "attempts";

        /// <summary> LevelStatistics.Clears, TotalsStatistics.TotalClears. </summary>
        public const string Clears = "clears";

        /// <summary> CheckpointDeaths.Deaths, LevelStatistics.Deaths, TotalsStatistics.TotalDeaths. </summary>
        public const string Deaths = "deaths";

        /// <summary> BestRun.Hits, LevelStatistics.Hits, TotalsStatistics.TotalHits. </summary>
        public const string Hits = "hits";

        /// <summary> BestRun.Dashes, LevelStatistics.Dashes. </summary>
        public const string Dashes = "dashes";

        /// <summary> LevelStatistics.Quits. </summary>
        public const string Quits = "quits";

        /// <summary> Word fragment, built into CheckpointRestarts. </summary>
        public const string Restarts = "restarts";

        /// <summary> LevelStatistics.SessionCount. </summary>
        public const string Sessions = "sessions";

        /// <summary> Word fragment, built into AppLaunches. </summary>
        public const string Launches = "launches";

        /// <summary> Word fragment, built into TotalDistanceMoved. </summary>
        public const string Distance = "distance";

        /// <summary> Word fragment, built into DistinctLevelsCleared, DistinctLevelsPlayed. </summary>
        public const string Distinct = "distinct";

        /// <summary> Word fragment, built into FramesSimulated. </summary>
        public const string Simulated = "simulated";

        /// <summary> LevelEditorStatistics.EditorOpens. </summary>
        public const string Opens = "opens";

        /// <summary> LevelEditorStatistics.Saves. </summary>
        public const string Saves = "saves";

        /// <summary> LevelEditorStatistics.Autosaves. </summary>
        public const string Autosaves = "autosaves";

        /// <summary> EditorTotalsStatistics.OperationsExecuted, LevelEditorStatistics.Operations. </summary>
        public const string Operations = "operations";

        /// <summary> Word fragment, built into LevelsCreated, ObjectsCreated. </summary>
        public const string Created = "created";

        /// <summary> Word fragment, built into LevelsDeleted. </summary>
        public const string Deleted = "deleted";

        /// <summary> Word fragment, built into FirstClearUtc, FirstPlayedUtc, LastEditedUtc, LastPlayedUtc and 1 more. </summary>
        public const string Utc = "utc";

        /// <summary> Word fragment, built into FirstClearUtc, FirstPlayedUtc. </summary>
        public const string First = "first";

        /// <summary> Word fragment, built into LastEditedUtc, LastPlayedLevelId, LastPlayedUtc. </summary>
        public const string Last = "last";

        /// <summary> Word fragment, built into RealSeconds. </summary>
        public const string Real = "real";

        /// <summary> Word fragment, built into LivesLeft. </summary>
        public const string Left = "left";

        /// <summary> Word fragment, built into EditSeconds. </summary>
        public const string Edit = "edit";

        /// <summary> Word fragment, built into LastEditedUtc. </summary>
        public const string Edited = "edited";

        /// <summary> Word fragment, built into DistinctLevelsPlayed, FirstPlayedUtc, LastPlayedLevelId, LastPlayedUtc and 2 more. </summary>
        public const string Played = "played";

        /// <summary> Word fragment, built into DistinctLevelsCleared, FirstClearUtc. </summary>
        public const string Cleared = "cleared";

        /// <summary> Word fragment, built into LoadingSeconds. </summary>
        public const string Loading = "loading";

        /// <summary> Word fragment, built into AppLaunches, AppSeconds. </summary>
        public const string App = "app";

        /// <summary> RunProfile.LifeCount. </summary>
        public const string Lives = "lives";

        /// <summary> Word fragment, built into SpeedCenti. </summary>
        public const string Centi = "centi";

        /// <summary> Word fragment, built into DeathsBeforeCheckpoint. </summary>
        public const string Before = "before";

        /// <summary> Word fragment, built into DeathsByBucket, DeathsByCheckpoint, HitsByBucket. </summary>
        public const string By = "by";

        /// <summary> Word fragment, built into CurrentClearStreak. </summary>
        public const string Current = "current";

        /// <summary> Word fragment, built into LongestClearStreak. </summary>
        public const string Longest = "longest";

        /// <summary> Word fragment, built into MostPlayedAttempts, MostPlayedLevelId. </summary>
        public const string Most = "most";

        /// <summary> Word fragment, built into CurrentClearStreak, LongestClearStreak. </summary>
        public const string Clear = "clear";

        /// <summary> Word fragment, built into CurrentClearStreak, LongestClearStreak. </summary>
        public const string Streak = "streak";

        /// <summary> Word fragment, built into LevelsCreated, LevelsDeleted. </summary>
        public const string Levels = "levels";

        /// <summary> Word fragment, built into TotalDistanceMoved. </summary>
        public const string Moved = "moved";

        /// <summary> Word fragment, built into AppSeconds, DeviceGyroSeconds, EditSeconds, EditorSeconds and 7 more. </summary>
        public const string Seconds = "seconds";

        /// <summary> GameStatistics.Avatar. </summary>
        public const string Avatar = "avatar";

        /// <summary> Word fragment, built into FramesSimulated. </summary>
        public const string Frames = "frames";

        /// <summary> Word fragment, built into GeneratorsRun. </summary>
        public const string Generators = "generators";

        /// <summary> Word fragment, built into GeneratorsRun. </summary>
        public const string Ran = "ran";

        /// <summary> Word kept in the key vocabulary; nothing is built from it today. </summary>
        public const string Runs = "runs";

        private const string _ = "_";

        // ---------------------------------------------------------------------------------------------
        // Combined names
        // ---------------------------------------------------------------------------------------------

        /// <summary> <c>"is_local"</c> - carried by no model today. </summary>
        public const string IsLocal = Is + _ + Local;

        /// <summary> IFrameDuration.FrameDuration, LevelSettings.FrameDuration, Prefab.FrameDuration. </summary>
        public const string FrameDurationShort = "fdur";

        /// <summary> ScreenLimitKey.ScreenLimit. </summary>
        public const string ScreenLimit = "slim";

        /// <summary> <c>"screen_limits"</c> - GameEvents.ScreenLimits. </summary>
        public const string ScreenLimits = Screen + _ + Limits;

        /// <summary> <c>"editor_settings"</c> - carried by no model today. </summary>
        public const string EditorSettings = Editor + _ + Settings;

        // GameEditorSettings' own keys. Every one of them lives INSIDE one of that model's nine
        // groups, which is what lets them stay this short - a key only has to be unique among its
        // siblings, so the group's own name carries the qualifier the flat shape used to spell out
        // (the old "grid_size" is "grid": { "size" } now). The flat keys these replaced are gone
        // from here entirely, and now that the snapshot that held them as literals is deleted, they
        // survive nowhere at all - a settings.json predating the restructure reads back as defaults.

        /// <summary> <c>"max_autosave_files"</c> - EditorSavingsSettings.MaxAutosaveFiles. </summary>
        public const string MaxFiles = Max + _ + Autosave + _ + Files;

        /// <summary> <c>"history_length"</c> - EditorSavingsSettings.HistoryLength. </summary>
        public const string HistoryLength = History + _ + Length;

        /// <summary> <c>"min_sz"</c> - EditorCameraSettings.MinSize. </summary>
        public const string MinSize = Min + _ + Size;

        /// <summary> <c>"max_sz"</c> - EditorCameraSettings.MaxSize. </summary>
        public const string MaxSize = Max + _ + Size;

        /// <summary> <c>"move_sens_x"</c> - EditorCameraSettings.MoveSensitivityX. </summary>
        public const string MoveSensitivityX = Move + _ + Sensitivity + _ + CoordX;

        /// <summary> <c>"move_sens_y"</c> - EditorCameraSettings.MoveSensitivityY. </summary>
        public const string MoveSensitivityY = Move + _ + Sensitivity + _ + CoordY;

        /// <summary> <c>"wheel_mult"</c> - EditorCameraSettings.WheelMultiplier. </summary>
        public const string WheelMultiplier = Wheel + _ + Multiplier;

        /// <summary> <c>"zoom_to_mouse"</c> - EditorCameraSettings.ZoomToMouse. </summary>
        public const string ZoomToMouse = Zoom + _ + To + _ + Mouse;

        /// <summary> <c>"active_default"</c> - EditorEffectsSettings.ActiveDefault, EditorGridSettings.ActiveDefault, EditorPlayerSettings.ActiveDefault. </summary>
        public const string ActiveDefault = Active + _ + Default;

        /// <summary> <c>"reset_gizmos"</c> - EditorPlayerSettings.ResetGizmos. </summary>
        public const string ResetGizmos = Reset + _ + Gizmos;

        /// <summary> <c>"bot_debug"</c> - EditorPlayerSettings.BotDebug. </summary>
        public const string BotDebug = Bot + _ + Debug;

        /// <summary> <c>"bot_debug_grid"</c> - EditorPlayerSettings.BotDebugGrid. </summary>
        public const string BotDebugGrid = Bot + _ + Debug + _ + Grid;

        /// <summary> <c>"bot_debug_target"</c> - EditorPlayerSettings.BotDebugTarget. </summary>
        public const string BotDebugTarget = Bot + _ + Debug + _ + Target;

        /// <summary> <c>"bot_debug_reach"</c> - EditorPlayerSettings.BotDebugReach. </summary>
        public const string BotDebugReach = Bot + _ + Debug + _ + Reach;

        /// <summary> <c>"multi_requires_hold"</c> - EditorSelectionSettings.MultiRequiresHold. </summary>
        public const string RequiresHold = Multi + _ + Requires + _ + Hold;

        /// <summary> <c>"preview_collider_on_select"</c> - EditorSelectionSettings.PreviewColliderOnSelect. </summary>
        public const string PreviewCollider = Preview + _ + Collider + _ + On + _ + Select;

        /// <summary> <c>"pick_invisible_aabb"</c> - EditorSelectionSettings.PickInvisibleAABB. </summary>
        public const string PickInvisibleAABB = Pick + _ + Invisible + _ + AABB;

        /// <summary> <c>"long_press_dly"</c> - EditorSelectionSettings.LongPressDelay. </summary>
        public const string LongPressDelay = Long + _ + Press + _ + Delay;

        /// <summary> <c>"long_press_move_threshold"</c> - EditorSelectionSettings.LongPressMoveThreshold. </summary>
        public const string LongPressThreshold = Long + _ + Press + _ + Move + _ + Threshold;

        /// <summary> <c>"collider_opacity_selection"</c> - EditorSelectionSettings.ColliderOpacitySelection. </summary>
        public const string ColliderOpacity = Collider + _ + Opacity + _ + Selection;

        /// <summary> <c>"collider_opacity_view"</c> - EditorSelectionSettings.ColliderOpacityView. </summary>
        public const string ColliderOpacityView = Collider + _ + Opacity + _ + View;

        /// <summary> <c>"snap_threshold"</c> - EditorTimelineSettings.SnapThresholdPx. </summary>
        public const string SnapThreshold = Snap + _ + Threshold;

        /// <summary> <c>"edge_handle"</c> - EditorTimelineSettings.EdgeHandlePx. </summary>
        public const string EdgeHandle = Edge + _ + Handle;

        /// <summary> <c>"global_loop"</c> - EditorTimelineSettings.GlobalLoop. </summary>
        public const string LoopGlobal = Global + _ + Loop;

        /// <summary> <c>"local_loop"</c> - EditorTimelineSettings.LocalLoop. </summary>
        public const string LoopLocal = Local + _ + Loop;

        /// <summary> <c>"time_format"</c> - EditorTimelineSettings.TimeFormat. </summary>
        public const string TimeFormat = Time + _ + Format;

        /// <summary> <c>"dirty_field_dly"</c> - EditorInterfaceSettings.DirtyFieldDelay. </summary>
        public const string DirtyFieldDelay = Dirty + _ + Field + _ + Delay;

        /// <summary> <c>"rot_display_unit"</c> - EditorInterfaceSettings.RotationDisplayUnit. </summary>
        public const string RotationUnit = Rotation + _ + Display + _ + Unit;

        /// <summary> <c>"log_value_clamps"</c> - EditorInterfaceSettings.LogValueClamps. </summary>
        public const string LogClamps = Log + _ + ValueWord + _ + Clamps;

        /// <summary> <c>"render_inframes"</c> - EditorInterfaceSettings.RenderInframes. </summary>
        public const string RenderInframes = Render + _ + Inframes;

        /// <summary> <c>"link_collider_to_shp"</c> - EditorInterfaceSettings.LinkColliderToShape. </summary>
        public const string LinkColliderShape = Link + _ + Collider + _ + To + _ + Shape;

        /// <summary> <c>"auto_open"</c> - EditorInterfaceSettings.SelectionAutoOpenActive. </summary>
        public const string AutoOpen = Auto + _ + Open;

        /// <summary> <c>"game_editor"</c> - UserSettings.GameEditor. </summary>
        public const string GameEditor = Game + _ + Editor;

        /// <summary> <c>"editor_ui"</c> - AudioSettings.EditorUI. </summary>
        public const string EditorUI = Editor + _ + UI;

        /// <summary> <c>"open_menu_on_lose"</c> - InterfaceSettings.OpenMenuOnLose. </summary>
        public const string OpenMenuOnLose = Open + _ + Menu + _ + On + _ + Lose;

        /// <summary> <c>"stats_active"</c> - InterfaceSettings.StatsActive. </summary>
        public const string StatsActive = Stats + _ + Active;

        /// <summary> <c>"stats_frame_objects"</c> - InterfaceSettings.StatsFrameObjects. </summary>
        public const string StatsFrameObjects = Stats + _ + Frame + _ + ObjectsFull;

        /// <summary> <c>"stats_level_objects"</c> - InterfaceSettings.StatsLevelObjects. </summary>
        public const string StatsLevelObjects = Stats + _ + Level + _ + ObjectsFull;

        /// <summary> <c>"stats_memory"</c> - InterfaceSettings.StatsMemory. </summary>
        public const string StatsMemory = Stats + _ + Memory;

        /// <summary> <c>"stats_profile"</c> - InterfaceSettings.StatsProfiling. </summary>
        public const string StatsProfiling = Stats + _ + Profile;

        /// <summary> <c>"stats_alignment_x"</c> - InterfaceSettings.StatsAlignmentX. </summary>
        public const string StatsAlignmentX = Stats + _ + Alignment + _ + CoordX;

        /// <summary> <c>"stats_alignment_y"</c> - InterfaceSettings.StatsAlignmentY. </summary>
        public const string StatsAlignmentY = Stats + _ + Alignment + _ + CoordY;

        /// <summary> <c>"menu_background"</c> - InterfaceSettings.MenuBackground. </summary>
        public const string MenuBackground = Menu + _ + Background;

        /// <summary> <c>"screen_orientation"</c> - InterfaceSettings.ScreenOrientation. </summary>
        public const string ScreenOrientation = Screen + _ + Orientation;

        /// <summary> <c>"show_game_progress"</c> - InterfaceSettings.ShowGameProgress. </summary>
        public const string ShowGameProgress = Show + _ + Game + _ + Progress;

        /// <summary> <c>"show_game_pause"</c> - InterfaceSettings.ShowGamePause. </summary>
        public const string ShowGamePause = Show + _ + Game + _ + Pause;

        /// <summary> <c>"show_game_iface"</c> - InterfaceSettings.ShowGameInterface. </summary>
        public const string ShowGameInterface = Show + _ + Game + _ + Interface;

        /// <summary> <c>"alert_on_exception"</c> - InterfaceSettings.AlertOnException. </summary>
        public const string AlertOnException = Alert + _ + On + _ + Exception;

        /// <summary> <c>"fps_target"</c> - EffectsGraphicsSettings.FpsTarget, GraphicsSettings.FpsTarget. </summary>
        public const string FpsTarget = Fps + _ + Target;

        /// <summary> <c>"fps_fixed"</c> - EffectsGraphicsSettings.FpsFixed, GraphicsSettings.FpsFixed. </summary>
        public const string FpsFixed = Fps + _ + Fixed;

        /// <summary> <c>"render_eff"</c> - AudioGraphicsSettings.RenderEffects. </summary>
        public const string RenderEffects = Render + _ + Effects;

        /// <summary> <c>"show_all_found_content"</c> - GeneralSettings.ShowAllFoundContent. </summary>
        public const string ShowAllFoundContent = Show + _ + All + _ + Found + _ + Content;

        /// <summary> <c>"resource_parallel_load_count"</c> - GeneralSettings.ResourceParallelLoadCount. </summary>
        public const string ResourceParallelLoadCount = Resource + _ + Parallel + _ + Load + _ + Count;

        /// <summary> <c>"resource_web_timeout"</c> - GeneralSettings.ResourceWebTimeout. </summary>
        public const string ResourceWebTimeout = Resource + _ + Web + _ + Timeout;

        /// <summary> <c>"target_delta_time"</c> - carried by no model today. </summary>
        public const string TargetDeltaTime = Target + _ + Delta + _ + Time;

        /// <summary> <c>"scrub_time"</c> - AudioGraphicsSettings.ScrubTime. </summary>
        public const string ScrubTime = Scrub + _ + Time;

        /// <summary> <c>"max_scrub_time"</c> - EffectsGraphicsSettings.MaxScrubTime. </summary>
        public const string MaxScrubTime = Max + _ + Scrub + _ + Time;

        /// <summary> <c>"replay_step_budget"</c> - EffectsGraphicsSettings.ReplayStepBudget. </summary>
        public const string ReplayStepBudget = Replay + _ + Step + _ + Budget;

        /// <summary> <c>"frame_step_budget"</c> - EffectsGraphicsSettings.FrameStepBudget. </summary>
        public const string FrameStepBudget = Frame + _ + Step + _ + Budget;

        // Replaced MaxDiffTime, and the key changed with it: what it measures is no longer "how far
        // audio may drift" but "how far the playhead must JUMP to count as a discontinuity". An old
        // settings.json simply falls back to the default for it, which is the intended outcome - the
        // stored number was tuned against a metric that no longer exists.

        /// <summary> <c>"resync_jump_time"</c> - AudioGraphicsSettings.ResyncJumpTime. </summary>
        public const string ResyncJumpTime = Resync + _ + Jump + _ + Time;

        /// <summary> <c>"dead_zone"</c> - AudioGraphicsSettings.SyncDeadZone. </summary>
        public const string SyncDeadZone = Dead + _ + Zone;

        /// <summary> <c>"pitch_correction"</c> - AudioGraphicsSettings.PitchCorrection. </summary>
        public const string PitchCorrection = Pitch + _ + Correction;

        // Controls. Replaced ClassicControlsType, whose key is simply gone: it had no gameplay
        // consumer at all, so an old settings.json losing it loses nothing that ever did anything. The
        // UserSettings domain deliberately did NOT bump for that - a removed OptIn property is skipped
        // on read, whereas a bump with no migration registered would throw on every existing file.

        /// <summary> <c>"keyboard_mouse"</c> - ControlsSettings.KeyboardMouse. </summary>
        public const string KeyboardMouse = Keyboard + _ + Mouse;

        /// <summary> <c>"device_gyro"</c> - ControlsSettings.DeviceGyro. </summary>
        public const string DeviceGyro = Device + _ + Gyro;

        /// <summary> <c>"manual_device"</c> - CommonControlsSettings.ManualDevice. </summary>
        public const string ManualDevice = Manual + _ + Device;

        /// <summary> <c>"cursor_visible"</c> - CommonControlsSettings.CursorVisible. </summary>
        public const string CursorVisible = Cursor + _ + Visible;

        /// <summary> <c>"cursor_sca"</c> - CommonControlsSettings.CursorScale. </summary>
        public const string CursorScale = Cursor + _ + Scale;

        /// <summary> <c>"cursor_recenter"</c> - CommonControlsSettings.CursorRecenter. </summary>
        public const string CursorRecenter = Cursor + _ + Recenter;

        /// <summary> <c>"cursor_return"</c> - CommonControlsSettings.CursorReturn. </summary>
        public const string CursorReturn = Cursor + _ + Return;

        // Same string as SyncDeadZone, which is an audio key - the two can never appear on one model,
        // which is exactly the condition this file reuses keys under.

        /// <summary> <c>"dead_zone"</c> - BaseDeviceControlsSettings.DeadZone. </summary>
        public const string DeadZone = Dead + _ + Zone;

        /// <summary> <c>"invert_x"</c> - BaseDeviceControlsSettings.InvertX. </summary>
        public const string InvertX = Invert + _ + CoordX;

        /// <summary> <c>"invert_y"</c> - BaseDeviceControlsSettings.InvertY. </summary>
        public const string InvertY = Invert + _ + CoordY;

        /// <summary> <c>"require_hold"</c> - KeyboardMouseControlsSettings.RequireHold. </summary>
        public const string RequireHold = Require + _ + Hold;

        /// <summary> <c>"hold_button"</c> - KeyboardMouseControlsSettings.HoldButton. </summary>
        public const string HoldButton = Hold + _ + Button;

        /// <summary> <c>"dash_on_double_click"</c> - KeyboardMouseControlsSettings.DashOnDoubleClick. </summary>
        public const string DashOnDoubleClick = Dash + _ + On + _ + Double + _ + Click;

        /// <summary> <c>"double_click_time"</c> - KeyboardMouseControlsSettings.DoubleClickTime. </summary>
        public const string DoubleClickTime = Double + _ + Click + _ + Time;

        /// <summary> <c>"dash_keys"</c> - KeyboardMouseControlsSettings.DashKeys. </summary>
        public const string DashKeys = Dash + _ + Keys;

        /// <summary> <c>"cursor_hide_abs"</c> - KeyboardMouseControlsSettings.CursorHideAbsolute. </summary>
        public const string CursorHideAbsolute = Cursor + _ + Hide + _ + Absolute;

        /// <summary> <c>"cursor_hide_rel"</c> - KeyboardMouseControlsSettings.CursorHideRelative. </summary>
        public const string CursorHideRelative = Cursor + _ + Hide + _ + Relative;

        /// <summary> <c>"finger_off_x"</c> - TouchscreenControlsSettings.FingerOffsetX. </summary>
        public const string FingerOffsetX = Finger + _ + Offset + _ + CoordX;

        /// <summary> <c>"finger_off_y"</c> - TouchscreenControlsSettings.FingerOffsetY. </summary>
        public const string FingerOffsetY = Finger + _ + Offset + _ + CoordY;

        /// <summary> <c>"dash_on_second_finger"</c> - TouchscreenControlsSettings.DashOnSecondFinger. </summary>
        public const string DashOnSecondFinger = Dash + _ + On + _ + Second + _ + Finger;

        /// <summary> <c>"dash_on_double_tap"</c> - TouchscreenControlsSettings.DashOnDoubleTap. </summary>
        public const string DashOnDoubleTap = Dash + _ + On + _ + Double + _ + Tap;

        /// <summary> <c>"double_tap_time"</c> - TouchscreenControlsSettings.DoubleTapTime. </summary>
        public const string DoubleTapTime = Double + _ + Tap + _ + Time;

        /// <summary> <c>"tap_max_travel"</c> - TouchscreenControlsSettings.TapMaxTravel. </summary>
        public const string TapMaxTravel = Tap + _ + Max + _ + Travel;

        /// <summary> <c>"joystick_anc"</c> - TouchscreenControlsSettings.JoystickAnchor. </summary>
        public const string JoystickAnchor = Joystick + _ + Anchor;

        /// <summary> <c>"joystick_sz"</c> - TouchscreenControlsSettings.JoystickSize. </summary>
        public const string JoystickSize = Joystick + _ + Size;

        /// <summary> <c>"joystick_travel"</c> - TouchscreenControlsSettings.JoystickTravel. </summary>
        public const string JoystickTravel = Joystick + _ + Travel;

        /// <summary> <c>"joystick_dynamic_origin"</c> - TouchscreenControlsSettings.JoystickDynamicOrigin. </summary>
        public const string JoystickDynamicOrigin = Joystick + _ + Dynamic + _ + Origin;

        /// <summary> <c>"dash_button_anc"</c> - DeviceGyroControlsSettings.DashButtonAnchor, TouchscreenControlsSettings.DashButtonAnchor. </summary>
        public const string DashButtonAnchor = Dash + _ + Button + _ + Anchor;

        /// <summary> <c>"dash_button_sz"</c> - DeviceGyroControlsSettings.DashButtonSize, TouchscreenControlsSettings.DashButtonSize. </summary>
        public const string DashButtonSize = Dash + _ + Button + _ + Size;

        /// <summary> <c>"dash_button_icon"</c> - TouchscreenControlsSettings.DashButtonIcon. </summary>
        public const string DashButtonIcon = Dash + _ + Button + _ + Icon;

        /// <summary> <c>"motion_stick"</c> - GamepadControlsSettings.MotionStick. </summary>
        public const string MotionStick = Motion + _ + Stick;

        /// <summary> <c>"response_crv"</c> - GamepadControlsSettings.ResponseCurve. </summary>
        public const string ResponseCurve = Response + _ + Curve;

        /// <summary> <c>"dash_buttons"</c> - GamepadControlsSettings.DashButtons. </summary>
        public const string DashButtons = Dash + _ + Buttons;

        /// <summary> <c>"axis_mapping"</c> - DeviceGyroControlsSettings.AxisMapping. </summary>
        public const string AxisMapping = Axis + _ + Mapping;

        /// <summary> <c>"max_tilt_ang"</c> - DeviceGyroControlsSettings.MaxTiltAngle. </summary>
        public const string MaxTiltAngle = Max + _ + Tilt + _ + Angle;

        /// <summary> <c>"calibrate_on_start"</c> - DeviceGyroControlsSettings.CalibrateOnStart. </summary>
        public const string CalibrateOnStart = Calibrate + _ + On + _ + Start;

        /// <summary> <c>"tilt_cntr_x"</c> - DeviceGyroControlsSettings.TiltCenterX. </summary>
        public const string TiltCenterX = Tilt + _ + Center + _ + CoordX;

        /// <summary> <c>"tilt_cntr_y"</c> - DeviceGyroControlsSettings.TiltCenterY. </summary>
        public const string TiltCenterY = Tilt + _ + Center + _ + CoordY;

        /// <summary> <c>"dash_src"</c> - DeviceGyroControlsSettings.DashSource. </summary>
        public const string DashSource = Dash + _ + Source;


        // Anti-aliasing. One key per field of AntiAliasingGraphicsSettings, plus the group's own
        // key on GraphicsSettings; the group reuses the shared Type key, which is safe because no
        // other model in this aggregate carries one.

        /// <summary> GraphicsSettings.AntiAliasing. </summary>
        public const string AntiAliasing = "aa";

        /// <summary> AntiAliasingGraphicsSettings.MSAA. </summary>
        public const string MSAA = "msaa";

        /// <summary> AntiAliasingGraphicsSettings.HDR. </summary>
        public const string HDR = "hdr";

        // Textures. The group's own key is the shared Textures one - a level's resource list and a
        // settings group can never appear on one model, which is the reuse this file allows.

        /// <summary> TexturesGraphicsSettings.Compression, TextureResource.Compression - the device's
        /// setting and the author's permission, two members of two unrelated models. </summary>
        public const string Compression = "compress";

        /// <summary> TexturesGraphicsSettings.Mipmaps. </summary>
        public const string Mipmaps = "mips";

        /// <summary> <c>"sz_limit"</c> - TexturesGraphicsSettings.SizeLimit. </summary>
        public const string SizeLimit = Size + _ + Limit;

        /// <summary> TexturesGraphicsSettings.Filtering. </summary>
        public const string Filter = "filter";

        /// <summary> TexturesGraphicsSettings.CompressionQuality. </summary>
        public const string Quality = "quality";

        // Display. Desktop-only presentation, and the group's own key is the shared Display one.

        /// <summary> <c>"window_mode"</c> - DisplayGraphicsSettings.WindowMode. </summary>
        public const string WindowMode = Window + _ + Mode;

        /// <summary> <c>"resolution_width"</c> - DisplayGraphicsSettings.ResolutionWidth. </summary>
        public const string ResolutionWidth = Resolution + _ + Width;

        /// <summary> <c>"resolution_height"</c> - DisplayGraphicsSettings.ResolutionHeight. </summary>
        public const string ResolutionHeight = Resolution + _ + Height;

        /// <summary> <c>"render_sca"</c> - DisplayGraphicsSettings.RenderScale. </summary>
        public const string RenderScale = Render + _ + Scale;

        /// <summary> DisplayGraphicsSettings.VSync. </summary>
        public const string VSync = "vsync";

        /// <summary> <c>"postprocessing"</c> - GraphicsSettings.PostProcessing. </summary>
        public const string PostProcessing = Post + Processing;

        /// <summary> <c>"render_blm"</c> - PostProcessingGraphicsSettings.RenderBloom. </summary>
        public const string RenderBloom = Render + _ + BloomShort;

        /// <summary> <c>"render_chr"</c> - PostProcessingGraphicsSettings.RenderChroma. </summary>
        public const string RenderChroma = Render + _ + ChromaticShort;

        /// <summary> <c>"render_vgn"</c> - PostProcessingGraphicsSettings.RenderVignette. </summary>
        public const string RenderVignette = Render + _ + VignetteShort;

        /// <summary> <c>"render_lns"</c> - PostProcessingGraphicsSettings.RenderLens. </summary>
        public const string RenderLens = Render + _ + LensShort;

        /// <summary> <c>"render_grn"</c> - PostProcessingGraphicsSettings.RenderGrain. </summary>
        public const string RenderGrain = Render + _ + GrainShort;

        /// <summary> <c>"render_mbr"</c> - PostProcessingGraphicsSettings.RenderMotionBlur. </summary>
        public const string RenderMotionBlur = Render + _ + MotionBlurShort;

        /// <summary> <c>"render_ccv"</c> - PostProcessingGraphicsSettings.RenderColorCurves. </summary>
        public const string RenderColorCurves = Render + _ + ColorCurvesShort;

        /// <summary> <c>"render_lgg"</c> - PostProcessingGraphicsSettings.RenderLiftGammaGain. </summary>
        public const string RenderLiftGammaGain = Render + _ + LiftGammaGainShort;

        /// <summary> <c>"render_smh"</c> - PostProcessingGraphicsSettings.RenderShadowsMidtonesHighlights. </summary>
        public const string RenderShadowsMidtonesHighlights = Render + _ + ShadowsMidtonesHighlightsShort;

        /// <summary> <c>"render_wbl"</c> - PostProcessingGraphicsSettings.RenderWhiteBalance. </summary>
        public const string RenderWhiteBalance = Render + _ + WhiteBalanceShort;

        /// <summary> <c>"render_agl"</c> - PostProcessingGraphicsSettings.RenderAnalogGlitch. </summary>
        public const string RenderAnalogGlitch = Render + _ + AnalogGlitchShort;

        /// <summary> <c>"render_dgl"</c> - PostProcessingGraphicsSettings.RenderDigitalGlitch. </summary>
        public const string RenderDigitalGlitch = Render + _ + DigitalGlitchShort;

        /// <summary> Color3ThemeRef.ThemeColorIndex, Color4ThemeRef.ThemeColorIndex. </summary>
        public const string ThemeIndex = "tidx";

        /// <summary> ThemeData.ThemeId, ThemeKeyframe.ThemeId. </summary>
        public const string ThemeId = "thid";

        /// <summary> EffectData.EffectId, EffectObject.EffectId. </summary>
        public const string EffectId = "eid";

        /// <summary> <c>"camera_events"</c> - GameLevel.CameraEvents. </summary>
        public const string CameraEvents = Camera + _ + Events;

        /// <summary> <c>"postprocessing_events"</c> - GameLevel.PostProcessingEvents. </summary>
        public const string PostProcessingEvents = Post + Processing + _ + Events;

        /// <summary> <c>"player_events"</c> - GameLevel.PlayerEvents. </summary>
        public const string PlayerEvents = Player + _ + Events;

        // Clipboard - one key per section of ClipboardData. Objects/PrefabObjects hold whole
        // objects to be created; the *Keys ones hold the same model types carrying nothing but the
        // copied keyframes, so the two never share a key even where they share a value type.

        /// <summary> <c>"prefab_objs"</c> - ClipboardData.PrefabObjects. </summary>
        public const string PrefabObjects = Prefab + _ + Objects;

        /// <summary> <c>"key_objs"</c> - ClipboardData.KeyObjects. </summary>
        public const string KeyObjects = Key + _ + Objects;

        /// <summary> <c>"key_tracks"</c> - ClipboardData.KeyTracks. </summary>
        public const string KeyTracks = Key + _ + Tracks;

        /// <summary> <c>"audio_tracks"</c> - ClipboardData.AudioTracks. </summary>
        public const string AudioTracks = Audio + _ + Tracks;

        /// <summary> <c>"game_keys"</c> - ClipboardData.GameKeys. </summary>
        public const string GameKeys = Game + _ + Keys;

        /// <summary> <c>"camera_keys"</c> - ClipboardData.CameraKeys. </summary>
        public const string CameraKeys = Camera + _ + Keys;

        /// <summary> <c>"postprocessing_keys"</c> - ClipboardData.PostProcessingKeys. </summary>
        public const string PostProcessingKeys = Post + Processing + _ + Keys;

        /// <summary> <c>"player_keys"</c> - ClipboardData.PlayerKeys. </summary>
        public const string PlayerKeys = Player + _ + Keys;

        // Instances

        /// <summary> <c>"level_id"</c> - LevelMeta.LevelId, LevelStatistics.LevelId. </summary>
        public const string LevelId = Level + _ + Id;

        /// <summary> Prefab.PrefabId, PrefabObject.PrefabId. </summary>
        public const string PrefabId = "pfid";

        /// <summary> <c>"aid"</c> - LevelTrack.AudioId. </summary>
        public const string AudioId = AudioShort + Id;

        /// <summary> <c>"id"</c> - ModificationKey.ObjectId, RectObject.ObjectId. </summary>
        public const string ObjectId = Id;

        /// <summary> LevelSettings.ObjectIdCounter, Prefab.ObjectIdCounter. </summary>
        public const string ObjectIdCounter = "idctr";

        /// <summary> LevelSettings.AudioIdCounter. </summary>
        public const string AudioIdCounter = "aidctr";

        /// <summary> <c>"prev_id"</c> - carried by no model today. </summary>
        public const string PrevObjectId = Prev + _ + Id;

        /// <summary> <c>"next_id"</c> - carried by no model today. </summary>
        public const string NextObjectId = Next + _ + Id;

        /// <summary> <c>"ids"</c> - PrefabObject.ObjectIds. </summary>
        public const string ObjectIds = Ids;

        // Two shape references on one object, both ShapeId-typed: what is drawn and what is hit.
        // They must stay distinct keys even though the values are interchangeable.

        /// <summary> <c>"shid"</c> - CompositeShape.ShapeId, EffectObjectCore.ParticleShapeId, ShapeObject.ShapeId. </summary>
        public const string ShapeId = ShapeShort + Id;

        /// <summary> <c>"cid"</c> - ShapeObject.ColliderId. </summary>
        public const string ColliderId = ColliderShort + Id;

        /// <summary> CompositeShape.ShapeName. </summary>
        public const string ShapeName = "shname";

        /// <summary> <c>"pid"</c> - RectObject.ParentObjectId. </summary>
        public const string ParentObjectId = ParentShort + ObjectId;

        /// <summary> <c>"local_frame"</c> - carried by no model today. </summary>
        public const string LocalFrame = Local + _ + Frame;

        /// <summary> <c>"lf"</c> - carried by no model today. </summary>
        public const string LocalFrameShort = LocalShort + FrameShort;

        /// <summary> EffectData.StopLocalFrame. </summary>
        public const string StopLocalFrame = "slf";

        /// <summary> EffectData.HasStopLocalFrame. </summary>
        public const string HasStopLocalFrame = "hslf";

        /// <summary> LevelTrack.OffsetTime. </summary>
        public const string OffsetTime = "offt";

        /// <summary> LevelTrack.AudioLayer. </summary>
        public const string AudioLayer = "al";

        /// <summary> <c>"eff_shp"</c> - carried by no model today. </summary>
        public const string EffShape = Eff + _ + Shape;

        /// <summary> <c>"eff_ang"</c> - carried by no model today. </summary>
        public const string EffAngle = Eff + _ + Angle;

        /// <summary> <c>"eff_sca"</c> - carried by no model today. </summary>
        public const string EffScale = Eff + _ + Scale;

        /// <summary> <c>"eff_clr"</c> - carried by no model today. </summary>
        public const string EffColor = Eff + _ + Color;

        /// <summary> <c>"prefab_idx"</c> - carried by no model today. </summary>
        public const string PrefabIndex = Prefab + _ + Index;

        /// <summary> TextObject.FontSizes. </summary>
        public const string FontSize = "fsize";

        /// <summary> <c>"wrap"</c> - TextObject.WordWrap. </summary>
        public const string WordWrap = Wrap;

        /// <summary> <c>"ha"</c> - TextObject.HorizontalAlignment. </summary>
        public const string HorizontalAlignment = HorizontalShort + AlignmentShortest;

        /// <summary> <c>"va"</c> - TextObject.VerticalAlignment. </summary>
        public const string VerticalAlignment = VerticalShort + AlignmentShortest;

        /// <summary> TextObject.Fillments. </summary>
        public const string Fillment = "fill";

        /// <summary> TextObject.Appearings. </summary>
        public const string Appearing = "appr";

        /// <summary> <c>"fill_dir"</c> - carried by no model today. </summary>
        public const string FillDirection = Fill + _ + Direction;

        /// <summary> <c>"appr_m"</c> - carried by no model today. </summary>
        public const string AppearingMode = Appearing + _ + ModeShort;

        /// <summary> TextObject.AppearingMask. </summary>
        public const string AppearingMask = "amsk";

        /// <summary> <c>"over_edge"</c> - carried by no model today. </summary>
        public const string OverEdge = Over + _ + Edge;

        /// <summary> <c>"under_edge"</c> - carried by no model today. </summary>
        public const string UnderEdge = Under + _ + Edge;

        /// <summary> <c>"resource_type"</c> - ResourceMeta.ResourceType. </summary>
        public const string ResourceType = Resource + _ + Type;

        /// <summary> <c>"resource_id"</c> - ResourceMeta.ResourceId. </summary>
        public const string ResourceId = Resource + _ + Id;

        /// <summary> <c>"resources_meta"</c> - LevelMeta.ResourcesMeta. </summary>
        public const string ResourcesMeta = Resources + _ + Meta;

        // The resource-id family, on the shape shid/cid/pid/aid already had. auid rather than
        // aid because LevelTrack carries BOTH its AudioId and its AudioResourceId.

        /// <summary> EffectObjectCore.TextureResourceId, ShapeObject.TextureResourceId, TextureResource.TextureResourceId. </summary>
        public const string TextureResourceId = "txid";

        /// <summary> TextureResource.TextureResourceUV. </summary>
        public const string TextureResourceUV = "txuv";

        /// <summary> CachedFontText.FontResourceId, FontResource.FontResourceId, TextObject.FontResourceId. </summary>
        public const string FontResourceId = "fnid";

        /// <summary> <c>"fontchrs"</c> - LevelHints.FontCharacters. </summary>
        public const string FontCharacters = Font + Chars;

        /// <summary> AudioResource.AudioResourceId, LevelTrack.AudioResourceId. </summary>
        public const string AudioResourceId = "auid";

        /// <summary> BytesResource.ByteResourceId. </summary>
        public const string ByteResourceId = "byid";

        /// <summary> TextResource.TextResourceId. </summary>
        public const string TextResourceId = "ttid";

        /// <summary> <c>"uri_type"</c> - ResourceKey.UriType. </summary>
        public const string UriType = Uri + _ + Type;

        // Values

        /// <summary> <c>"anga"</c> - EffectAngleRandomPerComponent.AngleA, EffectAngleRandomUniform.AngleA. </summary>
        public const string AngleA = Angle + ValueA;

        /// <summary> <c>"angb"</c> - EffectAngleRandomPerComponent.AngleB, EffectAngleRandomUniform.AngleB. </summary>
        public const string AngleB = Angle + ValueB;

        /// <summary> <c>"clra"</c> - EffectColorRandomPerComponent.Color4A, EffectColorRandomUniform.Color4A. </summary>
        public const string ColorA = Color + ValueA;

        /// <summary> <c>"clrb"</c> - EffectColorRandomPerComponent.Color4B, EffectColorRandomUniform.Color4B. </summary>
        public const string ColorB = Color + ValueB;

        /// <summary> <c>"crvx"</c> - EffectScaleCurvesBySpeed.CurveX, EffectScaleCurvesOverLife.CurveX. </summary>
        public const string CurveX = Curve + CoordX;

        /// <summary> <c>"crvy"</c> - EffectScaleCurvesBySpeed.CurveY, EffectScaleCurvesOverLife.CurveY. </summary>
        public const string CurveY = Curve + CoordY;

        /// <summary> <c>"scaa"</c> - EffectScaleRandomPerComponent.ScaleA, EffectScaleRandomUniform.ScaleA. </summary>
        public const string ScaleA = Scale + ValueA;

        /// <summary> <c>"scab"</c> - EffectScaleRandomPerComponent.ScaleB, EffectScaleRandomUniform.ScaleB. </summary>
        public const string ScaleB = Scale + ValueB;

        /// <summary> <c>"clrb"</c> - ColorVerticalKey.Color4Bottom. </summary>
        public const string ColorBottom = Color + AlignmentB;

        /// <summary> <c>"clrt"</c> - ColorVerticalKey.Color4Top. </summary>
        public const string ColorTop = Color + AlignmentT;

        /// <summary> <c>"clrl"</c> - ColorHorizontalKey.Color4Left. </summary>
        public const string ColorLeft = Color + AlignmentL;

        /// <summary> <c>"clrr"</c> - ColorHorizontalKey.Color4Right. </summary>
        public const string ColorRight = Color + AlignmentR;

        /// <summary> <c>"clrbl"</c> - Color4X4Key.Color4BL. </summary>
        public const string ColorBL = Color + AlignmentBL;

        /// <summary> <c>"clrbm"</c> - carried by no model today. </summary>
        public const string ColorBM = Color + AlignmentBM;

        /// <summary> <c>"clrbr"</c> - Color4X4Key.Color4BR. </summary>
        public const string ColorBR = Color + AlignmentBR;

        /// <summary> <c>"clrcl"</c> - carried by no model today. </summary>
        public const string ColorCL = Color + AlignmentCL;

        /// <summary> <c>"clrcm"</c> - carried by no model today. </summary>
        public const string ColorCM = Color + AlignmentCM;

        /// <summary> <c>"clrcr"</c> - carried by no model today. </summary>
        public const string ColorCR = Color + AlignmentCR;

        /// <summary> <c>"clrtl"</c> - Color4X4Key.Color4TL. </summary>
        public const string ColorTL = Color + AlignmentTL;

        /// <summary> <c>"clrtm"</c> - carried by no model today. </summary>
        public const string ColorTM = Color + AlignmentTM;

        /// <summary> <c>"clrtr"</c> - Color4X4Key.Color4TR. </summary>
        public const string ColorTR = Color + AlignmentTR;

        /// <summary> <c>"minr"</c> - Color3MinMax.MinR, Color4MinMax.MinR. </summary>
        public const string MinR = Min + ChannelR;

        /// <summary> <c>"ming"</c> - Color3MinMax.MinG, Color4MinMax.MinG. </summary>
        public const string MinG = Min + ChannelG;

        /// <summary> <c>"minb"</c> - Color3MinMax.MinB, Color4MinMax.MinB. </summary>
        public const string MinB = Min + ChannelB;

        /// <summary> <c>"mina"</c> - Color4MinMax.MinA. </summary>
        public const string MinA = Min + ChannelA;

        /// <summary> <c>"maxr"</c> - Color3MinMax.MaxR, Color4MinMax.MaxR. </summary>
        public const string MaxR = Max + ChannelR;

        /// <summary> <c>"maxg"</c> - Color3MinMax.MaxG, Color4MinMax.MaxG. </summary>
        public const string MaxG = Max + ChannelG;

        /// <summary> <c>"maxb"</c> - Color3MinMax.MaxB, Color4MinMax.MaxB. </summary>
        public const string MaxB = Max + ChannelB;

        /// <summary> <c>"maxa"</c> - Color4MinMax.MaxA. </summary>
        public const string MaxA = Max + ChannelA;

        /// <summary> <c>"minx"</c> - Vector2Rect.MinX, Vector2RectStep.MinX, Vector3Rect.MinX, Vector3RectStep.MinX and 2 more. </summary>
        public const string MinX = Min + CoordX;

        /// <summary> <c>"miny"</c> - Vector2Rect.MinY, Vector2RectStep.MinY, Vector3Rect.MinY, Vector3RectStep.MinY and 2 more. </summary>
        public const string MinY = Min + CoordY;

        /// <summary> <c>"minz"</c> - Vector3Rect.MinZ, Vector3RectStep.MinZ, Vector4Rect.MinZ, Vector4RectStep.MinZ. </summary>
        public const string MinZ = Min + CoordZ;

        /// <summary> <c>"minw"</c> - Vector4Rect.MinW, Vector4RectStep.MinW. </summary>
        public const string MinW = Min + CoordW;

        /// <summary> <c>"maxx"</c> - Vector2Rect.MaxX, Vector2RectStep.MaxX, Vector3Rect.MaxX, Vector3RectStep.MaxX and 2 more. </summary>
        public const string MaxX = Max + CoordX;

        /// <summary> <c>"maxy"</c> - Vector2Rect.MaxY, Vector2RectStep.MaxY, Vector3Rect.MaxY, Vector3RectStep.MaxY and 2 more. </summary>
        public const string MaxY = Max + CoordY;

        /// <summary> <c>"maxz"</c> - Vector3Rect.MaxZ, Vector3RectStep.MaxZ, Vector4Rect.MaxZ, Vector4RectStep.MaxZ. </summary>
        public const string MaxZ = Max + CoordZ;

        /// <summary> <c>"maxw"</c> - Vector4Rect.MaxW, Vector4RectStep.MaxW. </summary>
        public const string MaxW = Max + CoordW;

        /// <summary> <c>"p1"</c> - carried by no model today. </summary>
        public const string Point1 = PointShort + Num1;

        /// <summary> <c>"p2"</c> - carried by no model today. </summary>
        public const string Point2 = PointShort + Num2;

        /// <summary> <c>"p3"</c> - carried by no model today. </summary>
        public const string Point3 = PointShort + Num3;

        /// <summary> <c>"wm"</c> - CurveKeyframeValue.WeightedMode. </summary>
        public const string WeightedMode = WeightShort + ModeShort;

        /// <summary> <c>"tm"</c> - CurveKeyframeValue.TangentMode. </summary>
        public const string TangentMode = TangentShort + ModeShort;

        /// <summary> <c>"it"</c> - CurveKeyframeValue.InTangent. </summary>
        public const string InTangent = InShort + TangentShort;

        /// <summary> <c>"ot"</c> - CurveKeyframeValue.OutTangent. </summary>
        public const string OutTangent = OutShort + TangentShort;

        /// <summary> <c>"iw"</c> - CurveKeyframeValue.InWeight. </summary>
        public const string InWeight = InShort + WeightShort;

        /// <summary> <c>"ow"</c> - CurveKeyframeValue.OutWeight. </summary>
        public const string OutWeight = OutShort + WeightShort;

        /// <summary> CurveValue.PreWrapMode. </summary>
        public const string PreWrapMode = "prewrap";

        /// <summary> CurveValue.PostWrapMode. </summary>
        public const string PostWrapMode = "postwrap";

        /// <summary> <c>"clrkeys"</c> - GradientValue.ColorKeys. </summary>
        public const string ColorKeys = Color + Keys;

        /// <summary> <c>"alphakeys"</c> - GradientValue.AlphaKeys. </summary>
        public const string AlphaKeys = Alpha + Keys;

        /// <summary> <c>"clrspc"</c> - GradientValue.ColorSpace. </summary>
        public const string ColorSpace = Color + Space;

        /// <summary> <c>"minasp"</c> - ScreenLimitBounds.MinAspect. </summary>
        public const string MinAspect = Min + Aspect;

        /// <summary> <c>"maxasp"</c> - ScreenLimitBounds.MaxAspect. </summary>
        public const string MaxAspect = Max + Aspect;

        // MIN AND MAX ARE ALWAYS A PREFIX, never a suffix - min_aspect, min_size and minx all
        // read that way already and these two were the outliers.

        /// <summary> RectObject.AnchorsMin. </summary>
        public const string AnchorMin = "minanc";

        /// <summary> RectObject.AnchorsMax. </summary>
        public const string AnchorMax = "maxanc";

        /// <summary> <c>"lang_strs"</c> - carried by no model today. </summary>
        public const string LanguageStrings = Language + _ + Strings;


        // Effects

        /// <summary> EffectObjectCore.ParticleCount. </summary>
        public const string ParticleCount = "pc";

        /// <summary> <c>"particle_collider"</c> - carried by no model today. </summary>
        public const string ParticleCollider = Particle + _ + Collider;

        /// <summary> EffectObjectCore.ParticlePivot. </summary>
        public const string ParticlePivot = "pp";

        /// <summary> EffectAngleCurvesBySpeed.SpeedRange, EffectColorGradientBySpeed.SpeedRange, EffectScaleCurvesBySpeed.SpeedRange. </summary>
        public const string SpeedRange = "sprng";

        /// <summary> EffectObjectForces.StartGravityMin. </summary>
        public const string MinStartGravity = "mingrv";

        /// <summary> EffectObjectForces.StartGravityMax. </summary>
        public const string MaxStartGravity = "maxgrv";

        /// <summary> EffectObjectForces.StartVelocityMin. </summary>
        public const string MinStartVelocity = "minvel";

        /// <summary> EffectObjectForces.StartVelocityMax. </summary>
        public const string MaxStartVelocity = "maxvel";

        /// <summary> EffectObjectForces.StartAngularVelocityMin. </summary>
        public const string MinStartAngularVelocity = "minavel";

        /// <summary> EffectObjectForces.StartAngularVelocityMax. </summary>
        public const string MaxStartAngularVelocity = "maxavel";

        /// <summary> EffectObjectForces.LinearVelocity. </summary>
        public const string LinearVelocity = "lvel";

        /// <summary> EffectObjectForces.OrbitalVelocity. </summary>
        public const string OrbitalVelocity = "ovel";

        /// <summary> EffectObjectForces.OrbitalCenterOffset. </summary>
        public const string OrbitalCenterOffset = "ocoff";

        /// <summary> EffectObjectForces.VelocitySpeed. </summary>
        public const string VelocitySpeed = "vspd";

        /// <summary> <c>"velocity_point"</c> - PlayerEvents.VelocityPoints. </summary>
        public const string VelocityPoint = Velocity + _ + Point;

        /// <summary> EffectObjectForces.LinearForce. </summary>
        public const string LinearForce = "lfrc";

        /// <summary> EffectShapeTorus.MajorRadius. </summary>
        public const string MajorRadius = "mjrad";

        /// <summary> EffectShapeTorus.MinorRadius. </summary>
        public const string MinorRadius = "mnrad";

        /// <summary> EffectShapeCone.TopRadius. </summary>
        public const string TopRadius = "trad";

        /// <summary> EffectShapeCone.BaseRadius. </summary>
        public const string BaseRadius = "brad";

        // Audio

        /// <summary> LevelTrackEffects.StereoPans. </summary>
        public const string StereoPan = "stpan";

        /// <summary> LevelTrackEffects.Lowpass. </summary>
        public const string Lowpass = "lpas";

        /// <summary> LevelTrackEffects.Highpass. </summary>
        public const string Highpass = "hpas";

        /// <summary> LevelTrackEffects.PitchShifter. </summary>
        public const string PitchShifter = "ptsh";

        /// <summary> AudioEffect.MixLevel. </summary>
        public const string MixLevel = "mixl";

        /// <summary> AudioReverb.DryLevel. </summary>
        public const string DryLevel = "dlvl";

        /// <summary> AudioChorus.DryMix, AudioEcho.DryMix, AudioFlange.DryMix. </summary>
        public const string DryMix = "dmix";

        /// <summary> AudioEcho.WetMix, AudioFlange.WetMix. </summary>
        public const string WetMix = "wmix";

        /// <summary> AudioHighpass.CutoffFreq, AudioLowpass.CutoffFreq. </summary>
        public const string CutoffFreq = "cutf";

        /// <summary> AudioReverb.ReverbDelay. </summary>
        public const string ReverbDelay = "rvbd";

        /// <summary> AudioReverb.RoomHF. </summary>
        public const string RoomHF = "rmhf";

        /// <summary> AudioReverb.RoomLF. </summary>
        public const string RoomLF = "rmlf";

        /// <summary> AudioReverb.HFReference. </summary>
        public const string HFRef = "hfrf";

        /// <summary> AudioReverb.LFReference. </summary>
        public const string LFRef = "lfrf";

        /// <summary> AudioEcho.MaxChannels, AudioPitchShifter.MaxChannels. </summary>
        public const string MaxChannels = "mchn";

        /// <summary> AudioPitchShifter.FFTSize. </summary>
        public const string FFTSize = "fftz";

        /// <summary> AudioReverb.DecayTime. </summary>
        public const string DecayTime = "dcyt";

        /// <summary> AudioReverb.DecayHFRatio. </summary>
        public const string DecayHFRatio = "dchf";

        /// <summary> AudioReverb.ReflectDelay. </summary>
        public const string ReflectDelay = "rfld";

        /// <summary> AudioParamEQ.CenterFreq. </summary>
        public const string CenterFreq = "cntf";

        /// <summary> AudioParamEQ.OctaveRange. </summary>
        public const string OctaveRange = "octr";

        /// <summary> AudioParamEQ.FrequencyGain. </summary>
        public const string FreqGain = "fgn";

        /// <summary> <c>"wmix1"</c> - AudioChorus.WetMixTap1. </summary>
        public const string WetMixTap1 = WetMix + Num1;

        /// <summary> <c>"wmix2"</c> - AudioChorus.WetMixTap2. </summary>
        public const string WetMixTap2 = WetMix + Num2;

        /// <summary> <c>"wmix3"</c> - AudioChorus.WetMixTap3. </summary>
        public const string WetMixTap3 = WetMix + Num3;

        /// <summary> AudioCompressor.MakeUpGain. </summary>
        public const string MakeUpGain = "mkgn";

        /// <summary> AudioNormalize.FadeInTime. </summary>
        public const string FadeInTime = "fint";

        /// <summary> AudioNormalize.LowestVolume. </summary>
        public const string LowestVolume = "lvlm";

        /// <summary> AudioNormalize.MaxAmp. </summary>
        public const string MaxAmp = "mamp";

        // Post Processing

        /// <summary> AnalogGlitchKey.ScanLineJitter. </summary>
        public const string ScanLineJitter = "slj";

        /// <summary> AnalogGlitchKey.VerticalJump. </summary>
        public const string VerticalJump = "vj";

        /// <summary> AnalogGlitchKey.HorizontalShake. </summary>
        public const string HorizontalShake = "hs";

        /// <summary> <c>"clrdft"</c> - AnalogGlitchKey.ColorDrift. </summary>
        public const string ColorDrift = Color + "dft";

        /// <summary> ColorCurvesKey.HueVsHue. </summary>
        public const string HueVsHue = "hvsh";

        /// <summary> ColorCurvesKey.HueVsSat. </summary>
        public const string HueVsSat = "hvss";

        /// <summary> ColorCurvesKey.SatVsSat. </summary>
        public const string SatVsSat = "svss";

        /// <summary> ColorCurvesKey.LumVsSat. </summary>
        public const string LumVsSat = "lvss";

        /// <summary> <c>"crvmstr"</c> - ColorCurvesKey.Master. </summary>
        public const string CurveMaster = Curve + "mstr";

        /// <summary> <c>"crvr"</c> - ColorCurvesKey.Red. </summary>
        public const string CurveRed = Curve + ChannelR;

        /// <summary> <c>"crvg"</c> - ColorCurvesKey.Green. </summary>
        public const string CurveGreen = Curve + ChannelG;

        /// <summary> <c>"crvb"</c> - ColorCurvesKey.Blue. </summary>
        public const string CurveBlue = Curve + ChannelB;

        /// <summary> <c>"liftclr"</c> - LiftGammaGainKey.LiftColor3. </summary>
        public const string LiftColor = Lift + Color;

        /// <summary> <c>"gammaclr"</c> - LiftGammaGainKey.GammaColor3. </summary>
        public const string GammaColor = Gamma + Color;

        /// <summary> <c>"gainclr"</c> - LiftGammaGainKey.GainColor3. </summary>
        public const string GainColor = Gain + Color;

        /// <summary> <c>"shwclr"</c> - ShadowsMidtonesHighlightsKey.ShadowsColor3. </summary>
        public const string ShadowColor = Shadow + Color;

        /// <summary> <c>"mtnclr"</c> - ShadowsMidtonesHighlightsKey.MidtonesColor3. </summary>
        public const string MidtoneColor = Midtone + Color;

        /// <summary> <c>"hltclr"</c> - ShadowsMidtonesHighlightsKey.HighlightsColor3. </summary>
        public const string HighlightColor = Highlight + Color;

        /// <summary> <c>"shwlim"</c> - ShadowsMidtonesHighlightsKey.ShadowLimits. </summary>
        public const string ShadowLimit = Shadow + "lim";

        /// <summary> <c>"hltlim"</c> - ShadowsMidtonesHighlightsKey.HighlightLimits. </summary>
        public const string HighlightLimit = Highlight + "lim";

        // Licensing

        /// <summary> <c>"license_name"</c> - carried by no model today. </summary>
        public const string LicenseName = License + _ + Name;

        /// <summary> <c>"license_url"</c> - carried by no model today. </summary>
        public const string LicenseUrl = License + _ + Url;

        /// <summary> <c>"license_type"</c> - TypicalLicense.Type. </summary>
        public const string LicenseType = License + _ + Type;

        /// <summary> <c>"allows_distribution"</c> - CustomLicense.AllowsDistribution. </summary>
        public const string AllowsDistribution = Allows + _ + Distribution;

        /// <summary> <c>"allows_modification"</c> - CustomLicense.AllowsModification. </summary>
        public const string AllowsModification = Allows + _ + Modification;

        /// <summary> <c>"allows_commercial_use"</c> - CustomLicense.AllowsCommercialUse. </summary>
        public const string AllowsCommercialUse = Allows + _ + Commercial + _ + Use;

        /// <summary> <c>"requires_attribution"</c> - CustomLicense.RequiresAttribution. </summary>
        public const string RequiresAttribution = Requires + _ + Attribution;

        /// <summary> <c>"requires_src_disclosure"</c> - CustomLicense.RequiresSourceDisclosure. </summary>
        public const string RequiresSourceDisclosure = Requires + _ + Source + _ + Disclosure;

        /// <summary> <c>"requires_same_license"</c> - CustomLicense.RequiresSameLicense. </summary>
        public const string RequiresSameLicense = Requires + _ + Same + _ + License;

        /// <summary> <c>"age_rating"</c> - LevelMeta.LevelAgeRating. </summary>
        public const string AgeRating = Age + _ + Rating;

        /// <summary> <c>"content_descriptors"</c> - LevelMeta.LevelContentDescriptors. </summary>
        public const string ContentDescriptors = Content + _ + Descriptors;

        // SPELLED OUT RATHER THAN COMPOSED, and the difference matters here more than anywhere else
        // in this file. `Min + _ + Generation` would read `"min_g"`, because Generation is the
        // ENVELOPE's one-character key - a MULTIPLE key, paid for on every envelope in a level. This
        // one appears once per metadata.json, beside `duration`, `age_rating` and
        // `content_descriptors`, so the rule asks for full words.

        /// <summary> <c>"min_generation"</c> - LevelMeta.MinGeneration. </summary>
        public const string MinGeneration = "min_generation";

        /// <summary> <c>"permission_scope"</c> - PermissionGrant.Scope. </summary>
        public const string PermissionScope = Permission + _ + Scope;

        /// <summary> <c>"granted_at"</c> - PermissionGrant.GrantedAt. </summary>
        public const string GrantedAt = Granted + _ + At;

        /// <summary> <c>"expires_at"</c> - PermissionGrant.ExpiresAt. </summary>
        public const string ExpiresAt = Expires + _ + At;

        /// <summary> <c>"proof_url"</c> - PermissionGrant.ProofUrl. </summary>
        public const string ProofUrl = Proof + _ + Url;

        /// <summary> <c>"proof_text"</c> - PermissionGrant.ProofText. </summary>
        public const string ProofText = Proof + _ + Text;

        // Publishing

        /// <summary> <c>"profile_key"</c> - PublishProfile.ProfileKey. </summary>
        public const string ProfileKey = Profile + _ + Key;

        /// <summary> <c>"allow_licenses"</c> - PublishProfile.AllowedLicenses. </summary>
        public const string AllowedLicenses = Allow + _ + Licenses;

        /// <summary> <c>"allow_uri_type"</c> - PublishProfile.AllowedUriTypes. </summary>
        public const string AllowedUriTypes = Allow + _ + Uri + _ + Type;

        /// <summary> <c>"allow_unknown_license"</c> - PublishProfile.AllowUnknownLicense. </summary>
        public const string AllowUnknownLicense = Allow + _ + Unknown + _ + License;

        /// <summary> <c>"allow_permission"</c> - PublishProfile.AllowPermissionInstead. </summary>
        public const string AllowPermissionInstead = Allow + _ + Permission;

        /// <summary> <c>"require_resource_meta"</c> - PublishProfile.RequireResourceMeta. </summary>
        public const string RequireResourceMeta = Require + _ + Resource + _ + Meta;

        /// <summary> <c>"require_resource_url"</c> - PublishProfile.RequireResourceUrl. </summary>
        public const string RequireResourceUrl = Require + _ + Resource + _ + Url;

        /// <summary> <c>"require_attribution"</c> - PublishProfile.RequireAttribution. </summary>
        public const string RequireAttribution = Require + _ + Attribution;

        /// <summary> <c>"require_age_rating"</c> - PublishProfile.RequireAgeRating. </summary>
        public const string RequireAgeRating = Require + _ + Age + _ + Rating;

        /// <summary> <c>"require_authors"</c> - PublishProfile.RequireLevelAuthors. </summary>
        public const string RequireLevelAuthors = Require + _ + Authors;

        /// <summary> <c>"require_hashes"</c> - PublishProfile.RequireHashes. </summary>
        public const string RequireHashes = Require + _ + Hashes;

        /// <summary> <c>"unknown_src_trust"</c> - PublishProfile.UnknownSourceTrust. </summary>
        public const string UnknownSourceTrust = Unknown + _ + Source + _ + Trust;

        /// <summary> <c>"max_resource_bytes"</c> - PublishProfile.MaxResourceBytes. </summary>
        public const string MaxResourceBytes = Max + _ + Resource + _ + Bytes;

        /// <summary> <c>"max_data_bytes"</c> - PublishProfile.MaxDataFileBytes. </summary>
        public const string MaxDataFileBytes = Max + _ + Data + _ + Bytes;

        /// <summary> <c>"max_total_bytes"</c> - PublishProfile.MaxTotalBytes. </summary>
        public const string MaxTotalBytes = Max + _ + Total + _ + Bytes;

        // Statistics

        /// <summary> <c>"first_played_utc"</c> - LevelStatistics.FirstPlayedUtc, ProfileStatistics.FirstPlayedUtc. </summary>
        public const string FirstPlayedUtc = First + _ + Played + _ + Utc;

        /// <summary> <c>"last_played_utc"</c> - LevelStatistics.LastPlayedUtc, ProfileStatistics.LastPlayedUtc. </summary>
        public const string LastPlayedUtc = Last + _ + Played + _ + Utc;

        /// <summary> <c>"first_cleared_utc"</c> - LevelStatistics.FirstClearUtc. </summary>
        public const string FirstClearUtc = First + _ + Cleared + _ + Utc;

        /// <summary> <c>"last_edited_utc"</c> - LevelEditorStatistics.LastEditedUtc. </summary>
        public const string LastEditedUtc = Last + _ + Edited + _ + Utc;

        /// <summary> <c>"time_utc"</c> - BestRun.TimeUtc. </summary>
        public const string TimeUtc = Time + _ + Utc;

        /// <summary> <c>"real_seconds"</c> - LevelStatistics.TotalRealSeconds. </summary>
        public const string RealSeconds = Real + _ + Seconds;

        /// <summary> <c>"app_seconds"</c> - ProfileStatistics.TotalAppSeconds. </summary>
        public const string AppSeconds = App + _ + Seconds;

        /// <summary> <c>"edit_seconds"</c> - LevelEditorStatistics.TotalEditSeconds. </summary>
        public const string EditSeconds = Edit + _ + Seconds;

        /// <summary> <c>"menu_seconds"</c> - ScreenTimeStatistics.MenuSeconds. </summary>
        public const string MenuSeconds = Menu + _ + Seconds;

        /// <summary> <c>"game_seconds"</c> - ScreenTimeStatistics.GameSeconds. </summary>
        public const string GameSeconds = Game + _ + Seconds;

        /// <summary> <c>"editor_seconds"</c> - ScreenTimeStatistics.EditorSeconds. </summary>
        public const string EditorSeconds = Editor + _ + Seconds;

        /// <summary> <c>"loading_seconds"</c> - ScreenTimeStatistics.LoadingSeconds. </summary>
        public const string LoadingSeconds = Loading + _ + Seconds;

        /// <summary> <c>"app_launches"</c> - ProfileStatistics.AppLaunches. </summary>
        public const string AppLaunches = App + _ + Launches;

        /// <summary> <c>"checkpoint_restarts"</c> - LevelStatistics.CheckpointRestarts. </summary>
        public const string CheckpointRestarts = Checkpoint + _ + Restarts;

        /// <summary> <c>"best_frame"</c> - LevelStatistics.BestFrame. </summary>
        public const string BestFrame = Best + _ + Frame;

        /// <summary> <c>"best_progress"</c> - LevelStatistics.BestProgress. </summary>
        public const string BestProgress = Best + _ + Progress;

        /// <summary> <c>"lives_left"</c> - BestRun.LivesLeft. </summary>
        public const string LivesLeft = Lives + _ + Left;

        /// <summary> <c>"spd_centi"</c> - RunProfile.SpeedCenti. </summary>
        public const string SpeedCenti = Speed + _ + Centi;

        /// <summary> <c>"deaths_by_bucket"</c> - DifficultyStatistics.DeathsByBucket. </summary>
        public const string DeathsByBucket = Deaths + _ + By + _ + Bucket;

        /// <summary> <c>"hits_by_bucket"</c> - DifficultyStatistics.HitsByBucket. </summary>
        public const string HitsByBucket = Hits + _ + By + _ + Bucket;

        /// <summary> <c>"deaths_by_checkpoint"</c> - DifficultyStatistics.DeathsByCheckpoint. </summary>
        public const string DeathsByCheckpoint = Deaths + _ + By + _ + Checkpoint;

        /// <summary> <c>"deaths_before_checkpoint"</c> - DifficultyStatistics.DeathsBeforeCheckpoint. </summary>
        public const string DeathsBeforeCheckpoint = Deaths + _ + Before + _ + Checkpoint;

        /// <summary> <c>"bucket_frame_duration"</c> - DifficultyStatistics.BucketFrameDuration. </summary>
        public const string BucketFrameDuration = Bucket + _ + Frame + _ + Duration;

        /// <summary> <c>"distinct_played"</c> - TotalsStatistics.DistinctLevelsPlayed. </summary>
        public const string DistinctLevelsPlayed = Distinct + _ + Played;

        /// <summary> <c>"distinct_cleared"</c> - TotalsStatistics.DistinctLevelsCleared. </summary>
        public const string DistinctLevelsCleared = Distinct + _ + Cleared;

        /// <summary> <c>"frames_simulated"</c> - TotalsStatistics.TotalFramesSimulated. </summary>
        public const string FramesSimulated = Frames + _ + Simulated;

        /// <summary> <c>"current_clear_streak"</c> - StreakStatistics.CurrentClearStreak. </summary>
        public const string CurrentClearStreak = Current + _ + Clear + _ + Streak;

        /// <summary> <c>"longest_clear_streak"</c> - StreakStatistics.LongestClearStreak. </summary>
        public const string LongestClearStreak = Longest + _ + Clear + _ + Streak;

        /// <summary> <c>"most_played_level_id"</c> - StreakStatistics.MostPlayedLevelId. </summary>
        public const string MostPlayedLevelId = Most + _ + Played + _ + LevelId;

        /// <summary> <c>"most_played_attempts"</c> - StreakStatistics.MostPlayedAttempts. </summary>
        public const string MostPlayedAttempts = Most + _ + Played + _ + Attempts;

        /// <summary> <c>"last_played_level_id"</c> - StreakStatistics.LastPlayedLevelId. </summary>
        public const string LastPlayedLevelId = Last + _ + Played + _ + LevelId;

        /// <summary> <c>"total_dashes"</c> - AvatarStatistics.TotalDashes. </summary>
        public const string TotalDashes = Total + _ + Dashes;

        /// <summary> <c>"total_distance_moved"</c> - AvatarStatistics.TotalDistanceMoved. </summary>
        public const string TotalDistanceMoved = Total + _ + Distance + _ + Moved;

        /// <summary> <c>"levels_created"</c> - EditorTotalsStatistics.LevelsCreated. </summary>
        public const string LevelsCreated = Levels + _ + Created;

        /// <summary> <c>"levels_deleted"</c> - EditorTotalsStatistics.LevelsDeleted. </summary>
        public const string LevelsDeleted = Levels + _ + Deleted;

        /// <summary> <c>"objs_created"</c> - EditorTotalsStatistics.ObjectsCreated. </summary>
        public const string ObjectsCreated = Objects + _ + Created;

        /// <summary> <c>"generators_ran"</c> - EditorTotalsStatistics.GeneratorsRun. </summary>
        public const string GeneratorsRun = Generators + _ + Ran;

        /// <summary> <c>"total_resources"</c> - EditorTotalsStatistics.TotalResources. </summary>
        public const string TotalResources = Total + _ + Resources;

        /// <summary> <c>"keyboard_mouse_seconds"</c> - DeviceTimeStatistics.KeyboardMouseSeconds. </summary>
        public const string KeyboardMouseSeconds = KeyboardMouse + _ + Seconds;

        /// <summary> <c>"touchscreen_seconds"</c> - DeviceTimeStatistics.TouchscreenSeconds. </summary>
        public const string TouchscreenSeconds = Touchscreen + _ + Seconds;

        /// <summary> <c>"gamepad_seconds"</c> - DeviceTimeStatistics.GamepadSeconds. </summary>
        public const string GamepadSeconds = Gamepad + _ + Seconds;

        /// <summary> <c>"gyro_seconds"</c> - DeviceTimeStatistics.DeviceGyroSeconds. </summary>
        public const string DeviceGyroSeconds = Gyro + _ + Seconds;
    }
}