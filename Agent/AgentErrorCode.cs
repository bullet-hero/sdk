using System.Collections.Generic;

namespace BH.SDK.Agent
{
    /// <summary> The string codes an error carries in <c>error.data.code</c>. </summary>
    public static class AgentErrorCode
    {
        /// <summary> Server side: no endpoint file, or the game does not answer. </summary>
        public const string NotConnected = "not_connected";

        /// <summary> The two ends speak different protocol numbers. </summary>
        public const string ProtocolMismatch = "protocol_mismatch";

        /// <summary> The handshake token is wrong. </summary>
        public const string BadToken = "bad_token";

        /// <summary> The editor screen is not open. </summary>
        public const string NoEditor = "no_editor";

        /// <summary> The editor has no level loaded. </summary>
        public const string NoLevel = "no_level";

        /// <summary> The author is in Prefab Mode, which writes do not support yet. </summary>
        public const string PrefabMode = "prefab_mode";

        /// <summary> The author is doing something; retry later. </summary>
        public const string UserBusy = "user_busy";

        /// <summary> The author interrupted a capture or playback. </summary>
        public const string Preempted = "preempted";

        /// <summary> <c>expectedRevision</c> is not the current revision. </summary>
        public const string Stale = "stale";

        /// <summary> The editor is read-only or replaying history. </summary>
        public const string Locked = "locked";

        /// <summary> The busy gate is taken by a save or an export. </summary>
        public const string Busy = "busy";

        /// <summary> Outside what the AI may do. </summary>
        public const string Forbidden = "forbidden";

        /// <summary> The action would discard edits; save first. </summary>
        public const string UnsavedChanges = "unsaved_changes";

        /// <summary> The level needs a passphrase the AI cannot give. </summary>
        public const string Protected = "protected";

        /// <summary> The file was written by a newer build. </summary>
        public const string NewerGeneration = "newer_generation";

        /// <summary> A batch failed validation or an operation declined it. </summary>
        public const string Refused = "refused";

        /// <summary> The caller's params are wrong. </summary>
        public const string InvalidParams = "invalid_params";

        /// <summary> What the caller named does not exist. </summary>
        public const string NotFound = "not_found";

        /// <summary> A handler threw. </summary>
        public const string Internal = "internal";

        /// <summary> The author pressed Stop in the editor; no client is let in until they allow it again.
        /// </summary>
        public const string Stopped = "stopped";

        /// <summary> Every code, for a test that each one is unique. </summary>
        public static readonly IReadOnlyList<string> All = new[]
        {
            NotConnected, ProtocolMismatch, BadToken, NoEditor, NoLevel, PrefabMode, UserBusy, Preempted,
            Stale, Locked, Busy, Forbidden, UnsavedChanges, Protected, NewerGeneration, Refused,
            InvalidParams, NotFound, Internal, Stopped,
        };
    }
}