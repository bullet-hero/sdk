using System;
using System.Globalization;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent
{
    /// <summary>
    /// Reads a time the AI writes - a global frame or seconds - into the frame or frame count the model
    /// holds. Every failure names the input; nothing here clamps.
    /// </summary>
    public static class TimeAddress
    {
        // Lockstep: Shared's FrameUtils.ToFrame / ToFrameCount - the same float arithmetic.

        /// <summary> Prefix of a global frame: <c>"f:120"</c>. </summary>
        public const string FramePrefix = "f:";

        /// <summary> Prefix of seconds: <c>"s:12.5"</c>. </summary>
        public const string SecondsPrefix = "s:";

        /// <summary> A time as a global frame: <c>120</c>, <c>"120"</c>, <c>"f:120"</c> or <c>"s:12.5"</c>.
        /// </summary>
        public static bool TryParseFrame(JToken input, int framerate, out int frame, out string error)
        {
            frame = FrameRules.NoFrame;
            if (!TryRead(input, out var text, out var number, out error)) return false;
            return TryParse(text, number, framerate, false, out frame, out error);
        }

        /// <summary> A time given as text; see <see cref="TryParseFrame(JToken,int,out int,out string)"/>.
        /// </summary>
        public static bool TryParseFrame(string input, int framerate, out int frame, out string error)
            => TryParseFrame(input == null ? null : new JValue(input), framerate, out frame, out error);

        /// <summary> A duration as a frame count: <c>60</c>, <c>"60"</c>, <c>"f:60"</c> or <c>"s:2"</c>.
        /// </summary>
        public static bool TryParseDuration(JToken input, int framerate, out int frames, out string error)
        {
            frames = 0;
            if (!TryRead(input, out var text, out var number, out error)) return false;
            return TryParse(text, number, framerate, true, out frames, out error);
        }

        /// <summary> A duration given as text; see <see cref="TryParseDuration(JToken,int,out int,out
        /// string)"/>. </summary>
        public static bool TryParseDuration(string input, int framerate, out int frames, out string error)
            => TryParseDuration(input == null ? null : new JValue(input), framerate, out frames, out error);

        /// <summary> <see cref="TryParseFrame(JToken,int,out int,out string)"/>, throwing on failure.
        /// </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseFrame(JToken input, int framerate)
            => TryParseFrame(input, framerate, out var frame, out var error) ? frame : throw new FormatException(error);

        /// <summary> <see cref="TryParseDuration(JToken,int,out int,out string)"/>, throwing on failure.
        /// </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseDuration(JToken input, int framerate)
            => TryParseDuration(input, framerate, out var frames, out var error)
                ? frames : throw new FormatException(error);

        private static bool TryRead(JToken input, out string text, out double? number, out string error)
        {
            text = null;
            number = null;
            error = null;

            switch (input?.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    number = input.Value<double>();
                    return true;
                case JTokenType.String:
                    text = input.Value<string>().Trim();
                    return true;
                default:
                    error = $"A time is a number or a string such as \"f:120\" or \"s:12.5\", got {Show(input)}.";
                    return false;
            }
        }

        private static bool TryParse(string text, double? number, int framerate, bool isCount, out int result,
            out string error)
        {
            result = 0;
            var input = number.HasValue
                ? number.Value.ToString("R", CultureInfo.InvariantCulture)
                : text;

            if (!FrameRules.IsValidFramerate(framerate))
            {
                error = $"Cannot read time {Quote(input)}: framerate {framerate} is not valid.";
                return false;
            }

            if (number.HasValue) return TryFromFrames(number.Value, input, isCount, out result, out error);

            if (text.StartsWith(SecondsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var body = text.Substring(SecondsPrefix.Length);
                if (!float.TryParse(body, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                    || float.IsNaN(seconds) || float.IsInfinity(seconds))
                {
                    error = $"Cannot read time {Quote(input)}: seconds must be a number, as in \"s:12.5\".";
                    return false;
                }

                if (seconds < FrameRules.MinTime)
                {
                    error = $"Cannot read time {Quote(input)}: seconds cannot be negative.";
                    return false;
                }

                var scaled = Math.Floor(seconds * framerate + FrameRules.DeltaFramerate);
                return TryFromFrames(scaled + (isCount ? 0 : FrameRules.MinFrame), input, isCount, out result,
                    out error);
            }

            var digits = text.StartsWith(FramePrefix, StringComparison.OrdinalIgnoreCase)
                ? text.Substring(FramePrefix.Length)
                : text;

            if (digits.Length > 0 && !char.IsLetter(digits[0])
                && double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var frames))
                return TryFromFrames(frames, input, isCount, out result, out error);

            error = $"Cannot read time {Quote(input)}: use a frame (120, \"f:120\") or seconds (\"s:12.5\").";
            return false;
        }

        private static bool TryFromFrames(double frames, string input, bool isCount, out int result, out string error)
        {
            result = 0;
            var min = isCount ? FrameRules.MinFrameDuration : FrameRules.MinFrame;
            var max = isCount ? FrameRules.MaxFrameDuration : FrameRules.MaxFrame;

            if (double.IsNaN(frames) || frames != Math.Floor(frames))
            {
                error = $"Cannot read time {Quote(input)}: a frame is a whole number.";
                return false;
            }

            if (frames < min || frames > max)
            {
                error = isCount
                    ? $"Cannot read duration {Quote(input)}: it must be {min}-{max} frames, got {frames}."
                    : $"Cannot read time {Quote(input)}: a frame is {min}-{max}, got {frames}.";
                return false;
            }

            result = (int)frames;
            error = null;
            return true;
        }

        private static string Show(JToken input) => input == null || input.Type == JTokenType.Null
            ? "nothing"
            : $"{input.Type.ToString().ToLowerInvariant()} {input.ToString(Newtonsoft.Json.Formatting.None)}";

        private static string Quote(string input) => "\"" + input + "\"";
    }
}
