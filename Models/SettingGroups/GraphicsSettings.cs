using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.Graphics;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// Per-device rendering options: the global framerate policy plus one sub-group per subsystem
    /// that can be turned down independently. This is how a weak phone runs a level authored on a
    /// PC - the level is unchanged, the player just renders less of it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GraphicsSettings : IFrameable, IModel<GraphicsSettings>, IMoveable<GraphicsSettings>
    {
        /// <summary> Where the target framerate comes from - the screen's refresh rate or the fixed
        /// value below. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.FramerateTarget)]
        public FramerateTarget FramerateTarget { get; set; }

        // if 0 - doesn't setup framerate, use Unity default. Require project restart
        // if > 0 - target framerate

        /// <summary> Explicit framerate cap, used when FramerateTarget says so. </summary>
        [RuleMinValue(1)]
        [JsonProperty(Names.FixedFramerate)]
        public int FixedFramerate { get; set; }

        /// <summary> Audio playback/sync options - grouped here rather than in AudioSettings because
        /// these are performance trade-offs, not volume preferences. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Audio)]
        public AudioGraphicsSettings Audio { get; set; }

        /// <summary> Particle rendering options, including their own framerate budget. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Effects)]
        public EffectsGraphicsSettings Effects { get; set; }

        /// <summary> Per-effect switches for the post-processing stack. </summary>
        [RuleNotNull]
        [JsonProperty(Names.PostProcessing)]
        public PostProcessingGraphicsSettings PostProcessing { get; set; }

        /// <summary> How the camera's own target is configured - anti-aliasing method, MSAA sample
        /// count, HDR. The only sub-group here that is not a BaseGraphicsSettings; see its own
        /// header for why an inherited Render switch would be wrong for it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AntiAliasing)]
        public AntiAliasingGraphicsSettings AntiAliasing { get; set; }

        // Added after the domain was already at 1.0 and deliberately does NOT bump it, the same
        // additive call UserSettings' own Interface and Keybindings groups made: a settings.json
        // written before this group existed has no "textures" key, Newtonsoft leaves the
        // constructor's defaults in place, and every one of those defaults is Auto.

        /// <summary> How a level's images are turned into GPU textures here - compression, size cap,
        /// mip-maps. Read when a level's resources load, not per frame. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Textures)]
        public TexturesGraphicsSettings Textures { get; set; }

        // Additive on the same terms Textures was, and it does not bump the domain either. This one
        // is the group that has no effect at all on mobile - a phone has one window and it is the
        // screen - which is why it is disabled rather than hidden on the settings screen, and why
        // nothing here resolves per platform: a desktop honours all of it and a phone honours none.

        /// <summary> Window mode, window resolution and render scale. Desktop only. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Display)]
        public DisplayGraphicsSettings Display { get; set; }

        public GraphicsSettings()
        {
            FramerateTarget = FramerateTarget.ScreenHz;
            FixedFramerate = 60;
            Audio = new AudioGraphicsSettings();
            Effects = new EffectsGraphicsSettings();
            PostProcessing = new PostProcessingGraphicsSettings();
            AntiAliasing = new AntiAliasingGraphicsSettings();
            Textures = new TexturesGraphicsSettings();
            Display = new DisplayGraphicsSettings();
        }

        public GraphicsSettings(FramerateTarget framerateTarget, int fixedFramerate,
            AudioGraphicsSettings audio, EffectsGraphicsSettings effects,
            PostProcessingGraphicsSettings postProcessing, AntiAliasingGraphicsSettings antiAliasing,
            TexturesGraphicsSettings textures, DisplayGraphicsSettings display)
        {
            FramerateTarget = framerateTarget;
            FixedFramerate = fixedFramerate;
            Audio = audio;
            Effects = effects;
            PostProcessing = postProcessing;
            AntiAliasing = antiAliasing;
            Textures = textures;
            Display = display;
        }
    }
}