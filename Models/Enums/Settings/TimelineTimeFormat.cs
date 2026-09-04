namespace BH.SDK.Models.Enums.Settings
{
    // The zero value IS the default here, unlike MenuBackgroundKind's - a frame number is what the
    // ruler has always printed, so an older settings file with no key reads back as the behaviour it
    // already had and no migration exists. That is also why the members are ordered by how much they
    // add rather than by how they are offered: each one is the previous with one more component.
    //
    // The format names the MAXIMUM detail a label may carry, never the exact text: a label prints
    // only the components that carry information at the current grid step (TimeTextFormatter), so
    // TimecodeFrames zoomed out to a step of whole seconds prints "0:21" rather than "0:21:00".

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
        /// Bar and beat off the level's own beat grid - "11.3". The only format that answers "where
        /// in the music", and the only one whose labels are not evenly spaced: it is resolved
        /// against the tempo map, so a stretch the map does not cover has no bars at all and falls
        /// back to <see cref="Timecode"/>.
        /// </summary>
        BarsBeats = 3,
    }
}
