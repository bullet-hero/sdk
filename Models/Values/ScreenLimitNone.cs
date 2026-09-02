using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// "Do not constrain the view" - the IScreenLimit variant that accepts any aspect ratio and
    /// passes the device's own through untouched. Fieldless: the behavior is the whole value.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScreenLimitNone : IScreenLimit, IModel<ScreenLimitNone>
    {
        public ScreenLimitType GetModelType() => ScreenLimitType.None;
        public bool IsValid(float currentAspect) => true;
        public float GetValid(float currentAspect) => currentAspect;

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    }
}