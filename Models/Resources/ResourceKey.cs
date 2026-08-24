using System;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// One place a Resource can be fetched from. A resource lists several of these as fallbacks, so
    /// a level whose download link died still loads from the file next to it.
    /// </summary>
    [RuleContainer]
    public class ResourceKey : IModel<ResourceKey>
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
        
        public ResourceKey()
        {
            UriType = ResourceUriType.Undefined;
            Uri = string.Empty;
        }
        public ResourceKey(ResourceUriType uriType, string uri)
        {
            UriType = uriType;
            Uri = uri;
        }
        public void Reset()
        {
            UriType = ResourceUriType.Undefined;
            Uri = string.Empty;
        }

        public object Clone() => Copy();
        public ResourceKey Copy() => new(UriType, Uri);

        public void Update(ResourceKey src)
        {
            UriType = src.UriType;
            Uri = src.Uri;
        }

        public void Pull(ResourceKey src)
        {
            UriType = src.UriType;
            Uri = src.Uri;
        }

        public bool Equals(ResourceKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = UriType == other.UriType && Uri.Equals(other.Uri);
            return result;
        }

        public override bool Equals(object obj) => obj is ResourceKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine((int)UriType, Uri);
    }
}