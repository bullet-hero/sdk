using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A normalized 0..1 point inside a rect - what anchors and pivots are made of. A thin wrapper
    /// over IVector2 that exists to carry the clamp and the named presets below (corners, center,
    /// and the centroid offsets that make regular polygons rotate around their real center).
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Alignment : IModel<Alignment>
    {
        /// <summary> The point itself: (0,0) = left-bottom, (1,1) = right-top. Still polymorphic, so
        /// an alignment can legitimately be randomized like any other vector. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinAlignment, ValueRules.MaxAlignment)]
        [JsonProperty(Names.ValueShort)]
        public IVector2 Value { get; set; }

        /// <summary> The bottom of the left, as a bare vector. </summary>
        public static Vector2Value LeftBottomValue =>   new(0.0f, 0.0f);
        /// <summary> The vertical centre of the left, as a bare vector. </summary>
        public static Vector2Value LeftMiddleValue =>   new(0.0f, 0.5f);
        /// <summary> The top of the left, as a bare vector. </summary>
        public static Vector2Value LeftTopValue =>      new(0.0f, 1.0f);
        /// <summary> The bottom of the horizontal centre, as a bare vector. </summary>
        public static Vector2Value CenterBottomValue => new(0.5f, 0.0f);
        /// <summary> The vertical centre of the horizontal centre, as a bare vector. </summary>
        public static Vector2Value CenterMiddleValue => new(0.5f, 0.5f);
        /// <summary> The top of the horizontal centre, as a bare vector. </summary>
        public static Vector2Value CenterTopValue =>    new(0.5f, 1.0f);
        /// <summary> The bottom of the right, as a bare vector. </summary>
        public static Vector2Value RightBottomValue =>  new(1.0f, 0.0f);
        /// <summary> The vertical centre of the right, as a bare vector. </summary>
        public static Vector2Value RightMiddleValue =>  new(1.0f, 0.5f);
        /// <summary> The top of the right, as a bare vector. </summary>
        public static Vector2Value RightTopValue =>     new(1.0f, 1.0f);
        
        /// <summary> The centre of the box, as a bare vector. </summary>
        public static Vector2Value DefaultValue      => CenterMiddleValue;
        /// <summary> The centre of mass of a regular 3-gon - what it rotates about naturally, as opposed to the centre of its box, as a bare vector. </summary>
        public static Vector2Value Equilateral3Value => new(0.5f, 0.355570f); // 1f / cos(-30°) / 2f = 0.577350269189625764
        /// <summary> The centre of mass of a regular 5-gon - what it rotates about naturally, as opposed to the centre of its box, as a bare vector. </summary>
        public static Vector2Value Equilateral5Value => new(0.5f, 0.449739f); // 1f / cos(18°) / 2f = 0.525731112119133606
        /// <summary> The centre of mass of a regular 7-gon - what it rotates about naturally, as opposed to the centre of its box, as a bare vector. </summary>
        public static Vector2Value Equilateral7Value => new(0.5f, 0.474765f); // 1f / cos((90-360/7*2)°) / 2f = 0.512858431636276949
        /// <summary> The centre of mass of a regular 9-gon - what it rotates about naturally, as opposed to the centre of its box, as a bare vector. </summary>
        public static Vector2Value Equilateral9Value => new(0.5f, 0.484907f); // 1f / cos((90-360/9*2)°) / 2f = 0.507713305942872492
        
        /// <summary> The bottom of the left. </summary>
        public static Alignment LeftBottom =>   new(LeftBottomValue);
        /// <summary> The vertical centre of the left. </summary>
        public static Alignment LeftMiddle =>   new(LeftMiddleValue);
        /// <summary> The top of the left. </summary>
        public static Alignment LeftTop =>      new(LeftTopValue);
        /// <summary> The bottom of the horizontal centre. </summary>
        public static Alignment CenterBottom => new(CenterBottomValue);
        /// <summary> The vertical centre of the horizontal centre. </summary>
        public static Alignment CenterMiddle => new(CenterMiddleValue);
        /// <summary> The top of the horizontal centre. </summary>
        public static Alignment CenterTop =>    new(CenterTopValue);
        /// <summary> The bottom of the right. </summary>
        public static Alignment RightBottom =>  new(RightBottomValue);
        /// <summary> The vertical centre of the right. </summary>
        public static Alignment RightMiddle =>  new(RightMiddleValue);
        /// <summary> The top of the right. </summary>
        public static Alignment RightTop =>     new(RightTopValue);
        
        /// <summary> The centre of the box, which is what an unauthored pivot is. </summary>
        public static Alignment Default =>      new(CenterMiddleValue);
        /// <summary> The centre of mass of a regular 3-gon - what it rotates about naturally, as opposed to the centre of its box. </summary>
        public static Alignment Equilateral3 => new(Equilateral3Value);
        /// <summary> The centre of mass of a regular 5-gon - what it rotates about naturally, as opposed to the centre of its box. </summary>
        public static Alignment Equilateral5 => new(Equilateral5Value);
        /// <summary> The centre of mass of a regular 7-gon - what it rotates about naturally, as opposed to the centre of its box. </summary>
        public static Alignment Equilateral7 => new(Equilateral7Value);
        /// <summary> The centre of mass of a regular 9-gon - what it rotates about naturally, as opposed to the centre of its box. </summary>
        public static Alignment Equilateral9 => new(Equilateral9Value);
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Alignment()
        {
            Value = DefaultValue;
        }
        /// <summary> Built from its value. </summary>
        public Alignment(IVector2 value)
        {
            Value = value;
        }
    }
}