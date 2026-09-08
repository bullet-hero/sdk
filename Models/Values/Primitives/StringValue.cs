using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One piece of text, same in every language - the IString variant for content that shouldn't be
    /// translated (a song title, a URL) or simply isn't yet.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class StringValue : IString, IModel<StringValue>
    {
        /// <summary> The text itself. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.ValueShort)]
        public string Value { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public StringValue()
        {
            Value = string.Empty;
        }
        /// <summary> Built from its value. </summary>
        public StringValue(string value)
        {
            Value = value;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public StringType GetModelType() => StringType.Value;
    }
}