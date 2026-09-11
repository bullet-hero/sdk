using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored 3D vector with whether it is plain or one of the random forms. </summary>
    public class Vector3Converter : JsonConverterCustomType<IVector3, VectorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override VectorType GetCustomType(IVector3 value) => value.GetModelType();
        /// <summary> The class each vector form is. </summary>
        public override Type GetType(VectorType customType)
        {
            return customType switch
            {
                VectorType.Value => typeof(Vector3Value),
                VectorType.RandomRect => typeof(Vector3Rect),
                VectorType.RandomRectStep => typeof(Vector3RectStep),
                VectorType.RandomCircle => typeof(Vector3Circle),
                _ => Fallback(customType, typeof(Vector3Value))
            };
        }
    }
}