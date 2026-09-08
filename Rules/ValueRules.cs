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

        // Depth tie-break for objects sharing a layer, used by BOTH render paths. LayerCoefficient
        // puts whole layers exactly 1.0 apart, so two objects on one layer are coplanar - and each
        // path then fails its own way. Opaque: early-Z only rejects what DIFFERS in depth, so every
        // overlapping pair shades twice and ZTest LEqual hands the pixel to whichever drew last.
        // Transparent: the depth sort ties, and the tie is broken by an index that moves between
        // frames, so the pair visibly REORDERS. A small deterministic offset per object separates
        // them. Inframe objects are exempt - they stack themselves by MinLayerDelta above.
        //
        // Step times count must stay strictly below 1.0 or an object bleeds into the next layer's
        // band and the draw order the author sees stops matching the one they wrote. 512 leaves half
        // a layer spare; the full 1000 the step would allow leaves none.
        //
        // This is a DIFFERENT concern from MinLayerDelta above, which spaces the editor's own
        // overlay pieces so they do not z-fight each other. Do not merge the two constants.

        /// <summary> Granularity the layer Z offset step is quantized to, read by ABLayerMap. </summary>
        public const float LayerZOffsetStep = 0.001f;
        /// <summary> The layer Z offset count. </summary>
        public const int LayerZOffsetCount = 512;
        
        /// <summary> Lower bound of LayerKey.Layer, RectObject.Layer. </summary>
        public const int MinLayer = -1000;
        /// <summary> Upper bound of LayerKey.Layer, RectObject.Layer. </summary>
        public const int MaxLayer = 1000;
        /// <summary> Lowest layer selection allowed. </summary>
        public const float MinLayerSelection = MaxLayer + MinLayerDelta;

        // Between the two bands rather than beside them: the grid is a backdrop for the CONTENT, so
        // it has to sit above every authored layer, and it is also the one overlay a gizmo handle is
        // dragged across, so it has to sit below the handles. The selection band grows upwards from
        // MinLayerSelection by MinLayerDelta per line, so this leaves it ~25k lines of room before
        // the two could meet - the same reasoning that puts the gizmos at 1500.

        /// <summary> Lowest layer grid allowed. </summary>
        public const int MinLayerGrid = 1250;

        // The editor's viewport grid, whose cell size is a user preference (GameEditorSettings
        // .GridSize) rather than level data - a floor rather than a range, since how far out an
        // author zooms is what actually bounds it, and the overlay stops drawing on its own once the
        // cells stop being distinguishable.

        /// <summary> Lower bound of EditorGridSettings.Size. </summary>
        public const float MinGridSize = 0.001f;

        // The editor's collider overlay, which draws a semi-transparent fill over whatever a
        // ShapeObject's ColliderId actually is. Above the grid because a hitbox is content the author
        // is inspecting rather than a backdrop, and below the handles for the same reason the grid
        // is: a gizmo dragged across a collider must stay visible and grabbable. It allocates ONE
        // layer per drawn collider (overlapping translucent fills at an equal z pick their own draw
        // order and flicker), so it spends the band rather than sitting on a single value - 10k
        // slots at MinLayerDelta, far above any cap the overlay itself allows.

        /// <summary> Lowest layer colliders allowed. </summary>
        public const int MinLayerColliders = 1400;

        // The bot's own diagnostic overlays - its clearance grid, its chosen target, the reach it
        // believes it has. Above the collider fills because the whole question the grid answers is
        // "where would the bot rather be than in those", and below the handles like every other
        // overlay here. It spends the band a slot at a time for the collider fills' reason: the
        // grid is hundreds of translucent cells, and at an equal z they pick their own order.

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

        /// <summary> The layer used when nothing says otherwise, read by ABLevelImporter, ABPrefabImporter, RectObject. </summary>
        public const int DefaultLayer = 0;

        // A MULTIPLIER of whatever size the player already is, so the neutral value is 1 rather
        // than any number of world units - see PlayerEvents.Sizes. No maximum: a player scaled to
        // nothing is still there, controllable and hittable at a point, and a level that wants a
        // giant one is not this format's argument to have.

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
        
        // approximate size for min/max coordinates, because this allows
        // to calculate collision detection with at least 3 digits precision
        // S_max = (0.5·10⁻ᵈ) / (2ε) = 10⁻ᵈ / (4ε) = 2²¹ · 10⁻ᵈ = 2 097 152 · 10⁻ᵈ
        // d = 2, S_max = 20971.52, make this 10k on each side for more beauty.

        /// <summary> Lower bound of Checkpoint.Position, PosKey.Pos. </summary>
        public const float MinPos = -10000f;
        /// <summary> Upper bound of Checkpoint.Position, PosKey.Pos. </summary>
        public const float MaxPos = 10000f;
        /// <summary> The pos X used when nothing says otherwise. </summary>
        public const float DefaultPosX = 0f;
        /// <summary> The pos Y used when nothing says otherwise. </summary>
        public const float DefaultPosY = 0f;
        
        // A size is measured in the SAME world units a position is, so it gets the same range rather
        // than one of its own: an object may legitimately be as long as the space it is placed in,
        // and the old +-100 was a tenth of that with nothing behind the number. Real content proved
        // it: levels converted from Afterbeat carry sizes to 820, and 5% of their objects broke a
        // rule this format had no reason to hold them to.
        //
        // Derived rather than repeated, because the reason they agree is the point - a size that
        // outgrew MinPos/MaxPos would be an object bigger than any coordinate can address.

        /// <summary> Lower bound of ScaKey.Scale. </summary>
        public const float MinSca = MinPos;
        /// <summary> Upper bound of ScaKey.Scale. </summary>
        public const float MaxSca = MaxPos;
        /// <summary> The sca X used when nothing says otherwise. </summary>
        public const float DefaultScaX = 1f;
        /// <summary> The sca Y used when nothing says otherwise. </summary>
        public const float DefaultScaY = 1f;
        
        // Rotation is stored in RADIANS, so the generic +-1e6 it used to inherit is about 160 000
        // turns - a number no author writes and every angle-wrapping consumer has to survive. A
        // spinner is the case that needs room: an object turning continuously is authored as one
        // keyframe pair whose end angle keeps growing, so the cap is expressed in turns rather than
        // picked as a round radian figure. 1000 turns is ~8 minutes at 2 rev/s, past any real level.

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
        /// <summary> Upper bound of ZoomKey.Zoom. </summary>
        public const float MaxZoom = 100f;
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
        
        // A shape needs at least one triangle to be a shape at all - an empty one is a shape that
        // silently draws and collides with nothing, which is worse than no shape (that is what a
        // Null ShapeId already means, explicitly).
        //
        // THE CAP IS 128 BECAUSE 64 WAS NOT ENOUGH FOR THE GAME'S OWN SHAPES, which is the clearest
        // sign a bound is too tight: an inverted 32-sided ring is the box's rim, the ring's outer
        // rim and its inner disc, and that is 94 triangles. Six more built-in shapes sat at exactly
        // 64 with no room at all. Raising it can invalidate nothing - it only lets a hand-written
        // file carry more than it could before - and 128 triangles is still nothing to draw.

        /// <summary> Lowest shape triangles allowed, read by ABImportTests, ShapeCatalogServiceTests, ShapeSynthUtils and 1 more. </summary>
        public const int MinShapeTriangles = 1;
        /// <summary> Highest shape triangles allowed, read by ShapeCatalogServiceTests, ShapeGeometryUtils, ShapeGeometryUtilsTests and 2 more. </summary>
        public const int MaxShapeTriangles = 128;

        // Vertices are capped separately rather than derived from the triangle cap, because indexed
        // geometry shares corners: 64 triangles need 192 vertices unwelded and roughly a third of
        // that welded. The cap bounds the worst case, so a hand-written file cannot demand a vertex
        // buffer the triangle cap alone would suggest is impossible.

        /// <summary> Lowest shape vertices allowed, read by ShapeSynthUtils, ShapeSynthUtilsTests. </summary>
        public const int MinShapeVertices = 3;
        /// <summary> Highest shape vertices allowed, read by ShapeGeometryUtils, ShapeGeometryUtilsTests, ShapeSynthUtilsTests. </summary>
        public const int MaxShapeVertices = MaxShapeTriangles * 3;

        // A shape occupies exactly the object's own rect, the same box a quad used to. Rendering
        // reads UV out of the position (positionOS.xy + 0.5), so a point outside this range samples
        // past [0, 1]; collision would simply extend past what is drawn. Both failures are silent,
        // which is why the bound is enforced rather than documented.
        //
        // The game's own shapes obey it now too. The library that shipped before was centred on each
        // polygon's CIRCUMCENTRE, which for an odd side count is not the centre of its bounding box -
        // so 31 of 78 presets reached out to 0.577 and the editor carried a margin to compensate.
        // Centring on the bounding box instead brings every one of them inside, and costs nothing:
        // each form's longer axis measures exactly 1 either way.

        /// <summary> Lowest shape point allowed, read by ShapeCatalogServiceTests, ShapeGeometryUtils, ShapeSynthUtilsTests. </summary>
        public const float MinShapePoint = -0.5f;
        /// <summary> Highest shape point allowed, read by ABShapeMap, ShapeCatalogServiceTests, ShapeGeometryUtils and 1 more. </summary>
        public const float MaxShapePoint = 0.5f;

        // A curve needs two keys to define a segment and a gradient two stops to define a blend.
        // Below that there is nothing to interpolate between, and every consumer would have to
        // invent a fallback of its own.

        /// <summary> Lower bound of CurveValue.KeyFrames. </summary>
        public const int MinCurveKeys = 2;
        /// <summary> Upper bound of CurveValue.KeyFrames. </summary>
        public const int MaxCurveKeys = 16;
        /// <summary> Lower bound of GradientValue.AlphaKeys, GradientValue.ColorKeys. </summary>
        public const int MinGradientKeys = 2;
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
        
        // Also the fixed slot length of the player's per-frame text buffers, which is why it is a
        // round power of two rather than a number picked per field: a text object's authored string
        // and its rendered result each occupy exactly this much, so slot addressing stays two shifts
        // and two slots can never overlap. Raising it costs (slot length x text capacity x 2) bytes
        // twice over; lowering it silently truncates existing levels.

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

        // Licence text is the one field in the whole format meant to hold a wall of prose (a full
        // MIT/CC licence body), so it gets its own generous cap instead of the description one.

        /// <summary> Upper bound of CustomLicense.LicenseName. </summary>
        public const int MaxLicenseName = 256;
        /// <summary> Upper bound of CustomLicense.LicenseText. </summary>
        public const int MaxLicenseText = 65_536;

        // A Modification's field path ("pos[0].v"). Depth is what makes a path long, and the model
        // tree is nowhere near deep enough to need more than this.

        /// <summary> Upper bound of Modification.Key. </summary>
        public const int MaxModificationPath = 256;
    }
}