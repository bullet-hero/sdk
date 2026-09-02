using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Generic 3D track entry - reached where a third axis matters (effect forces), not for placing
    /// objects in the 2D scene.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector3Key : Keyframe, IModel<Vector3Key>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(Vector3Value))]
        [JsonProperty(Names.Vector3)]
        public IVector3 Value { get; set; }

        public Vector3Key()
        {
            Value = new Vector3Value();
        }
        public Vector3Key(IVector3 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}