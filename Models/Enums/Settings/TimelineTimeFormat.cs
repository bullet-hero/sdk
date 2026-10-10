namespace BH.SDK.Models.Enums.Settings
{
    // Zero is the default (a frame number). Names the MAXIMUM detail; a label prints only the components
    // the grid step needs. Members are ordered by detail added.

    /// <summary> How a timeline ruler and the playhead readout spell a frame. </summary>
    public enum TimelineTimeFormat : byte
    {
        /// <summary> The absolute frame index - "1275". Exact, and the only format that says
        /// nothing about the music. </summary>
        Frames = 0,

        /// <summary> Minutes and seconds - "0:21", growing an hours field only on a level long
        /// enough to need one. </summary>
        Timecode = 1,

        /// <summary> Minutes, seconds and the frame within the second - "0:21:15". The SMPTE
        /// shape, and the only time format that still addresses a single frame. </summary>
        TimecodeFrames = 2,

        /// <summary>
        /// Bar and beat off the beat grid, e.g. "11.3". Unevenly spaced; falls back to Timecode with no bars.
        /// </summary>
        BarsBeats = 3,
    }
}
