using System.Runtime.CompilerServices;

namespace BH.SDK.Avatars
{
    // WHEN SOMETHING HAPPENED, AND WHETHER ITS WINDOW IS STILL OPEN. It was a private nested struct on
    // the consumer's AvatarController, which is exactly why the warm bot's route verifier could not
    // reach it and restated the same three lines of arithmetic in its own type - two copies of "is this
    // window open" is how a verifier starts certifying routes the game will not fly. It is public and
    // here now, so there is one.
    //
    // TIME IS THE LEVEL'S, IN SECONDS, AND IT ARRIVES AS AN ARGUMENT. This type owns no clock: the game
    // hands it the level clock (which playback speed, pause and the checkpoint ramp all bend), and the
    // bake hands it the time of the frame being replayed. Reaching for a clock here would make the two
    // disagree the moment a run is played at anything but 1x.

    /// <summary> An instant on the level clock, and the windows measured from it. </summary>
    public readonly struct TimePoint
    {
        // Far enough in the past that no window can still be open, so "never happened" needs no
        // separate case anywhere - a bool beside every timer is a bool that can disagree with it.

        /// <summary> The instant that reads as "this has not happened yet". </summary>
        public const float Never = -1e9f;

        /// <summary> When it happened, on the level clock. </summary>
        public readonly float Time;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimePoint(float time) => Time = time;

        /// <summary> A point that has not happened. </summary>
        public static TimePoint Invalid => new(Never);

        /// <summary> Seconds since it happened; negative if it is still ahead. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetDelta(float time) => time - Time;

        /// <summary> Whether <paramref name="cooldown"/> seconds have not yet passed. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool InCooldown(float time, float cooldown) => time < Time + cooldown;

        // Inclusive at both ends, and the upper end is what a window of 0 turns off: `0 >= 0` would
        // otherwise read as open on the very frame the event landed, which is the difference between
        // "i-frames disabled" and "i-frames for one frame".

        /// <summary> Whether a window of <paramref name="timeActive"/> seconds is still open,
        /// <paramref name="deltaTime"/> seconds after the event. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsActive(float deltaTime, float timeActive)
            => timeActive >= deltaTime && deltaTime >= 0f;

        // Named apart from the static above rather than overloading it: both take two floats, so one
        // name would be two members with the same signature - and worse, "IsActive(x, y)" would mean
        // "x is a delta" in one and "x is an instant" in the other.

        /// <summary> Whether the window is still open at <paramref name="time"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActiveAt(float time, float timeActive) => IsActive(GetDelta(time), timeActive);
    }
}
