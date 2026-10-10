using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>hello</c> request: the first frame of every connection. </summary>
    public sealed class HelloRequest
    {
        /// <summary> The caller's <see cref="AgentProtocol.Version"/>. </summary>
        [JsonProperty("protocol")]
        public int Protocol { get; set; }

        /// <summary> The token from the endpoint file. </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary> A name for the log, such as the server's own. </summary>
        [JsonProperty("client", NullValueHandling = NullValueHandling.Ignore)]
        public string Client { get; set; }
    }

    /// <summary> The <c>hello</c> answer. </summary>
    public sealed class HelloResult
    {
        /// <summary> The game's <see cref="AgentProtocol.Version"/>. </summary>
        [JsonProperty("protocol")]
        public int Protocol { get; set; }

        /// <summary> The client version (<c>gv</c>). </summary>
        [JsonProperty("gv")]
        public string Gv { get; set; }

        /// <summary> The SDK version (<c>sv</c>). </summary>
        [JsonProperty("sv")]
        public string Sv { get; set; }

        /// <summary> The newest model generation this build writes (<c>mg</c>). </summary>
        [JsonProperty("mg")]
        public int Mg { get; set; }

        /// <summary> The current editor session, empty when no level is open. </summary>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }

    /// <summary> What the AI may do right now, stated so it does not have to try. </summary>
    public sealed class AgentRights
    {
        /// <summary> <c>edit.batch</c> and history would be accepted. </summary>
        [JsonProperty("write")]
        public bool Write { get; set; }

        /// <summary> A level can be opened or created. </summary>
        [JsonProperty("openLevels")]
        public bool OpenLevels { get; set; }

        /// <summary> The open level can be saved. </summary>
        [JsonProperty("save")]
        public bool Save { get; set; }

        /// <summary> Prefab Mode can be entered or left. </summary>
        [JsonProperty("prefabMode")]
        public bool PrefabMode { get; set; }
    }

    /// <summary> The <c>session.status</c> answer. The level members are empty when no level is open.
    /// </summary>
    public sealed class SessionStatusResult : AgentResultBase
    {
        /// <summary> Always true: the answer itself proves the connection. </summary>
        [JsonProperty("connected")]
        public bool Connected { get; set; } = true;

        /// <summary> The screen the game shows: <c>editor</c>, <c>menu</c> and so on. </summary>
        [JsonProperty("screen")]
        public string Screen { get; set; }

        /// <summary> The open level's id. </summary>
        [JsonProperty("levelId", NullValueHandling = NullValueHandling.Ignore)]
        public string LevelId { get; set; }

        /// <summary> The open level's name. </summary>
        [JsonProperty("levelName", NullValueHandling = NullValueHandling.Ignore)]
        public string LevelName { get; set; }

        /// <summary> The author is doing something that would refuse a write or a capture. </summary>
        [JsonProperty("authorBusy")]
        public bool AuthorBusy { get; set; }

        /// <summary> What the AI may do. </summary>
        [JsonProperty("rights")]
        public AgentRights Rights { get; set; } = new AgentRights();
    }
}
