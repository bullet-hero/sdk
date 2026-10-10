using BH.SDK.Utils;

namespace BH.SDK.Rules
{
    /// <summary> The bounds every authored number falls back to when nothing more specific applies, plus the
    /// layer bands - including the two reserved above authored content for the editor's own overlays. </summary>
    public static class ValueRules
    {
        /// <summary> The number 0, named so a rule reads as a bound rather than as a magic number. </summary>
        public const int IntZero = 0;

        /// <summary> The number 1, named so a rule reads as a bound rather than as a magic number. </summary>
        public const int IntOne = 1;

        /// <summary> The number 0, named so a rule reads as a bound rather than as a magic number. </summary>
        public const float FloatZero = 0f;

        /// <summary> The number 1, named so a rule reads as a bound rather than as a magic number. </summary>
        public const float FloatOne = 1f;

        // default value limits without type specification.
        // Choose it because Max * Max => close to int.MaxValue 

        /// <summary> Lower bound of IntMinMax.Max, IntMinMax.Min, IntMinMaxStep.Max and 2 more. </summary>
        public const int MinIntValue = -1_000_000;

        /// <summary> Upper bound of IntMinMax.Max, IntMinMax.Min, IntMinMaxStep.Max and 3 more. </summary>
        public const int MaxIntValue = 1_000_000;

        /// <summary> Lower bound of CurveKeyframeValue.InTangent, CurveKeyframeValue.InWeight, CurveKeyframeValue.OutTangent and 64 more. </summary>
        public const float MinFloatValue = -1_000_000f;

        /// <summary> Upper bound of CurveKeyframeValue.InTangent, CurveKeyframeValue.InWeight, CurveKeyframeValue.OutTangent and 71 more. </summary>
        public const float MaxFloatValue = 1_000_000f;

        // convert from logical layer to real z position

        /// <summary> The layer coefficient. </summary>
        public const float LayerCoefficient = -1f;
        // minimal allowed delta for no clipping editor object through each other

        /// <summary> Lowest layer delta allowed. </summary>
        public const float MinLayerDelta = 0.01f;

        // Depth tie-break for objects sharing a layer, on BOTH render paths (coplanar objects z-fight or
        // reorder). Step x Count must stay strictly below 1.0 or an object bleeds into the next layer.
        // Not MinLayerDelta (the editor overlays' spacing): do not merge. Details: BH.SDK/Docs/RULES.md.

        /// <summary> Granularity the layer Z offset step is quantized to. </summary>
        public const float LayerZOffsetStep = 0.001f;

        /// <summary> The layer Z offset count. </summary>
        public const int LayerZOffsetCount = 512;

        /// <summary> Lower bound of RectObject.Layer. </summary>
        public const int MinLayer = -1000;

        /// <summary> Upper bound of RectObject.Layer. </summary>
        public const int MaxLayer = 1000;

        // THE PLAYER LINE: the avatar renders inside the (0, 1) band, so a default-layer object draws BEHIND
        // it; in front is an explicit act (type 1). Anything that must know the line cites these constants.

        /// <summary> Highest authored layer that draws behind the player. </summary>
        public const int LastLayerBehindPlayer = 0;

        /// <summary> Lowest authored layer that draws in front of the player. </summary>
        public const int FirstLayerAbovePlayer = LastLayerBehindPlayer + 1;

        // The multiplayer reservation (nothing reads it yet): each avatar gets one slot of the band, 1 /
        // avatars present, not 1 / MaxAvatars. 128 is exact in binary float and keeps an eightfold depth
        // margin. Arithmetic and slot layout: BH.SDK/Docs/RULES.md, "Layers and depth bands".

        /// <summary> How many avatars may share the player band at once - the multiplayer cap. </summary>
        public const int MaxAvatars = 128;

        /// <summary> The narrowest slot one avatar is ever given, i.e. the slot width at the cap.
        /// A live session divides the band by how many avatars it actually has. </summary>
        public const float MinAvatarSlotWidth = 1f / MaxAvatars;

        /// <summary> Lowest layer selection allowed. </summary>
        public const float MinLayerSelection = MaxLayer + MinLayerDelta;

        // The grid is a backdrop for content, so above every authored layer, and below the handles a gizmo
        // is dragged across. The selection band grows upward from MinLayerSelection (~25k lines of room).

        /// <summary> Lowest layer grid allowed. </summary>
        public const int MinLayerGrid = 1250;

        // The viewport grid's cell size is a user preference: a floor only, the overlay stops drawing once
        // cells are indistinguishable.

        /// <summary> Lower bound of EditorGridSettings.Size. </summary>
        public const float MinGridSize = 0.001f;

