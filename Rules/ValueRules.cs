using BH.SDK.Utils;

namespace BH.SDK.Rules
{
    public static class ValueRules
    {
        public const int IntZero = 0;
        public const int IntOne = 1;
        public const float FloatZero = 0f;
        public const float FloatOne = 1f;
        
        // default value limits without type specification.
        // Choose it because Max * Max => close to int.MaxValue 
        public const int MinIntValue = -1_000_000;
        public const int MaxIntValue = 1_000_000;
        public const float MinFloatValue = -1_000_000f;
        public const float MaxFloatValue = 1_000_000f;
        
        // convert from logical layer to real z position
        public const float LayerCoefficient = -1f;
        // minimal allowed delta for no clipping editor object through each other
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
        public const float LayerZOffsetStep = 0.001f;
        public const int LayerZOffsetCount = 512;
        
        public const int MinLayer = -1000;
        public const int MaxLayer = 1000;
        public const float MinLayerSelection = MaxLayer + MinLayerDelta;

        // Between the two bands rather than beside them: the grid is a backdrop for the CONTENT, so
        // it has to sit above every authored layer, and it is also the one overlay a gizmo handle is
        // dragged across, so it has to sit below the handles. The selection band grows upwards from
        // MinLayerSelection by MinLayerDelta per line, so this leaves it ~25k lines of room before
        // the two could meet - the same reasoning that puts the gizmos at 1500.
        public const int MinLayerGrid = 1250;

        // The editor's viewport grid, whose cell size is a user preference (GameEditorSettings
        // .GridSize) rather than level data - a floor rather than a range, since how far out an
        // author zooms is what actually bounds it, and the overlay stops drawing on its own once the
        // cells stop being distinguishable.
        public const float MinGridSize = 0.001f;

        // The editor's collider overlay, which draws a semi-transparent fill over whatever a
        // ShapeObject's ColliderId actually is. Above the grid because a hitbox is content the author
        // is inspecting rather than a backdrop, and below the handles for the same reason the grid
        // is: a gizmo dragged across a collider must stay visible and grabbable. It allocates ONE
        // layer per drawn collider (overlapping translucent fills at an equal z pick their own draw
        // order and flicker), so it spends the band rather than sitting on a single value - 10k
        // slots at MinLayerDelta, far above any cap the overlay itself allows.
        public const int MinLayerColliders = 1400;

        // The bot's own diagnostic overlays - its clearance grid, its chosen target, the reach it
        // believes it has. Above the collider fills because the whole question the grid answers is
        // "where would the bot rather be than in those", and below the handles like every other
        // overlay here. It spends the band a slot at a time for the collider fills' reason: the
        // grid is hundreds of translucent cells, and at an equal z they pick their own order.
        public const int MinLayerBotDebug = 1450;

        public const int MinLayerGizmos = 1500;

        // Not an authored limit and not validated by any rule - the camera has no Layer field. This
        // is the z range the camera itself is allowed to occupy at runtime, wide enough to sit
        // outside every authored layer and every editor overlay band above them.
        public const float MinCameraLayer = -2000f;
        public const float MaxCameraLayer = 2000f;

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
        
        public const float MinColor = 0f;
        public const float MaxColor = 1f;
        public const float DefaultColorR = 1f;
        public const float DefaultColorG = 1f;
        public const float DefaultColorB = 1f;
        public const float DefaultColorA = 1f;
        
        // approximate size for min/max coordinates, because this allows
        // to calculate collision detection with at least 3 digits precision
        // S_max = (0.5·10⁻ᵈ) / (2ε) = 10⁻ᵈ / (4ε) = 2²¹ · 10⁻ᵈ = 2 097 152 · 10⁻ᵈ
        // d = 2, S_max = 20971.52, make this 10k on each side for more beauty.
        public const float MinPos = -10000f;
        public const float MaxPos = 10000f;
        public const float DefaultPosX = 0f;
        public const float DefaultPosY = 0f;
        
        // A size is measured in the SAME world units a position is, so it gets the same range rather
        // than one of its own: an object may legitimately be as long as the space it is placed in,
        // and the old +-100 was a tenth of that with nothing behind the number. Real content proved
        // it: levels converted from Afterbeat carry sizes to 820, and 5% of their objects broke a
        // rule this format had no reason to hold them to.
        //
        // Derived rather than repeated, because the reason they agree is the point - a size that
        // outgrew MinPos/MaxPos would be an object bigger than any coordinate can address.
        public const float MinSca = MinPos;
        public const float MaxSca = MaxPos;
        public const float DefaultScaX = 1f;
        public const float DefaultScaY = 1f;
        
