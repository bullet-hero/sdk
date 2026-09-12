using Unity.Mathematics;

// ReSharper disable InconsistentNaming

namespace BH.SDK
{
    public struct color2
    {
        public float4 value1;
        public float4 value2;
        
        public const int ByteSize = 32; // sizeof(float4 * 2)
        
        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9411765f, 0.9725491f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) aliceBlue = (color.aliceBlue, color.aliceBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9803922f, 0.9215687f, 0.8431373f, 1f)
        /// </summary>
        public static readonly (float4, float4) antiqueWhite = (color.antiqueWhite, color.antiqueWhite);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4980392f, 1f, 0.8313726f, 1f)
        /// </summary>
        public static readonly (float4, float4) aquamarine = (color.aquamarine, color.aquamarine);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9411765f, 1f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) azure = (color.azure, color.azure);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9607844f, 0.9607844f, 0.8627452f, 1f)
        /// </summary>
        public static readonly (float4, float4) beige = (color.beige, color.beige);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.8941177f, 0.7686275f, 1f)
        /// </summary>
        public static readonly (float4, float4) bisque = (color.bisque, color.bisque);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) black = (color.black, color.black);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9215687f, 0.8039216f, 1f)
        /// </summary>
        public static readonly (float4, float4) blanchedAlmond = (color.blanchedAlmond, color.blanchedAlmond);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) blue = (color.blue, color.blue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5411765f, 0.1686275f, 0.8862746f, 1f)
        /// </summary>
        public static readonly (float4, float4) blueViolet = (color.blueViolet, color.blueViolet);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6470588f, 0.1647059f, 0.1647059f, 1f)
        /// </summary>
        public static readonly (float4, float4) brown = (color.brown, color.brown);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8705883f, 0.7215686f, 0.5294118f, 1f)
        /// </summary>
        public static readonly (float4, float4) burlywood = (color.burlywood, color.burlywood);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.372549f, 0.6196079f, 0.627451f, 1f)
        /// </summary>
        public static readonly (float4, float4) cadetBlue = (color.cadetBlue, color.cadetBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4980392f, 1f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) chartreuse = (color.chartreuse, color.chartreuse);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8235295f, 0.4117647f, 0.1176471f, 1f)
        /// </summary>
        public static readonly (float4, float4) chocolate = (color.chocolate, color.chocolate);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 0f, 0f)
        /// </summary>
        public static readonly (float4, float4) clear = (color.clear, color.clear);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.4980392f, 0.3137255f, 1f)
        /// </summary>
        public static readonly (float4, float4) coral = (color.coral, color.coral);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.3921569f, 0.5843138f, 0.9294118f, 1f)
        /// </summary>
        public static readonly (float4, float4) cornflowerBlue = (color.cornflowerBlue, color.cornflowerBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9725491f, 0.8627452f, 1f)
        /// </summary>
        public static readonly (float4, float4) cornsilk = (color.cornsilk, color.cornsilk);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8627452f, 0.07843138f, 0.2352941f, 1f)
        /// </summary>
        public static readonly (float4, float4) crimson = (color.crimson, color.crimson);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 1f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) cyan = (color.cyan, color.cyan);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 0.5450981f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkBlue = (color.darkBlue, color.darkBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.5450981f, 0.5450981f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkCyan = (color.darkCyan, color.darkCyan);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7215686f, 0.5254902f, 0.04313726f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkGoldenRod = (color.darkGoldenRod, color.darkGoldenRod);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6627451f, 0.6627451f, 0.6627451f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkGray = (color.darkGray, color.darkGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.3921569f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkGreen = (color.darkGreen, color.darkGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7411765f, 0.7176471f, 0.4196079f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkKhaki = (color.darkKhaki, color.darkKhaki);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5450981f, 0f, 0.5450981f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkMagenta = (color.darkMagenta, color.darkMagenta);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.3333333f, 0.4196079f, 0.1843137f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkOliveGreen = (color.darkOliveGreen, color.darkOliveGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.5490196f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkOrange = (color.darkOrange, color.darkOrange);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6f, 0.1960784f, 0.8000001f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkOrchid = (color.darkOrchid, color.darkOrchid);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5450981f, 0f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkRed = (color.darkRed, color.darkRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9137256f, 0.5882353f, 0.4784314f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkSalmon = (color.darkSalmon, color.darkSalmon);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5607843f, 0.7372549f, 0.5607843f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkSeaGreen = (color.darkSeaGreen, color.darkSeaGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.282353f, 0.2392157f, 0.5450981f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkSlateBlue = (color.darkSlateBlue, color.darkSlateBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1843137f, 0.3098039f, 0.3098039f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkSlateGray = (color.darkSlateGray, color.darkSlateGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.8078432f, 0.8196079f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkTurquoise = (color.darkTurquoise, color.darkTurquoise);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5803922f, 0f, 0.8274511f, 1f)
        /// </summary>
        public static readonly (float4, float4) darkViolet = (color.darkViolet, color.darkViolet);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.07843138f, 0.5764706f, 1f)
        /// </summary>
        public static readonly (float4, float4) deepPink = (color.deepPink, color.deepPink);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.7490196f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) deepSkyBlue = (color.deepSkyBlue, color.deepSkyBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4117647f, 0.4117647f, 0.4117647f, 1f)
        /// </summary>
        public static readonly (float4, float4) dimGray = (color.dimGray, color.dimGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1176471f, 0.5647059f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) dodgerBlue = (color.dodgerBlue, color.dodgerBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6980392f, 0.1333333f, 0.1333333f, 1f)
        /// </summary>
        public static readonly (float4, float4) firebrick = (color.firebrick, color.firebrick);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9803922f, 0.9411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) floralWhite = (color.floralWhite, color.floralWhite);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1333333f, 0.5450981f, 0.1333333f, 1f)
        /// </summary>
        public static readonly (float4, float4) forestGreen = (color.forestGreen, color.forestGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8627452f, 0.8627452f, 0.8627452f, 1f)
        /// </summary>
        public static readonly (float4, float4) gainsboro = (color.gainsboro, color.gainsboro);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9725491f, 0.9725491f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) ghostWhite = (color.ghostWhite, color.ghostWhite);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.8431373f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) gold = (color.gold, color.gold);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.854902f, 0.6470588f, 0.1254902f, 1f)
        /// </summary>
        public static readonly (float4, float4) goldenRod = (color.goldenRod, color.goldenRod);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray = (color.gray, color.gray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly (float4, float4) grey = (color.grey, color.grey);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1f, 0.1f, 0.1f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray1 = (color.gray1, color.gray1);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.2f, 0.2f, 0.2f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray2 = (color.gray2, color.gray2);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.3f, 0.3f, 0.3f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray3 = (color.gray3, color.gray3);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4f, 0.4f, 0.4f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray4 = (color.gray4, color.gray4);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray5 = (color.gray5, color.gray5);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6f, 0.6f, 0.6f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray6 = (color.gray6, color.gray6);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7f, 0.7f, 0.7f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray7 = (color.gray7, color.gray7);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8f, 0.8f, 0.8f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray8 = (color.gray8, color.gray8);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9f, 0.9f, 0.9f, 1f)
        /// </summary>
        public static readonly (float4, float4) gray9 = (color.gray9, color.gray9);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 1f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) green = (color.green, color.green);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6784314f, 1f, 0.1843137f, 1f)
        /// </summary>
        public static readonly (float4, float4) greenYellow = (color.greenYellow, color.greenYellow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9411765f, 1f, 0.9411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) honeydew = (color.honeydew, color.honeydew);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.4117647f, 0.7058824f, 1f)
        /// </summary>
        public static readonly (float4, float4) hotPink = (color.hotPink, color.hotPink);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8039216f, 0.3607843f, 0.3607843f, 1f)
        /// </summary>
        public static readonly (float4, float4) indianRed = (color.indianRed, color.indianRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.2941177f, 0f, 0.509804f, 1f)
        /// </summary>
        public static readonly (float4, float4) indigo = (color.indigo, color.indigo);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 1f, 0.9411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) ivory = (color.ivory, color.ivory);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9411765f, 0.9019608f, 0.5490196f, 1f)
        /// </summary>
        public static readonly (float4, float4) khaki = (color.khaki, color.khaki);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9019608f, 0.9019608f, 0.9803922f, 1f)
        /// </summary>
        public static readonly (float4, float4) lavender = (color.lavender, color.lavender);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9411765f, 0.9607844f, 1f)
        /// </summary>
        public static readonly (float4, float4) lavenderBlush = (color.lavenderBlush, color.lavenderBlush);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4862745f, 0.9882354f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) lawnGreen = (color.lawnGreen, color.lawnGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9803922f, 0.8039216f, 1f)
        /// </summary>
        public static readonly (float4, float4) lemonChiffon = (color.lemonChiffon, color.lemonChiffon);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6784314f, 0.8470589f, 0.9019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightBlue = (color.lightBlue, color.lightBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9411765f, 0.5019608f, 0.5019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightCoral = (color.lightCoral, color.lightCoral);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8784314f, 1f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightCyan = (color.lightCyan, color.lightCyan);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9333334f, 0.8666667f, 0.509804f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightGoldenRod = (color.lightGoldenRod, color.lightGoldenRod);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9803922f, 0.9803922f, 0.8235295f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightGoldenRodYellow = (color.lightGoldenRodYellow, color.lightGoldenRodYellow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8274511f, 0.8274511f, 0.8274511f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightGray = (color.lightGray, color.lightGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5647059f, 0.9333334f, 0.5647059f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightGreen = (color.lightGreen, color.lightGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.7137255f, 0.7568628f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightPink = (color.lightPink, color.lightPink);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.627451f, 0.4784314f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSalmon = (color.lightSalmon, color.lightSalmon);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1254902f, 0.6980392f, 0.6666667f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSeaGreen = (color.lightSeaGreen, color.lightSeaGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5294118f, 0.8078432f, 0.9803922f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSkyBlue = (color.lightSkyBlue, color.lightSkyBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5176471f, 0.4392157f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSlateBlue = (color.lightSlateBlue, color.lightSlateBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4666667f, 0.5333334f, 0.6f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSlateGray = (color.lightSlateGray, color.lightSlateGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6901961f, 0.7686275f, 0.8705883f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightSteelBlue = (color.lightSteelBlue, color.lightSteelBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 1f, 0.8784314f, 1f)
        /// </summary>
        public static readonly (float4, float4) lightYellow = (color.lightYellow, color.lightYellow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1960784f, 0.8039216f, 0.1960784f, 1f)
        /// </summary>
        public static readonly (float4, float4) limeGreen = (color.limeGreen, color.limeGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9803922f, 0.9411765f, 0.9019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) linen = (color.linen, color.linen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) magenta = (color.magenta, color.magenta);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6901961f, 0.1882353f, 0.3764706f, 1f)
        /// </summary>
        public static readonly (float4, float4) maroon = (color.maroon, color.maroon);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4f, 0.8039216f, 0.6666667f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumAquamarine = (color.mediumAquamarine, color.mediumAquamarine);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 0.8039216f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumBlue = (color.mediumBlue, color.mediumBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7294118f, 0.3333333f, 0.8274511f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumOrchid = (color.mediumOrchid, color.mediumOrchid);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5764706f, 0.4392157f, 0.8588236f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumPurple = (color.mediumPurple, color.mediumPurple);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.2352941f, 0.7019608f, 0.4431373f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumSeaGreen = (color.mediumSeaGreen, color.mediumSeaGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.482353f, 0.4078432f, 0.9333334f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumSlateBlue = (color.mediumSlateBlue, color.mediumSlateBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.9803922f, 0.6039216f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumSpringGreen = (color.mediumSpringGreen, color.mediumSpringGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.282353f, 0.8196079f, 0.8000001f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumTurquoise = (color.mediumTurquoise, color.mediumTurquoise);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7803922f, 0.08235294f, 0.5215687f, 1f)
        /// </summary>
        public static readonly (float4, float4) mediumVioletRed = (color.mediumVioletRed, color.mediumVioletRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.09803922f, 0.09803922f, 0.4392157f, 1f)
        /// </summary>
        public static readonly (float4, float4) midnightBlue = (color.midnightBlue, color.midnightBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9607844f, 1f, 0.9803922f, 1f)
        /// </summary>
        public static readonly (float4, float4) mintCream = (color.mintCream, color.mintCream);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.8941177f, 0.882353f, 1f)
        /// </summary>
        public static readonly (float4, float4) mistyRose = (color.mistyRose, color.mistyRose);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.8941177f, 0.7098039f, 1f)
        /// </summary>
        public static readonly (float4, float4) moccasin = (color.moccasin, color.moccasin);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.8705883f, 0.6784314f, 1f)
        /// </summary>
        public static readonly (float4, float4) navajoWhite = (color.navajoWhite, color.navajoWhite);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0f, 0.5019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) navyBlue = (color.navyBlue, color.navyBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9921569f, 0.9607844f, 0.9019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) oldLace = (color.oldLace, color.oldLace);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5019608f, 0.5019608f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) olive = (color.olive, color.olive);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4196079f, 0.5568628f, 0.1372549f, 1f)
        /// </summary>
        public static readonly (float4, float4) oliveDrab = (color.oliveDrab, color.oliveDrab);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.6470588f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) orange = (color.orange, color.orange);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.2705882f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) orangeRed = (color.orangeRed, color.orangeRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.854902f, 0.4392157f, 0.8392158f, 1f)
        /// </summary>
        public static readonly (float4, float4) orchid = (color.orchid, color.orchid);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9333334f, 0.909804f, 0.6666667f, 1f)
        /// </summary>
        public static readonly (float4, float4) paleGoldenRod = (color.paleGoldenRod, color.paleGoldenRod);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5960785f, 0.9843138f, 0.5960785f, 1f)
        /// </summary>
        public static readonly (float4, float4) paleGreen = (color.paleGreen, color.paleGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6862745f, 0.9333334f, 0.9333334f, 1f)
        /// </summary>
        public static readonly (float4, float4) paleTurquoise = (color.paleTurquoise, color.paleTurquoise);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8588236f, 0.4392157f, 0.5764706f, 1f)
        /// </summary>
        public static readonly (float4, float4) paleVioletRed = (color.paleVioletRed, color.paleVioletRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.937255f, 0.8352942f, 1f)
        /// </summary>
        public static readonly (float4, float4) papayaWhip = (color.papayaWhip, color.papayaWhip);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.854902f, 0.7254902f, 1f)
        /// </summary>
        public static readonly (float4, float4) peachPuff = (color.peachPuff, color.peachPuff);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8039216f, 0.5215687f, 0.2470588f, 1f)
        /// </summary>
        public static readonly (float4, float4) peru = (color.peru, color.peru);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.7529413f, 0.7960785f, 1f)
        /// </summary>
        public static readonly (float4, float4) pink = (color.pink, color.pink);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8666667f, 0.627451f, 0.8666667f, 1f)
        /// </summary>
        public static readonly (float4, float4) plum = (color.plum, color.plum);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6901961f, 0.8784314f, 0.9019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) powderBlue = (color.powderBlue, color.powderBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.627451f, 0.1254902f, 0.9411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) purple = (color.purple, color.purple);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4f, 0.2f, 0.6f, 1f)
        /// </summary>
        public static readonly (float4, float4) rebeccaPurple = (color.rebeccaPurple, color.rebeccaPurple);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0f, 0f, 1f)
        /// </summary>
        public static readonly (float4, float4) red = (color.red, color.red);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7372549f, 0.5607843f, 0.5607843f, 1f)
        /// </summary>
        public static readonly (float4, float4) rosyBrown = (color.rosyBrown, color.rosyBrown);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.254902f, 0.4117647f, 0.882353f, 1f)
        /// </summary>
        public static readonly (float4, float4) royalBlue = (color.royalBlue, color.royalBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5450981f, 0.2705882f, 0.07450981f, 1f)
        /// </summary>
        public static readonly (float4, float4) saddleBrown = (color.saddleBrown, color.saddleBrown);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9803922f, 0.5019608f, 0.4470589f, 1f)
        /// </summary>
        public static readonly (float4, float4) salmon = (color.salmon, color.salmon);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9568628f, 0.6431373f, 0.3764706f, 1f)
        /// </summary>
        public static readonly (float4, float4) sandyBrown = (color.sandyBrown, color.sandyBrown);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1803922f, 0.5450981f, 0.3411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) seaGreen = (color.seaGreen, color.seaGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9607844f, 0.9333334f, 1f)
        /// </summary>
        public static readonly (float4, float4) seashell = (color.seashell, color.seashell);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.627451f, 0.3215686f, 0.1764706f, 1f)
        /// </summary>
        public static readonly (float4, float4) sienna = (color.sienna, color.sienna);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.7529413f, 0.7529413f, 0.7529413f, 1f)
        /// </summary>
        public static readonly (float4, float4) silver = (color.silver, color.silver);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5294118f, 0.8078432f, 0.9215687f, 1f)
        /// </summary>
        public static readonly (float4, float4) skyBlue = (color.skyBlue, color.skyBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4156863f, 0.3529412f, 0.8039216f, 1f)
        /// </summary>
        public static readonly (float4, float4) slateBlue = (color.slateBlue, color.slateBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.4392157f, 0.5019608f, 0.5647059f, 1f)
        /// </summary>
        public static readonly (float4, float4) slateGray = (color.slateGray, color.slateGray);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9803922f, 0.9803922f, 1f)
        /// </summary>
        public static readonly (float4, float4) snow = (color.snow, color.snow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8627452f, 0.1921569f, 0.1960784f, 1f)
        /// </summary>
        public static readonly (float4, float4) softRed = (color.softRed, color.softRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.1882353f, 0.682353f, 0.7490196f, 1f)
        /// </summary>
        public static readonly (float4, float4) softBlue = (color.softBlue, color.softBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.5490196f, 0.7882354f, 0.1411765f, 1f)
        /// </summary>
        public static readonly (float4, float4) softGreen = (color.softGreen, color.softGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.9333334f, 0.5490196f, 1f)
        /// </summary>
        public static readonly (float4, float4) softYellow = (color.softYellow, color.softYellow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 1f, 0.4980392f, 1f)
        /// </summary>
        public static readonly (float4, float4) springGreen = (color.springGreen, color.springGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.2745098f, 0.509804f, 0.7058824f, 1f)
        /// </summary>
        public static readonly (float4, float4) steelBlue = (color.steelBlue, color.steelBlue);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8235295f, 0.7058824f, 0.5490196f, 1f)
        /// </summary>
        public static readonly (float4, float4) tan = (color.tan, color.tan);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0f, 0.5019608f, 0.5019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) teal = (color.teal, color.teal);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8470589f, 0.7490196f, 0.8470589f, 1f)
        /// </summary>
        public static readonly (float4, float4) thistle = (color.thistle, color.thistle);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.3882353f, 0.2784314f, 1f)
        /// </summary>
        public static readonly (float4, float4) tomato = (color.tomato, color.tomato);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.2509804f, 0.8784314f, 0.8156863f, 1f)
        /// </summary>
        public static readonly (float4, float4) turquoise = (color.turquoise, color.turquoise);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9333334f, 0.509804f, 0.9333334f, 1f)
        /// </summary>
        public static readonly (float4, float4) violet = (color.violet, color.violet);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.8156863f, 0.1254902f, 0.5647059f, 1f)
        /// </summary>
        public static readonly (float4, float4) violetRed = (color.violetRed, color.violetRed);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9607844f, 0.8705883f, 0.7019608f, 1f)
        /// </summary>
        public static readonly (float4, float4) wheat = (color.wheat, color.wheat);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 1f, 1f, 1f)
        /// </summary>
        public static readonly (float4, float4) white = (color.white, color.white);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.9607844f, 0.9607844f, 0.9607844f, 1f)
        /// </summary>
        public static readonly (float4, float4) whiteSmoke = (color.whiteSmoke, color.whiteSmoke);
        
        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.92f, 0.016f, 1f)
        /// </summary>
        public static readonly (float4, float4) yellow = (color.yellow, color.yellow);

        /// <summary>
        /// (float4, float4) Preset of RGBA(0.6039216f, 0.8039216f, 0.1960784f, 1f)
        /// </summary>
        public static readonly (float4, float4) yellowGreen = (color.yellowGreen, color.yellowGreen);

        /// <summary>
        /// (float4, float4) Preset of RGBA(1f, 0.92f, 0.016f, 1f)
        /// </summary>
        public static readonly (float4, float4) yellowNice = (color.yellowNice, color.yellowNice);
    }
}