        // Collider fill overlay: above the grid, below the handles. One layer per drawn collider, since
        // overlapping translucent fills at an equal z flicker.

        /// <summary> Lowest layer colliders allowed. </summary>
        public const int MinLayerColliders = 1400;

        // The bot's diagnostic overlays, above the collider fills and below the handles. One layer per
        // cell, for the collider fills' reason.

        /// <summary> Lowest layer bot debug allowed. </summary>
        public const int MinLayerBotDebug = 1450;

        /// <summary> Lowest layer gizmos allowed. </summary>
        public const int MinLayerGizmos = 1500;

        // Not an authored limit and not validated by any rule - the camera has no Layer field. This
        // is the z range the camera itself is allowed to occupy at runtime, wide enough to sit
        // outside every authored layer and every editor overlay band above them.

        /// <summary> Lowest camera layer allowed. </summary>
        public const float MinCameraLayer = -2000f;

        /// <summary> Highest camera layer allowed. </summary>
        public const float MaxCameraLayer = 2000f;

        /// <summary> The layer used when nothing says otherwise, read by RectObject, RulePrefabRootFixedAttribute, RulePrefabRootFixedTests. </summary>
        public const int DefaultLayer = 0;

        // A MULTIPLIER of the player's current size (neutral 1, see PlayerEvents.Sizes). No maximum: a
        // player scaled to nothing is still there and hittable.

        /// <summary> Smallest player size a level may ask for. </summary>
        public const float MinPlayerSize = 0f;

        /// <summary> What the player is scaled by when a level says nothing. </summary>
        public const float DefaultPlayerSize = 1f;

        // Multiplies every speed the avatar has at once - see PlayerEvents.Speeds. Zero is a player
        // that is frozen where it stands while the level keeps running, which the Controls track
        // already expresses in its own way, so there is nothing to forbid here either.

        /// <summary> Slowest the player may be asked to move, as a multiple of its own speed. </summary>
        public const float MinPlayerSpeed = 0f;

        /// <summary> What the player's speed is multiplied by when a level says nothing. </summary>
        public const float DefaultPlayerSpeed = 1f;

        /// <summary> Lower bound of Color3MinMax.MaxB, Color3MinMax.MaxG, Color3MinMax.MaxR and 18 more. </summary>
        public const float MinColor = 0f;

        /// <summary> Upper bound of Color3MinMax.MaxB, Color3MinMax.MaxG, Color3MinMax.MaxR and 18 more. </summary>
        public const float MaxColor = 1f;

        /// <summary> The color R used when nothing says otherwise. </summary>
        public const float DefaultColorR = 1f;

        /// <summary> The color G used when nothing says otherwise. </summary>
        public const float DefaultColorG = 1f;

        /// <summary> The color B used when nothing says otherwise. </summary>
        public const float DefaultColorB = 1f;

        /// <summary> The color A used when nothing says otherwise. </summary>
        public const float DefaultColorA = 1f;

        // Collision keeps 3 digits of precision up to S_max = 2^21 * 10^-d (d = 2: 20971.52), so the
        // coordinate range is 10k a side.

        /// <summary> Lower bound of Checkpoint.Position, PosKey.Pos. </summary>
        public const float MinPos = -10000f;

        /// <summary> Upper bound of Checkpoint.Position, PosKey.Pos. </summary>
        public const float MaxPos = 10000f;

        /// <summary> The pos X used when nothing says otherwise. </summary>
        public const float DefaultPosX = 0f;

        /// <summary> The pos Y used when nothing says otherwise. </summary>
        public const float DefaultPosY = 0f;

        // A size is in the same world units as a position, so it is DERIVED from the position range: it
        // cannot outgrow what a coordinate can address. Imported levels carry sizes to 820.

        /// <summary> Lower bound of ScaKey.Scale. </summary>
        public const float MinSca = MinPos;

        /// <summary> Upper bound of ScaKey.Scale. </summary>
        public const float MaxSca = MaxPos;

        /// <summary> The sca X used when nothing says otherwise. </summary>
        public const float DefaultScaX = 1f;

        /// <summary> The sca Y used when nothing says otherwise. </summary>
        public const float DefaultScaY = 1f;

        // Radians, capped in turns: a spinner is one keyframe pair whose end angle keeps growing. 1000
        // turns is about 8 minutes at 2 rev/s.

        /// <summary> Highest rotation turns allowed, read by AngleKey. </summary>
        public const int MaxRotationTurns = 1000;

        /// <summary> Lower bound of AngleKey.Angle. </summary>
        public const float MinRotation = -BHSDKMath.PI2 * MaxRotationTurns;

        /// <summary> Upper bound of AngleKey.Angle. </summary>
        public const float MaxRotation = BHSDKMath.PI2 * MaxRotationTurns;

