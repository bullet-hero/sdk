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
    /// Generic 4-component track entry, for parameters that travel as a quadruple (rects, shader-like
    /// params). The widest of the vector key types and the least specific in meaning.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector4Key : Keyframe, IModel<Vector4Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector4Value))]
        [JsonProperty(Names.Vector4)]
        public IVector4 Value { get; set; }

        public Vector4Key()
        {
            Value = new Vector4Value();
        }
        public Vector4Key(IVector4 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}