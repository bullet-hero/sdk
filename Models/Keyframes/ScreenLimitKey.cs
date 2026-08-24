using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Changes how the visible area is constrained partway through a level - e.g. widening the view
    /// for a cinematic section and pinning it back for the next pattern.
    /// </summary>
    [RuleContainer]
    public class ScreenLimitKey : Keyframe, IModel<ScreenLimitKey>
    {
        // Same default-fix target as GameEvents.ScreenLimit (the single-value predecessor this
        // keyframe track replaces) - mappers choose limitations for the screen themselves.

        /// <summary> Constraint in force from this frame on: None / Fixed / Bounds. </summary>
        [RuleNotNull(typeof(ScreenLimitFixed))]
        [JsonProperty(Names.ScreenLimit)]
        public IScreenLimit ScreenLimit { get; set; }

        public ScreenLimitKey()
        {
            ScreenLimit = new ScreenLimitNone();
        }
        public ScreenLimitKey(IScreenLimit screenLimit, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            ScreenLimit = screenLimit;
        }
        public override void Reset()
        {
            base.Reset();
            ScreenLimit = new ScreenLimitNone();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ScreenLimitKey ICopyable<ScreenLimitKey>.Copy() => CopyImpl();
        
        private ScreenLimitKey CopyImpl() => new(ScreenLimit.Copy(), Frame, Ease);

        public void Update(ScreenLimitKey src)
        {
            base.Update(src);

            ScreenLimit = src.ScreenLimit.Copy();
        }

        public void Pull(ScreenLimitKey src)
        {
            base.Pull(src);

            ScreenLimit = ScreenLimit.PullFrom(src.ScreenLimit);
        }

        public override bool Equals(object obj) => obj is ScreenLimitKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ScreenLimit);

        public bool Equals(ScreenLimitKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && ScreenLimit.Equals(other.ScreenLimit);
            return result;
        }
    }
}
