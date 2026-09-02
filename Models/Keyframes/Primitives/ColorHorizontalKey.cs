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
    /// Left-to-right gradient across a texture object: two colors instead of four, with the top and
    /// bottom corners of each side sharing one value.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ColorHorizontalKey : Keyframe, IColor4X4Key, IModel<ColorHorizontalKey>
    {
        /// <summary> Color of both left corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorLeft)]
        public IColor4 Color4Left { get; set; }

        /// <summary> Color of both right corners. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.ColorRight)]
        public IColor4 Color4Right { get; set; }

        public ColorHorizontalKey()
        {
            Color4Left = Color4Value.white;
            Color4Right = Color4Value.white;
        }
        public ColorHorizontalKey(IColor4 color4, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Left = color4.Copy();
            Color4Right = color4.Copy();
        }
        public ColorHorizontalKey(IColor4 color4Left, IColor4 color4Right, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Color4Left = color4Left;
            Color4Right = color4Right;
        }
        
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Horizontal;
    }
}