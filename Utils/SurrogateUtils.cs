namespace BH.SDK.Utils
{
    // A char is a UTF-16 CODE UNIT, not a character: everything past the BMP - every emoji, every
    // musical and mathematical symbol - is a PAIR of them, and either half on its own is not a
    // character at all. Nothing in this format stores code points and nothing should; the only
    // places the distinction can bite are the ones that cut a string BY LENGTH or address it BY
    // INDEX, and this is the one implementation all of them share. Left to themselves they each
    // spelled it as Substring or an indexer and each produced the same broken half - a consumer then
    // renders U+FFFD, which is silent enough to reach a build.
    //
    // What this deliberately does NOT do is grapheme clustering (UAX #29): a combining mark is still
    // its own index, so "e" + U+0301 is two characters to every caller here. That is a documented
    // limit rather than an omission - full clustering needs a Unicode table plus a Unicode version
    // to keep current, and Docs/Issues/PRE_RELEASE_STRUCTURAL_ANALYSIS section 7 prices that out
    // against what it buys. Every caller inherits both the guarantee and the limit.

    /// <summary> Where a string may be cut without leaving half a character behind. </summary>
    public static class SurrogateUtils
    {
        /// <summary> Whether this code unit is the FIRST half of a surrogate pair. </summary>
        public static bool IsLead(char value) => value >= '\uD800' && value <= '\uDBFF';

        /// <summary> Whether this code unit is the SECOND half of a surrogate pair. </summary>
        public static bool IsTrail(char value) => value >= '\uDC00' && value <= '\uDFFF';

        /// <summary> Whether these two code units are the two halves of one character. </summary>
        public static bool IsPair(char lead, char trail) => IsLead(lead) && IsTrail(trail);

        // Only the cut itself is inspected, never the rest of the string: input that arrives already
        // broken - a hand-edited file, a foreign tool, a level cut by an older build - is cut where
        // it was asked and keeps the damage it came with. Repairing it here would mean this deciding
        // what a character the caller never wrote should have been.

        /// <summary> The largest length at or below <paramref name="maxLength"/> that does not split
        /// a surrogate pair. </summary>
        public static int ClampLength(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || maxLength <= 0) return 0;
            if (maxLength >= value.Length) return value.Length;

            return IsPair(value[maxLength - 1], value[maxLength]) ? maxLength - 1 : maxLength;
        }

        /// <summary> The string cut to <paramref name="maxLength"/> code units at worst, never
        /// through the middle of a character. Returns the same instance when it already fits, so a
        /// string within its bound costs nothing. </summary>
        public static string Truncate(string value, int maxLength)
        {
            if (value == null) return null;

            var length = ClampLength(value, maxLength);
            return length == value.Length ? value : value.Substring(0, length);
        }
    }
}
