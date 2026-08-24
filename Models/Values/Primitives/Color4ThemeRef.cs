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
    /// RGBA counterpart of Color3ThemeRef - alpha comes from the referenced palette slot too, so a
    /// theme can make a whole class of objects translucent without touching them.
    /// </summary>
    [RuleContainer]
    public class Color4ThemeRef : IColor4, IModel<Color4ThemeRef>
    {
        /// <summary> Slot inside the active ThemeData.Matrix (0-63), resolved per frame. </summary>
        [RuleInRange(ValueRules.MinThemeIndex, ValueRules.MaxThemeIndex)]
        [JsonProperty(Names.ThemeIndex)]
        public int ThemeColorIndex { get; set; }
        
        public ColorType GetModelType() => ColorType.ThemeRef;

        public Color4ThemeRef()
        {
            ThemeColorIndex = ValueRules.MinThemeIndex;
        }
        public Color4ThemeRef(int themeColorIndex)
        {
            ThemeColorIndex = themeColorIndex;
        }
        public void Reset()
        {
            ThemeColorIndex = ValueRules.MinThemeIndex;
        }
        
        public object Clone() => Copy();
        IColor4 ICopyable<IColor4>.Copy() => new Color4ThemeRef(ThemeColorIndex);
        public Color4ThemeRef Copy() => new(ThemeColorIndex);

        public void Update(Color4ThemeRef src)
        {
            ThemeColorIndex = src.ThemeColorIndex;
        }

        public void Pull(Color4ThemeRef src)
        {
            ThemeColorIndex = src.ThemeColorIndex;
        }

        void IUpdatable<IColor4>.Update(IColor4 src)
        {
            if (src is Color4ThemeRef value) Update(value);
        }
        void IMoveable<IColor4>.Pull(IColor4 src)
        {
            if (src is Color4ThemeRef value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Color4ThemeRef value && Equals(value);
        public override int GetHashCode() => ThemeColorIndex;
        
        public bool Equals(IColor4 other) => other is Color4ThemeRef value && Equals(value);
        public bool Equals(Color4ThemeRef other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = ThemeColorIndex.Equals(other.ThemeColorIndex);
            return result;
        }
    }
}