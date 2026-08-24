using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// One piece of text, same in every language - the IString variant for content that shouldn't be
    /// translated (a song title, a URL) or simply isn't yet.
    /// </summary>
    [RuleContainer]
    public class StringValue : IString, IModel<StringValue>
    {
        /// <summary> The text itself. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.ValueShort)]
        public string Value { get; set; }

        public StringValue()
        {
            Value = string.Empty;
        }
        public StringValue(string value)
        {
            Value = value;
        }
        public void Reset()
        {
            Value = string.Empty;
        }

        public StringType GetModelType() => StringType.Value;
        
        public object Clone() => Copy();
        IString ICopyable<IString>.Copy() => new StringValue(Value);
        public StringValue Copy() => new(Value);

        public void Update(StringValue src)
        {
            Value = src.Value;
        }

        public void Pull(StringValue src)
        {
            Value = src.Value;
        }

        void IUpdatable<IString>.Update(IString src)
        {
            if (src is StringValue value) Update(value);
        }
        void IMoveable<IString>.Pull(IString src)
        {
            if (src is StringValue value) Pull(value);
        }

        public override bool Equals(object obj) => obj is StringValue value && Equals(value);
        public override int GetHashCode() => Value != null ? Value.GetHashCode() : 0;
        
        public bool Equals(IString other) => other is StringValue value && Equals(value);
        public bool Equals(StringValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Value.Equals(other.Value);
            return result;
        }
    }
}