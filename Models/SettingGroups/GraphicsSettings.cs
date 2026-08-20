using System;
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
    public class GraphicsSettings : IFrameable, IModel<GraphicsSettings>, IMoveable<GraphicsSettings>
    {
        /// <summary> Where the target framerate comes from - the screen's refresh rate or the fixed
        /// value below. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.FramerateTarget)]
        public FramerateTarget FramerateTarget { get; set; }

        // if 0 - doesn't setup framerate, use Unity default. Require project restart
        // if > 0 - target framerate

        /// <summary> Explicit framerate cap, used when FramerateTarget says so. </summary>
        [RuleMin(1)]
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

        public GraphicsSettings()
        {
            FramerateTarget = FramerateTarget.ScreenHz;
            FixedFramerate = 60;
            Audio = new AudioGraphicsSettings();
            Effects = new EffectsGraphicsSettings();
            PostProcessing = new PostProcessingGraphicsSettings();
            AntiAliasing = new AntiAliasingGraphicsSettings();
        }
        public GraphicsSettings(FramerateTarget framerateTarget, int fixedFramerate,
            AudioGraphicsSettings audio, EffectsGraphicsSettings effects,
            PostProcessingGraphicsSettings postProcessing, AntiAliasingGraphicsSettings antiAliasing)
        {
            FramerateTarget = framerateTarget;
            FixedFramerate = fixedFramerate;
            Audio = audio;
            Effects = effects;
            PostProcessing = postProcessing;
            AntiAliasing = antiAliasing;
        }
        public void Reset()
        {
            FramerateTarget = FramerateTarget.ScreenHz;
            FixedFramerate = 60;
            Audio.Reset();
            Effects.Reset();
            PostProcessing.Reset();
            AntiAliasing.Reset();
        }

        public object Clone() => Copy();
        public GraphicsSettings Copy() => new(FramerateTarget, FixedFramerate, (AudioGraphicsSettings)Audio.Clone(),
            (EffectsGraphicsSettings)Effects.Clone(), (PostProcessingGraphicsSettings)PostProcessing.Clone(),
            AntiAliasing.Copy());

        public void Pull(GraphicsSettings source)
        {
            FramerateTarget = source.FramerateTarget;
            Audio.Pull(source.Audio);
            Effects.Pull(source.Effects);
            PostProcessing.Pull(source.PostProcessing);
            AntiAliasing.Pull(source.AntiAliasing);
        }

        public override bool Equals(object obj) => obj is GraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine((int)FramerateTarget,
            FixedFramerate, Audio, Effects, PostProcessing, AntiAliasing);

        public bool Equals(GraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return FramerateTarget == other.FramerateTarget
                   && FixedFramerate == other.FixedFramerate
                   && Audio.Equals(other.Audio)
                   && Effects.Equals(other.Effects)
                   && PostProcessing.Equals(other.PostProcessing)
                   && AntiAliasing.Equals(other.AntiAliasing);
        }
    }
}