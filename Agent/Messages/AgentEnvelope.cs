using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent.Messages
{
    /// <summary> A JSON-RPC 2.0 request frame. </summary>
    public sealed class AgentRequest
    {
        /// <summary> Always <see cref="AgentProtocol.JsonRpcVersion"/>. </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = AgentProtocol.JsonRpcVersion;

        /// <summary> The caller's correlation id, a number or a string. </summary>
        [JsonProperty("id")]
        public JToken Id { get; set; }

        /// <summary> One of <see cref="AgentProtocol.Methods"/>. </summary>
        [JsonProperty("method")]
        public string Method { get; set; }

        /// <summary> The method's request object; absent for a method without params. </summary>
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Params { get; set; }
    }

    /// <summary> A JSON-RPC 2.0 response frame: exactly one of <see cref="Result"/> and <see cref="Error"/>.
    /// </summary>
    public sealed class AgentResponse
    {
        /// <summary> Always <see cref="AgentProtocol.JsonRpcVersion"/>. </summary>
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = AgentProtocol.JsonRpcVersion;

        /// <summary> The id of the request this answers. </summary>
        [JsonProperty("id")]
        public JToken Id { get; set; }

        /// <summary> The method's result object. </summary>
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Result { get; set; }

        /// <summary> The failure, when there is one. </summary>
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public AgentRpcError Error { get; set; }
    }

    /// <summary> The <c>error</c> member of a failed response. </summary>
    public sealed class AgentRpcError
    {
        /// <summary> One of <see cref="AgentProtocol.JsonRpcCode"/>. </summary>
        [JsonProperty("code")]
        public int Code { get; set; }

        /// <summary> A sentence for the AI, naming the field or the reason. </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary> The readable code and whatever detail it carries. </summary>
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public AgentErrorData Data { get; set; }
    }

    /// <summary> What an error says beyond its message; only the members its code uses are set. </summary>
    public sealed class AgentErrorData
    {
        /// <summary> One of <see cref="AgentErrorCode"/>. </summary>
        [JsonProperty("code")]
        public string Code { get; set; }

        /// <summary> The current revision, on <c>stale</c>. </summary>
        [JsonProperty("revision", NullValueHandling = NullValueHandling.Ignore)]
        public int? Revision { get; set; }

        /// <summary> The index of the operation that failed, on <c>refused</c>. </summary>
        [JsonProperty("index", NullValueHandling = NullValueHandling.Ignore)]
        public int? Index { get; set; }

        /// <summary> Why the operation failed, on <c>refused</c>. </summary>
        [JsonProperty("reason", NullValueHandling = NullValueHandling.Ignore)]
        public string Reason { get; set; }

        /// <summary> The protocol number of the answering end, on <c>protocol_mismatch</c>. </summary>
        [JsonProperty("serverProtocol", NullValueHandling = NullValueHandling.Ignore)]
        public int? ServerProtocol { get; set; }

        /// <summary> The protocol number the caller stated, on <c>protocol_mismatch</c>. </summary>
        [JsonProperty("clientProtocol", NullValueHandling = NullValueHandling.Ignore)]
        public int? ClientProtocol { get; set; }
    }
}
