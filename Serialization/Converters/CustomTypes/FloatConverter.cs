using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored float with whether it is plain or one of the random forms. </summary>
    public class FloatConverter : JsonConverterCustomType<IFloat, FloatType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override FloatType GetCustomType(IFloat value) => value.GetModelType();
        /// <summary> The class each float form is. </summary>
        public override Type GetType(FloatType customType)
        {
            return customType switch
            {
                FloatType.Value => typeof(FloatValue),
                FloatType.RandomMinMax => typeof(FloatMinMax),
                FloatType.RandomMinMaxStep => typeof(FloatMinMaxStep),
                _ => Fallback(customType, typeof(FloatValue))
            };
        }
    }
}