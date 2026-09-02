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
    /// Generic 2D track entry, for vector parameters with no dedicated key type of their own -
    /// PosKey and ScaKey exist separately only because their rules and ranges differ.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector2Key : Keyframe, IModel<Vector2Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector2Value))]
        [JsonProperty(Names.Vector2)]
        public IVector2 Value { get; set; }

        public Vector2Key()
        {
            Value = new Vector2Value();
        }
        public Vector2Key(IVector2 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}