        // Camera shake amplitude and rate. Amplitude is in the same world units MinPos/MaxPos
        // bounds, and a shake worth more than a tenth of the playfield is already a screen-clearing
        // effect; the rate shares the bound because a negative one only inverts the phase.

        /// <summary> Lower bound of ShakeKey.Intensity, ShakeKey.IntensityX, ShakeKey.IntensityY and 1 more. </summary>
        public const float MinShake = -1000f;

        /// <summary> Upper bound of ShakeKey.Intensity, ShakeKey.IntensityX, ShakeKey.IntensityY and 1 more. </summary>
        public const float MaxShake = 1000f;

        // Texture tiling and offset. Both used to inherit the generic +-1e6 through Vector2Value:
        // legal data that asks the sampler to repeat a texture a million times across one object.

        /// <summary> Lower bound of UVKey.Offset, UVKey.Tiling. </summary>
        public const float MinUv = -1000f;

        /// <summary> Upper bound of UVKey.Offset, UVKey.Tiling. </summary>
        public const float MaxUv = 1000f;

        // 100^2 = 10000, apply to coord rules

        /// <summary> Lower bound of Alignment.Value, AlignmentKey.Value. </summary>
        public const float MinAlignment = -100f;

        /// <summary> Upper bound of Alignment.Value, AlignmentKey.Value. </summary>
        public const float MaxAlignment = 100f;

        /// <summary> The alignment X used when nothing says otherwise. </summary>
        public const float DefaultAlignmentX = 0.5f;

        /// <summary> The alignment Y used when nothing says otherwise. </summary>
        public const float DefaultAlignmentY = 0.5f;

        /// <summary> Lower bound of ZoomKey.Zoom. </summary>
        public const float MinZoom = 0f;
        // Raised from 100 because real imported content crosses it: 21% of one community level's
        // zoom keyframes landed between 102 and 142, silently clamped. Nothing normalizes by this bound - it is read
        // only by ZoomKey's range rule, the editor's zoom field and two generator parameters.

        /// <summary> Upper bound of ZoomKey.Zoom. </summary>
        public const float MaxZoom = 200f;

        /// <summary> The zoom used when nothing says otherwise, read by BeatFlashGenerator, ZoomKey. </summary>
        public const float DefaultZoom = 10f;

        /// <summary> The uv X used when nothing says otherwise, read by TextureResource, UVKey. </summary>
        public const float DefaultUvX = 1f; // tilling x

        /// <summary> The uv Y used when nothing says otherwise, read by TextureResource, UVKey. </summary>
        public const float DefaultUvY = 1f; // tilling y

        /// <summary> The uv Z used when nothing says otherwise, read by TextureResource, UVKey. </summary>
        public const float DefaultUvZ = 0f; // offset x

        /// <summary> The uv W used when nothing says otherwise, read by TextureResource, UVKey. </summary>
        public const float DefaultUvW = 0f; // offset y

        /// <summary> Lower bound of Color3ThemeRef.ThemeColorIndex, Color4ThemeRef.ThemeColorIndex. </summary>
        public const int MinThemeIndex = 0;

        /// <summary> Upper bound of Color3ThemeRef.ThemeColorIndex, Color4ThemeRef.ThemeColorIndex. </summary>
        public const int MaxThemeIndex = 63;

        /// <summary> Bounds ThemeData.Matrix. </summary>
        public const int ThemeCount = 64;

        // 128 because 64 did not fit the game's own shapes (an inverted 32-sided ring is 94 triangles).
        // A shape needs at least one triangle: an empty one draws and collides with nothing. See RULES.md.

        /// <summary> Lowest shape triangles allowed, read by ShapeCatalogServiceTests, ShapeSynthUtils, ShapeSynthUtilsTests. </summary>
        public const int MinShapeTriangles = 1;

        /// <summary> Highest shape triangles allowed, read by ShapeCatalogServiceTests, ShapeGeometryUtils, ShapeGeometryUtilsTests and 2 more. </summary>
        public const int MaxShapeTriangles = 128;

        // Capped apart from the triangles: indexed geometry shares corners (64 triangles need 192 vertices
        // unwelded, roughly a third welded). Bounds the worst case.

        /// <summary> Lowest shape vertices allowed, read by ShapeSynthUtils, ShapeSynthUtilsTests. </summary>
        public const int MinShapeVertices = 3;

        /// <summary> Highest shape vertices allowed, read by ShapeGeometryUtils, ShapeGeometryUtilsTests, ShapeSynthUtilsTests. </summary>
        public const int MaxShapeVertices = MaxShapeTriangles * 3;

