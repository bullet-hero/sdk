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
    // TODO activate for player when add events
    
    /// <summary>
    /// A point that pushes or pulls the player at a given frame. Not wired into gameplay yet (see
    /// the TODO above) - the format reserves the shape, the player does not read it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class VelocityPoint : Keyframe, IModel<VelocityPoint>
    {
        /// <summary> Where the force radiates from. </summary>
        [RuleNotNull(typeof(Vector2Value))]
        [JsonProperty(Names.Center)]
        public IVector2 Center { get; set; }

        /// <summary> Strength of the push; negative pulls toward Center instead. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Force)]
        public float Force { get; set; }

        public VelocityPoint()
        {
            Center = new Vector2Value();
            Force = 1f;
        }
        public VelocityPoint(IVector2 center, float force, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Center = center;
            Force = force;
        }
    }
}