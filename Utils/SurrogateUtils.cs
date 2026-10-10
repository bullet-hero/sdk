namespace BH.SDK.Utils
{
    // A char is a UTF-16 code unit: cutting or indexing by length must not split a surrogate pair. No
    // grapheme clustering (a combining mark is its own index) - a documented limit (Docs/UTILS_DESIGN.md).

    /// <summary> Where a string may be cut without leaving half a character behind. </summary>
    public static class SurrogateUtils
    {
        /// <summary> Whether this code unit is the FIRST half of a surrogate pair. </summary>
        public static bool IsLead(char value) => value >= '\uD800' && value <= '\uDBFF';

        /// <summary> Whether this code unit is the SECOND half of a surrogate pair. </summary>
        public static bool IsTrail(char value) => value >= '\uDC00' && value <= '\uDFFF';

        /// <summary> Whether these two code units are the two halves of one character. </summary>
        public static bool IsPair(char lead, char trail) => IsLead(lead) && IsTrail(trail);

        // Only the cut is inspected: input that arrives already broken keeps its damage.

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
