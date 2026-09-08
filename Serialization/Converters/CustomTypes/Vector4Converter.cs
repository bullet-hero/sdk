using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored 4D vector with whether it is plain or one of the random forms. </summary>
    public class Vector4Converter : JsonConverterCustomType<IVector4, VectorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override VectorType GetCustomType(IVector4 value) => value.GetModelType();
        /// <summary> The class each vector form is. </summary>
        public override Type GetType(VectorType customType)
        {
            return customType switch
            {
                VectorType.Value => typeof(Vector4Value),
                VectorType.RandomRect => typeof(Vector4Rect),
                VectorType.RandomRectStep => typeof(Vector4RectStep),
                VectorType.RandomCircle => typeof(Vector4Circle),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}