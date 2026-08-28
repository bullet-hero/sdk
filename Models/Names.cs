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
        public const string Ease = "ease";
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
        public const string Value = "value";
        public const string Data = "data";
        public const string TypeShort = "t";
        public const string ValueShort = "v";

        public const string Level = "level";
        public const string Version = "version";
        public const string Meta = "meta";
        public const string Track = "track";
        public const string Tracks = "tracks";
        public const string Settings = "settings";
        public const string General = "general";
        public const string Graphics = "graphics";
        public const string Interface = "iface";
        public const string Stats = "stats";
        public const string Show = "show";
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

        public const string Bool = "bool";
        public const string Byte = "byte";
        public const string Int = "int";
        public const string Float = "flt";
        public const string Vector2 = "vec2";
        public const string Vector3 = "vec3";
        public const string Vector4 = "vec4";

        public const string String = "string";
        public const string Strings = "strings";
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
        public const string Size = "size";
        public const string Angle = "ang";
        public const string Curve = "curve";
        public const string Gradient = "gradient";
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
        public const string AlignmentB = "B"; // bottom
        public const string AlignmentT = "T"; // top
        public const string AlignmentL = "L"; // left
        public const string AlignmentR = "R"; // right
        public const string AlignmentBL = "BL"; // bottom-left
        public const string AlignmentBM = "BM"; // bottom-middle
        public const string AlignmentBR = "BR"; // bottom-right
        public const string AlignmentCL = "CL"; // center-left
        public const string AlignmentCM = "CM"; // center-middle
        public const string AlignmentCR = "CR"; // center-right
        public const string AlignmentTL = "TL"; // top-left
        public const string AlignmentTM = "TM"; // top-middle
        public const string AlignmentTR = "TR"; // top-right

        public const string Speed = "speed";
        public const string Range = "range";
        public const string Core = "core";
        public const string Force = "force";
        public const string Forces = "forces";
        public const string Linear = "linear";
        public const string Velocity = "velocity";
        public const string Point = "point";
        public const string Points = "points";
        public const string PointShort = "p";
        public const string Angular = "aglular";
        public const string Orbital = "orbital";
        public const string Gravity = "gravity";
        public const string Offset = "offset";
        public const string Center = "center";
        public const string CenterShort = "c";
        public const string Intensity = "intensity";
        public const string Collision = "collision";
        public const string Collisions = "collisions";

        public const string Radius = "radius";
        public const string RadiusShort = "r";
        public const string Major = "major";
        public const string Minor = "minor";
        public const string Top = "top";
        public const string Base = "base";

        public const string Thickness = "thickness";
        public const string Arc = "arc";
        public const string Width = "width";
        public const string Height = "height";
        public const string WidthShort = "w";
        public const string HeightShort = "h";
        public const string Spread = "spread";

        public const string Matrix = "matrix";
        public const string Theme = "theme";
        public const string Themes = "themes";
        public const string Marker = "marker";
        public const string Markers = "markers";
        public const string Beat = "beat";
        public const string Beats = "beats";
        public const string Bpm = "bpm";
        public const string BeatsPerBar = "bpb";
        public const string Checkpoint = "checkpoint";
        public const string Checkpoints = "checkpoints";
        public const string Background = "background";
        public const string Backgrounds = "backgrounds";
        public const string BackgroundShort = "bg";

        public const string Object = "object";
        public const string Objects = "objects";
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
        public const string Anchor = "anchor";
        public const string LayerShort = "l";
        public const string PivotShort = "p";
        public const string AnchorShort = "a";

        public const string Collider = "collider";
        public const string ColliderShort = "c";
        public const string ShapeShort = "sh";
        public const string Vertices = "vts";
        public const string Indices = "idxs";
        public const string Texture = "texture";
        public const string Textures = "textures";
        public const string Subling = "sbl";
        public const string Text = "text";
        public const string Texts = "texts";
        public const string Font = "font";
        public const string Fonts = "fonts";
        public const string Chars = "chars";
        public const string Fill = "fill";
        public const string Mask = "mask";
        public const string Direction = "direction";
        public const string Resource = "resource";
        public const string Res = "res";
        public const string UV = "uv";
        public const string Tilling = "tilling";

        public const string Loop = "loop";
        public const string Particle = "particle";
        public const string Count = "count";
        public const string Counter = "counter";
        public const string Lifetime = "lifetime";
        public const string Has = "has";
        public const string Stop = "stop";
        public const string Local = "local";
        public const string Global = "global";
        public const string LocalShort = "l";
        public const string GlobalShort = "g";
        public const string Effect = "effect";
        public const string Effects = "effects";
        public const string Eff = "eff";
        public const string Shape = "shape";
        public const string Shapes = "shapes";
        public const string ShapesOpaque = "shapes_opaque";
        public const string ShapesTransparent = "shapes_transparent";
        public const string Shader = "shader";
        public const string Triangle = "triangle";
        public const string Triangles = "triangles";

        public const string Prefab = "prefab";
        public const string Prefabs = "prefabs";
        public const string Mod = "mod";
        public const string Key = "key";
        public const string Property = "property";
        public const string Path = "path";
        public const string PathShort = "p";
        public const string Order = "order";

        public const string Author = "author";
        public const string Authors = "authors";
        public const string Source = "source";
        public const string Sources = "sources";
        public const string Src = "src";
        public const string Link = "link";
        public const string Uri = "uri";
        public const string Url = "url";

        public const string Vertical = "vertical";
        public const string Horizontal = "horizontal";
        public const string Alignment = "alignment";
        public const string VerticalShort = "v";
        public const string HorizontalShort = "h";
        public const string AlignmentShort = "align";
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
        public const string Scatter = "scatter";
        public const string Multiplier = "multiplier";
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
        public const string Smoothness = "smt";
        public const string Rounded = "rnd";
        public const string Temperature = "tmp";
        public const string Tint = "tnt";

        public const string In = "in";
        public const string Out = "out";
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
        public const string Space = "space";
        public const string Aspect = "aspect";

        public const string Seed = "seed";
        public const string Framerate = "framerate";
        public const string Fps = "fps";
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
        public const string Stereo = "stereo";
        public const string Pan = "pan";
        public const string Mixer = "mixer";

        public const string Up = "up";
        public const string Down = "down";
        public const string Pass = "pass";
        public const string Low = "low";
        public const string High = "high";
        public const string Echo = "echo";
        public const string Reverb = "reverb";
        public const string Chorus = "chorus";
        public const string Shifter = "shifter";
        public const string Distortion = "distortion";
        public const string Flange = "flange";
        public const string Compressor = "compressor";
        public const string Normalize = "normalize";
        public const string ParamEQ = "parameq";

        public const string Mix = "mix";
        public const string Dry = "dry";
        public const string Wet = "wet";
        public const string Cutoff = "cutoff";
        public const string Freq = "freq";
        public const string Delay = "delay";
        public const string Decay = "decay";
        public const string Ratio = "ratio";
        public const string HF = "hf";
        public const string LF = "lf";
        public const string Channels = "channels";
        public const string Tap1 = "tap1";
        public const string Tap2 = "tap2";
        public const string Tap3 = "tap3";
        public const string Room = "room";
        public const string Reflect = "reflect";
        public const string Reflections = "reflections";
        public const string Diffusion = "diffusion";
        public const string Density = "density";
        public const string Ref = "ref";
        public const string Reference = "reference";
        public const string Rate = "rate";
        public const string Depth = "depth";
        public const string Feedback = "feedback";
        public const string Overlap = "overlap";
        public const string Fade = "fade";
        public const string Volume = "volume";
        public const string Lowest = "lowest";
        public const string Amp = "amp";
        public const string FFT = "fft";
        public const string Octave = "octave";
        public const string Threshold = "threshold";
        public const string Attack = "attack";
        public const string Release = "release";
        public const string Make = "make";

        public const string Autosave = "autosave";
        public const string File = "file";
        public const string Files = "files";
        public const string Logo = "Logo";

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
        public const string Menu = "menu";
        public const string Lose = "lose";

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
        public const string FrameDurationShort = FrameShort + _ + DurationShort;
        public const string ScreenLimit = Screen + _ + Limit;
        public const string ScreenLimits = Screen + _ + Limits;

        public const string EditorSettings = Editor + _ + Settings;

        // GameEditorSettings' own keys. Every one of them lives INSIDE one of that model's nine
        // groups, which is what lets them stay this short - a key only has to be unique among its
        // siblings, so the group's own name carries the qualifier the flat shape used to spell out
        // (the old "grid_size" is "grid": { "size" } now). The flat keys these replaced are gone
        // from here entirely and survive only as literals in GameEditorSettingsV1_0, per the
        // Versions README: a snapshot must not track current naming.

        public const string MaxFiles = Max + _ + Files;
        public const string HistoryLength = History + _ + Length;
        public const string MinSize = Min + _ + Size;
        public const string MaxSize = Max + _ + Size;
        public const string MoveSensitivityX = Move + _ + Sensitivity + _ + CoordX;
        public const string MoveSensitivityY = Move + _ + Sensitivity + _ + CoordY;
        public const string WheelMultiplier = Wheel + _ + Multiplier;
        public const string ZoomToMouse = Zoom + _ + Mouse;
        public const string ActiveDefault = Active + _ + Default;
        public const string ResetGizmos = Reset + _ + Gizmos;
        public const string BotDebug = Bot + _ + Debug;
        public const string BotDebugGrid = Bot + _ + Debug + _ + Grid;
        public const string BotDebugTarget = Bot + _ + Debug + _ + Target;
        public const string BotDebugReach = Bot + _ + Debug + _ + Reach;
        public const string RequiresHold = Requires + _ + Hold;
        public const string PreviewCollider = Preview + _ + Collider;
        public const string PickInvisibleAABB = Pick + _ + Invisible + _ + AABB;
        public const string LongPressDelay = Long + _ + Press + _ + Delay;
        public const string LongPressThreshold = Long + _ + Press + _ + Threshold;
        public const string ColliderOpacity = Collider + _ + Opacity;
        public const string ColliderOpacityView = Collider + _ + Opacity + _ + View;
        public const string SnapThreshold = Snap + _ + Threshold;
        public const string EdgeHandle = Edge + _ + Handle;
        public const string LoopGlobal = Loop + _ + Global;
        public const string LoopLocal = Loop + _ + Local;
        public const string DirtyFieldDelay = Dirty + _ + Field + _ + Delay;
        public const string RotationUnit = Rotation + _ + Unit;
        public const string LogClamps = Log + _ + Clamps;
        public const string RenderInframes = Render + _ + Inframes;

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
        public const string FramerateTarget = Framerate + _ + Target;
        public const string FixedFramerate = Fixed + _ + Framerate;
        public const string RenderEffects = Render + _ + Effects;
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
        public const string DashOnDoubleClick = Dash + _ + Double + _ + Click;
        public const string DoubleClickTime = Double + _ + Click + _ + Time;
        public const string DashKeys = Dash + _ + Keys;
        public const string HideCursorAbsolute = Hide + _ + Cursor + _ + Absolute;
        public const string HideCursorRelative = Hide + _ + Cursor + _ + Relative;

        public const string FingerOffsetX = Finger + _ + Offset + _ + CoordX;
        public const string FingerOffsetY = Finger + _ + Offset + _ + CoordY;
        public const string DashOnSecondFinger = Dash + _ + Second + _ + Finger;
        public const string DashOnDoubleTap = Dash + _ + Double + _ + Tap;
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

        public const string CalibrateOnStart = Calibrate + _ + Start;
        public const string TiltCenterX = Tilt + _ + Center + _ + CoordX;
        public const string TiltCenterY = Tilt + _ + Center + _ + CoordY;
        public const string DashSource = Dash + _ + Source;


        // Anti-aliasing. One key per field of AntiAliasingGraphicsSettings, plus the group's own
        // key on GraphicsSettings; the group reuses the shared Type key, which is safe because no
        // other model in this aggregate carries one.
        public const string AntiAliasing = "aa";
        public const string Msaa = "msaa";
        public const string Hdr = "hdr";

        // Textures. The group's own key is the shared Textures one - a level's resource list and a
        // settings group can never appear on one model, which is the reuse this file allows.
        public const string Compression = "compress";
        public const string Mipmaps = "mips";
        public const string SizeLimit = Size + _ + Limit;

        // Display. Desktop-only presentation, and the group's own key is the shared Display one.
        public const string WindowMode = Window + _ + Mode;
        public const string ResolutionWidth = Resolution + _ + Width;
        public const string ResolutionHeight = Resolution + _ + Height;
        public const string RenderScale = Render + _ + Scale;

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

        public const string ThemeIndex = Theme + _ + Index;
        public const string ThemeId = Theme + _ + Id;
        public const string EffectId = Effect + _ + Id;

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
        public const string PrefabId = Prefab + _ + Id;
        public const string AudioId = AudioShort + Id;
        public const string ObjectId = Id;
        public const string ObjectIdCounter = Id + _ + Counter;
        public const string AudioIdCounter = AudioShort + Id + _ + Counter;
        public const string PrevObjectId = Prev + _ + Id;
        public const string NextObjectId = Next + _ + Id;

        public const string ObjectIds = Ids;

        // Two shape references on one object, both ShapeId-typed: what is drawn and what is hit.
        // They must stay distinct keys even though the values are interchangeable.
        public const string ShapeId = ShapeShort + Id;
        public const string ColliderId = ColliderShort + Id;
        public const string ShapeName = Shape + _ + Name;
        public const string ParentObjectId = ParentShort + ObjectId;

        public const string LocalFrame = Local + _ + Frame;
        public const string LocalFrameShort = LocalShort + FrameShort;
        public const string StopLocalFrame = Stop + _ + LocalFrame;
        public const string HasStopLocalFrame = Has + _ + StopLocalFrame;
        public const string OffsetTime = Offset + _ + Time;
        public const string AudioLayer = Audio + _ + Layer;

        public const string EffShape = Eff + _ + Shape;
        public const string EffAngle = Eff + _ + Angle;
        public const string EffScale = Eff + _ + Scale;
        public const string EffColor = Eff + _ + Color;

        public const string PrefabIndex = Prefab + _ + Index;
        public const string FontSize = Font + Size;
        public const string WordWrap = Word + Wrap;
        public const string HorizontalAlignment = HorizontalShort + _ + Alignment;
        public const string VerticalAlignment = VerticalShort + _ + Alignment;

        public const string Fillment = "fillment";
        public const string Appearing = "appearing";
        public const string FillDirection = Fill + _ + Direction;
        public const string AppearingMode = Appearing + _ + ModeShort;
        public const string AppearingMask = Appearing + _ + Mask;
        public const string OverEdge = Over + _ + Edge;
        public const string UnderEdge = Under + _ + Edge;

        public const string ResourceType = Resource + _ + Type;
        public const string ResourceId = Resource + _ + Id;
        public const string ResourcesMeta = Resources + _ + Meta;
        public const string TextureResourceId = Texture + _ + Res + _ + Id;
        public const string TextureResourceUV = Texture + _ + Res + _ + UV;
        public const string FontResourceId = Font + _ + Res + _ + Id;
        public const string FontCharacters = Font + _ + Chars;
        public const string AudioResourceId = Audio + _ + Res + _ + Id;
        public const string ByteResourceId = Byte + _ + Res + _ + Id;
        public const string TextResourceId = Text + _ + Res + _ + Id;
        public const string UriType = Uri + _ + Type;
        public const string SublingIndex = Subling + _ + Index;

        // Values

        public const string AngleA = Angle + _ + ValueA;
        public const string AngleB = Angle + _ + ValueB;
        public const string ColorA = Color + _ + ValueA;
        public const string ColorB = Color + _ + ValueB;
        public const string CurveX = Curve + _ + CoordX;
        public const string CurveY = Curve + _ + CoordY;
        public const string ScaleX = Scale + _ + CoordX;
        public const string ScaleY = Scale + _ + CoordY;

        public const string ColorBottom = Color + _ + AlignmentB;
        public const string ColorTop = Color + _ + AlignmentT;
        public const string ColorLeft = Color + _ + AlignmentL;
        public const string ColorRight = Color + _ + AlignmentR;
        public const string ColorBL = Color + _ + AlignmentBL;
        public const string ColorBM = Color + _ + AlignmentBM;
        public const string ColorBR = Color + _ + AlignmentBR;
        public const string ColorCL = Color + _ + AlignmentCL;
        public const string ColorCM = Color + _ + AlignmentCM;
        public const string ColorCR = Color + _ + AlignmentCR;
        public const string ColorTL = Color + _ + AlignmentTL;
        public const string ColorTM = Color + _ + AlignmentTM;
        public const string ColorTR = Color + _ + AlignmentTR;

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
        public const string InTangent = In + _ + TangentShort;
        public const string OutTangent = Out + _ + TangentShort;
        public const string InWeight = In + _ + WeightShort;
        public const string OutWeight = Out + _ + WeightShort;

        public const string PreWrapMode = Pre + _ + Wrap + _ + Mode;
        public const string PostWrapMode = Post + _ + Wrap + _ + Mode;

        public const string ColorKeys = Color + _ + Keys;
        public const string AlphaKeys = Alpha + _ + Keys;
        public const string ColorSpace = Color + _ + Space;

        public const string MinAspect = Min + _ + Aspect;
        public const string MaxAspect = Max + _ + Aspect;

        public const string AnchorMin = Anchor + _ + Min;
        public const string AnchorMax = Anchor + _ + Max;

        public const string LanguageStrings = Language + _ + Strings;


        // Effects

        public const string ParticleCount = Particle + _ + Count;
        public const string ParticleCollider = Particle + _ + Collider;
        public const string ParticlePivot = Particle + _ + Pivot;
        public const string SpeedRange = Speed + _ + Range;

        public const string GravityMin = Gravity + _ + Min;
        public const string GravityMax = Gravity + _ + Max;
        public const string VelocityMin = Velocity + _ + Min;
        public const string VelocityMax = Velocity + _ + Max;
        public const string AngularVelocityMin = Angular + _ + VelocityMin;
        public const string AngularVelocityMax = Angular + _ + VelocityMax;
        public const string LinearVelocity = Linear + _ + Velocity;
        public const string OrbitalVelocity = Orbital + _ + Velocity;
        public const string OrbitalCenterOffset = Orbital + _ + Center + _ + Offset;
        public const string VelocitySpeed = Velocity + _ + Speed;
        public const string VelocityPoint = Velocity + _ + Point;
        public const string LinearForce = Linear + _ + Force;

        public const string RadiusMajor = Radius + _ + Major;
        public const string RadiusMinor = Radius + _ + Minor;
        public const string TopRadius = Top + _ + Radius;
        public const string BaseRadius = Base + _ + Radius;

        // Audio

        public const string StereoPan = Stereo + _ + Pan;
        public const string Lowpass = Low + Pass;
        public const string Highpass = High + Pass;
        public const string PitchShifter = Pitch + Shifter;

        public const string MixLevel = Mix + _ + Level;
        public const string DryLevel = Dry + _ + Level;
        public const string DryMix = Dry + Mix;
        public const string WetMix = Wet + Mix;
        public const string CutoffFreq = Cutoff + _ + Freq;
        public const string ReverbDelay = Reverb + _ + Delay;
        public const string RoomHF = Room + HF;
        public const string RoomLF = Room + LF;
        public const string HFRef = HF + Ref;
        public const string LFRef = LF + Ref;
        public const string MaxChannels = Max + _ + Channels;
        public const string FFTSize = FFT + Size;
        public const string DecayTime = Decay + _ + Time;
        public const string DecayHFRatio = Decay + _ + HF + Ratio;
        public const string ReflectDelay = Reflect + _ + Delay;
        public const string CenterFreq = Center + _ + Freq;
        public const string OctaveRange = Octave + _ + Range;
        public const string FreqGain = Freq + _ + Gain;
        public const string WetMixTap1 = WetMix + _ + Tap1;
        public const string WetMixTap2 = WetMix + _ + Tap2;
        public const string WetMixTap3 = WetMix + _ + Tap3;
        public const string MakeUpGain = Make + Up + _ + Gain;
        public const string FadeInTime = Fade + _ + In + _ + Time;
        public const string LowestVolume = Lowest + _ + Volume;
        public const string MaximumAmp = Max + _ + Amp;

        // Post Processing

        public const string ScanLineJitter = Scan + _ + Line + _ + Jitter;
        public const string VerticalJump = Vertical + _ + Jump;
        public const string HorizontalShake = Horizontal + _ + Shake;
        public const string ColorDrift = Color + _ + Drift;
        public const string HueVsHue = Hue + _ + Vs + _ + Hue;
        public const string SatVsSat = Sat + _ + Vs + _ + Sat;
        public const string LiftColor = Lift + _ + Color;
        public const string GammaColor = Gamma + _ + Color;
        public const string GainColor = Gain + _ + Color;
        public const string ShadowColor = Shadow + _ + Color;
        public const string MidtoneColor = Midtone + _ + Color;
        public const string HighlightColor = Highlight + _ + Color;
        public const string ShadowLimit = Shadow + _ + Limit;
        public const string HighlightLimit = Highlight + _ + Limit;

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
        public const string CheckpointRestarts = Checkpoints + _ + Restarts;

        public const string BestFrame = Best + _ + Frame;
        public const string BestProgress = Best + _ + Progress;
        public const string LivesLeft = Lives + _ + Left;
        public const string SpeedCenti = Speed + _ + Centi;

        public const string DeathsByBucket = Deaths + _ + Bucket;
        public const string HitsByBucket = Hits + _ + Bucket;
        public const string DeathsByCheckpoint = Deaths + _ + Checkpoints;
        public const string DeathsBeforeCheckpoint = Deaths + _ + Before + _ + Checkpoints;
        public const string BucketFrameDuration = Bucket + _ + Frame + _ + Duration;

        public const string DistinctLevelsPlayed = Distinct + _ + Played;
        public const string DistinctLevelsCleared = Distinct + _ + Cleared;
        public const string FramesSimulated = Frames + _ + Simulated;

        public const string CurrentClearStreak = Clears + _ + Streaks;
        public const string LongestClearStreak = Max + _ + Clears + _ + Streaks;
        public const string MostPlayedLevelId = Max + _ + Played + _ + Level;
        public const string MostPlayedAttempts = Max + _ + Played + _ + Attempts;
        public const string LastPlayedLevelId = Last + _ + Played + _ + Level;

        public const string TotalDashes = Total + _ + Dashes;
        public const string TotalDistanceMoved = Distance + _ + Moved;

        public const string LevelsCreated = Level + _ + Created;
        public const string LevelsDeleted = Level + _ + Deleted;
        public const string ObjectsCreated = Objects + _ + Created;
        public const string GeneratorsRun = Generators + _ + Ran;
        public const string TotalResources = Total + _ + Resources;

        public const string KeyboardMouseSeconds = KeyboardMouse + _ + Seconds;
        public const string TouchscreenSeconds = Touchscreen + _ + Seconds;
        public const string GamepadSeconds = Gamepad + _ + Seconds;
        public const string DeviceGyroSeconds = Gyro + _ + Seconds;
    }
}