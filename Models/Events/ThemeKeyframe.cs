using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Events
{
    /// <summary>
    /// Switches which palette the level is using at a given frame - the outer half of the theme
    /// indirection every ColorType.ThemeRef depends on. A real animated track, unlike the one-shot
    /// Marker/Checkpoint it is stored alongside.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ThemeKeyframe : Keyframe, IModel<ThemeKeyframe>
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
    }
}
