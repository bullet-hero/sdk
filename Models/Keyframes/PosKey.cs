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
    /// Position key of a RectObject or the camera. Coordinates are local to the parent, so moving a
    /// parent carries its children along without touching their own keys.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class PosKey : Keyframe, IModel<PosKey>
    {
        /// <summary> Target position at this frame. Polymorphic, so a position can be re-rolled per
        /// frame (random spawn) instead of being fixed. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinPos, ValueRules.MaxPos)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Pos { get; set; }

        public PosKey()
        {
            Pos = new Vector2Value();
        }
        public PosKey(IVector2 vector2, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Pos = vector2;
        }
    }
}