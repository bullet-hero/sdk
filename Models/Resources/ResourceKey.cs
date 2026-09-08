using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// One place a Resource can be fetched from. A resource lists several of these as fallbacks, so
    /// a level whose download link died still loads from the file next to it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ResourceKey : IModel<ResourceKey>
    {
        // URI - Universal Resource Identifier, either for paths, urls or keys

        /// <summary> How Uri should be interpreted (file path / URL / addressable key) - it cannot
        /// be guessed reliably from the string itself. </summary>
        [RuleEnumValid(ResourceUriType.Undefined)]
        [JsonProperty(Names.UriType)]
        public ResourceUriType UriType { get; set; }

        /// <summary> The location itself, in whatever form UriType says. </summary>
        [RuleNotNull, RuleStringMax(ResourceRules.MaxUriLength)]
        [JsonProperty(Names.Uri)]
        public string Uri { get; set; }
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ResourceKey()
        {
            UriType = ResourceUriType.Undefined;
            Uri = string.Empty;
        }
        /// <summary> Built from its type and uri. </summary>
        public ResourceKey(ResourceUriType uriType, string uri)
        {
            UriType = uriType;
            Uri = uri;
        }
    }
}