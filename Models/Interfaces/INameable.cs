using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Interfaces
{
    /// <summary> Carries an author-facing name - what the editor's lists and hierarchies show. </summary>
    public interface INameable
    {
        /// <summary> The name the author typed. Never localized and never an identity - ids are what things are addressed by. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }
    }
}