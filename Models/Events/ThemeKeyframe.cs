using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Events
{
    /// <summary>
    /// Switches which palette the level is using at a given frame - the outer half of the theme
    /// indirection every ColorType.ThemeRef depends on. A real animated track, unlike the one-shot
    /// Marker/Checkpoint it is stored alongside.
    /// </summary>
    [RuleContainer]
    public class ThemeKeyframe : Keyframe, IModel<ThemeKeyframe>
    {
        /// <summary> Palette that becomes active from this frame on. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.ThemeId)]
        public ThemeId ThemeId { get; set; }

        public ThemeKeyframe()
        {
            ThemeId = ThemeId.Null;
        }
        public ThemeKeyframe(ThemeId themeId, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            ThemeId = themeId;
        }
        public override void Reset()
        {
            base.Reset();
            ThemeId = ThemeId.Null;
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        ThemeKeyframe ICopyable<ThemeKeyframe>.Copy() => CopyImpl();
        
        private ThemeKeyframe CopyImpl() => new(ThemeId, Frame, Ease);
        
        public void Update(ThemeKeyframe src)
        {
            base.Update(src);

            ThemeId = src.ThemeId;
        }

        public void Pull(ThemeKeyframe src)
        {
            base.Pull(src);

            ThemeId = src.ThemeId;
        }

        public bool Equals(ThemeKeyframe other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && ThemeId.Equals(other.ThemeId);
            return result;
        }

        public override bool Equals(object obj) => obj is ThemeKeyframe value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ThemeId);
    }
}
