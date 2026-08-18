using System;
using System.Collections.Generic;
using BH.SDK.Generators.External;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Audio
{
    // The single most common way a level actually begins: pick a song, get a level whose timeline
    // already matches it. Doing that by hand means creating a level, importing the clip, adding a
    // track, and computing FrameDuration from a duration nobody has in front of them.
    //
    // The SDK cannot measure that duration - it has no decoder, on purpose - so DurationSeconds is
    // an ExternalAnalysis input the host fills in (see IAudioFileInput). A duration of zero is not
    // an error: the generator falls back to a fixed default length, so a level still gets created
    // when a host cannot measure the file.

    /// <summary>
    /// Creates a level built around one audio file: the clip as a resource, a track playing it from
    /// frame zero, and a timeline long enough to hold it.
    /// </summary>
    public class AudioFileLevelGenerator : BaseLevelGenerator<AudioFileLevelGenerator.Parameters>
    {
        public override string NameKey => "gen_level_audio_file";

        public override GeneratorRequirements Requirements => GeneratorRequirements.ExternalAnalysis;

        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, nameof(Parameters.LevelName), nameof(Parameters.Framerate))
            .Section(GeneratorSections.Additional, nameof(Parameters.LevelDescription),
                nameof(Parameters.OffsetSeconds), nameof(Parameters.TailSeconds),
                nameof(Parameters.AudioPath), nameof(Parameters.UriType), nameof(Parameters.DurationSeconds))
            .Range(nameof(Parameters.Framerate), FrameRules.MinFramerate, FrameRules.MaxFramerate)
            .Range(nameof(Parameters.OffsetSeconds), AudioRules.MinOffsetTime, AudioRules.MaxOffsetTime)
            .Range(nameof(Parameters.TailSeconds), 0f, 60f)
            .Unit(nameof(Parameters.Framerate), "fps")
            .Unit(nameof(Parameters.OffsetSeconds), "s")
            .Unit(nameof(Parameters.TailSeconds), "s")
            // Filled by the host through IAudioFileInput - showing them would invite the author to
            // fight the file picker over values it is about to overwrite.
            .Hidden(nameof(Parameters.AudioPath))
            .Hidden(nameof(Parameters.UriType))
            .Hidden(nameof(Parameters.DurationSeconds))
            .Range(nameof(Parameters.DurationSeconds), 0f, 3600f)
            .Build();

        protected override GeneratedLevel CreateTyped(Parameters parameters)
        {
            var framerate = Framerate(parameters.Framerate);
            var level = new Level();
            level.Settings.Framerate = framerate;
            level.Settings.FrameDuration = FrameDuration(parameters, framerate);

            var resourceId = new AudioResourceId(AudioResourceId.MaxUserDefinedValue);
            level.Resources.Audios[resourceId] = new AudioResource(resourceId, new List<ResourceKey>
            {
                new(parameters.UriType, parameters.AudioPath ?? string.Empty),
            });

            // The track covers the whole timeline: a span of FrameDuration frames starting at zero,
            // so it ends exactly on the level's end boundary and its last sounding frame is
            // FrameDuration - 1. This used to be the easiest off-by-one in the format to write by
            // accident, back when the end was a separate inclusive field.
            var audioId = level.Settings.GetNextAudioId();
            level.Audio.Tracks[audioId] = new LevelTrack(audioId, resourceId,
                new FrameSpan(FrameRules.MinFrame, level.Settings.FrameDuration), parameters.OffsetSeconds,
                AudioRules.SpeedDefault, AudioRules.VolumeDefault, AudioRules.MinAudioLayer,
                TrackName(parameters), new LevelTrackEffects());

            var meta = new LevelMeta
            {
                LevelName = ResolveName(parameters).Copy(),
                LevelDescription = parameters.LevelDescription.Copy(),
            };
            return new GeneratedLevel(level, meta);
        }

        protected override GeneratorCost EstimateTyped(Parameters parameters)
            => new(0, 0, ResourceCount);

        /// <summary> Song length plus the tail, in frames - the tail is what leaves room to author
        /// an ending after the music stops instead of cutting the level off on the last beat. </summary>
        private static int FrameDuration(Parameters parameters, int framerate)
        {
            var seconds = parameters.DurationSeconds > 0f ? parameters.DurationSeconds : DefaultSeconds;
            var tail = parameters.TailSeconds > 0f ? parameters.TailSeconds : 0f;
            var frames = (int)Math.Ceiling((seconds + tail) * framerate);

            if (frames < FrameRules.MinFrameDuration) frames = FrameRules.MinFrameDuration;
            if (frames > FrameRules.MaxFrameDuration) frames = FrameRules.MaxFrameDuration;
            return frames;
        }

        /// <summary> The author's own name if they typed one, otherwise the file's - a level called
        /// after its song is a better default than an empty title. </summary>
        private static IString ResolveName(Parameters parameters)
        {
            if (parameters.LevelName is StringValue literal && string.IsNullOrEmpty(literal.Value))
                return new StringValue(FileNameOf(parameters.AudioPath));
            return parameters.LevelName;
        }

        private static string TrackName(Parameters parameters)
        {
            var name = FileNameOf(parameters.AudioPath);
            return string.IsNullOrEmpty(name) ? "audio" : name;
        }

        // Hand-rolled rather than Path.GetFileNameWithoutExtension: a level authored on Windows can
        // be opened on Linux, so both separators have to be handled regardless of what the running
        // platform considers one.
        private static string FileNameOf(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var start = 0;
            for (var i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] != '/' && path[i] != '\\') continue;
                start = i + 1;
                break;
            }

            var end = path.Length;
            for (var i = path.Length - 1; i > start; i--)
            {
                if (path[i] != '.') continue;
                end = i;
                break;
            }
            return path.Substring(start, end - start);
        }

        private const int ResourceCount = 1;
        private const float DefaultSeconds = 60f;

        private static int Framerate(int value)
            => value < FrameRules.MinFramerate ? FrameRules.MinFramerate
                : value > FrameRules.MaxFramerate ? FrameRules.MaxFramerate : value;

        // The ExternalAnalysis inputs are FIELDS like everything else, with the interface implemented
        // explicitly on top of them. A property would be invisible to the form builder (it reflects
        // over public fields), which would make Hints.Hidden on them meaningless and leave an author
        // no way to ever inspect what the host filled in.
        public class Parameters : IAudioFileInput
        {
            public IString LevelName = new StringValue();
            public IString LevelDescription = new StringValue();
            public int Framerate = 60;
            public float OffsetSeconds;
            public float TailSeconds = 2f;

            public string AudioPath = string.Empty;
            public ResourceUriType UriType = ResourceUriType.LevelPath;
            public float DurationSeconds;

            string IAudioFileInput.AudioPath
            {
                get => AudioPath;
                set => AudioPath = value;
            }
            ResourceUriType IAudioFileInput.UriType
            {
                get => UriType;
                set => UriType = value;
            }
            float IAudioFileInput.DurationSeconds
            {
                get => DurationSeconds;
                set => DurationSeconds = value;
            }
        }
    }
}
