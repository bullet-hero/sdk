using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// The same text in several languages, picked by the player's locale at display time. The
    /// translated-content IString variant, as opposed to the single-string StringValue.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class StringLocalized : IString, IModel<StringLocalized>
    {
        /// <summary> One entry per language, unique by language code. Order carries no meaning; the
        /// list is a lookup, not a priority chain. </summary>
        [RuleNotNull, RuleCollectionNoNullItems]
        [RuleCollectionUnique(nameof(StringLanguage.LanguageCode))]
        [JsonProperty(Names.Strings)]
        public List<StringLanguage> Strings { get; set; }

        public StringLocalized()
        {
            Strings = new List<StringLanguage>();
        }
        public StringLocalized(params StringLanguage[] strings)
        {
            Strings = strings.ToList();
        }
        public StringLocalized(List<StringLanguage> strings)
        {
            Strings = strings;
        }

        public StringType GetModelType() => StringType.Localized;
    }
}