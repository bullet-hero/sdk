using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One (language, text) pair inside a StringLocalized. Not an IString itself - it is an entry of
    /// a localized value, never a value a field can hold on its own.
    /// </summary>
    [RuleContainer]
    public class StringLanguage : IModel<StringLanguage>
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
        public void Reset()
        {
            LanguageCode = ValueRules.DefaultLanguageCode;
            Value = string.Empty;
        }

        public object Clone() => Copy();
        public StringLanguage Copy() => new(LanguageCode, Value);

        public void Update(StringLanguage src)
        {
            LanguageCode = src.LanguageCode;
            Value = src.Value;
        }

        public void Pull(StringLanguage src)
        {
            LanguageCode = src.LanguageCode;
            Value = src.Value;
        }

        public bool Equals(StringLanguage other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = LanguageCode == other.LanguageCode
                         && Value.Equals(other.Value);
            return result;
        }

        public override bool Equals(object obj) => obj is StringLanguage value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(LanguageCode, Value);
    }
}