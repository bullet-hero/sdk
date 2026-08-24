using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The one graphics sub-group that does NOT derive from BaseGraphicsSettings, and the reason is
    // its own Type field: an inherited Render would mean "is anti-aliasing on", which is precisely
    // what Type = None already says. Two switches for one decision can disagree, and whichever one
    // a reader happened to check would be the wrong one half the time.
    //
    // Hdr lives here rather than beside the post-processing switches because it is the same
    // decision as the sample count - both describe what the camera's target IS, and both are paid
    // for in the same tile memory. The game ships with it off: nothing it renders needs values
    // above 1, and HDR would double the bandwidth MSAA multiplies.

    /// <summary>
    /// How the camera's own render target is configured - the anti-aliasing method, the MSAA sample
    /// count it uses, and whether the target is HDR.
    /// </summary>
    [RuleContainer]
    public class AntiAliasingGraphicsSettings : IModel<AntiAliasingGraphicsSettings>,
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
        [JsonProperty(Names.Msaa)]
        public MsaaType Msaa { get; set; }

        /// <summary> Whether the camera renders into an HDR target. Off everywhere by default. </summary>
        [JsonProperty(Names.Hdr)]
        public bool Hdr { get; set; }

        public AntiAliasingGraphicsSettings()
        {
            Type = AntiAliasingType.Msaa;
            Msaa = MsaaType.X2;
            Hdr = false;
        }
        public AntiAliasingGraphicsSettings(AntiAliasingType type, MsaaType msaa, bool hdr)
        {
            Type = type;
            Msaa = msaa;
            Hdr = hdr;
        }
        public void Reset()
        {
            Type = AntiAliasingType.Msaa;
            Msaa = MsaaType.X2;
            Hdr = false;
        }

        public object Clone() => Copy();
        public AntiAliasingGraphicsSettings Copy() => new(Type, Msaa, Hdr);

        public void Pull(AntiAliasingGraphicsSettings source)
        {
            Type = source.Type;
            Msaa = source.Msaa;
            Hdr = source.Hdr;
        }

        public void Update(AntiAliasingGraphicsSettings src)
        {
            Type = src.Type;
            Msaa = src.Msaa;
            Hdr = src.Hdr;
        }

        public override bool Equals(object obj) => obj is AntiAliasingGraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine((int)Type, (int)Msaa, Hdr);

        public bool Equals(AntiAliasingGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Type == other.Type
                   && Msaa == other.Msaa
                   && Hdr == other.Hdr;
        }
    }
}
