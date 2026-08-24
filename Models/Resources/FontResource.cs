using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// A typeface the level brings with it, so text renders the same on a device that has never
    /// seen that font. Referenced by TextObject.FontResourceId.
    /// </summary>
    [RuleContainer]
    public class FontResource : Resource, IModel<FontResource>
    {
        /// <summary> Identity of this font within the level. </summary>
        [RuleIPrimitiveIntMax(FontResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.FontResourceId)]
        public FontResourceId FontResourceId { get; set; }

        public override ResourceType Type => ResourceType.Font;

        public FontResource()
        {
            FontResourceId = FontResourceId.Null;
        }
        public FontResource(FontResourceId fontResourceId, List<ResourceKey> sources) : base(sources)
        {
            FontResourceId = fontResourceId;
        }
        public override void Reset()
        {
            base.Reset();
            FontResourceId = FontResourceId.Null;
        }
        
        public override object Clone() => CopyImpl();
        public override Resource Copy() => CopyImpl();
        FontResource ICopyable<FontResource>.Copy() => CopyImpl();
        
        private FontResource CopyImpl() => new(FontResourceId, Sources.CopyList());

        public void Update(FontResource src)
        {
            base.Update(src);

            FontResourceId = src.FontResourceId;
        }

        public void Pull(FontResource src)
        {
            base.Pull(src);

            FontResourceId = src.FontResourceId;
        }

        public override bool Equals(object obj) => obj is FontResource value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), FontResourceId);

        public bool Equals(FontResource other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && FontResourceId.Equals(other.FontResourceId);
            return result;
        }
    }
}