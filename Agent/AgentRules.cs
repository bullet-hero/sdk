using BH.SDK.Models.Resources;

namespace BH.SDK.Agent
{
    /// <summary> Limits and timings of the bridge, shared by the game and the MCP server. </summary>
    public static class AgentRules
    {
        /// <summary> Items a list returns when the caller gives no <c>limit</c>. </summary>
        public const int DefaultPageSize = 100;

        /// <summary> The largest <c>limit</c> a list honours. </summary>
        public const int MaxPageSize = 500;

        /// <summary> Operations in one <c>edit.batch</c>. </summary>
        public const int MaxBatchOps = 512;

        /// <summary> The server does not repeat a hint it sent within this many calls. </summary>
        public const int HintRepeatAfterCalls = 50;

        /// <summary> One file of a level's <c>ai/</c> folder. </summary>
        public const int MaxContextFileBytes = 256 * 1024;

        /// <summary> A level's whole <c>ai/</c> folder. </summary>
        public const int MaxContextFolderBytes = 1024 * 1024;

        /// <summary> Entries the session's change log keeps; older ones are dropped. </summary>
        public const int ChangeLogCapacity = 256;

        /// <summary> How long the server waits for any one answer, in seconds. </summary>
        public const int CallTimeoutSeconds = 60;

        /// <summary> How long a capture may take, in seconds. </summary>
        public const int CaptureTimeoutSeconds = 20;

        /// <summary> Renders after a seek before a still is stable; the first one shows the old moment.
        /// </summary>
        public const int EffectSettleFrames = 2;

        /// <summary> The smallest side an agent image may be asked for, in pixels. </summary>
        public const int MinImageSide = 64;

        /// <summary> Frames one <c>frame.stats</c> call samples at most; a finer step is widened to fit.
        /// </summary>
        public const int MaxFrameStatsSamples = 240;

        /// <summary> Width of the grid hazard coverage is measured on, laid over the camera rect. </summary>
        public const int CoverageWidth = 128;

        /// <summary> Height of that grid. </summary>
        public const int CoverageHeight = 72;

        /// <summary> Object labels one schematic draws at most; the rest are counted in its legend. </summary>
        public const int MaxSchematicLabels = 256;

        /// <summary> The side of one shape's cell in <c>shapes.atlas</c>, in pixels. </summary>
        public const int AtlasCellSize = 64;

        /// <summary> Frames one <c>view.filmstrip</c> captures at most; the whole sheet still fits the image
        /// limit, so more frames means smaller cells. </summary>
        public const int MaxFilmstripFrames = 16;

        /// <summary> Frames a <c>view.filmstrip</c> over a range takes when the caller gives no count. </summary>
        public const int DefaultFilmstripFrames = 6;

        /// <summary> Frames a beat may sit from the grid before <c>music.check</c> reports it, when the
        /// caller gives no tolerance. </summary>
        public const int DefaultBeatTolerance = 1;

        /// <summary> The longest range <c>music.play</c> plays, in seconds; it stays under
        /// <see cref="CallTimeoutSeconds"/>, since the call answers when the range has played. </summary>
        public const int MaxPlaySeconds = 45;

        /// <summary> Height of one band row of <c>music.strip</c>, in pixels. </summary>
        public const int MusicStripBandHeight = 24;

        /// <summary> Height of each tick lane of <c>music.strip</c> (grid, markers, hazard starts), in
        /// pixels. </summary>
        public const int MusicStripLaneHeight = 10;

        /// <summary> The outline of a collider - something that hurts - on agent images. </summary>
        public static readonly Pixel HazardColor = new Pixel(255, 48, 48, 255);

        /// <summary> The outline of the camera rect on agent images. </summary>
        public static readonly Pixel CameraColor = new Pixel(255, 214, 0, 255);
    }
}