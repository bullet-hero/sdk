using System;
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
    public class ScreenLimitNone : IScreenLimit, IModel<ScreenLimitNone>
    {
        public ScreenLimitType GetModelType() => ScreenLimitType.None;
        public bool IsValid(float currentAspect) => true;
        public float GetValid(float currentAspect) => currentAspect;

        public void Reset() { }
        
        public object Clone() => Copy();
        IScreenLimit ICopyable<IScreenLimit>.Copy() => Copy();
        public ScreenLimitNone Copy() => new();

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public void Update(ScreenLimitNone src)
        {
        }

        public void Pull(ScreenLimitNone src)
        {
        }

        void IUpdatable<IScreenLimit>.Update(IScreenLimit src)
        {
            if (src is ScreenLimitNone value) Update(value);
        }
        void IMoveable<IScreenLimit>.Pull(IScreenLimit src)
        {
            if (src is ScreenLimitNone value) Pull(value);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => obj is ScreenLimitNone value && Equals(value);

        public bool Equals(IScreenLimit other) => other is ScreenLimitNone value && Equals(value);
        public bool Equals(ScreenLimitNone other) => other is not null;
    }
}