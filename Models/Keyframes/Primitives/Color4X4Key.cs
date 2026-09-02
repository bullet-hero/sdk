using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Fully independent corner colors - the widest member of the four-corner family, giving a
    /// barycentric blend across the quad. The other three variants exist so the common cases
    /// (flat, horizontal, vertical) don't have to store four colors that are mostly equal.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color4X4Key : Keyframe, IColor4X4Key, IModel<Color4X4Key>
    {
        /// <summary> Bottom-left corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBL)]
        public IColor4 Color4BL { get; set; }

        /// <summary> Bottom-right corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBR)]
        public IColor4 Color4BR { get; set; }

        /// <summary> Top-left corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTL)]
        public IColor4 Color4TL { get; set; }

        /// <summary> Top-right corner color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTR)]
        public IColor4 Color4TR { get; set; }
        
        public Color4X4Key()
        {
            Color4BL = Color4Value.white;
            Color4BR = Color4Value.white;
            Color4TL = Color4Value.white;
            Color4TR = Color4Value.white;
        }
        public Color4X4Key(IColor4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4BL = value.Copy();
            Color4BR = value.Copy();
            Color4TL = value.Copy();
            Color4TR = value.Copy();
        }
        public Color4X4Key(IColor4 color4BL, IColor4 color4BR, IColor4 color4TL, IColor4 color4TR,
            int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4BL = color4BL;
            Color4BR = color4BR;
            Color4TL = color4TL;
            Color4TR = color4TR;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.BariCentrical;
    }
}