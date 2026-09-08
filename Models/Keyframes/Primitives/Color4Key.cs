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
    /// Flat color key and the simplest member of the four-corner family: one color painted on all
    /// four corners. Doubles as the plain RGBA key for text and any single-tint track.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color4Key : Keyframe, IColor4X4Key, IModel<Color4Key>
    {
        /// <summary> Color at this frame, applied uniformly across the rect. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.Color)]
        public IColor4 Value { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Color4Key()
        {
            Value = Color4Value.white;
        }
        /// <summary> Built from its value, frame and default ease. </summary>
        public Color4Key(IColor4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public Color4X4KeyType GetModelType() => Color4X4KeyType.Value;
    }
}