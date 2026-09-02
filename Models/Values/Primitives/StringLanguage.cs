using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One (language, text) pair inside a StringLocalized. Not an IString itself - it is an entry of
    /// a localized value, never a value a field can hold on its own.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class StringLanguage : IModel<StringLanguage>
    {
        /// <summary> Locale tag this translation answers to ("en", "ru"), matched against the
        /// player's language. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxLanguageCode)]
        [RuleStringPattern(ValueRules.LanguageCodePattern, ValueRules.DefaultLanguageCode)]
        [JsonProperty(Names.Language)]
        public string LanguageCode { get; set; }

        /// <summary> The translated text. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.ValueShort)]
        public string Value { get; set; }

        public StringLanguage()
        {
            LanguageCode = ValueRules.DefaultLanguageCode;
            Value = string.Empty;
        }
        public StringLanguage(string languageCode, string value)
        {
            LanguageCode = languageCode;
            Value = value;
        }
    }
}