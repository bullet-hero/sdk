using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> One file of a level's <c>ai/</c> folder, as <c>context.list</c> prints it. </summary>
    public sealed class ContextFileEntry
    {
        /// <summary> The file's name inside <c>ai/</c>. </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary> Its size on disk. </summary>
        [JsonProperty("bytes")]
        public long Bytes { get; set; }
    }

    /// <summary> The <c>context.list</c> answer: what the open level's <c>ai/</c> folder holds. </summary>
    public sealed class ContextListResult : AgentResultBase
    {
        /// <summary> Whether the folder exists; it appears with the first <c>context.write</c>. </summary>
        [JsonProperty("exists")]
        public bool Exists { get; set; }

        /// <summary> Whether the folder travels with the level's archives; null when it has no readable
        /// manifest, which keeps it at home. The author decides it in the level's metadata. </summary>
        [JsonProperty("share")]
        public bool? Share { get; set; }

        /// <summary> Whether <c>context.write</c> may write to this level at all. </summary>
        [JsonProperty("writable")]
        public bool Writable { get; set; }

        /// <summary> The context files, manifest excluded, in name order. </summary>
        [JsonProperty("files")]
        public List<ContextFileEntry> Files { get; set; } = new List<ContextFileEntry>();

        /// <summary> The whole folder's size, manifest included. </summary>
        [JsonProperty("totalBytes")]
        public long TotalBytes { get; set; }

        /// <summary> The most one file may hold (<see cref="AgentRules.MaxContextFileBytes"/>). </summary>
        [JsonProperty("maxFileBytes")]
        public int MaxFileBytes { get; set; }

        /// <summary> The most the folder holds (<see cref="AgentRules.MaxContextFolderBytes"/>). </summary>
        [JsonProperty("maxFolderBytes")]
        public int MaxFolderBytes { get; set; }
    }

    /// <summary> The <c>context.read</c> request. </summary>
    public sealed class ContextReadRequest
    {
        /// <summary> A file name inside <c>ai/</c>; absent means <c>CONTEXT.md</c>. </summary>
        [JsonProperty("file", NullValueHandling = NullValueHandling.Ignore)]
        public string File { get; set; }
    }

    /// <summary> The <c>context.read</c> answer; a missing file is no error, it reads as empty. </summary>
    public sealed class ContextReadResult : AgentResultBase
    {
        /// <summary> The file read. </summary>
        [JsonProperty("file")]
        public string File { get; set; }

        /// <summary> Whether the file exists. </summary>
        [JsonProperty("exists")]
        public bool Exists { get; set; }

        /// <summary> Its size on disk; zero when it does not exist. </summary>
        [JsonProperty("bytes")]
        public long Bytes { get; set; }

        /// <summary> Its text; empty when it does not exist. </summary>
        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary> A section to add to a markdown context file. </summary>
    public sealed class ContextAppendSection
    {
        /// <summary> The heading's text, with or without its leading <c>#</c>. </summary>
        [JsonProperty("heading")]
        public string Heading { get; set; }

        /// <summary> The text added at the end of that section. </summary>
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    /// <summary> The <c>context.write</c> request: exactly one of <see cref="Content"/> and
    /// <see cref="AppendSection"/>. </summary>
    public sealed class ContextWriteRequest
    {
        /// <summary> A file name inside <c>ai/</c>; absent means <c>CONTEXT.md</c>. </summary>
        [JsonProperty("file", NullValueHandling = NullValueHandling.Ignore)]
        public string File { get; set; }

        /// <summary> The whole new text of the file. </summary>
        [JsonProperty("content", NullValueHandling = NullValueHandling.Ignore)]
        public string Content { get; set; }

        /// <summary> Text added under a heading, the section created at the end when absent. </summary>
        [JsonProperty("appendSection", NullValueHandling = NullValueHandling.Ignore)]
        public ContextAppendSection AppendSection { get; set; }
    }

    /// <summary> The <c>context.write</c> answer. The write is a file, not a history step: Undo does not
    /// revert it. </summary>
    public sealed class ContextWriteResult : AgentResultBase
    {
        /// <summary> The file written. </summary>
        [JsonProperty("file")]
        public string File { get; set; }

        /// <summary> Its size on disk after the write. </summary>
        [JsonProperty("bytes")]
        public long Bytes { get; set; }

        /// <summary> Whether the file did not exist before. </summary>
        [JsonProperty("created")]
        public bool Created { get; set; }

        /// <summary> Whether this write created the <c>ai/</c> folder and its manifest. </summary>
        [JsonProperty("folderCreated")]
        public bool FolderCreated { get; set; }
    }

    /// <summary> The <c>guide.get</c> request. </summary>
    public sealed class GuideGetRequest
    {
        /// <summary> One of <see cref="AgentGuideManifest.Topics"/>; absent means <c>index</c>. </summary>
        [JsonProperty("topic", NullValueHandling = NullValueHandling.Ignore)]
        public string Topic { get; set; }
    }

    /// <summary> The <c>guide.get</c> answer: one topic as markdown. </summary>
    public sealed class GuideGetResult : AgentResultBase
    {
        /// <summary> The topic served. </summary>
        [JsonProperty("topic")]
        public string Topic { get; set; }

        /// <summary> The rendered page. </summary>
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
