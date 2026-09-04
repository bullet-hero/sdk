using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// A rect that renders text. Purely visual - unlike ShapeObject it carries no collider, so
    /// text can never kill the player, and its rect acts as the layout box rather than a hitbox.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TextObject : RectObject, IModel<TextObject>, IUpdatable<TextObject>
    {
        public override ObjectType GetModelType() => ObjectType.TextObject;

        /// <summary> The text to show, localizable - a level can read differently per language
        /// without duplicating the object. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(ValueRules.MaxGameString)]
        [JsonProperty(Names.Text)]
        public IString Text { get; set; }

        // positive with 0 - game-defined (0 is NotoSans), negative - user-defined
        // more about resourceId and how it works, read in Resource.cs file

        /// <summary> Typeface, defaulting to the bundled one so a level always renders even without
        /// its own font shipped alongside. </summary>
        [RuleIPrimitiveIntNotNull, RuleReferenceExists(ResourceReferenceKind.Font)]
        [JsonProperty(Names.FontResourceId)]
        public FontResourceId FontResourceId { get; set; }

        /// <summary> Tint track. Flat Color4Key only, not the four-corner family a ShapeObject
        /// uses - glyphs have no quad to grade across. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(Color4Key.Frame))]
        [JsonProperty(Names.Color)]
        public List<Color4Key> Colors { get; set; }

        // THE ONLY TRACK HERE WHOSE KEY CLASS IS POLYMORPHIC, and it is what makes auto sizing a
        // property of a FRAME rather than of the object: a FontSizeKey draws at the size it carries,
        // an AutoFontSizeKey is fitted into the band it carries, and a track mixes the two freely.
        // Which one a frame between two keys is in follows the LATER key of the pair, exactly as the
        // Fillment/Appearing modes below do - and across that boundary a plain key blends as the
        // degenerate band min = max = Value, so nothing jumps.

        /// <summary> Font size track, animated independently of the object's Scales - one resizes
        /// glyphs, the other stretches the whole rendered block. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(Keyframe.Frame)), RuleCollectionNoNullItems]
        [JsonProperty(Names.FontSize)]
        public List<IFontSizeKey> FontSizes { get; set; }

        // The two tracks below are per-character effects rather than transform ones, so they are
        // resolved by the player's text job over the string itself rather than by the usual
        // keyframe -> transform path. Both default to "off" through TextRules' fallbacks, which is
        // what keeps text authored before they existed unchanged.
        //
        // Their direction/mode lives on each KEY rather than on the object, exactly like Ease: a
        // track can start writing forward and finish from the centre without being split across two
        // objects, and between two keys the later one's setting wins.

        /// <summary> How much of the text is written over time, 0..1, each key carrying the
        /// direction it is written from. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(FillmentKey.Frame)), RuleCollectionNoNullItems]
        [JsonProperty(Names.Fillment)]
        public List<FillmentKey> Fillments { get; set; }

        /// <summary> How much of the text hides behind AppearingMask over time, 0..1, each key
        /// carrying the order it hides in. Length is unchanged - a hidden character is substituted,
        /// not removed. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(AppearingKey.Frame)), RuleCollectionNoNullItems]
        [JsonProperty(Names.Appearing)]
        public List<AppearingKey> Appearings { get; set; }

        /// <summary> Characters a hidden one is replaced by, picked per character index. One
        /// character censors, several decode. Object-wide, unlike the mode - it is the text's own
        /// alphabet, not something to animate. </summary>
        [RuleNotNull, RuleStringMax(TextRules.MaxAppearingMask)]
        [JsonProperty(Names.AppearingMask)]
        public string AppearingMask { get; set; }

        /// <summary> Whether long lines wrap at the rect's width instead of overflowing it. </summary>
        [JsonProperty(Names.WordWrap)]
        public bool WordWrap { get; set; }

        /// <summary> Horizontal placement of the text inside its rect. </summary>
        [RuleEnumValid(TextRules.HorizontalAlignment_Default)]
        [JsonProperty(Names.HorizontalAlignment)]
        public TextObjectHorizontalAlignment HorizontalAlignment { get; set; }

        /// <summary> Vertical placement of the text inside its rect. </summary>
        [RuleEnumValid(TextRules.VerticalAlignment_Default)]
        [JsonProperty(Names.VerticalAlignment)]
        public TextObjectVerticalAlignment VerticalAlignment { get; set; }

        public TextObject()
        {
            Text = new StringValue();
            FontResourceId = FontResourceId.Default;
            Colors = new List<Color4Key>();
            FontSizes = new List<IFontSizeKey>();
            Fillments = new List<FillmentKey>();
            Appearings = new List<AppearingKey>();
            AppearingMask = TextRules.AppearingMask_Default;

            WordWrap = TextRules.WordWrap_Default;
            HorizontalAlignment = TextRules.HorizontalAlignment_Default;
            VerticalAlignment = TextRules.VerticalAlignment_Default;
        }

        public TextObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span,
            int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots,
            IString text, FontResourceId fontResourceId, List<Color4Key> colors, List<IFontSizeKey> fontSizes,
            List<FillmentKey> fillments, List<AppearingKey> appearings, string appearingMask, bool wordWrap,
            TextObjectHorizontalAlignment horizontalAlignment, TextObjectVerticalAlignment verticalAlignment)
            : base(objectId, parentObjectId, name, active, span, layer,
                positions, rotations, scales, sizes, anchorsMin, anchorsMax, pivots)
        {
            Text = text;
            FontResourceId = fontResourceId;
            Colors = colors;
            FontSizes = fontSizes;
            Fillments = fillments;
            Appearings = appearings;
            AppearingMask = appearingMask;
            WordWrap = wordWrap;
            HorizontalAlignment = horizontalAlignment;
            VerticalAlignment = verticalAlignment;
        }
    }
}