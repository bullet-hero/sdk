using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // Not a BaseGraphicsSettings: an inherited Render would be a second switch for what Type = None says.
    // Hdr lives here (the sample count's tile memory); it ships off. Docs/SETTING_GROUPS.md

    /// <summary>
    /// How the camera's own render target is configured - the anti-aliasing method, the MSAA sample
    /// count it uses, and whether the target is HDR.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AntiAliasingGraphicsSettings : IModel<AntiAliasingGraphicsSettings>,
        IMoveable<AntiAliasingGraphicsSettings>
    {
        /// <summary> Which method is used. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Type)]
        public AntiAliasingType Type { get; set; }

        /// <summary> Samples per pixel, read only when <see cref="Type"/> is
        /// <see cref="AntiAliasingType.Msaa"/> - kept across a switch to another method so turning
        /// MSAA back on restores the count the player chose. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.MSAA)]
        public MsaaType MSAA { get; set; }

        /// <summary> Whether the camera renders into an HDR target. Off everywhere by default. </summary>
        [JsonProperty(Names.HDR)]
        public bool HDR { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AntiAliasingGraphicsSettings()
        {
            Type = AntiAliasingType.Msaa;
            MSAA = MsaaType.X2;
            HDR = false;
        }
        /// <summary> Built from its type, msaa and hdr. </summary>
        public AntiAliasingGraphicsSettings(AntiAliasingType type, MsaaType msaa, bool hdr)
        {
            Type = type;
            MSAA = msaa;
            HDR = hdr;
        }
    }
}
