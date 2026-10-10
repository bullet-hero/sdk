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

        // An AutoFontSizeKey's band: max is the size DRAWN while the text fits (matches FontSize_Fallback,
        // so switching to auto looks unchanged); min is a floor, 0 meaning none.

        /// <summary> The auto font size min used when nothing says otherwise, read by AutoFontSizeKey, FontSizeKeyTests. </summary>
        public const float AutoFontSize_Min_Default = 0f;
        /// <summary> The auto font size max used when nothing says otherwise, read by AutoFontSizeKey, FontSizeKeyTests. </summary>
        public const float AutoFontSize_Max_Default = 1f;


        // Cap on one font's character set in LevelHints.FontCharacters: a glyph-atlas warm-up hint, so
        // this bounds what a builder writes, not what a reader accepts, and no [RuleXxx] enforces it.
        // 512 covers Latin, Cyrillic and Greek; a CJK level warms a prefix.

        /// <summary> Upper bound of CachedFontText.Characters. </summary>
        public const int MaxFontBufferSize = 512;

        /// <summary> The default for word wrap, read by TextObject. </summary>
        public const bool WordWrap_Default = true;
        /// <summary> What TextObject.HorizontalAlignment holds when nothing says otherwise. </summary>
        public const TextObjectHorizontalAlignment HorizontalAlignment_Default = TextObjectHorizontalAlignment.Center;
        /// <summary> What TextObject.VerticalAlignment holds when nothing says otherwise. </summary>
        public const TextObjectVerticalAlignment VerticalAlignment_Default = TextObjectVerticalAlignment.Middle;

        // The fallbacks keep text written before these tracks existed identical: an empty track means
        // "effect off" = fully written, nothing hidden. Inverting either makes old text vanish.

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

        // A SET, not a character: several give a scatter picked per character index. Bounded because the
        // player copies it into a fixed slot per text and every member is warmed into the glyph atlas.

        /// <summary> The appearing mask used when nothing says otherwise, read by FontCharacterServiceTests, TextObject. </summary>
        public const string AppearingMask_Default = "X";
        /// <summary> Upper bound of TextObject.AppearingMask. </summary>
        public const int MaxAppearingMask = 16;
    }
}