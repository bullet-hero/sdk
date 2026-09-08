using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored int with whether it is plain or one of the random forms. </summary>
    public class IntConverter : JsonConverterCustomType<IInt, IntType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override IntType GetCustomType(IInt value) => value.GetModelType();
        /// <summary> The class each int form is. </summary>
        public override Type GetType(IntType customType)
        {
            return customType switch
            {
                IntType.Value => typeof(IntValue),
                IntType.RandomMinMax => typeof(IntMinMax),
                IntType.RandomMinMaxStep => typeof(IntMinMaxStep),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}