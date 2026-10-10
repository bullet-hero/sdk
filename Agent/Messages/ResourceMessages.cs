using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>resource.import</c> request: a file on disk becomes a level resource. </summary>
    public sealed class ResourceImportRequest
    {
        /// <summary> The revision of the read this write is based on. </summary>
        [JsonProperty("expectedRevision")]
        public int ExpectedRevision { get; set; }

        /// <summary> An absolute path to a file on this machine. </summary>
        [JsonProperty("path")]
        public string Path { get; set; }

        /// <summary> <c>texture</c> (png, jpg, jpeg), <c>font</c> (ttf, otf, ttc) or <c>audio</c> (wav, mp3, ogg).
        /// </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    /// <summary> The <c>resource.import</c> answer. </summary>
    public sealed class ResourceImportResult : AgentResultBase
    {
        /// <summary> The kind imported. </summary>
        [JsonProperty("kind")]
        public string Kind { get; set; }

        /// <summary> The new resource's id (a negative number), as <c>resources.list</c> prints it. </summary>
        [JsonProperty("id")]
        public int Id { get; set; }

        /// <summary> The file's name inside the level folder. </summary>
        [JsonProperty("file")]
        public string File { get; set; }

        /// <summary> What the import could not do for the caller; never a failure. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
