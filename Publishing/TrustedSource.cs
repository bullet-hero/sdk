using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Publishing
{
    // One entry of the site roster the licensing policy already lists in prose. Making it data is
    // what turns that document into moderation: the client can grade a resource the moment its URL
    // is typed, and a server queue can be sorted by which entries actually need a human.
    //
    // Matching is by HOST, not by the URL string. A site is its domain - the path, the query and the
    // scheme all vary per resource, and "starts with https://freesound.org/" fails the moment
    // someone pastes a www- or a country-prefixed link. Subdomains match their parent domain, which
    // is what makes one entry cover a whole site.

    /// <summary> One known resource site and how far it can be taken at its word. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TrustedSource : IModel<TrustedSource>
    {
        /// <summary> Stable identifier of the entry, for referring to it from outside the file. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Key)]
        public string Key { get; set; }

        /// <summary> Display name of the site. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Title)]
        public string Title { get; set; }

        /// <summary> The site's own page, for a moderator following the record back. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxUrl)]
        [JsonProperty(Names.Url)]
        public string Url { get; set; }

        /// <summary> Hosts this entry covers ("freesound.org"), subdomains included. </summary>
        [RuleNotNull, RuleCollectionMaxCount(MaxDomains), RuleCollectionNoNullItems]
        [JsonProperty(Names.Domains)]
        public List<string> Domains { get; set; }

        /// <summary> How far the site can be taken at its word. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Trust)]
        public SourceTrust Trust { get; set; }

        /// <summary> Licenses the site normally carries - advisory, for showing an author what to
        /// expect and for spotting a record that claims something the site never issues. </summary>
        [RuleNotNull, RuleCollectionMaxCount(MaxLicenses)]
        [JsonProperty(Names.Licenses)]
        public List<TypicalLicenseType> Licenses { get; set; }

        /// <summary> Why the entry is graded the way it is, in one line a moderator reads. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorDescription)]
        [JsonProperty(Names.Note)]
        public string Note { get; set; }

        public const int MaxDomains = 16;
        public const int MaxLicenses = 32;

        public TrustedSource()
        {
            Key = string.Empty;
            Title = string.Empty;
            Url = string.Empty;
            Domains = new List<string>();
            Trust = SourceTrust.Unknown;
            Licenses = new List<TypicalLicenseType>();
            Note = string.Empty;
        }
        public TrustedSource(string key, string title, string url, List<string> domains,
            SourceTrust trust, List<TypicalLicenseType> licenses, string note)
        {
            Key = key;
            Title = title;
            Url = url;
            Domains = domains;
            Trust = trust;
            Licenses = licenses;
            Note = note;
        }

        /// <summary> True when the given host is this site or a subdomain of it. </summary>
        public bool CoversHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return false;

            foreach (var domain in Domains)
            {
                if (string.IsNullOrWhiteSpace(domain)) continue;
                if (host.Equals(domain, StringComparison.OrdinalIgnoreCase)) return true;
                if (host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        // Uri.TryCreate rather than string surgery, and a bare-host fallback for what authors
        // actually type: "freesound.org/people/x/sounds/1" is not an absolute URI and would
        // otherwise grade as an unknown source purely for missing a scheme.

        /// <summary> Host part of a URL an author typed, lowercased and without "www.". Empty when
        /// the string holds nothing host-shaped. </summary>
        public static string ExtractHost(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return string.Empty;

            var trimmed = url.Trim();
            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out uri))
                    return string.Empty;
            }

            var host = uri.Host.ToLowerInvariant();
            return host.StartsWith("www.", StringComparison.Ordinal) ? host.Substring(4) : host;
        }
    }
}