        // Rotation is stored in RADIANS, so the generic +-1e6 it used to inherit is about 160 000
        // turns - a number no author writes and every angle-wrapping consumer has to survive. A
        // spinner is the case that needs room: an object turning continuously is authored as one
        // keyframe pair whose end angle keeps growing, so the cap is expressed in turns rather than
        // picked as a round radian figure. 1000 turns is ~8 minutes at 2 rev/s, past any real level.
        public const int MaxRotationTurns = 1000;
        public const float MinRotation = -BHSDKMath.PI2 * MaxRotationTurns;
        public const float MaxRotation = BHSDKMath.PI2 * MaxRotationTurns;

        // Camera shake amplitude and rate. Amplitude is in the same world units MinPos/MaxPos
        // bounds, and a shake worth more than a tenth of the playfield is already a screen-clearing
        // effect; the rate shares the bound because a negative one only inverts the phase.
        public const float MinShake = -1000f;
        public const float MaxShake = 1000f;

        // Texture tiling and offset. Both used to inherit the generic +-1e6 through Vector2Value:
        // legal data that asks the sampler to repeat a texture a million times across one object.
        public const float MinUv = -1000f;
        public const float MaxUv = 1000f;

        // 100^2 = 10000, apply to coord rules
        public const float MinAlignment = -100f;
        public const float MaxAlignment = 100f;
        public const float DefaultAlignmentX = 0.5f;
        public const float DefaultAlignmentY = 0.5f;
        
        public const float MinZoom = 0f;
        public const float MaxZoom = 100f;
        public const float DefaultZoom = 10f;
        
        public const float DefaultUvX = 1f; // tilling x
        public const float DefaultUvY = 1f; // tilling y
        public const float DefaultUvZ = 0f; // offset x
        public const float DefaultUvW = 0f; // offset y
        
        public const int MinThemeIndex = 0;
        public const int MaxThemeIndex = 63;
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
        public const int MinShapeTriangles = 1;
        public const int MaxShapeTriangles = 128;

        // Vertices are capped separately rather than derived from the triangle cap, because indexed
        // geometry shares corners: 64 triangles need 192 vertices unwelded and roughly a third of
        // that welded. The cap bounds the worst case, so a hand-written file cannot demand a vertex
        // buffer the triangle cap alone would suggest is impossible.
        public const int MinShapeVertices = 3;
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
        public const float MinShapePoint = -0.5f;
        public const float MaxShapePoint = 0.5f;

        // A curve needs two keys to define a segment and a gradient two stops to define a blend.
        // Below that there is nothing to interpolate between, and every consumer would have to
        // invent a fallback of its own.
        public const int MinCurveKeys = 2;
        public const int MaxCurveKeys = 16;
        public const int MinGradientKeys = 2;
        public const int MaxGradientKeys = 8;

        public const float MinCurveTime = 0f;
        public const float MaxCurveTime = 1f;
        public const float MinGradientTime = 0f;
        public const float MaxGradientTime = 1f;
        
        public const int MinAspectWidth = 1;
        public const int MinAspectHeight = 1;
        public const int MaxAspectWidth = 100;
        public const int MaxAspectHeight = 100;
        public const int DefaultAspectWidth = 16;
        public const int DefaultAspectHeight = 9;
        
        // Also the fixed slot length of the player's per-frame text buffers, which is why it is a
        // round power of two rather than a number picked per field: a text object's authored string
        // and its rendered result each occupy exactly this much, so slot addressing stays two shifts
        // and two slots can never overlap. Raising it costs (slot length x text capacity x 2) bytes
        // twice over; lowering it silently truncates existing levels.
        public const int MaxGameString = 1024;

        public const string DefaultLanguageCode = "en";
        public const int MaxUrl = 512;
        public const int MaxEditorName = 512;
        public const int MaxEditorDescription = 4096;

        // BCP-47 tags: "en", "pt-BR", "zh-Hans-CN". Bounded and shaped, because the code is a lookup
        // key - an unbounded or malformed one just silently never matches a player's locale.
        public const int MaxLanguageCode = 16;
        public const string LanguageCodePattern = "^[A-Za-z]{2,8}(-[A-Za-z0-9]{1,8})*$";

        // Licence text is the one field in the whole format meant to hold a wall of prose (a full
        // MIT/CC licence body), so it gets its own generous cap instead of the description one.
        public const int MaxLicenseName = 256;
        public const int MaxLicenseText = 65_536;

        // A Modification's field path ("pos[0].v"). Depth is what makes a path long, and the model
        // tree is nowhere near deep enough to need more than this.
        public const int MaxModificationPath = 256;
    }
}