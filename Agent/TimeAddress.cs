using System;
using System.Globalization;
using System.Text.RegularExpressions;
using BH.SDK.Rules;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Agent
{
    /// <summary>
    /// Reads a time the AI writes - a global frame, seconds, or a musical address - into the frame or frame
    /// count the model holds. Every failure names the input; nothing here clamps.
    /// </summary>
    public static class TimeAddress
    {
        // Lockstep: Shared's FrameUtils.ToFrame / ToFrameCount - the same float arithmetic.

        /// <summary> Prefix of a global frame: <c>"f:120"</c>. </summary>
        public const string FramePrefix = "f:";

        /// <summary> Prefix of seconds: <c>"s:12.5"</c>. </summary>
        public const string SecondsPrefix = "s:";

        /// <summary> Prefix of a bar, optionally with a beat: <c>"bar:17"</c>, <c>"bar:17.3"</c>. </summary>
        public const string BarPrefix = "bar:";

        /// <summary> Prefix of a global beat, 1-based and fractional: <c>"beat:64.5"</c>. </summary>
        public const string BeatPrefix = "beat:";

        /// <summary> Prefix of a marker, optionally shifted: <c>"marker:drop"</c>,
        /// <c>"marker:drop+2bars"</c>. </summary>
        public const string MarkerPrefix = "marker:";

        private const string NoGrid = "the level has no beat grid (add a beat segment first)";

        private static readonly Regex MusicalCount = new(@"^(?<n>[0-9]+(?:\.[0-9]+)?)\s*(?<unit>bars?|beats?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex FrameShift = new(@"^(?<base>.+?)\s*(?<sign>[+-])\s*(?<n>[0-9]+)\s*f$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private const string BeatAfterDot = ". After the dot comes a beat number, not a fraction: \"bar:17.3\" " +
                                            "is beat 3 of bar 17, and \"bar:17+1.5beats\" moves by part of a beat";

        private static readonly Regex MusicalShift = new(
            @"^(?<base>.+?)\s*(?<sign>[+-])\s*(?<n>[0-9]+(?:\.[0-9]+)?)\s*(?<unit>bars?|beats?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex MarkerShift = new(
            @"^(?<name>.+?)\s*(?<sign>[+-])\s*(?<n>[0-9]+(?:\.[0-9]+)?)\s*(?<unit>bars?|beats?)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary> A time as a global frame: <c>120</c>, <c>"120"</c>, <c>"f:120"</c> or <c>"s:12.5"</c>.
        /// </summary>
        public static bool TryParseFrame(JToken input, int framerate, out int frame, out string error)
            => TryParseFrame(input, framerate, null, out frame, out error);

        /// <summary> A time given as text; see <see cref="TryParseFrame(JToken,int,out int,out string)"/>.
        /// </summary>
        public static bool TryParseFrame(string input, int framerate, out int frame, out string error)
            => TryParseFrame(input == null ? null : new JValue(input), framerate, out frame, out error);

        /// <summary> A time as a global frame, musical forms included: <c>"bar:17.3"</c>, <c>"beat:64.5"</c>,
        /// <c>"marker:drop"</c>, <c>"marker:drop-1beat"</c>. A null context has no grid and no markers.
        /// </summary>
        public static bool TryParseFrame(JToken input, int framerate, TimeAddressContext context, out int frame,
            out string error)
        {
            frame = FrameRules.NoFrame;
            if (!TryRead(input, out var text, out var number, out error)) return false;
            return TryParse(text, number, framerate, false, context, FrameRules.NoFrame, out frame, out error);
        }

        /// <summary> A time given as text; see
        /// <see cref="TryParseFrame(JToken,int,TimeAddressContext,out int,out string)"/>. </summary>
        public static bool TryParseFrame(string input, int framerate, TimeAddressContext context, out int frame,
            out string error)
            => TryParseFrame(input == null ? null : new JValue(input), framerate, context, out frame, out error);

        /// <summary> A duration as a frame count: <c>60</c>, <c>"60"</c>, <c>"f:60"</c> or <c>"s:2"</c>.
        /// </summary>
        public static bool TryParseDuration(JToken input, int framerate, out int frames, out string error)
            => TryParseDuration(input, framerate, null, FrameRules.NoFrame, out frames, out error);

        /// <summary> A duration given as text; see <see cref="TryParseDuration(JToken,int,out int,out
        /// string)"/>. </summary>
        public static bool TryParseDuration(string input, int framerate, out int frames, out string error)
            => TryParseDuration(input == null ? null : new JValue(input), framerate, out frames, out error);

        /// <summary> A duration, musical forms included (<c>"2bars"</c>, <c>"3beats"</c>), measured from
        /// <paramref name="fromFrame"/>; from <see cref="FrameRules.NoFrame"/> only a one-tempo grid
        /// can answer. </summary>
        public static bool TryParseDuration(JToken input, int framerate, TimeAddressContext context, int fromFrame,
            out int frames, out string error)
        {
            frames = 0;
            if (!TryRead(input, out var text, out var number, out error)) return false;
            return TryParse(text, number, framerate, true, context, fromFrame, out frames, out error);
        }

        /// <summary> A duration given as text; see
        /// <see cref="TryParseDuration(JToken,int,TimeAddressContext,int,out int,out string)"/>. </summary>
        public static bool TryParseDuration(string input, int framerate, TimeAddressContext context, int fromFrame,
            out int frames, out string error)
            => TryParseDuration(input == null ? null : new JValue(input), framerate, context, fromFrame, out frames,
                out error);

        /// <summary> <see cref="TryParseFrame(JToken,int,out int,out string)"/>, throwing on failure.
        /// </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseFrame(JToken input, int framerate)
            => TryParseFrame(input, framerate, out var frame, out var error) ? frame : throw new FormatException(error);

        /// <summary> <see cref="TryParseFrame(JToken,int,TimeAddressContext,out int,out string)"/>,
        /// throwing on failure. </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseFrame(JToken input, int framerate, TimeAddressContext context)
            => TryParseFrame(input, framerate, context, out var frame, out var error)
                ? frame
                : throw new FormatException(error);

        /// <summary> <see cref="TryParseDuration(JToken,int,out int,out string)"/>, throwing on failure.
        /// </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseDuration(JToken input, int framerate)
            => TryParseDuration(input, framerate, out var frames, out var error)
                ? frames : throw new FormatException(error);

        /// <summary> <see cref="TryParseDuration(JToken,int,TimeAddressContext,int,out int,out string)"/>,
        /// throwing on failure. </summary>
        /// <exception cref="FormatException"> The message names the input. </exception>
        public static int ParseDuration(JToken input, int framerate, TimeAddressContext context, int fromFrame)
            => TryParseDuration(input, framerate, context, fromFrame, out var frames, out var error)
                ? frames
                : throw new FormatException(error);

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

        private static bool TryParse(string text, double? number, int framerate, bool isCount,
            TimeAddressContext context, int fromFrame, out int result, out string error)
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

            if (isCount && TrySplitSum(text, out var terms))
                return TrySum(terms, input, framerate, context, fromFrame, out result, out error);

            if (!isCount && MusicalShift.Match(text) is { Success: true } moved
                         && !text.StartsWith(MarkerPrefix, StringComparison.OrdinalIgnoreCase))
                return TryMusicallyShiftedTime(moved, input, framerate, context, out result, out error);

            if (!isCount && FrameShift.Match(text) is { Success: true } shifted
                         && !text.StartsWith(FramePrefix, StringComparison.OrdinalIgnoreCase))
                return TryShiftedTime(shifted, input, framerate, context, out result, out error);

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

            if (isCount && MusicalCount.Match(text) is { Success: true } count)
            {
                if (context?.MusicUnavailable != null) return Fail(input, true, context.MusicUnavailable, out error);
                return TryMusicalCount(count, input, context, fromFrame, out result, out error);
            }

            if (!isCount && IsMusical(text))
            {
                if (context?.MusicUnavailable != null) return Fail(input, false, context.MusicUnavailable, out error);
                return TryMusicalTime(text, input, context, out result, out error);
            }

            var digits = text.StartsWith(FramePrefix, StringComparison.OrdinalIgnoreCase)
                ? text.Substring(FramePrefix.Length)
                : text;

            if (digits.Length > 0 && !char.IsLetter(digits[0])
                && double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var frames))
                return TryFromFrames(frames, input, isCount, out result, out error);

            error = isCount
                ? $"Cannot read duration {Quote(input)}: use frames (60, \"f:60\"), seconds (\"s:2\"), " +
                  "with a beat grid bars or beats (\"2bars\", \"3beats\"), or a sum of them (\"1bars+1f\")."
                : $"Cannot read time {Quote(input)}: use a frame (120, \"f:120\"), seconds (\"s:12.5\"), " +
                  "with a beat grid \"bar:17\", \"bar:17.3\" or \"beat:64.5\", a marker (\"marker:drop\"), " +
                  "any of them shifted by frames (\"bar:17-1f\") or along the grid (\"bar:17+2beats\").";
            return false;
        }

        // "2bars+1f": terms of a duration add up, each musical one measured from where the previous ends.
        private static bool TrySplitSum(string text, out string[] terms)
        {
            terms = null;
            if (text.IndexOf('+') <= 0) return false;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)) return false;
            terms = text.Split('+');
            foreach (var term in terms)
                if (term.Trim().Length == 0)
                    return false;
            return true;
        }

        private static bool TrySum(string[] terms, string input, int framerate, TimeAddressContext context,
            int fromFrame, out int result, out string error)
        {
            result = 0;
            double total = 0;
            foreach (var raw in terms)
            {
                var term = raw.Trim();
                var plain = term.EndsWith("f", StringComparison.OrdinalIgnoreCase)
                            && !MusicalCount.IsMatch(term)
                    ? term.Substring(0, term.Length - 1)
                    : term;
                var from = fromFrame == FrameRules.NoFrame ? FrameRules.NoFrame : fromFrame + (int)total;
                if (!TryParse(plain, null, framerate, true, context, from, out var part, out var reason))
                {
                    error = $"Cannot read duration {Quote(input)}: the term {Quote(term)} does not read " +
                            $"({reason})";
                    return false;
                }

                total += part;
            }

            return TryFromFrames(total, input, true, out result, out error);
        }

        // "bar:17+2beats", "s:10-1bar": any time but a marker (which reads its own shift), moved along the grid.
        private static bool TryMusicallyShiftedTime(Match shift, string input, int framerate,
            TimeAddressContext context, out int result, out string error)
        {
            result = 0;
            var baseText = shift.Groups["base"].Value.Trim();
            if (!TryParse(baseText, null, framerate, false, context, FrameRules.NoFrame, out var frame,
                    out var reason))
            {
                error = $"Cannot read time {Quote(input)}: {Quote(baseText)} does not read ({reason})";
                return false;
            }

            if (context?.MusicUnavailable != null) return Fail(input, false, context.MusicUnavailable, out error);
            if (context == null || !context.HasGrid) return Fail(input, false, NoGrid, out error);

            var sign = shift.Groups["sign"].Value == "-" ? -1 : 1;
            if (!TryShift(context.Grid, frame, sign, shift, input, out var moved, out error)) return false;
            return TryFromFrames(moved, input, false, out result, out error);
        }

        // "bar:17-1f", "marker:drop+3f": any time, moved by whole frames.
        private static bool TryShiftedTime(Match shift, string input, int framerate, TimeAddressContext context,
            out int result, out string error)
        {
            result = 0;
            var baseText = shift.Groups["base"].Value.Trim();
            if (!TryParse(baseText, null, framerate, false, context, FrameRules.NoFrame, out var frame,
                    out var reason))
            {
                error = $"Cannot read time {Quote(input)}: {Quote(baseText)} does not read ({reason})";
                return false;
            }

            var amount = double.Parse(shift.Groups["n"].Value, NumberStyles.None, CultureInfo.InvariantCulture);
            var sign = shift.Groups["sign"].Value == "-" ? -1 : 1;
            return TryFromFrames(frame + sign * amount, input, false, out result, out error);
        }

        private static bool IsMusical(string text) =>
            text.StartsWith(BarPrefix, StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(BeatPrefix, StringComparison.OrdinalIgnoreCase)
            || text.StartsWith(MarkerPrefix, StringComparison.OrdinalIgnoreCase);

        private static bool TryMusicalTime(string text, string input, TimeAddressContext context, out int result,
            out string error)
        {
            result = 0;
            if (text.StartsWith(MarkerPrefix, StringComparison.OrdinalIgnoreCase))
                return TryMarker(text.Substring(MarkerPrefix.Length).Trim(), input, context, out result, out error);

            if (context == null || !context.HasGrid) return Fail(input, false, NoGrid, out error);

            var grid = context.Grid;
            int frame;
            string reason;
            if (text.StartsWith(BarPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var body = text.Substring(BarPrefix.Length).Trim();
                var dot = body.IndexOf('.');
                var barText = dot < 0 ? body : body.Substring(0, dot);
                if (!TryWhole(barText, out var bar))
                    return Fail(input, false, "a bar is a whole number, as in \"bar:17\" or \"bar:17.3\"; " +
                                              "move it with \"bar:17+2beats\" or \"bar:17-1f\"", out error);

                if (dot < 0)
                {
                    if (!grid.TryFrameOfBar(bar, out frame, out reason)) return Fail(input, false, reason, out error);
                }
                else
                {
                    if (!TryWhole(body.Substring(dot + 1), out var beat))
                        return Fail(input, false, "the beat after the dot is a whole number, as in \"bar:17.3\"",
                            out error);
                    if (!grid.TryFrameOfBar(bar, beat, out frame, out reason))
                        return Fail(input, false, reason.Contains("has beats") ? reason + BeatAfterDot : reason,
                            out error);
                }
            }
            else
            {
                var body = text.Substring(BeatPrefix.Length).Trim();
                if (!TryNumber(body, out var beat))
                    return Fail(input, false, "a beat is a number, as in \"beat:64\" or \"beat:64.5\"", out error);
                if (!grid.TryFrameOfBeat(beat, out frame, out reason)) return Fail(input, false, reason, out error);
            }

            return TryFromFrames(frame, input, false, out result, out error);
        }

        private static bool TryMarker(string body, string input, TimeAddressContext context, out int result,
            out string error)
        {
            result = 0;
            if (body.Length == 0) return Fail(input, false, "a marker needs a name, as in \"marker:drop\"", out error);
            if (context == null || !context.HasMarkers)
                return Fail(input, false, "no markers can be resolved here", out error);

            if (context.TryGetMarker(body, out var frame))
                return TryFromFrames(frame, input, false, out result, out error);

            var shift = MarkerShift.Match(body);
            if (!shift.Success || !context.TryGetMarker(shift.Groups["name"].Value, out frame))
            {
                var name = shift.Success ? shift.Groups["name"].Value : body;
                return Fail(input, false, $"the level has no marker named {Quote(name)}", out error);
            }

            if (!context.HasGrid) return Fail(input, false, NoGrid, out error);

            var sign = shift.Groups["sign"].Value == "-" ? -1 : 1;
            if (!TryShift(context.Grid, frame, sign, shift, input, out var shifted, out error)) return false;
            return TryFromFrames(shifted, input, false, out result, out error);
        }

        private static bool TryMusicalCount(Match count, string input, TimeAddressContext context, int fromFrame,
            out int result, out string error)
        {
            result = 0;
            if (context == null || !context.HasGrid) return Fail(input, true, NoGrid, out error);

            var grid = context.Grid;
            var bars = IsBars(count);
            var amount = double.Parse(count.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            if (bars && amount != Math.Floor(amount))
                return Fail(input, true, "bars are whole, as in \"2bars\"; use beats for a fraction", out error);

            if (fromFrame == FrameRules.NoFrame)
            {
                if (!grid.TryUniform(out var framesPerBeat, out var beatsPerBar))
                    return Fail(input, true, "a duration in bars or beats depends on where it starts, and this " +
                                             "level's tempo changes; give it a start", out error);

                var frames = Math.Round(amount * (bars ? beatsPerBar : 1) * framesPerBeat,
                    MidpointRounding.AwayFromZero);
                return TryFromFrames(frames, input, true, out result, out error);
            }

            if (!TryShift(grid, fromFrame, 1, count, input, out var end, out error, true)) return false;
            return TryFromFrames(end - fromFrame, input, true, out result, out error);
        }

        private static bool TryShift(MusicGrid grid, int frame, int sign, Match match, string input, out int result,
            out string error, bool isCount = false)
        {
            var amount = double.Parse(match.Groups["n"].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
            string reason;
            bool ok;
            if (IsBars(match))
            {
                if (amount != Math.Floor(amount) || amount > int.MaxValue)
                {
                    result = 0;
                    return Fail(input, isCount, "bars are whole, as in \"+2bars\"; use beats for a fraction",
                        out error);
                }

                ok = grid.TryShiftBars(frame, sign * (int)amount, out result, out reason);
            }
            else
            {
                ok = grid.TryShiftBeats(frame, sign * amount, out result, out reason);
            }

            if (ok)
            {
                error = null;
                return true;
            }

            return Fail(input, isCount, reason, out error);
        }

        private static bool IsBars(Match match) =>
            match.Groups["unit"].Value.StartsWith("bar", StringComparison.OrdinalIgnoreCase);

        private static bool TryWhole(string text, out int value) =>
            int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out value);

        private static bool TryNumber(string text, out double value) =>
            double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
            && !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool Fail(string input, bool isCount, string reason, out string error)
        {
            error = $"Cannot read {(isCount ? "duration" : "time")} {Quote(input)}: {reason}.";
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
