using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored 2D vector with whether it is plain or one of the random forms. </summary>
    public class Vector2Converter : JsonConverterCustomType<IVector2, VectorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override VectorType GetCustomType(IVector2 value) => value.GetModelType();

        /// <summary> The class each vector form is. </summary>
        public override Type GetType(VectorType customType)
        {
            return customType switch
            {
                VectorType.Value => typeof(Vector2Value),
                VectorType.RandomRect => typeof(Vector2Rect),
                VectorType.RandomRectStep => typeof(Vector2RectStep),
                VectorType.RandomCircle => typeof(Vector2Circle),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}