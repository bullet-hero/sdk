namespace BH.SDK.Agent
{
    /// <summary>
    /// The bridge's wire contract: one TCP connection to the running game, newline-delimited JSON-RPC 2.0.
    /// Every name the game and the MCP server must agree on is a constant here, never a literal.
    /// </summary>
    public static class AgentProtocol
    {
        /// <summary> The protocol number both ends state in <c>hello</c>; a different one is refused.
        /// </summary>
        public const int Version = 1;

        /// <summary> The JSON-RPC version every envelope carries. </summary>
        public const string JsonRpcVersion = "2.0";

        /// <summary> The only address the bridge binds and the server connects to. </summary>
        public const string Host = "127.0.0.1";

        /// <summary> The longest frame, in bytes, either side accepts. </summary>
        public const int MaxMessageBytes = 32 * 1024 * 1024;

        /// <summary> Ends every frame. </summary>
        public const char LineTerminator = '\n';

        /// <summary> Length of the session token, in hex digits (128 random bits). </summary>
        public const int TokenLength = 32;

        /// <summary> The MCP tool name of a bridge method: the same words with <c>_</c> for <c>.</c>.
        /// </summary>
        public static string ToolName(string method) => method.Replace('.', '_');

        /// <summary> The bridge method names, <c>&lt;area&gt;.&lt;verb&gt;</c>. </summary>
        public static class Methods
        {
            /// <summary> The handshake; the first request of every connection. </summary>
            public const string Hello = "hello";

            /// <summary> Connection, screen, level and author state. </summary>
            public const string SessionStatus = "session.status";

            /// <summary> The levels in the editor's list. </summary>
            public const string LevelList = "level.list";

            /// <summary> Opens a writable level. </summary>
            public const string LevelOpen = "level.open";

            /// <summary> Creates a new level. </summary>
            public const string LevelCreate = "level.create";

            /// <summary> Saves the open level. </summary>
            public const string LevelSave = "level.save";

            /// <summary> The hierarchy as indented lines. </summary>
            public const string LevelOutline = "level.outline";

            /// <summary> Objects by time, name, type or parent. </summary>
            public const string ObjectQuery = "object.query";

            /// <summary> One or a few objects in full. </summary>
            public const string ObjectGet = "object.get";

            /// <summary> The keyframes of chosen tracks. </summary>
            public const string KeysGet = "keys.get";

            /// <summary> Markers, beat segments, checkpoints and audio tracks. </summary>
            public const string EventsGet = "events.get";

            /// <summary> Textures, fonts, audios, prefabs, themes and effects. </summary>
            public const string ResourcesList = "resources.list";

            /// <summary> Shape ids by form or name. </summary>
            public const string ShapesFind = "shapes.find";

            /// <summary> The one write method. </summary>
            public const string EditBatch = "edit.batch";

            /// <summary> Opens a template for editing; later edits land inside it. </summary>
            public const string PrefabModeEnter = "prefab_mode.enter";

            /// <summary> Leaves one template, or all of them. </summary>
            public const string PrefabModeExit = "prefab_mode.exit";

            /// <summary> The per-placement overrides of a prefab placement or one of its copies. </summary>
            public const string PrefabOverrides = "prefab.overrides";

            /// <summary> The generators that write into the open level or template. </summary>
            public const string GeneratorList = "generator.list";

            /// <summary> One generator's parameters as a JSON schema. </summary>
            public const string GeneratorSchema = "generator.schema";

            /// <summary> Runs one generator as one undo step. </summary>
            public const string GeneratorRun = "generator.run";

            /// <summary> Copies a file from disk into the level as a texture, font or audio resource. </summary>
            public const string ResourceImport = "resource.import";

            /// <summary> A node of the Raw tree: its value and its children. </summary>
            public const string RawGet = "raw.get";

            /// <summary> Writes a node of the Raw tree; nothing is validated, the answer lists new findings.
            /// </summary>
            public const string RawSet = "raw.set";

            /// <summary> The editor commands the AI may run, and the ones it may not, with the reason. </summary>
            public const string CommandList = "command.list";

            /// <summary> Runs one whitelisted editor command. </summary>
            public const string CommandRun = "command.run";

            /// <summary> The validation findings of the open level and its metadata, paged. </summary>
            public const string Validate = "validate.list";

            /// <summary> Repairs what the rule engine can repair, as one undo step. </summary>
            public const string ValidateFix = "validate.fix";

            /// <summary> The operation history. </summary>
            public const string HistoryGet = "history.get";

            /// <summary> Undoes the last history step. </summary>
            public const string HistoryUndo = "history.undo";

            /// <summary> Redoes the last undone step. </summary>
            public const string HistoryRedo = "history.redo";

            /// <summary> What changed since a revision, and who did it. </summary>
            public const string ChangesSince = "changes.since";

            /// <summary> The author's selection. </summary>
            public const string SelectionGet = "selection.get";

            /// <summary> Replaces the author's selection. </summary>
            public const string SelectionSet = "selection.set";
        }

        /// <summary> The numeric <c>error.code</c> of a JSON-RPC error; the readable code is in its data.
        /// </summary>
        public static class JsonRpcCode
        {
            /// <summary> The frame is not a valid request. </summary>
            public const int InvalidRequest = -32600;

            /// <summary> No such method. </summary>
            public const int MethodNotFound = -32601;

            /// <summary> The params are wrong. </summary>
            public const int InvalidParams = -32602;

            /// <summary> The handler threw. </summary>
            public const int Internal = -32603;

            /// <summary> Every error with an <see cref="AgentErrorCode"/>. </summary>
            public const int Application = -32000;
        }
    }
}