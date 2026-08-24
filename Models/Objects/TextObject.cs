using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Text;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// A rect that renders text. Purely visual - unlike ShapeObject it carries no collider, so
    /// text can never kill the player, and its rect acts as the layout box rather than a hitbox.
    /// </summary>
    [RuleContainer]
    public class TextObject : RectObject, IModel<TextObject>, IUpdatable<TextObject>
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

        /// <summary> Font size track, animated independently of the object's Scales - one resizes
        /// glyphs, the other stretches the whole rendered block. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame)), RuleCollectionNoNullItems]
        [JsonProperty(Names.FontSize)]
        public List<FloatKey> FontSizes { get; set; }

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
            FontSizes = new List<FloatKey>();
            Fillments = new List<FillmentKey>();
            Appearings = new List<AppearingKey>();
            AppearingMask = TextRules.AppearingMask_Default;

            WordWrap = TextRules.WordWrap_Default;
            HorizontalAlignment = TextRules.HorizontalAlignment_Default;
            VerticalAlignment = TextRules.VerticalAlignment_Default;
        }
        public TextObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots,
            IString text, FontResourceId fontResourceId, List<Color4Key> colors, List<FloatKey> fontSizes,
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
        public override void Reset()
        {
            base.Reset();
            Text = new StringValue();
            FontResourceId = FontResourceId.Default;
            Colors.Clear();
            FontSizes.Clear();
            Fillments.Clear();
            Appearings.Clear();
            AppearingMask = TextRules.AppearingMask_Default;

            WordWrap = TextRules.WordWrap_Default;
            HorizontalAlignment = TextRules.HorizontalAlignment_Default;
            VerticalAlignment = TextRules.VerticalAlignment_Default;
        }
        
        public override object Clone() => CopyImpl();
        public override RectObject Copy() => CopyImpl();
        TextObject ICopyable<TextObject>.Copy() => CopyImpl();
        
        private TextObject CopyImpl() => new(ObjectId, ParentObjectId, Name, Active, Span, Layer,
            Positions.CopyList(), Rotations.CopyList(), Scales.CopyList(), Sizes.CopyList(),
            AnchorsMin.CopyList(), AnchorsMax.CopyList(), Pivots.CopyList(), Text.Copy(), FontResourceId,
            Colors.CopyList(), FontSizes.CopyList(), Fillments.CopyList(), Appearings.CopyList(),
            AppearingMask, WordWrap, HorizontalAlignment, VerticalAlignment);

        public void Update(TextObject src)
        {
            base.Update(src);

            Text = src.Text.Copy();
            FontResourceId = src.FontResourceId;
            Colors = src.Colors.CopyList();
            FontSizes = src.FontSizes.CopyList();
            Fillments = src.Fillments.CopyList();
            Appearings = src.Appearings.CopyList();
            AppearingMask = src.AppearingMask;

            WordWrap = src.WordWrap;
            HorizontalAlignment = src.HorizontalAlignment;
            VerticalAlignment = src.VerticalAlignment;
        }

        public void Pull(TextObject src)
        {
            base.Pull(src);

            Text = Text.PullFrom(src.Text);
            FontResourceId = src.FontResourceId;
            Colors = src.Colors.CopyList();
            FontSizes = src.FontSizes.CopyList();
            Fillments = src.Fillments.CopyList();
            Appearings = src.Appearings.CopyList();
            AppearingMask = src.AppearingMask;
            WordWrap = src.WordWrap;
            HorizontalAlignment = src.HorizontalAlignment;
            VerticalAlignment = src.VerticalAlignment;
        }

        public override bool Equals(object obj) => obj is TextObject value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(Text);
            hashCode.Add(FontResourceId);
            hashCode.Add(Colors.GetListHashCode());
            hashCode.Add(FontSizes.GetListHashCode());
            hashCode.Add(Fillments.GetListHashCode());
            hashCode.Add(Appearings.GetListHashCode());
            hashCode.Add(AppearingMask);
            hashCode.Add(WordWrap);
            hashCode.Add((int)HorizontalAlignment);
            hashCode.Add((int)VerticalAlignment);
            return hashCode.ToHashCode();
        }

        public bool Equals(TextObject other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            
            var result = EqualsObject(other)
                         && EqualsTextObject(other);
            return result;
        }
        public override bool Equals(RectObject other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            switch (other)
            {
                case TextObject textObject:
                {
                    var result = EqualsObject(textObject)
                                 && EqualsTextObject(textObject);
                    return result;
                }
                default:
                {
                    var result = EqualsObject(other);
                    return result;
                }
            }
        }
        
        private bool EqualsTextObject(TextObject other)
        {
            var result = Text.Equals(other.Text)
                         && FontResourceId.Equals(other.FontResourceId)
                         && Colors.ListEquals(other.Colors)
                         && FontSizes.ListEquals(other.FontSizes)
                         && Fillments.ListEquals(other.Fillments)
                         && Appearings.ListEquals(other.Appearings)
                         && AppearingMask == other.AppearingMask
                         && WordWrap == other.WordWrap
                         && HorizontalAlignment == other.HorizontalAlignment
                         && VerticalAlignment == other.VerticalAlignment;
            return result;
        }
    }
}