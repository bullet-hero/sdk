namespace BH.SDK.Interop.AfterBeat
{
    // One file rather than one per type, unlike Models/Enums/: these are a transcription of another
    // project's format, not this project's vocabulary. They are read together, they change together
    // if that format changes, and splitting them into twenty files would suggest each is a decision
    // somebody here made.
    //
    // Numbering is the FORMAT's, never renumbered for tidiness - the gaps are real. Randomization
    // Type has no 2 (the wiki's own table skips it), so a value of 2 read from a file is genuinely
    // unknown rather than a member nobody bothered to write down.

    // Object type is the one place where the format's description and the files it writes disagree,
    // and the disagreement is invisible: the documented table starts at 4 (Hit / No Hit / Empty),
    // while a real level is mostly ot = 0. Read against the documented table alone, 0 is "not Hit",
    // so two thirds of a level's objects arrive with no collider and the level plays as scenery.
    //
    // Both numberings are therefore accepted. The older one is the legacy Project Arrhythmia set
    // (Normal / Helper / Decoration / Empty / Solid) and it agrees with the documented one on what
    // things MEAN, only not on what they are numbered - Helper and Decoration are drawn but never
    // hit, Solid is hit like Normal.
    //
    // The member names below are the SOURCE GAME's own (reflected out of its assembly), which is
    // why 3 and 6 are both empty and only 6 has "Alpha" in the name. Measured over three real
    // levels: 6 is what an empty is actually written as (445 of 446 carry no shape at all), 3 never
    // appears, 0 carries shapes and text, and 7 - a particle emitter, absent from the documented
    // table - carries a shape on 176 of its 191 objects.

    /// <summary> Afterbeat object type - .vgd objects[].ot, in both the documented numbering and
    /// the legacy one real files still use. </summary>
    public enum ABObjectType
    {
        /// <summary> Legacy: drawn and hit. </summary>
        Normal = 0,

        /// <summary> Legacy: drawn, never hit. </summary>
        Helper = 1,

        /// <summary> Legacy: drawn, never hit. </summary>
        Decoration = 2,

        /// <summary> Legacy: no geometry at all. </summary>
        Empty = 3,

        Hit = 4,
        NoHit = 5,

        /// <summary> No geometry either, and the one real files actually write for an empty - see
        /// this block's own header. </summary>
        AlphaEmpty = 6,

        /// <summary> A particle emitter. Drawn, never hit, and the emission itself has no
        /// equivalent here. </summary>
        Particles = 7,
    }

    // Anything that is not exactly 1 is a Rectangle, including a value the file never wrote - the
    // source game rounds and then tests against 1 alone (BeatmapObject.GetParticleEmitterShapeType),
    // so an out-of-range number is a box rather than an error.

    /// <summary> Afterbeat particle emitter volume - .vgd objects[].e[0].k[0].ev[8]. </summary>
    public enum ABParticleEmitterShapeType
    {
        Rectangle = 0,
        Circle = 1,
    }

    // Draw order is where the two formats agree least, and the disagreement is in three places at
    // once. Afterbeat sorts by an ABSOLUTE depth 0-60 with SMALLER in front, while this format
    // sorts by a PARENT-RELATIVE Layer with HIGHER in front. Afterbeat also organises a level into
    // editor layers and bins, which are bookkeeping there - they decide which timeline rows are
    // shown and nothing else - while here the timeline rows ARE the draw order, so importing that
    // organisation means spending draw order on it. And its player is drawn in front of every
    // ordinary object whatever its depth, so the whole depth range maps behind this format's avatar.
    //
    // No single mapping is right for every level, which is why this is an author's choice - but only
    // Auto keeps a level's range bounded by what the source format has rather than by how the level
    // is organised, which is why it is the default.

    /// <summary> What an import derives this format's draw order from. </summary>
    public enum ABLayerImport
    {
        /// <summary> Depth alone, packed into consecutive layers inside each band and anchored on
        /// the player line - the last layer behind the player is 0. The default: it draws the level
        /// in the order the source game drew it and costs one layer per depth the level actually
        /// uses, rather than one per depth the format allows. Packed rather than absolute, so an
        /// export returns the ORDER rather than the original depths. </summary>
        Auto = 0,

        /// <summary> Render depth alone - .vgd objects[].d - exactly as the source level draws it.
        /// A level whose author left every depth at its default arrives as one stack of clips. </summary>
        OnlyDepth = 1,

        /// <summary> The source editor's own grouping alone - .vgd objects[].ed.l and .ed.b, the
        /// bin being the finer of the two. What the source level DREW in front is discarded; what
        /// its author ORGANISED is kept. </summary>
        OnlyEditor = 2,

        /// <summary> Editor grouping and depth both, each editor group given a fixed band
        /// <see cref="ABOptions.EditorGroupStride"/> layers wide. Nothing is packed, so a
        /// group using three depths still costs a whole band - and a level with many groups runs
        /// out of draw order and is clamped. </summary>
        DepthAndEditor = 3,
    }

    // Not the two checkboxes the source editor shows ("above player", "in background") - one enum
    // with three values, which is why they cannot both be ticked over there either. Absent from a
    // document means Default: the key is written only when it is not.

    /// <summary> Which render band an object lives in - .vgd objects[].rl. </summary>
    public enum ABRenderLayer
    {
        /// <summary> Drawn with the level's own content, ordered by depth against it. The whole
        /// band is BEHIND the player, depth 0 included - the source game gives every object here
        /// one sorting order and separates them by draw distance alone, while its player sits on a
        /// sorting order above all of them. </summary>
        Default = 0,

        /// <summary> Drawn in front of everything in <see cref="Default"/>, player included. </summary>
        AbovePlayer = 1,

        /// <summary> Drawn behind everything in <see cref="Default"/>, with the background. </summary>
        Background = 2,
    }

    /// <summary> How an object decides when it dies - .vgd objects[].ak_t. The meaning of ak_o
    /// changes with it, which is why the two are never read apart. </summary>
    public enum ABAutokillType
    {
        // THE SOURCE GAME'S OWN TWO ANSWERS FOR THIS ONE DISAGREE, and which one an importer
        // copies decides whether the object plays at all. BeatmapObject.GetObjectLifeLength takes
        // an _oldStyle flag: true returns a literal 5000 seconds, false returns the last keyframe.
        // Every spawner passes TRUE (ObjectManager.cs, three call sites) and every editor timeline
        // path passes FALSE (ObjectEditor, EditorElement_ObjectPanel, Prefab.GetLength). So a level
        // holding one of these LOOKS finite in the editor and never dies when it plays - the kill
        // callback is scheduled 5000 seconds out and the sequence only runs to the end of the song.
        //
        // This is also the value an omitted ak_t reads back as: the field carries no [DefaultValue]
        // and the source serializer runs DefaultValueHandling.Ignore, so the key is written only
        // when the rule is something else. It is therefore a documented member rather than an
        // unknown one - and one that no level in the corpus has ever actually used.

        /// <summary> A legacy .lsb import that never carried a rule at all: it never dies. </summary>
        OldStyleNoAutokill = 0,

        /// <summary> Dies on its last keyframe. </summary>
        LastKeyframe = 1,

        /// <summary> Dies ak_o seconds after its last keyframe. </summary>
        LastKeyframeOffset = 2,

        /// <summary> Lives ak_o seconds from its own start. </summary>
        FixedTime = 3,

        /// <summary> Dies at absolute song time ak_o. </summary>
        SongTime = 4,
    }

    /// <summary> Per-object colour gradient - .vgd objects[].gt. </summary>
    public enum ABGradientType
    {
        None = 0,
        Linear = 1,
        InvertedLinear = 2,
        Radial = 3,
        InvertedRadial = 4,
    }

    // All five are real, and the format's own description lists four - it has no 2. The source
    // game's ObjectHelpers.RandomVector2Parser/RandomFloatParser switch on all five, and the names
    // below are what those branches DO rather than what the description calls them: every one of
    // them reads the keyframe's own value as one END of a range and "er" as the other, never as an
    // offset from it.

    /// <summary> How a keyframe randomizes its value - .vgd objects[].e[].k[].r. </summary>
    public enum ABRandomType
    {
        None = 0,

        /// <summary> Uniform between the value and its "er" counterpart, snapped to er[2] when that
        /// is set. </summary>
        Linear = 1,

        /// <summary> The same range, rounded to a whole number. Absent from the format's own table
        /// and implemented by the game all the same. </summary>
        LinearRounded = 2,

        /// <summary> One coin flip picks either the value or its "er" counterpart - and for a
        /// vector, the SAME flip decides both components. </summary>
        Toggle = 3,

        /// <summary> The value MULTIPLIED by a factor drawn uniformly from er[0]..er[1] - a scale,
        /// not an offset, and not an accumulation onto the previous keyframe's roll. </summary>
        Scale = 4,
    }

    // Six families, and the numbering is not the order anybody would write them in - it is
    // BeatmapObject.ShapeType's own, reflected out of the source game. Family 3 is called "Misc"
    // there rather than "Arrow", which is what the format's description calls it; the two arrows do
    // live in it, but the name is the game's, since a seventh member added to that family later
    // would make "Arrow" a lie and "Misc" still true.

    /// <summary> Main shape family - .vgd objects[].s, paired with .so. </summary>
    public enum ABShape
    {
        Square = 0,
        Circle = 1,
        Triangle = 2,
        Misc = 3,
        Text = 4,
        Hexagon = 5,
    }

    // Every family's option list ends with a CUSTOM POLYGON, and the index it sits at is the only
    // statement of how many presets that family has - BeatmapObject.IsCustom is a switch of exactly
    // these five numbers. Anything below the number is a preset, the number itself is the custom
    // polygon (parameters in csp), anything above it is not a legal shape at all.

    /// <summary> Which <c>so</c> value is the custom polygon of each <see cref="ABShape"/> family,
    /// i.e. also how many presets that family has. </summary>
    public static class ABShapeOptions
    {
        /// <summary> The option value that makes a square a custom polygon. </summary>
        public const int SquareCustom = 3;

        /// <summary> The same for a circle. </summary>
        public const int CircleCustom = 9;

        /// <summary> The same for a triangle. </summary>
        public const int TriangleCustom = 6;

        /// <summary> The same for the misc family. </summary>
        public const int MiscCustom = 2;

        /// <summary> The same for a hexagon. </summary>
        public const int HexagonCustom = 6;

        /// <summary> The custom-polygon option of one family, or -1 for a family that has none
        /// (Text, and anything this converter does not know). </summary>
        public static int GetCustomOption(int shape) => (ABShape)shape switch
        {
            ABShape.Square => SquareCustom,
            ABShape.Circle => CircleCustom,
            ABShape.Triangle => TriangleCustom,
            ABShape.Misc => MiscCustom,
            ABShape.Hexagon => HexagonCustom,
            _ => -1,
        };
    }

    /// <summary> Which of the fourteen fixed .vgd events[] arrays an index is. The order IS the
    /// format - an array's meaning is its position, it carries no name of its own. </summary>
    public enum ABEventTrack
    {
        CameraPosition = 0,
        CameraZoom = 1,
        CameraRotation = 2,
        CameraShake = 3,
        Theme = 4,
        Chromatic = 5,
        Bloom = 6,
        Vignette = 7,
        LensDistortion = 8,
        Grain = 9,
        Gradient = 10,
        Glitch = 11,
        Hue = 12,
        PlayerForce = 13,
    }

    /// <summary> Screen-gradient blend mode - .vgd events[10].ev[4]. </summary>
    public enum ABGradientOverlayMode
    {
        Linear = 0,
        Additive = 1,
        Multiply = 2,
        Screen = 3,
    }

    /// <summary> What activates a trigger - .vgd triggers[].event_trigger. </summary>
    public enum ABTriggerActivator
    {
        Time = 0,
        PlayerHit = 1,
        PlayerDeath = 2,
        LevelStart = 3,
        LevelRestart = 4,
        LevelRewind = 5,
    }

    /// <summary> What a trigger does - .vgd triggers[].event_type. </summary>
    public enum ABTriggerEvent
    {
        VnInk = 0,
        VnTimeline = 1,
        PlayerBubble = 2,
        PlayerLocation = 3,
        PlayerDash = 4,
        PlayerMoveX = 5,
        PlayerMoveY = 6,
        BackgroundSpin = 7,
        BackgroundMove = 8,
        PlayerDashDirection = 9,
        TimeGoTo = 10,
        TimeSpeed = 11,
    }

    /// <summary> Prefab category - .vgp type. Note the legacy .lsp numbering is different; nothing
    /// here reads .lsp. </summary>
    public enum ABPrefabType
    {
        Character = 0,
        CharacterParts = 1,
        Props = 2,
        Bullets = 3,
        Pulses = 4,
        Bombs = 5,
        Spinners = 6,
        Beams = 7,
        Static = 8,
        Misc1 = 9,
        Misc2 = 10,
        Misc3 = 11,
    }

    /// <summary> Level difficulty - .vgm song.difficulty. </summary>
    public enum ABDifficulty
    {
        Basic = 0,
        Moderate = 1,
        Advanced = 2,
        Expert = 3,
        Master = 4,
    }

    /// <summary> Which service .vgm artist.link points at; the link value is a fragment, not a URL. </summary>
    public enum ABLinkType
    {
        Spotify = 0,
        Soundcloud = 1,
        Bandcamp = 2,
        YoutubeMusic = 3,
        Newgrounds = 4,
    }

    /// <summary> Which game the song came from - .vgm references.game.id. </summary>
    public enum ABGameReference
    {
        None = 0,
        Custom = 1,
        Adofai = 2,
        ThousandXResist = 3,
        WindowKill = 4,
    }

    /// <summary> Steam Workshop visibility - .vgm beatmap.visibility. </summary>
    public enum ABVisibility
    {
        Public = 0,
        Friends = 1,
        Private = 2,
    }

    /// <summary> Camera jiggle preference - .vgm song.cam_jiggle. </summary>
    public enum ABCamJiggle
    {
        PlayerSelected = 0,
        ForceJiggle = 1,
        ForceNoJiggle = 2,
    }

    /// <summary> Default play mode of the level in the editor - .vgd editor.general.test_mode. </summary>
    public enum ABTestMode
    {
        Zen = 0,
        Normal = 1,
    }

    /// <summary> Selection outline style in the editor - .vgd editor.general.outline_mode. </summary>
    public enum ABOutlineMode
    {
        Standard = 0,
        Reduced = 1,
        None = 2,
    }
}