        // A shape occupies exactly the object's own rect: UV is read from positionOS.xy + 0.5, and collision
        // would extend past what is drawn. Both failures are silent, so the bound is enforced.

        /// <summary> Lowest shape point allowed, read by ShapeCatalogServiceTests, ShapeGeometryUtils, ShapeSynthUtilsTests. </summary>
        public const float MinShapePoint = -0.5f;

        /// <summary> Highest shape point allowed, read by ShapeCatalogServiceTests, ShapeGeometryUtils, ShapeSynthUtilsTests. </summary>
        public const float MaxShapePoint = 0.5f;

        // A curve needs two keys to define a segment: below that there is nothing to interpolate
        // between, and every consumer would have to invent a fallback of its own.

        /// <summary> Lower bound of CurveValue.KeyFrames. </summary>
        public const int MinCurveKeys = 2;

        /// <summary> Upper bound of CurveValue.KeyFrames. </summary>
        public const int MaxCurveKeys = 16;

        // A gradient needs only ONE stop: a constant colour every consumer already evaluates. Held to the
        // curve's number it made the editor's own "remove key" illegal a step early.

        /// <summary> Lower bound of GradientValue.AlphaKeys, GradientValue.ColorKeys. </summary>
        public const int MinGradientKeys = 1;

        /// <summary> Upper bound of GradientValue.AlphaKeys, GradientValue.ColorKeys. </summary>
        public const int MaxGradientKeys = 8;

        /// <summary> Lower bound of CurveKeyframeValue.Time. </summary>
        public const float MinCurveTime = 0f;

        /// <summary> Upper bound of CurveKeyframeValue.Time. </summary>
        public const float MaxCurveTime = 1f;

        /// <summary> Lower bound of GradientAlphaKeyValue.Time, GradientColorKeyValue.Time. </summary>
        public const float MinGradientTime = 0f;

        /// <summary> Upper bound of GradientAlphaKeyValue.Time, GradientColorKeyValue.Time. </summary>
        public const float MaxGradientTime = 1f;

        /// <summary> Lower bound of ScreenAspect.Width. </summary>
        public const int MinAspectWidth = 1;

        /// <summary> Lower bound of ScreenAspect.Height. </summary>
        public const int MinAspectHeight = 1;

        /// <summary> Upper bound of ScreenAspect.Width. </summary>
        public const int MaxAspectWidth = 100;

        /// <summary> Upper bound of ScreenAspect.Height. </summary>
        public const int MaxAspectHeight = 100;

        /// <summary> The aspect width used when nothing says otherwise, read by ScreenAspect. </summary>
        public const int DefaultAspectWidth = 16;

        /// <summary> The aspect height used when nothing says otherwise, read by ScreenAspect. </summary>
        public const int DefaultAspectHeight = 9;

        // Also the fixed slot length of the player's per-frame text buffers, hence a power of two (two
        // shifts of addressing). Raising it costs slot x text capacity x 2 bytes twice; lowering truncates.

        /// <summary> Upper bound of LevelMeta.LevelName, StringLanguage.Value, StringValue.Value and 1 more. </summary>
        public const int MaxGameString = 1024;

        /// <summary> What DefaultModel.Value, StringLanguage.LanguageCode holds when nothing says otherwise. </summary>
        public const string DefaultLanguageCode = "en";

        /// <summary> Upper bound of Author.Url, CustomLicense.LicenseUrl, PermissionGrant.ProofUrl and 2 more. </summary>
        public const int MaxUrl = 512;

        /// <summary> Upper bound of Author.Credit, Author.Name, BeatSegment.Name and 12 more. </summary>
        public const int MaxEditorName = 512;

        /// <summary> Upper bound of LevelMeta.LevelDescription, Marker.Description, ResourceMeta.ResourceDescription and 1 more. </summary>
        public const int MaxEditorDescription = 4096;

        // BCP-47 tags: "en", "pt-BR", "zh-Hans-CN". Bounded and shaped, because the code is a lookup
        // key - an unbounded or malformed one just silently never matches a player's locale.

        /// <summary> Upper bound of GeneralSettings.Language, StringLanguage.LanguageCode. </summary>
        public const int MaxLanguageCode = 16;

        /// <summary> Bounds DefaultModel.Value, LanguageModel.Value, StringLanguage.LanguageCode. </summary>
        public const string LanguageCodePattern = "^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$";

        // CustomLicense.LicenseText has NO bound here on purpose - see the note on the property. A
        // licence body is prose whose meaning does not survive truncation, so there is nothing to cap
        // it to; the name beside it is an ordinary label and keeps its own.

        /// <summary> Upper bound of CustomLicense.LicenseName. </summary>
        public const int MaxLicenseName = 256;
    }
}