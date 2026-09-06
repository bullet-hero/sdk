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

        public const string Frame = "frame";
        public const string Time = "time";
        public const string FrameShort = "f";
        public const string TimeShort = "t";
        public const string Ease = "e";
        public const string Min = "min";
        public const string Max = "max";
        public const string Step = "step";
        public const string Budget = "budget";
        public const string Replay = "replay";
        public const string Start = "start";
        public const string End = "end";
        public const string Prev = "prev";
        public const string Next = "next";
        public const string StartShort = "s";
        public const string EndShort = "e";

        public const string Type = "type";
        public const string Kind = "kind";
        public const string Value = "v";
        /// <summary> The word "value", for a settings key that spells it out - Value
        /// itself is the ENVELOPE's payload key and is one character. </summary>
        public const string ValueWord = "value";
        public const string Data = "data";
        public const string TypeShort = "tp";
        public const string ValueShort = "v";

        public const string Level = "level";
        public const string LevelShort = "lvl";
        public const string Version = "vrs";
        public const string Meta = "meta";
        public const string Track = "track";
        public const string Tracks = "tracks";
        public const string Settings = "settings";
        public const string General = "general";
        public const string Graphics = "graphics";
        public const string Interface = "iface";
        public const string Stats = "stats";
        public const string Show = "show";
        public const string All = "all";
        public const string Progress = "progress";
        public const string Pause = "pause";
        public const string Game = "game";
        public const string UI = "ui";
        public const string Audio = "audio";
        public const string Audios = "audios";
        public const string AudioShort = "a";
        public const string Resources = "resources";
        public const string Target = "target";
        public const string Fixed = "fixed";
        public const string Render = "render";
        public const string RenderShort = "r";
        public const string Load = "load";
        public const string Parallel = "parallel";
        public const string Web = "web";
        public const string Timeout = "timeout";
        public const string Delta = "delta";
        public const string Scrub = "scrub";
        public const string Diff = "diff";
        public const string Resync = "resync";
        public const string Dead = "dead";
        public const string Zone = "zone";
        public const string Correction = "correction";

        public const string Is = "is";
        public const string Name = "name";
        public const string Title = "title";
        public const string Description = "desc";
        public const string Active = "active";
        public const string ActiveShort = "a";

        // EVERY KEYFRAME PAYLOAD IS ONE KEY. A keyframe's concrete type comes from the static
        // type of the track holding it, and the polymorphic value inside carries its own
        // positional tag - nothing ever inferred a type from the property name, so spelling the
        // type out cost 3 characters on 176 383 of volcano's keys and bought nothing.
        public const string Bool = ValueShort;
        public const string Byte = "byte";
        public const string Int = ValueShort;
        public const string Float = ValueShort;
        public const string Vector2 = ValueShort;
        public const string Vector3 = ValueShort;
        public const string Vector4 = ValueShort;

        public const string String = "string";
        public const string Strings = "strs";
        public const string Language = "lang";

        public const string Index = "idx";
        public const string Id = "id";
        public const string Ids = "ids";
        public const string Guid = "guid";

        public const string Position = "pos";
        public const string Rotation = "rot";
        public const string Scale = "sca";
        public const string Zoom = "zoom";
        public const string Shake = "shake";
        public const string Size = "sz";
        public const string Angle = "ang";
        public const string Curve = "crv";
        public const string Gradient = "grd";
        public const string Color = "clr";
        public const string Alpha = "alpha";
        public const string AlphaShort = "a";

        public const string ChannelR = "r";
        public const string ChannelG = "g";
        public const string ChannelB = "b";
        public const string ChannelA = "a";
        public const string CoordX = "x";
        public const string CoordY = "y";
        public const string CoordZ = "z";
        public const string CoordW = "w";
        public const string ValueA = "a";
        public const string ValueB = "b";
        public const string ValueC = "c";
        public const string ValueD = "d";
        public const string Num0 = "0";
        public const string Num1 = "1";
        public const string Num2 = "2";
        public const string Num3 = "3";
        public const string Num4 = "4";
        public const string Num5 = "5";
        public const string Num6 = "6";
        public const string Num7 = "7";
        public const string Num8 = "8";
        public const string Num9 = "9";
        public const string AlignmentB = "b"; // bottom
        public const string AlignmentT = "t"; // top
        public const string AlignmentL = "l"; // left
        public const string AlignmentR = "r"; // right
        public const string AlignmentBL = "bl"; // bottom-left
        public const string AlignmentBM = "bm"; // bottom-middle
        public const string AlignmentBR = "br"; // bottom-right
        public const string AlignmentCL = "cl"; // center-left
        public const string AlignmentCM = "cm"; // center-middle
        public const string AlignmentCR = "cr"; // center-right
        public const string AlignmentTL = "tl"; // top-left
        public const string AlignmentTM = "tm"; // top-middle
        public const string AlignmentTR = "tr"; // top-right

        public const string Speed = "spd";
        public const string Range = "range";
        public const string Core = "core";
        public const string Force = "frc";
        public const string Forces = "frcs";
        public const string Linear = "linear";
        public const string Velocity = "velocity";
        public const string Point = "point";
        public const string Points = "points";
        public const string PointShort = "p";
        public const string Angular = "angular";
        public const string Orbital = "orbital";
        public const string Gravity = "gravity";
        public const string Offset = "off";
        public const string Center = "cntr";
        public const string CenterShort = "c";
        public const string Intensity = "intns";
        public const string Collision = "collision";
        public const string Collisions = "collisions";

        public const string Radius = "rad";
        public const string RadiusShort = "r";
        public const string Major = "major";
        public const string Minor = "minor";
        public const string Top = "top";
        public const string Base = "base";

        public const string Thickness = "thk";
        public const string Arc = "arc";
        public const string Width = "width";
        public const string Height = "height";
        public const string WidthShort = "w";
        public const string HeightShort = "h";
        public const string Spread = "spr";

        public const string Matrix = "mtx";
        public const string Theme = "theme";
        public const string Themes = "themes";
        public const string Marker = "marker";
        public const string Markers = "markers";
        public const string Beat = "beat";
        public const string Beats = "beats";
        public const string BPM = "bpm";
        public const string BeatsPerBar = "bpb";
        public const string Checkpoint = "checkpoint";
        public const string Checkpoints = "checkpoints";
        public const string Background = "background";
        public const string Backgrounds = "backgrounds";
        public const string BackgroundShort = "bg";

        public const string Object = "object";
        public const string Objects = "objs";
        public const string Instance = "instance";
        public const string Instances = "instances";
        public const string Parent = "parent";
        public const string ObjectShort = "obj";
        public const string ParentShort = "p";

        public const string BloomShort = "blm";
        public const string ChromaticShort = "chr";
        public const string VignetteShort = "vgn";
        public const string LensShort = "lns";
        public const string GrainShort = "grn";
        public const string MotionBlurShort = "mbr";
        public const string ColorCurvesShort = "ccv";
        public const string LiftGammaGainShort = "lgg";
        public const string ShadowsMidtonesHighlightsShort = "smh";
        public const string WhiteBalanceShort = "wbl";
        public const string AnalogGlitchShort = "agl";
        public const string DigitalGlitchShort = "dgl";

        public const string Classic = "classic";
        public const string Control = "control";
        public const string Controls = "controls";
        public const string Visibles = "visibles";
        public const string Layer = "layer";
        public const string Layers = "layers";
        public const string Pivot = "pivot";
        public const string Anchor = "anc";
        public const string LayerShort = "l";
        public const string PivotShort = "pv";
        public const string AnchorShort = "a";

        public const string Collider = "collider";
        public const string ColliderShort = "c";
        public const string ShapeShort = "sh";
        public const string Vertices = "vts";
        public const string Indices = "idxs";
        public const string Texture = "texture";
        public const string Textures = "textures";
        public const string Text = "text";
        public const string Texts = "texts";
        public const string Font = "font";
        public const string Fonts = "fonts";
        public const string Chars = "chrs";
        public const string Fill = "fill";
        public const string Mask = "mask";
        public const string Direction = "dir";
        public const string Resource = "resource";
        public const string Res = "res";
        public const string UV = "uv";
        public const string Tiling = "til";

        public const string Loop = "loop";
        public const string LoopShort = "l";
        public const string Particle = "particle";
        public const string Count = "count";
        public const string Counter = "counter";
        public const string Lifetime = "lt";
        public const string Has = "has";
        public const string Stop = "stop";
        public const string Local = "local";
        public const string Global = "global";
        public const string LocalShort = "l";
        public const string GlobalShort = "g";
        public const string Effect = "effect";
        public const string Effects = "eff";
        public const string Eff = "eff";
        public const string Shape = "shp";
        public const string Shapes = "shapes";
        public const string ShapesOpaque = "shapes_opaque";
        public const string ShapesTransparent = "shapes_transparent";
        public const string Shader = ShapeShort;
        public const string Triangle = "triangle";
        public const string Triangles = "triangles";

        public const string Prefab = "prefab";
        public const string Prefabs = "prefabs";
        public const string Mod = "mod";
        public const string Key = "key";
        public const string KeyShort = "k";
        public const string Property = "property";
        public const string Path = "path";
        public const string PathShort = "p";
        public const string Order = "order";

        public const string Author = "author";
        public const string Authors = "authors";

        public const string Credit = "credit";

        // ONE SPELLING FOR A SOURCE. Resource.Sources wrote src while ResourceMeta and
        // PublishProfile wrote sources, for the same concept.
        public const string Src = "src";
        public const string Source = Src;
        public const string Sources = Src;
        public const string Link = "link";
        public const string Uri = "uri";
        public const string Url = "url";

        public const string Vertical = "vertical";
        public const string Horizontal = "horizontal";
        public const string Alignment = "alignment";
        public const string VerticalShort = "v";
        public const string HorizontalShort = "h";
        public const string AlignmentShort = "align";
        public const string AlignmentShortest = "a";
        public const string Over = "over";
        public const string Under = "under";
        public const string Edge = "edge";
        public const string Distrib = "distrib";

        public const string Scan = "scan";
        public const string Line = "line";
        public const string Jitter = "jitter";
        public const string Jump = "jump";
        public const string Jmp = "jmp";
        public const string Drift = "drift";
        public const string Hue = "hue";
        public const string Sat = "sat";
        public const string Vs = "vs";
        public const string Lum = "lum";
        public const string Master = "master";
        public const string Red = "red";
        public const string Green = "green";
        public const string Blue = "blue";
        public const string Scatter = "sctr";
        public const string Multiplier = "mult";
        public const string Multi = "multi";
        public const string Lift = "lift";
        public const string Gamma = "gamma";
        public const string Gain = "gain";
        public const string Shadow = "shw";
        public const string Midtone = "mtn";
        public const string Highlight = "hlt";
        public const string Limit = "limit";
        public const string Limits = "limits";
        public const string LimitShort = "lmt";
        public const string Hint = "hint";
        public const string Hints = "hints";
        public const string HintShort = "hnt";
        public const string Capacity = "capacity";
        public const string CapacityShort = "cap";
        public const string Smoothness = "smo";
        public const string Rounded = "rou";
        public const string Temperature = "tem";
        public const string Tint = "tnt";

        public const string In = "in";
        public const string Out = "out";
        public const string InShort = "i";
        public const string OutShort = "o";
        public const string Tangent = "tangent";
        public const string Weight = "weight";
        public const string TangentShort = "t";
        public const string WeightShort = "w";
        public const string Weighted = "weighted";
        public const string Mode = "mode";
        public const string ModeShort = "m";
        public const string Keys = "keys";
        public const string Pre = "pre";
        public const string Post = "post";
        public const string Word = "word";
        public const string Wrap = "wrap";
        public const string Space = "spc";
        public const string Aspect = "asp";

        public const string Seed = "seed";
        public const string Fps = "fps";
        public const string Format = "format";
        public const string Length = "length";
        public const string LengthShort = "len";
        public const string Duration = "duration";
        public const string Tags = "tags";
        public const string DurationShort = "dur";
        public const string Span = "span";
        public const string SpanShort = "sp";
        public const string Screen = "screen";
        public const string Orientation = "orientation";
        public const string Window = "window";
        public const string Display = "display";
        public const string Resolution = "resolution";
        public const string Event = "event";
        public const string Events = "events";
        public const string Camera = "camera";
        public const string Processing = "processing";
        public const string Player = "player";
        public const string Editor = "editor";

        public const string Pitch = "pitch";
        public const string PitchShort = "pth";
        public const string Stereo = "stereo";
        public const string Pan = "pan";
        public const string Mixer = "mixer";

        public const string Up = "up";
        public const string Down = "down";
        public const string Pass = "pass";
        public const string Low = "low";
        public const string High = "high";
        public const string Echo = "echo";
        public const string Reverb = "rvrb";
        public const string Chorus = "chrs";
        public const string Shifter = "shifter";
        public const string Distortion = "dist";
        public const string Flange = "flng";
        public const string Compressor = "cmpr";
        public const string Normalize = "nrml";
        public const string ParamEQ = "pmeq";

        public const string Mix = "mix";
        public const string Dry = "dry";
        public const string Wet = "wet";
        public const string Cutoff = "cutoff";
        public const string Freq = "freq";
        public const string Delay = "dly";
        public const string Decay = "dcy";
        public const string Ratio = "ratio";
        public const string HF = "hf";
        public const string LF = "lf";
        public const string Channels = "channels";
        public const string Tap1 = "tap1";
        public const string Tap2 = "tap2";
        public const string Tap3 = "tap3";
        public const string Room = "room";
        public const string Reflect = "rfl";
        public const string Reflections = "rfls";
        public const string Diffusion = "dffs";
        public const string Density = "dnst";
        public const string Ref = "ref";
        public const string Reference = "reference";
        public const string Rate = "rate";
        public const string Depth = "dpth";
        public const string Feedback = "fdbk";
        public const string Overlap = "ovlp";
        public const string Fade = "fade";
        public const string Volume = "vlm";
        public const string Lowest = "lowest";
        public const string Amp = "amp";
        public const string FFT = "fft";
        public const string Octave = "octave";
        public const string Threshold = "threshold";
        public const string ThresholdShort = "thld";
        public const string Attack = "atk";
        public const string Release = "rls";
        public const string Make = "make";

        public const string Autosave = "autosave";
        public const string File = "file";
        public const string Files = "files";
        public const string Logo = "logo";

        public const string Aggressive = "aggressive";
        public const string License = "license";
        public const string Use = "use";
        public const string Same = "same";
        public const string Allow = "allow";
        public const string Allows = "allows";
        public const string Require = "require";
        public const string Requires = "requires";
        public const string Distribution = "distribution";
        public const string Modification = "modification";
        public const string Commercial = "commercial";
        public const string Attribution = "attribution";
        public const string Disclosure = "disclosure";
        public const string Age = "age";
        public const string Rating = "rating";
        public const string Content = "content";
        public const string Found = "found";
        public const string Descriptors = "descriptors";
        public const string Hashes = "hashes";
        public const string Grantor = "grantor";
        public const string Permission = "permission";
        public const string Permissions = "permissions";
        public const string Granted = "granted";
        public const string Expires = "expires";
        public const string At = "at";
        public const string Proof = "proof";
        public const string Scope = "scope";
        public const string Trust = "trust";
        public const string Domains = "domains";
        public const string Note = "note";
        public const string Profile = "profile";
        public const string Unknown = "unknown";
        public const string Licenses = "licenses";
        public const string Bytes = "bytes";
        public const string Total = "total";

        public const string Common = "common";
        public const string Shared = "shared";
        public const string Priority = "priority";
        public const string Selection = "selection";
        public const string Preview = "preview";
        public const string Pick = "pick";
        public const string Invisible = "invisible";
        public const string AABB = "aabb";
        public const string Manual = "manual";
        public const string Device = "device";
        public const string Keyboard = "keyboard";
        public const string Mouse = "mouse";
        public const string Touchscreen = "touchscreen";
        public const string Gamepad = "gamepad";
        public const string Touchpad = "touchpad";
        public const string Gyro = "gyro";
        public const string Sensitivity = "sens";
        public const string Smoothing = "smoothing";
        public const string Invert = "invert";
        public const string Switch = "switch";
        public const string Cooldown = "cooldown";
        public const string Cursor = "cursor";
        public const string Visible = "visible";
        public const string Recenter = "recenter";
        public const string Return = "return";
        public const string Hide = "hide";
        public const string Absolute = "abs";
        public const string Relative = "rel";
        public const string Hold = "hold";
        public const string Button = "button";
        public const string Buttons = "buttons";
        public const string Dash = "dash";
        public const string Double = "double";
        public const string Click = "click";
        public const string Tap = "tap";
        public const string Travel = "travel";
        public const string Finger = "finger";
        public const string Second = "second";
        public const string Handedness = "handedness";
        public const string Joystick = "joystick";
        public const string Dynamic = "dynamic";
        public const string Origin = "origin";
        public const string Icon = "icon";
        public const string Motion = "motion";
        public const string Stick = "stick";
        public const string Response = "response";
        public const string Pad = "pad";
        public const string Area = "area";
        public const string Axis = "axis";
        public const string Mapping = "mapping";
        public const string Always = "always";
        public const string On = "on";
        public const string Activation = "activation";
        public const string Calibrate = "calibrate";
        public const string Tilt = "tilt";
        public const string Brand = "brand";
        public const string Glyph = "glyph";
        public const string Style = "style";
        public const string Default = "default";
        public const string Gizmos = "gizmos";
        public const string Inframes = "inframes";
        public const string Grid = "grid";
        public const string Opacity = "opacity";
        public const string Reset = "reset";
        public const string Serialize = "serialize";
        public const string Bot = "bot";
        public const string Debug = "debug";
        public const string Reach = "reach";
        public const string Copy = "copy";

        public const string Savings = "savings";
        public const string History = "history";
        public const string Timeline = "timeline";
        public const string Wheel = "wheel";
        public const string Move = "move";
        public const string Long = "long";
        public const string Press = "press";
        public const string Snap = "snap";
        public const string Handle = "handle";
        public const string View = "view";
        public const string Dirty = "dirty";
        public const string Field = "field";
        public const string Clamps = "clamps";
        public const string Log = "log";
        public const string Unit = "unit";
        public const string Open = "open";
        public const string Select = "select";
        public const string To = "to";
        public const string Menu = "menu";
        public const string Lose = "lose";
        public const string Auto = "auto";

        // Statistics

        public const string Statistics = "stats";
        public const string Screens = "screens";
        public const string Totals = "totals";
        public const string Streaks = "streaks";
        public const string Devices = "devices";
        public const string Difficulty = "difficulty";
        public const string Records = "records";
        public const string Best = "best";
        public const string Bucket = "bucket";
        public const string Attempts = "attempts";
        public const string Clears = "clears";
        public const string Deaths = "deaths";
        public const string Hits = "hits";
        public const string Dashes = "dashes";
        public const string Quits = "quits";
        public const string Restarts = "restarts";
        public const string Sessions = "sessions";
        public const string Launches = "launches";
        public const string Distance = "distance";
        public const string Distinct = "distinct";
        public const string Simulated = "simulated";
        public const string Opens = "opens";
        public const string Saves = "saves";
        public const string Autosaves = "autosaves";
        public const string Operations = "operations";
        public const string Created = "created";
        public const string Deleted = "deleted";
        public const string Utc = "utc";
        public const string First = "first";
        public const string Last = "last";
        public const string Real = "real";
        public const string Left = "left";
        public const string Edit = "edit";
        public const string Edited = "edited";
        public const string Played = "played";
        public const string Cleared = "cleared";
        public const string Loading = "loading";
        public const string App = "app";
        public const string Lives = "lives";
        public const string Centi = "centi";
        public const string Before = "before";
        public const string By = "by";
        public const string Current = "current";
        public const string Longest = "longest";
        public const string Most = "most";
        public const string Clear = "clear";
        public const string Streak = "streak";
        public const string Levels = "levels";
        public const string Moved = "moved";
        public const string Seconds = "seconds";
        public const string Avatar = "avatar";
        public const string Frames = "frames";
        public const string Generators = "generators";
        public const string Ran = "ran";
        public const string Runs = "runs";

        private const string _ = "_";

        // ---------------------------------------------------------------------------------------------
        // Combined names
        // ---------------------------------------------------------------------------------------------

        public const string IsLocal = Is + _ + Local;
        public const string FrameDurationShort = "fdur";
        public const string ScreenLimit = "slim";
        public const string ScreenLimits = Screen + _ + Limits;

        public const string EditorSettings = Editor + _ + Settings;

        // GameEditorSettings' own keys. Every one of them lives INSIDE one of that model's nine
        // groups, which is what lets them stay this short - a key only has to be unique among its
        // siblings, so the group's own name carries the qualifier the flat shape used to spell out
        // (the old "grid_size" is "grid": { "size" } now). The flat keys these replaced are gone
        // from here entirely, and now that the snapshot that held them as literals is deleted, they
        // survive nowhere at all - a settings.json predating the restructure reads back as defaults.

        public const string MaxFiles = Max + _ + Autosave + _ + Files;
        public const string HistoryLength = History + _ + Length;
        public const string MinSize = Min + _ + Size;
        public const string MaxSize = Max + _ + Size;
        public const string MoveSensitivityX = Move + _ + Sensitivity + _ + CoordX;
        public const string MoveSensitivityY = Move + _ + Sensitivity + _ + CoordY;
        public const string WheelMultiplier = Wheel + _ + Multiplier;
        public const string ZoomToMouse = Zoom + _ + To + _ + Mouse;
        public const string ActiveDefault = Active + _ + Default;
        public const string ResetGizmos = Reset + _ + Gizmos;
        public const string BotDebug = Bot + _ + Debug;
        public const string BotDebugGrid = Bot + _ + Debug + _ + Grid;
        public const string BotDebugTarget = Bot + _ + Debug + _ + Target;
        public const string BotDebugReach = Bot + _ + Debug + _ + Reach;
        public const string RequiresHold = Multi + _ + Requires + _ + Hold;
        public const string PreviewCollider = Preview + _ + Collider + _ + On + _ + Select;
        public const string PickInvisibleAABB = Pick + _ + Invisible + _ + AABB;
        public const string LongPressDelay = Long + _ + Press + _ + Delay;
        public const string LongPressThreshold = Long + _ + Press + _ + Move + _ + Threshold;
        public const string ColliderOpacity = Collider + _ + Opacity + _ + Selection;
        public const string ColliderOpacityView = Collider + _ + Opacity + _ + View;
        public const string SnapThreshold = Snap + _ + Threshold;
        public const string EdgeHandle = Edge + _ + Handle;
        public const string LoopGlobal = Global + _ + Loop;
        public const string LoopLocal = Local + _ + Loop;
        public const string TimeFormat = Time + _ + Format;
        public const string DirtyFieldDelay = Dirty + _ + Field + _ + Delay;
        public const string RotationUnit = Rotation + _ + Display + _ + Unit;
        public const string LogClamps = Log + _ + ValueWord + _ + Clamps;
        public const string RenderInframes = Render + _ + Inframes;
        public const string LinkColliderShape = Link + _ + Collider + _ + To + _ + Shape;
        public const string AutoOpen = Auto + _ + Open;

        public const string GameEditor = Game + _ + Editor;
        public const string OpenMenuOnLose = Open + _ + Menu + _ + On + _ + Lose;
        public const string StatsActive = Stats + _ + Active;
        public const string StatsAlignmentX = Stats + _ + Alignment + _ + CoordX;
        public const string StatsAlignmentY = Stats + _ + Alignment + _ + CoordY;
        public const string MenuBackground = Menu + _ + Background;
        public const string ScreenOrientation = Screen + _ + Orientation;
        public const string ShowGameProgress = Show + _ + Game + _ + Progress;
        public const string ShowGamePause = Show + _ + Game + _ + Pause;
        public const string ShowGameInterface = Show + _ + Game + _ + Interface;
        public const string FpsTarget = Fps + _ + Target;
        public const string FpsFixed = Fps + _ + Fixed;
        public const string RenderEffects = Render + _ + Effects;
        public const string ShowAllFoundContent = Show + _ + All + _ + Found + _ + Content;
        public const string ResourceParallelLoadCount = Resource + _ + Parallel + _ + Load + _ + Count;
        public const string ResourceWebTimeout = Resource + _ + Web + _ + Timeout;
        public const string TargetDeltaTime = Target + _ + Delta + _ + Time;
        public const string ScrubTime = Scrub + _ + Time;
        public const string MaxScrubTime = Max + _ + Scrub + _ + Time;
        public const string ReplayStepBudget = Replay + _ + Step + _ + Budget;

        public const string FrameStepBudget = Frame + _ + Step + _ + Budget;

        // Replaced MaxDiffTime, and the key changed with it: what it measures is no longer "how far
        // audio may drift" but "how far the playhead must JUMP to count as a discontinuity". An old
        // settings.json simply falls back to the default for it, which is the intended outcome - the
        // stored number was tuned against a metric that no longer exists.
        public const string ResyncJumpTime = Resync + _ + Jump + _ + Time;
        public const string SyncDeadZone = Dead + _ + Zone;
        public const string PitchCorrection = Pitch + _ + Correction;

        // Controls. Replaced ClassicControlsType, whose key is simply gone: it had no gameplay
        // consumer at all, so an old settings.json losing it loses nothing that ever did anything. The
        // UserSettings domain deliberately did NOT bump for that - a removed OptIn property is skipped
        // on read, whereas a bump with no migration registered would throw on every existing file.

        public const string KeyboardMouse = Keyboard + _ + Mouse;
        public const string DeviceGyro = Device + _ + Gyro;

        public const string ManualDevice = Manual + _ + Device;
        public const string CursorVisible = Cursor + _ + Visible;
        public const string CursorScale = Cursor + _ + Scale;
        public const string CursorRecenter = Cursor + _ + Recenter;
        public const string CursorReturn = Cursor + _ + Return;

        // Same string as SyncDeadZone, which is an audio key - the two can never appear on one model,
        // which is exactly the condition this file reuses keys under.
        public const string DeadZone = Dead + _ + Zone;
        public const string InvertX = Invert + _ + CoordX;
        public const string InvertY = Invert + _ + CoordY;

        public const string RequireHold = Require + _ + Hold;
        public const string HoldButton = Hold + _ + Button;
        public const string DashOnDoubleClick = Dash + _ + On + _ + Double + _ + Click;
        public const string DoubleClickTime = Double + _ + Click + _ + Time;
        public const string DashKeys = Dash + _ + Keys;
        public const string CursorHideAbsolute = Cursor + _ + Hide + _ + Absolute;
        public const string CursorHideRelative = Cursor + _ + Hide + _ + Relative;

        public const string FingerOffsetX = Finger + _ + Offset + _ + CoordX;
        public const string FingerOffsetY = Finger + _ + Offset + _ + CoordY;
        public const string DashOnSecondFinger = Dash + _ + On + _ + Second + _ + Finger;
        public const string DashOnDoubleTap = Dash + _ + On + _ + Double + _ + Tap;
        public const string DoubleTapTime = Double + _ + Tap + _ + Time;
        public const string TapMaxTravel = Tap + _ + Max + _ + Travel;
        public const string JoystickAnchor = Joystick + _ + Anchor;
        public const string JoystickSize = Joystick + _ + Size;
        public const string JoystickTravel = Joystick + _ + Travel;
        public const string JoystickDynamicOrigin = Joystick + _ + Dynamic + _ + Origin;
        public const string DashButtonAnchor = Dash + _ + Button + _ + Anchor;
        public const string DashButtonSize = Dash + _ + Button + _ + Size;
        public const string DashButtonIcon = Dash + _ + Button + _ + Icon;

        public const string MotionStick = Motion + _ + Stick;
        public const string ResponseCurve = Response + _ + Curve;
        public const string DashButtons = Dash + _ + Buttons;

        public const string AxisMapping = Axis + _ + Mapping;
        public const string MaxTiltAngle = Max + _ + Tilt + _ + Angle;

        public const string CalibrateOnStart = Calibrate + _ + On + _ + Start;
        public const string TiltCenterX = Tilt + _ + Center + _ + CoordX;
        public const string TiltCenterY = Tilt + _ + Center + _ + CoordY;
        public const string DashSource = Dash + _ + Source;


        // Anti-aliasing. One key per field of AntiAliasingGraphicsSettings, plus the group's own
        // key on GraphicsSettings; the group reuses the shared Type key, which is safe because no
        // other model in this aggregate carries one.
        public const string AntiAliasing = "aa";
        public const string MSAA = "msaa";
        public const string HDR = "hdr";

        // Textures. The group's own key is the shared Textures one - a level's resource list and a
        // settings group can never appear on one model, which is the reuse this file allows.
        public const string Compression = "compress";
        public const string Mipmaps = "mips";
        public const string SizeLimit = Size + _ + Limit;
        public const string Filter = "filter";
        public const string Quality = "quality";

        // Display. Desktop-only presentation, and the group's own key is the shared Display one.
        public const string WindowMode = Window + _ + Mode;
        public const string ResolutionWidth = Resolution + _ + Width;
        public const string ResolutionHeight = Resolution + _ + Height;
        public const string RenderScale = Render + _ + Scale;
        public const string VSync = "vsync";

        public const string PostProcessing = Post + Processing;
        public const string RenderBloom = Render + _ + BloomShort;
        public const string RenderChroma = Render + _ + ChromaticShort;
        public const string RenderVignette = Render + _ + VignetteShort;
        public const string RenderLens = Render + _ + LensShort;
        public const string RenderGrain = Render + _ + GrainShort;
        public const string RenderMotionBlur = Render + _ + MotionBlurShort;
        public const string RenderColorCurves = Render + _ + ColorCurvesShort;
        public const string RenderLiftGammaGain = Render + _ + LiftGammaGainShort;
        public const string RenderShadowsMidtonesHighlights = Render + _ + ShadowsMidtonesHighlightsShort;
        public const string RenderWhiteBalance = Render + _ + WhiteBalanceShort;
        public const string RenderAnalogGlitch = Render + _ + AnalogGlitchShort;
        public const string RenderDigitalGlitch = Render + _ + DigitalGlitchShort;

        public const string ThemeIndex = "tidx";
        public const string ThemeId = "thid";
        public const string EffectId = "eid";

        public const string CameraEvents = Camera + _ + Events;
        public const string PostProcessingEvents = Post + Processing + _ + Events;
        public const string PlayerEvents = Player + _ + Events;

        // Clipboard - one key per section of ClipboardData. Objects/PrefabObjects hold whole
        // objects to be created; the *Keys ones hold the same model types carrying nothing but the
        // copied keyframes, so the two never share a key even where they share a value type.

        public const string PrefabObjects = Prefab + _ + Objects;
        public const string KeyObjects = Key + _ + Objects;
        public const string KeyTracks = Key + _ + Tracks;
        public const string AudioTracks = Audio + _ + Tracks;
        public const string GameKeys = Game + _ + Keys;
        public const string CameraKeys = Camera + _ + Keys;
        public const string PostProcessingKeys = Post + Processing + _ + Keys;
        public const string PlayerKeys = Player + _ + Keys;

        // Instances

        public const string LevelId = Level + _ + Id;
        public const string PrefabId = "pfid";
        public const string AudioId = AudioShort + Id;
        public const string ObjectId = Id;
        public const string ObjectIdCounter = "idctr";
        public const string AudioIdCounter = "aidctr";
        public const string PrevObjectId = Prev + _ + Id;
        public const string NextObjectId = Next + _ + Id;

        public const string ObjectIds = Ids;

        // Two shape references on one object, both ShapeId-typed: what is drawn and what is hit.
        // They must stay distinct keys even though the values are interchangeable.
        public const string ShapeId = ShapeShort + Id;
        public const string ColliderId = ColliderShort + Id;
        public const string ShapeName = "shname";
        public const string ParentObjectId = ParentShort + ObjectId;

        public const string LocalFrame = Local + _ + Frame;
        public const string LocalFrameShort = LocalShort + FrameShort;
        public const string StopLocalFrame = "slf";
        public const string HasStopLocalFrame = "hslf";
        public const string OffsetTime = "offt";
        public const string AudioLayer = "al";

        public const string EffShape = Eff + _ + Shape;
        public const string EffAngle = Eff + _ + Angle;
        public const string EffScale = Eff + _ + Scale;
        public const string EffColor = Eff + _ + Color;

        public const string PrefabIndex = Prefab + _ + Index;
        public const string FontSize = "fsize";
        public const string WordWrap = Wrap;
        public const string HorizontalAlignment = HorizontalShort + AlignmentShortest;
        public const string VerticalAlignment = VerticalShort + AlignmentShortest;

        public const string Fillment = "fill";
        public const string Appearing = "appr";
        public const string FillDirection = Fill + _ + Direction;
        public const string AppearingMode = Appearing + _ + ModeShort;
        public const string AppearingMask = "amsk";
        public const string OverEdge = Over + _ + Edge;
        public const string UnderEdge = Under + _ + Edge;

        public const string ResourceType = Resource + _ + Type;
        public const string ResourceId = Resource + _ + Id;

        public const string ResourcesMeta = Resources + _ + Meta;

        // The resource-id family, on the shape shid/cid/pid/aid already had. auid rather than
        // aid because LevelTrack carries BOTH its AudioId and its AudioResourceId.
        public const string TextureResourceId = "txid";
        public const string TextureResourceUV = "txuv";
        public const string FontResourceId = "fnid";
        public const string FontCharacters = Font + Chars;
        public const string AudioResourceId = "auid";
        public const string ByteResourceId = "byid";
        public const string TextResourceId = "ttid";
        public const string UriType = Uri + _ + Type;

        // Values

        public const string AngleA = Angle + ValueA;
        public const string AngleB = Angle + ValueB;
        public const string ColorA = Color + ValueA;
        public const string ColorB = Color + ValueB;
        public const string CurveX = Curve + CoordX;
        public const string CurveY = Curve + CoordY;
        public const string ScaleA = Scale + ValueA;
        public const string ScaleB = Scale + ValueB;

        public const string ColorBottom = Color + AlignmentB;
        public const string ColorTop = Color + AlignmentT;
        public const string ColorLeft = Color + AlignmentL;
        public const string ColorRight = Color + AlignmentR;
        public const string ColorBL = Color + AlignmentBL;
        public const string ColorBM = Color + AlignmentBM;
        public const string ColorBR = Color + AlignmentBR;
        public const string ColorCL = Color + AlignmentCL;
        public const string ColorCM = Color + AlignmentCM;
        public const string ColorCR = Color + AlignmentCR;
        public const string ColorTL = Color + AlignmentTL;
        public const string ColorTM = Color + AlignmentTM;
        public const string ColorTR = Color + AlignmentTR;

        public const string MinR = Min + ChannelR;
        public const string MinG = Min + ChannelG;
        public const string MinB = Min + ChannelB;
        public const string MinA = Min + ChannelA;
        public const string MaxR = Max + ChannelR;
        public const string MaxG = Max + ChannelG;
        public const string MaxB = Max + ChannelB;
        public const string MaxA = Max + ChannelA;

        public const string MinX = Min + CoordX;
        public const string MinY = Min + CoordY;
        public const string MinZ = Min + CoordZ;
        public const string MinW = Min + CoordW;
        public const string MaxX = Max + CoordX;
        public const string MaxY = Max + CoordY;
        public const string MaxZ = Max + CoordZ;
        public const string MaxW = Max + CoordW;

        public const string Point1 = PointShort + Num1;
        public const string Point2 = PointShort + Num2;
        public const string Point3 = PointShort + Num3;

        public const string WeightedMode = WeightShort + ModeShort;
        public const string TangentMode = TangentShort + ModeShort;
        public const string InTangent = InShort + TangentShort;
        public const string OutTangent = OutShort + TangentShort;
        public const string InWeight = InShort + WeightShort;
        public const string OutWeight = OutShort + WeightShort;

        public const string PreWrapMode = "prewrap";
        public const string PostWrapMode = "postwrap";

        public const string ColorKeys = Color + Keys;
        public const string AlphaKeys = Alpha + Keys;
        public const string ColorSpace = Color + Space;

        public const string MinAspect = Min + Aspect;
        public const string MaxAspect = Max + Aspect;

        // MIN AND MAX ARE ALWAYS A PREFIX, never a suffix - min_aspect, min_size and minx all
        // read that way already and these two were the outliers.
        public const string AnchorMin = "minanc";
        public const string AnchorMax = "maxanc";

        public const string LanguageStrings = Language + _ + Strings;


        // Effects

        public const string ParticleCount = "pc";
        public const string ParticleCollider = Particle + _ + Collider;
        public const string ParticlePivot = "pp";
        public const string SpeedRange = "sprng";

        public const string MinStartGravity = "mingrv";
        public const string MaxStartGravity = "maxgrv";
        public const string MinStartVelocity = "minvel";
        public const string MaxStartVelocity = "maxvel";
        public const string MinStartAngularVelocity = "minavel";
        public const string MaxStartAngularVelocity = "maxavel";
        public const string LinearVelocity = "lvel";
        public const string OrbitalVelocity = "ovel";
        public const string OrbitalCenterOffset = "ocoff";
        public const string VelocitySpeed = "vspd";
        public const string VelocityPoint = Velocity + _ + Point;
        public const string LinearForce = "lfrc";

        public const string MajorRadius = "mjrad";
        public const string MinorRadius = "mnrad";
        public const string TopRadius = "trad";
        public const string BaseRadius = "brad";

        // Audio

        public const string StereoPan = "stpan";
        public const string Lowpass = "lpas";
        public const string Highpass = "hpas";
        public const string PitchShifter = "ptsh";

        public const string MixLevel = "mixl";
        public const string DryLevel = "dlvl";
        public const string DryMix = "dmix";
        public const string WetMix = "wmix";
        public const string CutoffFreq = "cutf";
        public const string ReverbDelay = "rvbd";
        public const string RoomHF = "rmhf";
        public const string RoomLF = "rmlf";
        public const string HFRef = "hfrf";
        public const string LFRef = "lfrf";
        public const string MaxChannels = "mchn";
        public const string FFTSize = "fftz";
        public const string DecayTime = "dcyt";
        public const string DecayHFRatio = "dchf";
        public const string ReflectDelay = "rfld";
        public const string CenterFreq = "cntf";
        public const string OctaveRange = "octr";
        public const string FreqGain = "fgn";
        public const string WetMixTap1 = WetMix + Num1;
        public const string WetMixTap2 = WetMix + Num2;
        public const string WetMixTap3 = WetMix + Num3;
        public const string MakeUpGain = "mkgn";
        public const string FadeInTime = "fint";
        public const string LowestVolume = "lvlm";
        public const string MaxAmp = "mamp";

        // Post Processing

        public const string ScanLineJitter = "slj";
        public const string VerticalJump = "vj";
        public const string HorizontalShake = "hs";
        public const string ColorDrift = Color + "dft";
        public const string HueVsHue = "hvsh";
        public const string HueVsSat = "hvss";
        public const string SatVsSat = "svss";
        public const string LumVsSat = "lvss";
        public const string CurveMaster = Curve + "mstr";
        public const string CurveRed = Curve + ChannelR;
        public const string CurveGreen = Curve + ChannelG;
        public const string CurveBlue = Curve + ChannelB;
        public const string LiftColor = Lift + Color;
        public const string GammaColor = Gamma + Color;
        public const string GainColor = Gain + Color;
        public const string ShadowColor = Shadow + Color;
        public const string MidtoneColor = Midtone + Color;
        public const string HighlightColor = Highlight + Color;
        public const string ShadowLimit = Shadow + "lim";
        public const string HighlightLimit = Highlight + "lim";

        // Licensing

        public const string LicenseName = License + _ + Name;
        public const string LicenseUrl = License + _ + Url;
        public const string LicenseType = License + _ + Type;
        public const string AllowsDistribution = Allows + _ + Distribution;
        public const string AllowsModification = Allows + _ + Modification;
        public const string AllowsCommercialUse = Allows + _ + Commercial + _ + Use;
        public const string RequiresAttribution = Requires + _ + Attribution;
        public const string RequiresSourceDisclosure = Requires + _ + Source + _ + Disclosure;
        public const string RequiresSameLicense = Requires + _ + Same + _ + License;
        public const string AgeRating = Age + _ + Rating;
        public const string ContentDescriptors = Content + _ + Descriptors;
        public const string PermissionScope = Permission + _ + Scope;
        public const string GrantedAt = Granted + _ + At;
        public const string ExpiresAt = Expires + _ + At;
        public const string ProofUrl = Proof + _ + Url;
        public const string ProofText = Proof + _ + Text;

        // Publishing

        public const string ProfileKey = Profile + _ + Key;
        public const string AllowedLicenses = Allow + _ + Licenses;
        public const string AllowedUriTypes = Allow + _ + Uri + _ + Type;
        public const string AllowUnknownLicense = Allow + _ + Unknown + _ + License;
        public const string AllowPermissionInstead = Allow + _ + Permission;
        public const string RequireResourceMeta = Require + _ + Resource + _ + Meta;
        public const string RequireResourceUrl = Require + _ + Resource + _ + Url;
        public const string RequireAttribution = Require + _ + Attribution;
        public const string RequireAgeRating = Require + _ + Age + _ + Rating;
        public const string RequireLevelAuthors = Require + _ + Authors;
        public const string RequireHashes = Require + _ + Hashes;
        public const string UnknownSourceTrust = Unknown + _ + Source + _ + Trust;
        public const string MaxResourceBytes = Max + _ + Resource + _ + Bytes;
        public const string MaxDataFileBytes = Max + _ + Data + _ + Bytes;
        public const string MaxTotalBytes = Max + _ + Total + _ + Bytes;

        // Statistics

        public const string FirstPlayedUtc = First + _ + Played + _ + Utc;
        public const string LastPlayedUtc = Last + _ + Played + _ + Utc;
        public const string FirstClearUtc = First + _ + Cleared + _ + Utc;
        public const string LastEditedUtc = Last + _ + Edited + _ + Utc;
        public const string TimeUtc = Time + _ + Utc;

        public const string RealSeconds = Real + _ + Seconds;
        public const string AppSeconds = App + _ + Seconds;
        public const string EditSeconds = Edit + _ + Seconds;
        public const string MenuSeconds = Menu + _ + Seconds;
        public const string GameSeconds = Game + _ + Seconds;
        public const string EditorSeconds = Editor + _ + Seconds;
        public const string LoadingSeconds = Loading + _ + Seconds;

        public const string AppLaunches = App + _ + Launches;
        public const string CheckpointRestarts = Checkpoint + _ + Restarts;

        public const string BestFrame = Best + _ + Frame;
        public const string BestProgress = Best + _ + Progress;
        public const string LivesLeft = Lives + _ + Left;
        public const string SpeedCenti = Speed + _ + Centi;

        public const string DeathsByBucket = Deaths + _ + By + _ + Bucket;
        public const string HitsByBucket = Hits + _ + By + _ + Bucket;
        public const string DeathsByCheckpoint = Deaths + _ + By + _ + Checkpoint;
        public const string DeathsBeforeCheckpoint = Deaths + _ + Before + _ + Checkpoint;
        public const string BucketFrameDuration = Bucket + _ + Frame + _ + Duration;

        public const string DistinctLevelsPlayed = Distinct + _ + Played;
        public const string DistinctLevelsCleared = Distinct + _ + Cleared;
        public const string FramesSimulated = Frames + _ + Simulated;

        public const string CurrentClearStreak = Current + _ + Clear + _ + Streak;
        public const string LongestClearStreak = Longest + _ + Clear + _ + Streak;
        public const string MostPlayedLevelId = Most + _ + Played + _ + LevelId;
        public const string MostPlayedAttempts = Most + _ + Played + _ + Attempts;
        public const string LastPlayedLevelId = Last + _ + Played + _ + LevelId;

        public const string TotalDashes = Total + _ + Dashes;
        public const string TotalDistanceMoved = Total + _ + Distance + _ + Moved;

        public const string LevelsCreated = Levels + _ + Created;
        public const string LevelsDeleted = Levels + _ + Deleted;
        public const string ObjectsCreated = Objects + _ + Created;
        public const string GeneratorsRun = Generators + _ + Ran;
        public const string TotalResources = Total + _ + Resources;

        public const string KeyboardMouseSeconds = KeyboardMouse + _ + Seconds;
        public const string TouchscreenSeconds = Touchscreen + _ + Seconds;
        public const string GamepadSeconds = Gamepad + _ + Seconds;
        public const string DeviceGyroSeconds = Gyro + _ + Seconds;
    }
}