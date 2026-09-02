using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Opaque color track entry, used for the level background. No alpha by construction - there is
    /// nothing behind the background to blend with.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color3Key : Keyframe, IModel<Color3Key>
    {
        /// <summary> Color at this frame; may be a ThemeRef, which is how backgrounds follow the
        /// active theme. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.Color)]
        public IColor3 Value { get; set; }

        public Color3Key()
        {
            Value = Color3Value.white;
        }
        public Color3Key(IColor3 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}