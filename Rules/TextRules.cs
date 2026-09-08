using BH.SDK.Models.Enums.Text;

// ReSharper disable InconsistentNaming

namespace BH.SDK.Rules
{
    /// <summary> What an authored text object may be: its size bounds, its fallbacks, and how long its string
    /// may get. </summary>
    public static class TextRules
    {
        /// <summary> The size X used when none is authored. </summary>
        public const float Size_X_Fallback = 1f;
        /// <summary> The size Y used when none is authored. </summary>
        public const float Size_Y_Fallback = 1f;
        /// <summary> The font size used when none is authored, read by FontSizeKey, FontSizeKeyTests. </summary>
        public const float FontSize_Fallback = 1f;

        // Font size is a keyframed float, so it otherwise inherits the generic +/-1e6 value range -
        // a range in which a negative or astronomically large size is legal data. Zero is allowed:
        // it means "invisible this frame", which is a normal way to animate text in and out.

        /// <summary> Lowest font size allowed. </summary>
        public const float MinFontSize = 0f;
        /// <summary> Highest font size allowed. </summary>
        public const float MaxFontSize = 1000f;

        // The band an AutoFontSizeKey starts life with, and the two numbers are not symmetrical: the
        // max is the size the text is DRAWN at while it fits, so it matches FontSize_Fallback and a
        // key switched to auto sizing looks unchanged until the text stops fitting. The min is a
        // floor, and 0 means "no floor" - shrink as far as it takes rather than overflow.

        /// <summary> The auto font size min used when nothing says otherwise, read by AutoFontSizeKey, FontSizeKeyTests. </summary>
        public const float AutoFontSize_Min_Default = 0f;
        /// <summary> The auto font size max used when nothing says otherwise, read by AutoFontSizeKey, FontSizeKeyTests. </summary>
        public const float AutoFontSize_Max_Default = 1f;


        // Cap on ONE font's distinct-character set in LevelHints.FontCharacters. That set is a
        // glyph-atlas warm-up hint and nothing else - a consumer that ignores it still renders every
        // character - so this bounds what a builder writes rather than what a reader must accept, and
        // no [RuleXxx] enforces it. 512 covers Latin, Cyrillic and Greek together with room to
        // spare; a CJK level overruns it and simply warms a prefix, which is exactly the graceful
        // degradation an advisory value should have.

        /// <summary> Upper bound of CachedFontText.Characters. </summary>
        public const int MaxFontBufferSize = 512;

        /// <summary> The default for word wrap, read by ABObjectExporter, TextObject. </summary>
        public const bool WordWrap_Default = true;
        /// <summary> What TextObject.HorizontalAlignment holds when nothing says otherwise. </summary>
        public const TextObjectHorizontalAlignment HorizontalAlignment_Default = TextObjectHorizontalAlignment.Center;
        /// <summary> What TextObject.VerticalAlignment holds when nothing says otherwise. </summary>
        public const TextObjectVerticalAlignment VerticalAlignment_Default = TextObjectVerticalAlignment.Middle;

        // The two fallbacks below are what keeps every text object written before these tracks
        // existed looking exactly as it did: an empty track must read as "this effect is off", and
        // off means fully written and nothing hidden. Getting either one backwards makes every
        // existing level's text vanish on the next load.

        /// <summary> Fraction of the text written. Empty track = all of it. </summary>
        public const float Fillment_Fallback = 1f;

        /// <summary> Lower bound of FillmentKey.Value. </summary>
        public const float MinFillment = 0f;
        /// <summary> Upper bound of FillmentKey.Value. </summary>
        public const float MaxFillment = 1f;

        /// <summary> Fraction of the characters hidden behind the mask. Empty track = none. </summary>
        public const float Appearing_Fallback = 0f;

        /// <summary> Lower bound of AppearingKey.Value. </summary>
        public const float MinAppearing = 0f;
        /// <summary> Upper bound of AppearingKey.Value. </summary>
        public const float MaxAppearing = 1f;

        /// <summary> What FillmentKey.Direction holds when nothing says otherwise. </summary>
        public const TextFillDirection FillDirection_Default = TextFillDirection.Forward;
        /// <summary> What AppearingKey.Mode holds when nothing says otherwise. </summary>
        public const TextAppearingMode AppearingMode_Default = TextAppearingMode.Random;

        // A SET rather than a single character: one character gives the classic "XXXX", several give
        // a scatter picked per character index, which is the difference between a censored line and a
        // decoding one. Bounded because the player copies it into a fixed-size slot per text, and
        // because every character in it has to be warmed into the font's glyph atlas (see
        // Services/FontCharacterService) whether or not it is ever drawn.

        /// <summary> The appearing mask used when nothing says otherwise, read by FontCharacterServiceTests, TextObject. </summary>
        public const string AppearingMask_Default = "X";
        /// <summary> Upper bound of TextObject.AppearingMask. </summary>
        public const int MaxAppearingMask = 16;
    }
}