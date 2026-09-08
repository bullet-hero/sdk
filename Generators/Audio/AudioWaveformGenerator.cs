using BH.SDK.Generators.External;
using BH.SDK.Generators.Spawn;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;

namespace BH.SDK.Generators.Audio
{
    // Peaks come from the host (Unity side: Timelines/Utils/AudioWaveformCache, which already
    // downsamples a clip for the audio timeline) rather than being computed here - the SDK has no
    // decoder. With no peaks supplied this produces nothing at all, deliberately: a waveform of
    // invented numbers is worse than an empty run, because it looks like it worked.

    /// <summary> Which side of the baseline the bars grow on - the baseline being OriginY, the axis
    /// the whole chart is read against. </summary>
    public enum WaveformAlign
    {
        /// <summary> Bars stand on the axis and grow up. </summary>
        Bottom,

        /// <summary> Bars are centred on the axis and grow both ways, spanning twice the height. </summary>
        Center,

        /// <summary> Bars hang from the axis and grow down. </summary>
        Top,
    }

    /// <summary>
    /// A bar chart of a track's waveform, as real level objects - the song's shape becomes scenery.
    /// </summary>
    public class AudioWaveformGenerator : BaseSpawnGenerator<AudioWaveformGenerator.Parameters>
    {
        /// <summary> <c>"gen_audio_waveform"</c>, the key a host lists this generator under. </summary>
        public override string NameKey => "gen_audio_waveform";

        /// <summary> What must be true before a host offers this run. </summary>
        public override GeneratorRequirements Requirements => GeneratorRequirements.ExternalAnalysis;

        /// <summary> The order, labels and ranges a host lays its form out with. </summary>
        public override GeneratorHints Hints { get; } = new GeneratorHints.Builder()
            .Section(GeneratorSections.Main, SpawnParameters.MainFields)
            .Section(GeneratorSections.Main, nameof(Parameters.Source), nameof(Parameters.BarCount),
                nameof(Parameters.BarWidth), nameof(Parameters.Height), nameof(Parameters.Align))
            .Section(GeneratorSections.Additional, SpawnParameters.AdditionalFields)
            .Section(GeneratorSections.Additional, nameof(Parameters.Spacing), nameof(Parameters.MinHeight),
                nameof(Parameters.OriginX), nameof(Parameters.OriginY), nameof(Parameters.Peaks))
            .Range(nameof(Parameters.BarCount), 0, 512)
            .Range(nameof(Parameters.BarWidth), 0.01f, ValueRules.MaxSca)
            .Range(nameof(Parameters.Spacing), 0f, ValueRules.MaxPos)
            .Range(nameof(Parameters.Height), 0.01f, ValueRules.MaxSca)
            .Range(nameof(Parameters.MinHeight), 0f, ValueRules.MaxSca)
            // Source stays visible - which track to visualize is an authoring choice; the sampled
            // peaks behind it are not.
            .Hidden(nameof(Parameters.Peaks))
            .Range(nameof(Parameters.OriginX), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(Parameters.OriginY), ValueRules.MinPos, ValueRules.MaxPos)
            .Range(nameof(SpawnParameters.Size), ValueRules.MinSca, ValueRules.MaxSca)
            .Build();

        /// <summary> Writes this run's objects into the scope. </summary>
        protected override void Generate(GeneratorContext context, Parameters parameters)
        {
            var bars = BarCount(parameters);
            if (bars <= 0) return;

            var totalWidth = bars * parameters.BarWidth + (bars - 1) * parameters.Spacing;
            var left = parameters.OriginX - totalWidth * 0.5f + parameters.BarWidth * 0.5f;

            for (var i = 0; i < bars; i++)
            {
                var peak = PeakAt(parameters, bars, i);
                var height = parameters.MinHeight + peak * parameters.Height;
                if (parameters.Align == WaveformAlign.Center) height *= 2f;

                var x = left + i * (parameters.BarWidth + parameters.Spacing);

                // An object's own position is its CENTRE, so growing from the axis means offsetting
                // by half the bar - only a Center-aligned bar sits on the axis itself. This is the
                // whole difference between the three alignments; nothing else about a bar changes.
                var y = parameters.Align switch
                {
                    WaveformAlign.Bottom => parameters.OriginY + height * 0.5f,
                    WaveformAlign.Top => parameters.OriginY - height * 0.5f,
                    _ => parameters.OriginY,
                };

                var obj = Spawn(context, parameters, $"waveform_{i}", context.Span);
                AddPosition(obj, x, y, obj.Span.StartFrame);
                SetSize(obj, parameters.BarWidth, height);
            }
        }

        /// <summary> What this run would add, answered before it runs. </summary>
        protected override GeneratorCost EstimateTyped(GeneratorContext context, Parameters parameters)
        {
            var bars = BarCount(parameters);
            return new GeneratorCost(bars, bars * KeysPerBar);
        }

        /// <summary> Bars actually drawn: the author's requested count, capped by how many peaks the
        /// host supplied, and defaulting to one bar per peak when no count is set. </summary>
        private static int BarCount(Parameters parameters)
        {
            var available = parameters.Peaks?.Length ?? 0;
            if (available <= 0) return 0;
            if (parameters.BarCount <= 0) return available;
            return parameters.BarCount < available ? parameters.BarCount : available;
        }

        // Bars are resampled from the peak array rather than read one-to-one, so asking for fewer
        // bars than there are peaks summarises the whole track instead of showing only its start.
        private static float PeakAt(Parameters parameters, int bars, int index)
        {
            var peaks = parameters.Peaks;
            if (peaks == null || peaks.Length == 0) return 0f;

            var from = (int)((long)index * peaks.Length / bars);
            var to = (int)((long)(index + 1) * peaks.Length / bars);
            if (to <= from) to = from + 1;
            if (to > peaks.Length) to = peaks.Length;

            var max = 0f;
            for (var i = from; i < to; i++)
            {
                var value = peaks[i] < 0f ? -peaks[i] : peaks[i];
                if (value > max) max = value;
            }
            return max > 1f ? 1f : max;
        }

        private const int KeysPerBar = 3; // position + size + colour

        /// <summary> Which samples are charted and how the bars are laid out. Public mutable fields, like every
        /// parameters class here - a form binds to them and a preset serializes from them. </summary>
        public class Parameters : SpawnParameters, IWaveformInput
        {
            /// <summary> Zero means "one bar per supplied peak". </summary>
            public int BarCount = 64;
            /// <summary> How wide one bar is. </summary>
            public float BarWidth = 0.2f;
            /// <summary> The gap between neighbouring bars. </summary>
            public float Spacing = 0.05f;
            /// <summary> How tall the loudest bar gets. </summary>
            public float Height = 6f;
            /// <summary> How tall the quietest one stays, so silence still reads as a line. </summary>
            public float MinHeight = 0.1f;
            /// <summary> Where the chart starts. </summary>
            public float OriginX;

            /// <summary> The axis every bar is measured from - see WaveformAlign. </summary>
            public float OriginY = -6f;

            /// <summary> Which side of the baseline the bars grow on. </summary>
            public WaveformAlign Align = WaveformAlign.Bottom;

            /// <summary> Which track is charted. </summary>
            public AudioResourceId Source = AudioResourceId.Null;
            /// <summary> The samples themselves; filled by the host, since the SDK decodes no audio. </summary>
            public float[] Peaks = System.Array.Empty<float>();

            AudioResourceId IWaveformInput.Source
            {
                get => Source;
                set => Source = value;
            }
            float[] IWaveformInput.Peaks
            {
                get => Peaks;
                set => Peaks = value;
            }
        }
    }
}
