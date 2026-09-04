using BH.SDK.Models.Enums.Text;

// ReSharper disable InconsistentNaming

namespace BH.SDK.Rules
{
    public static class TextRules
    {
        public const float Size_X_Fallback = 1f;
        public const float Size_Y_Fallback = 1f;
        public const float FontSize_Fallback = 1f;

        // Font size is a keyframed float, so it otherwise inherits the generic +/-1e6 value range -
        // a range in which a negative or astronomically large size is legal data. Zero is allowed:
        // it means "invisible this frame", which is a normal way to animate text in and out.
        public const float MinFontSize = 0f;
        public const float MaxFontSize = 1000f;

        // The band an AutoFontSizeKey starts life with, and the two numbers are not symmetrical: the
        // max is the size the text is DRAWN at while it fits, so it matches FontSize_Fallback and a
        // key switched to auto sizing looks unchanged until the text stops fitting. The min is a
        // floor, and 0 means "no floor" - shrink as far as it takes rather than overflow.
        public const float AutoFontSize_Min_Default = 0f;
        public const float AutoFontSize_Max_Default = 1f;


        // Cap on ONE font's distinct-character set in LevelHints.FontCharacters. That set is a
        // glyph-atlas warm-up hint and nothing else - a consumer that ignores it still renders every
        // character - so this bounds what a builder writes rather than what a reader must accept, and
        // no [RuleXxx] enforces it. 512 covers Latin, Cyrillic and Greek together with room to
        // spare; a CJK level overruns it and simply warms a prefix, which is exactly the graceful
        // degradation an advisory value should have.
        public const int MaxFontBufferSize = 512;

        public const bool WordWrap_Default = true;
        public const TextObjectHorizontalAlignment HorizontalAlignment_Default = TextObjectHorizontalAlignment.Center;
        public const TextObjectVerticalAlignment VerticalAlignment_Default = TextObjectVerticalAlignment.Middle;

        // The two fallbacks below are what keeps every text object written before these tracks
        // existed looking exactly as it did: an empty track must read as "this effect is off", and
        // off means fully written and nothing hidden. Getting either one backwards makes every
        // existing level's text vanish on the next load.

        /// <summary> Fraction of the text written. Empty track = all of it. </summary>
        public const float Fillment_Fallback = 1f;

        public const float MinFillment = 0f;
        public const float MaxFillment = 1f;

        /// <summary> Fraction of the characters hidden behind the mask. Empty track = none. </summary>
        public const float Appearing_Fallback = 0f;

        public const float MinAppearing = 0f;
        public const float MaxAppearing = 1f;

        public const TextFillDirection FillDirection_Default = TextFillDirection.Forward;
        public const TextAppearingMode AppearingMode_Default = TextAppearingMode.Random;

        // A SET rather than a single character: one character gives the classic "XXXX", several give
        // a scatter picked per character index, which is the difference between a censored line and a
        // decoding one. Bounded because the player copies it into a fixed-size slot per text, and
        // because every character in it has to be warmed into the font's glyph atlas (see
        // Services/FontCharacterService) whether or not it is ever drawn.
        public const string AppearingMask_Default = "X";
        public const int MaxAppearingMask = 16;
    }
}