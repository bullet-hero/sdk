using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;

namespace BH.SDK.Generators.External
{
    // ExternalAnalysis says "some parameters come from the host"; these interfaces say WHICH, so a host
    // can offer the matching control and write the result back before Run. By interface, not field name:
    // a rename is a compile error. The SDK implements none of the analysis (no decoder, FFT, loader).

    /// <summary> A generator that needs an audio file chosen and measured by the host. </summary>
    public interface IAudioFileInput
    {
        /// <summary> Where the clip lives, in whatever form UriType says. </summary>
        string AudioPath { get; set; }

        /// <summary> How the path should be interpreted when the level loads. </summary>
        ResourceUriType UriType { get; set; }

        /// <summary> Clip length in seconds. The SDK cannot decode audio, so this is the host's to
        /// fill; zero means "not measured yet" and the generator falls back to its own default. </summary>
        float DurationSeconds { get; set; }
    }

    /// <summary> A generator that draws an existing track's waveform, so it needs that waveform
    /// sampled by the host (Unity side: Timelines/Utils/AudioWaveformCache). </summary>
    public interface IWaveformInput
    {
        /// <summary> Which of the level's audio tracks to visualize. </summary>
        AudioResourceId Source { get; set; }

        /// <summary> Normalized peaks, one per bar, in the range [0,1]. Empty means "not sampled
        /// yet" - the generator then produces nothing rather than guessing a shape. </summary>
        float[] Peaks { get; set; }
    }

    /// <summary> A generator driven by beat positions the host detected. </summary>
    public interface IBeatFramesInput
    {
        /// <summary> Frames the beats land on. Empty means "not detected yet". </summary>
        int[] BeatFrames { get; set; }
    }

    // Not part of ExternalAnalysis: it exists so a FORM can show the framerate beside the target. A
    // generator takes its math from GeneratorContext.Settings, never from this field, which a host may
    // leave unfilled.

    /// <summary> A generator whose form wants the current framerate on display beside its own. </summary>
    public interface ICurrentFramerateInput
    {
        /// <summary> The level's framerate as it is right now, mirrored in for display. </summary>
        int CurrentFramerate { get; set; }
    }

    /// <summary> A generator that turns an image into level content, so it needs the image decoded
    /// by the host (Unity side: PixelTexture from a Texture2D, see UnityExtensions). </summary>
    public interface IPixelTextureInput
    {
        /// <summary> Which of the level's texture resources the pixels came from - kept so the host
        /// can re-decode after a reimport. </summary>
        TextureResourceId Source { get; set; }

        /// <summary> The decoded image. Null means "not decoded yet". </summary>
        PixelTexture Texture { get; set; }
    }
}
