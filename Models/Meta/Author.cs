using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    /// <summary>
    /// One credited person, used both for a level's own authors and for the authors of a single
    /// resource. No identity of its own - two records with the same name are the same person only
    /// by convention.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Author : IModel<Author>
    {
        /// <summary> Display name, localizable - a handle can be spelled differently per script. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public IString Name { get; set; }

        // A credit line, not a role enum: the vocabulary is open (mixing, mastering, playtesting,
        // "level design, second half") and any fixed list would be a lie the moment a level needs a
        // word not in it. Localizable for the same reason Name is - a handle and what it did are
        // read by the same person in the same language.

        /// <summary> What they are credited for ("music", "cover art") - what turns a list of names
        /// into credits. Empty is ordinary: the level says who, not always what. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Credit)]
        public IString Credit { get; set; }

        /// <summary> Where to find them (profile, portfolio) - what makes attribution actionable
        /// rather than just a name. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxUrl)]
        [JsonProperty(Names.Url)]
        public string Url { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Author()
        {
            Name = new StringValue();
            Credit = new StringValue();
            Url = string.Empty;
        }

        // Credit trails url despite reading between name and url, so the six call sites that predate
        // it still compile - a record with no credit is a legal record, not an incomplete one.

        /// <summary> Built from its name, url and null. </summary>
        public Author(IString name, string url, IString credit = null)
        {
            Name = name;
            Url = url;
            Credit = credit ?? new StringValue();
        }
    }
}