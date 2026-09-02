using System;
using System.Runtime.CompilerServices;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
// ReSharper disable ArrangeAccessorOwnerBody

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A literal RGBA color - the plain IColor4 variant, and the only color model ThemeData stores
    /// in its palette (a theme slot must be a real color, never another reference).
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color4Value : IColor4, IModel<Color4Value>
    {
        /// <summary> Red channel, normalized 0..1 (not 0..255). </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelR)]
        public float R { get; set; }

        /// <summary> Green channel, normalized 0..1. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelG)]
        public float G { get; set; }

        /// <summary> Blue channel, normalized 0..1. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelB)]
        public float B { get; set; }

        /// <summary> Opacity, 0 = invisible, 1 = opaque. The only channel IColor3 lacks. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelA)]
        public float A { get; set; }

        public Color4Value()
        {
            R = ValueRules.MaxColor;
            G = ValueRules.MaxColor;
            B = ValueRules.MaxColor;
            A = ValueRules.MaxColor;
        }
        public Color4Value(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ColorType GetModelType() => ColorType.Value;

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9411765f, 0.9725491f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value aliceBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9411765f, 0.9725491f, 1f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9803922f, 0.9215687f, 0.8431373f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value antiqueWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9803922f, 0.9215687f, 0.8431373f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4980392f, 1f, 0.8313726f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value aquamarine
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4980392f, 1f, 0.8313726f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9411765f, 1f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value azure
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.9411765f, 1f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9607844f, 0.9607844f, 0.8627452f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value beige
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9607844f, 0.9607844f, 272f * (float)Math.E / 857f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.8941177f, 0.7686275f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value bisque
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.8941177f, 0.7686275f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value black
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9215687f, 0.8039216f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value blanchedAlmond
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9215687f, 0.8039216f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value blue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5411765f, 0.1686275f, 0.8862746f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value blueViolet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5411765f, 0.1686275f, 0.8862746f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6470588f, 0.1647059f, 0.1647059f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value brown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6470588f, 0.1647059f, 0.1647059f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8705883f, 0.7215686f, 0.5294118f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value burlywood
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8705883f, 0.7215686f, 0.5294118f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.372549f, 0.6196079f, 0.627451f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value cadetBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.372549f, 0.6196079f, 0.627451f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4980392f, 1f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value chartreuse
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.4980392f, 1f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8235295f, 0.4117647f, 0.1176471f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value chocolate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8235295f, 0.4117647f, 0.1176471f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 0f, 0f)
        ///           </para>
        /// </summary>
        public static Color4Value clear
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 0.0f, 0.0f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.4980392f, 0.3137255f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value coral
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.4980392f, 0.3137255f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.3921569f, 0.5843138f, 0.9294118f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value cornflowerBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.3921569f, 0.5843138f, 0.9294118f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9725491f, 0.8627452f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value cornsilk
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9725491f, 272f * (float)Math.E / 857f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8627452f, 0.07843138f, 0.2352941f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value crimson
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(272f * (float)Math.E / 857f, 0.07843138f, 0.2352941f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 1f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value cyan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 1f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 0.5450981f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 0.5450981f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.5450981f, 0.5450981f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkCyan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.0f, 0.5450981f, 0.5450981f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7215686f, 0.5254902f, 0.04313726f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkGoldenRod
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7215686f, 0.5254902f, 0.04313726f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6627451f, 0.6627451f, 0.6627451f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6627451f, 0.6627451f, 0.6627451f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.3921569f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.3921569f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7411765f, 0.7176471f, 0.4196079f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkKhaki
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7411765f, 0.7176471f, 0.4196079f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5450981f, 0f, 0.5450981f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkMagenta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5450981f, 0.0f, 0.5450981f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.3333333f, 0.4196079f, 0.1843137f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkOliveGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.3333333f, 0.4196079f, 0.1843137f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.5490196f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkOrange
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.5490196f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6f, 0.1960784f, 0.8000001f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkOrchid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6f, 0.1960784f, 0.8000001f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5450981f, 0f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.5450981f, 0.0f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9137256f, 0.5882353f, 0.4784314f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkSalmon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9137256f, 0.5882353f, 0.4784314f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5607843f, 0.7372549f, 0.5607843f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkSeaGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5607843f, 0.7372549f, 0.5607843f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.282353f, 0.2392157f, 0.5450981f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkSlateBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.282353f, 0.2392157f, 0.5450981f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1843137f, 0.3098039f, 0.3098039f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkSlateGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1843137f, 0.3098039f, 0.3098039f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.8078432f, 0.8196079f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkTurquoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.0f, 0.8078432f, 0.8196079f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5803922f, 0f, 0.8274511f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value darkViolet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5803922f, 0.0f, 0.8274511f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.07843138f, 0.5764706f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value deepPink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.07843138f, 0.5764706f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.7490196f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value deepSkyBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.7490196f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4117647f, 0.4117647f, 0.4117647f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value dimGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4117647f, 0.4117647f, 0.4117647f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1176471f, 0.5647059f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value dodgerBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1176471f, 0.5647059f, 1f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6980392f, 0.1333333f, 0.1333333f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value firebrick
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6980392f, 0.1333333f, 0.1333333f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9803922f, 0.9411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value floralWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9803922f, 0.9411765f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1333333f, 0.5450981f, 0.1333333f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value forestGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1333333f, 0.5450981f, 0.1333333f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8627452f, 0.8627452f, 0.8627452f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gainsboro
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return new Color4Value(272f * (float)Math.E / 857f, 272f * (float)Math.E / 857f, 272f * (float)Math.E / 857f,
                    1f);
            }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9725491f, 0.9725491f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value ghostWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9725491f, 0.9725491f, 1f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.8431373f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gold
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.8431373f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.854902f, 0.6470588f, 0.1254902f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value goldenRod
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.854902f, 0.6470588f, 0.1254902f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray => gray5;

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value grey => gray5;

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1f, 0.1f, 0.1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.1f, 0.1f, 0.1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.2f, 0.2f, 0.2f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.2f, 0.2f, 0.2f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.3f, 0.3f, 0.3f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.3f, 0.3f, 0.3f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4f, 0.4f, 0.4f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray4
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.4f, 0.4f, 0.4f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray5
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.5f, 0.5f, 0.5f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6f, 0.6f, 0.6f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray6
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.6f, 0.6f, 0.6f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7f, 0.7f, 0.7f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray7
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.7f, 0.7f, 0.7f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8f, 0.8f, 0.8f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray8
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.8f, 0.8f, 0.8f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9f, 0.9f, 0.9f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value gray9
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.9f, 0.9f, 0.9f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 1f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value green
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 1f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6784314f, 1f, 0.1843137f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value greenYellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6784314f, 1f, 0.1843137f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9411765f, 1f, 0.9411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value honeydew
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9411765f, 1f, 0.9411765f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.4117647f, 0.7058824f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value hotPink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.4117647f, 0.7058824f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8039216f, 0.3607843f, 0.3607843f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value indianRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8039216f, 0.3607843f, 0.3607843f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.2941177f, 0f, 0.509804f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value indigo
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.2941177f, 0.0f, 0.509804f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 1f, 0.9411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value ivory
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 1f, 0.9411765f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9411765f, 0.9019608f, 0.5490196f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value khaki
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9411765f, 0.9019608f, 0.5490196f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9019608f, 0.9019608f, 0.9803922f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lavender
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9019608f, 0.9019608f, 0.9803922f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9411765f, 0.9607844f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lavenderBlush
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9411765f, 0.9607844f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4862745f, 0.9882354f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lawnGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4862745f, 0.9882354f, 0.0f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9803922f, 0.8039216f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lemonChiffon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9803922f, 0.8039216f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6784314f, 0.8470589f, 0.9019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6784314f, 0.8470589f, 0.9019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9411765f, 0.5019608f, 0.5019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightCoral
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9411765f, 0.5019608f, 0.5019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8784314f, 1f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightCyan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.8784314f, 1f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9333334f, 0.8666667f, 0.509804f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightGoldenRod
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9333334f, 0.8666667f, 0.509804f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9803922f, 0.9803922f, 0.8235295f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightGoldenRodYellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9803922f, 0.9803922f, 0.8235295f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8274511f, 0.8274511f, 0.8274511f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8274511f, 0.8274511f, 0.8274511f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5647059f, 0.9333334f, 0.5647059f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5647059f, 0.9333334f, 0.5647059f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.7137255f, 0.7568628f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightPink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.7137255f, 0.7568628f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.627451f, 0.4784314f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSalmon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.627451f, 0.4784314f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1254902f, 0.6980392f, 0.6666667f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSeaGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1254902f, 0.6980392f, 0.6666667f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5294118f, 0.8078432f, 0.9803922f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSkyBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5294118f, 0.8078432f, 0.9803922f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5176471f, 0.4392157f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSlateBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5176471f, 0.4392157f, 1f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4666667f, 0.5333334f, 0.6f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSlateGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4666667f, 0.5333334f, 0.6f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6901961f, 0.7686275f, 0.8705883f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightSteelBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6901961f, 0.7686275f, 0.8705883f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 1f, 0.8784314f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value lightYellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 1f, 0.8784314f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1960784f, 0.8039216f, 0.1960784f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value limeGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1960784f, 0.8039216f, 0.1960784f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9803922f, 0.9411765f, 0.9019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value linen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9803922f, 0.9411765f, 0.9019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value magenta
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.0f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6901961f, 0.1882353f, 0.3764706f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value maroon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6901961f, 0.1882353f, 0.3764706f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4f, 0.8039216f, 0.6666667f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumAquamarine
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4f, 0.8039216f, 0.6666667f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 0.8039216f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 0.8039216f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7294118f, 0.3333333f, 0.8274511f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumOrchid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7294118f, 0.3333333f, 0.8274511f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5764706f, 0.4392157f, 0.8588236f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumPurple
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5764706f, 0.4392157f, 0.8588236f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.2352941f, 0.7019608f, 0.4431373f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumSeaGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.2352941f, 0.7019608f, 0.4431373f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.482353f, 0.4078432f, 0.9333334f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumSlateBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.482353f, 0.4078432f, 0.9333334f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.9803922f, 0.6039216f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumSpringGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.0f, 0.9803922f, 0.6039216f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.282353f, 0.8196079f, 0.8000001f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumTurquoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.282353f, 0.8196079f, 0.8000001f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7803922f, 0.08235294f, 0.5215687f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mediumVioletRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7803922f, 0.08235294f, 0.5215687f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.09803922f, 0.09803922f, 0.4392157f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value midnightBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.09803922f, 0.09803922f, 0.4392157f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9607844f, 1f, 0.9803922f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mintCream
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9607844f, 1f, 0.9803922f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.8941177f, 0.882353f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value mistyRose
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.8941177f, 0.882353f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.8941177f, 0.7098039f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value moccasin
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.8941177f, 0.7098039f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.8705883f, 0.6784314f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value navajoWhite
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.8705883f, 0.6784314f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0f, 0.5019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value navyBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 0.0f, 0.5019608f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9921569f, 0.9607844f, 0.9019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value oldLace
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9921569f, 0.9607844f, 0.9019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5019608f, 0.5019608f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value olive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5019608f, 0.5019608f, 0.0f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4196079f, 0.5568628f, 0.1372549f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value oliveDrab
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4196079f, 0.5568628f, 0.1372549f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.6470588f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value orange
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.6470588f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.2705882f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value orangeRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.2705882f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.854902f, 0.4392157f, 0.8392158f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value orchid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.854902f, 0.4392157f, 0.8392158f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9333334f, 0.909804f, 0.6666667f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value paleGoldenRod
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9333334f, 0.909804f, 0.6666667f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5960785f, 0.9843138f, 0.5960785f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value paleGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5960785f, 0.9843138f, 0.5960785f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6862745f, 0.9333334f, 0.9333334f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value paleTurquoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6862745f, 0.9333334f, 0.9333334f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8588236f, 0.4392157f, 0.5764706f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value paleVioletRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8588236f, 0.4392157f, 0.5764706f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.937255f, 0.8352942f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value papayaWhip
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.937255f, 0.8352942f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.854902f, 0.7254902f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value peachPuff
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.854902f, 0.7254902f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8039216f, 0.5215687f, 0.2470588f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value peru
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8039216f, 0.5215687f, 0.2470588f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.7529413f, 0.7960785f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value pink
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.7529413f, 0.7960785f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8666667f, 0.627451f, 0.8666667f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value plum
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8666667f, 0.627451f, 0.8666667f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6901961f, 0.8784314f, 0.9019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value powderBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6901961f, 0.8784314f, 0.9019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.627451f, 0.1254902f, 0.9411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value purple
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.627451f, 0.1254902f, 0.9411765f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4f, 0.2f, 0.6f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value rebeccaPurple
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.4f, 0.2f, 0.6f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0f, 0f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value red
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 0.0f, 0.0f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7372549f, 0.5607843f, 0.5607843f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value rosyBrown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7372549f, 0.5607843f, 0.5607843f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.254902f, 0.4117647f, 0.882353f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value royalBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.254902f, 0.4117647f, 0.882353f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5450981f, 0.2705882f, 0.07450981f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value saddleBrown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5450981f, 0.2705882f, 0.07450981f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9803922f, 0.5019608f, 0.4470589f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value salmon
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9803922f, 0.5019608f, 0.4470589f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9568628f, 0.6431373f, 0.3764706f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value sandyBrown
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9568628f, 0.6431373f, 0.3764706f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1803922f, 0.5450981f, 0.3411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value seaGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1803922f, 0.5450981f, 0.3411765f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9607844f, 0.9333334f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value seashell
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9607844f, 0.9333334f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.627451f, 0.3215686f, 0.1764706f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value sienna
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.627451f, 0.3215686f, 0.1764706f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.7529413f, 0.7529413f, 0.7529413f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value silver
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.7529413f, 0.7529413f, 0.7529413f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5294118f, 0.8078432f, 0.9215687f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value skyBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5294118f, 0.8078432f, 0.9215687f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4156863f, 0.3529412f, 0.8039216f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value slateBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4156863f, 0.3529412f, 0.8039216f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.4392157f, 0.5019608f, 0.5647059f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value slateGray
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.4392157f, 0.5019608f, 0.5647059f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9803922f, 0.9803922f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value snow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9803922f, 0.9803922f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8627452f, 0.1921569f, 0.1960784f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value softRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(272f * (float)Math.E / 857f, 0.1921569f, 0.1960784f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.1882353f, 0.682353f, 0.7490196f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value softBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.1882353f, 0.682353f, 0.7490196f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.5490196f, 0.7882354f, 0.1411765f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value softGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.5490196f, 0.7882354f, 0.1411765f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.9333334f, 0.5490196f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value softYellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.9333334f, 0.5490196f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 1f, 0.4980392f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value springGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(0.0f, 1f, 0.4980392f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.2745098f, 0.509804f, 0.7058824f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value steelBlue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.2745098f, 0.509804f, 0.7058824f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8235295f, 0.7058824f, 0.5490196f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value tan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8235295f, 0.7058824f, 0.5490196f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0f, 0.5019608f, 0.5019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value teal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.0f, 0.5019608f, 0.5019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8470589f, 0.7490196f, 0.8470589f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value thistle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8470589f, 0.7490196f, 0.8470589f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.3882353f, 0.2784314f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value tomato
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.3882353f, 0.2784314f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.2509804f, 0.8784314f, 0.8156863f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value turquoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.2509804f, 0.8784314f, 0.8156863f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9333334f, 0.509804f, 0.9333334f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value violet
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9333334f, 0.509804f, 0.9333334f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.8156863f, 0.1254902f, 0.5647059f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value violetRed
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.8156863f, 0.1254902f, 0.5647059f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9607844f, 0.8705883f, 0.7019608f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value wheat
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9607844f, 0.8705883f, 0.7019608f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 1f, 1f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value white
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new Color4Value(1f, 1f, 1f, 1f);
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.9607844f, 0.9607844f, 0.9607844f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value whiteSmoke
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.9607844f, 0.9607844f, 0.9607844f, 1f); }
        }

        /// <summary>
        ///   <para>ColorValue Preset of RGBA(1f, 0.92f, 0.016f, 1f).</para>
        /// </summary>
        public static Color4Value yellow
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.92156863f, 0.015686275f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(0.6039216f, 0.8039216f, 0.1960784f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value yellowGreen
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(0.6039216f, 0.8039216f, 0.1960784f, 1f); }
        }

        /// <summary>
        ///   <para>
        ///               ColorValue Preset of RGBA(1f, 0.92f, 0.016f, 1f)
        ///           </para>
        /// </summary>
        public static Color4Value yellowNice
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return new Color4Value(1f, 0.92156863f, 0.015686275f, 1f); }
        }
    }
}