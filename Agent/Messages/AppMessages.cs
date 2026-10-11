using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Agent.Messages
{
    /// <summary> The <c>app.navigate</c> request. </summary>
    public sealed class AppNavigateRequest
    {
        /// <summary> One line of launch arguments, in the command line's own syntax. </summary>
        [JsonProperty("args")]
        public string Args { get; set; }
    }

    /// <summary> The <c>app.navigate</c> answer. </summary>
    public sealed class AppNavigateResult
    {
        /// <summary> The screen now up, as <c>session.status</c> names it. </summary>
        [JsonProperty("screen")]
        public string Screen { get; set; }

        /// <summary> What the parser dropped from the line, one line each; empty when nothing. </summary>
        [JsonProperty("warnings")]
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
