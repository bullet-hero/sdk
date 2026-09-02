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
    /// Bottom-to-top gradient across a texture object - the vertical twin of ColorHorizontalKey.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ColorVerticalKey : Keyframe, IColor4X4Key, IModel<ColorVerticalKey>
    {
        /// <summary> Color of both bottom corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorBottom)]
        public IColor4 Color4Bottom { get; set; }

        /// <summary> Color of both top corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorTop)]
        public IColor4 Color4Top { get; set; }

        public ColorVerticalKey()
        {
            Color4Bottom = Color4Value.white;
            Color4Top = Color4Value.white;
        }
        public ColorVerticalKey(IColor4 color4, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Bottom = color4.Copy();
            Color4Top = color4.Copy();
        }
        public ColorVerticalKey(IColor4 color4Bottom, IColor4 color4Top, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Bottom = color4Bottom;
            Color4Top = color4Top;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Vertical;
    }
}