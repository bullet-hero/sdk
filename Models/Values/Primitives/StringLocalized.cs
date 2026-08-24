using System;
using System.Collections.Generic;
using System.Linq;
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
    public class StringLocalized : IString, IModel<StringLocalized>
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
        public void Reset()
        {
            Strings.Clear();
        }

        public StringType GetModelType() => StringType.Localized;

        public object Clone() => Copy();
        IString ICopyable<IString>.Copy() => Copy();
        public StringLocalized Copy() => new(Strings.CopyList());
        
        public void Update(StringLocalized src)
        {
            Strings = src.Strings.CopyList();
        }

        public void Pull(StringLocalized src)
        {
            Strings = src.Strings.CopyList();
        }

        void IUpdatable<IString>.Update(IString src)
        {
            if (src is StringLocalized value) Update(value);
        }
        void IMoveable<IString>.Pull(IString src)
        {
            if (src is StringLocalized value) Pull(value);
        }

        public override bool Equals(object obj) => obj is StringLocalized value && Equals(value);
        public override int GetHashCode() => Strings.GetListHashCode();
        
        public bool Equals(IString other) => other is StringLocalized value && Equals(value);
        public bool Equals(StringLocalized other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Strings.ListEquals(other.Strings);
            return result;
        }
    }
}