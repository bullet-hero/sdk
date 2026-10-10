using System;

namespace BH.SDK.Generators
{
    /// <summary> What a generator needs from its host before it can run. A host checks these to offer,
    /// disable or pre-fill; the generator may assume every requirement it declared was met. </summary>
    [Flags]
    public enum GeneratorRequirements
    {
        None = 0,

        /// <summary> Needs GeneratorContext.Game/Audio, which are null while a Prefab template is
        /// the active scope - a template has objects but no level-global event tracks and no audio.
        /// A host disables such a generator in Prefab Mode instead of running it into a null. </summary>
        LevelScope = 1 << 0,

        /// <summary> Needs a non-empty GeneratorContext.Selection. </summary>
        Selection = 1 << 1,

        /// <summary> At least one parameter is filled in by the host rather than by the form - audio
        /// duration, waveform peaks, beat frames. The SDK carries no decoder and no DSP, so those
        /// values can only come from outside; the corresponding fields are hidden from the form
        /// through GeneratorHints.Visible. </summary>
        ExternalAnalysis = 1 << 2,
    }
}
