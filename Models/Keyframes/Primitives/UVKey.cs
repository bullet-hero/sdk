using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Animates how a texture is mapped onto a ShapeObject - scrolling and repeating the image
    /// without moving the object itself. Both fields are concrete Vector2Value, not IVector2: a
    /// randomized UV would tear the image differently every frame.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class UVKey : Keyframe, IModel<UVKey>
    {
        /// <summary> Repeat count per axis; values above 1 tile the texture. </summary>
        [RuleNotNull]
        [RuleIVector2InRange(ValueRules.MinUv, ValueRules.MaxUv)]
        [JsonProperty(Names.Tiling)]
        public Vector2Value Tiling { get; set; }

        /// <summary> Shift of the texture within the rect - animate it for a scrolling surface. </summary>
        [RuleNotNull]
        [RuleIVector2InRange(ValueRules.MinUv, ValueRules.MaxUv)]
        [JsonProperty(Names.Offset)]
        public Vector2Value Offset { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public UVKey()
        {
            Tiling = new Vector2Value(ValueRules.DefaultUvX, ValueRules.DefaultUvY);
            Offset = new Vector2Value(ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
        }
        /// <summary> Built from its tilling, offset, frame and default ease. </summary>
        public UVKey(Vector2Value tilling, Vector2Value offset, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Tiling = tilling;
            Offset = offset;
        }
    }
}