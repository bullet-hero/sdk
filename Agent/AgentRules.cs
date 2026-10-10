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
    }
}
