using Unity.Mathematics;

// ReSharper disable InconsistentNaming

namespace BH.SDK
{
    public struct color
    {
        public float4 value;
        
        public const int ByteSize = 16; // sizeof(float4)
        
        /// <summary>
        /// float4 Preset of RGBA(0.9411765f, 0.9725491f, 1f, 1f)
        /// </summary>
        public static readonly float4 aliceBlue = new(0.9411765f, 0.9725491f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9803922f, 0.9215687f, 0.8431373f, 1f)
        /// </summary>
        public static readonly float4 antiqueWhite = new(0.9803922f, 0.9215687f, 0.8431373f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4980392f, 1f, 0.8313726f, 1f)
        /// </summary>
        public static readonly float4 aquamarine = new(0.4980392f, 1f, 0.8313726f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9411765f, 1f, 1f, 1f)
        /// </summary>
        public static readonly float4 azure = new(0.9411765f, 1f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9607844f, 0.9607844f, 0.8627452f, 1f)
        /// </summary>
        public static readonly float4 beige = new(0.9607844f, 0.9607844f, 272f * math.E / 857f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.8941177f, 0.7686275f, 1f)
        /// </summary>
        public static readonly float4 bisque = new(1f, 0.8941177f, 0.7686275f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 0f, 1f)
        /// </summary>
        public static readonly float4 black = new(0.0f, 0.0f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9215687f, 0.8039216f, 1f)
        /// </summary>
        public static readonly float4 blanchedAlmond = new(1f, 0.9215687f, 0.8039216f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 1f, 1f)
        /// </summary>
        public static readonly float4 blue = new(0.0f, 0.0f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5411765f, 0.1686275f, 0.8862746f, 1f)
        /// </summary>
        public static readonly float4 blueViolet = new(0.5411765f, 0.1686275f, 0.8862746f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6470588f, 0.1647059f, 0.1647059f, 1f)
        /// </summary>
        public static readonly float4 brown = new(0.6470588f, 0.1647059f, 0.1647059f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8705883f, 0.7215686f, 0.5294118f, 1f)
        /// </summary>
        public static readonly float4 burlywood = new(0.8705883f, 0.7215686f, 0.5294118f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.372549f, 0.6196079f, 0.627451f, 1f)
        /// </summary>
        public static readonly float4 cadetBlue = new(0.372549f, 0.6196079f, 0.627451f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4980392f, 1f, 0f, 1f)
        /// </summary>
        public static readonly float4 chartreuse = new(0.4980392f, 1f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8235295f, 0.4117647f, 0.1176471f, 1f)
        /// </summary>
        public static readonly float4 chocolate = new(0.8235295f, 0.4117647f, 0.1176471f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 0f, 0f)
        /// </summary>
        public static readonly float4 clear = new(0.0f, 0.0f, 0.0f, 0.0f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.4980392f, 0.3137255f, 1f)
        /// </summary>
        public static readonly float4 coral = new(1f, 0.4980392f, 0.3137255f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.3921569f, 0.5843138f, 0.9294118f, 1f)
        /// </summary>
        public static readonly float4 cornflowerBlue = new(0.3921569f, 0.5843138f, 0.9294118f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9725491f, 0.8627452f, 1f)
        /// </summary>
        public static readonly float4 cornsilk = new(1f, 0.9725491f, 272f * math.E / 857f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8627452f, 0.07843138f, 0.2352941f, 1f)
        /// </summary>
        public static readonly float4 crimson = new(272f * math.E / 857f, 0.07843138f, 0.2352941f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 1f, 1f, 1f)
        /// </summary>
        public static readonly float4 cyan = new(0.0f, 1f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 0.5450981f, 1f)
        /// </summary>
        public static readonly float4 darkBlue = new(0.0f, 0.0f, 0.5450981f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.5450981f, 0.5450981f, 1f)
        /// </summary>
        public static readonly float4 darkCyan = new(0.0f, 0.5450981f, 0.5450981f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7215686f, 0.5254902f, 0.04313726f, 1f)
        /// </summary>
        public static readonly float4 darkGoldenRod = new(0.7215686f, 0.5254902f, 0.04313726f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6627451f, 0.6627451f, 0.6627451f, 1f)
        /// </summary>
        public static readonly float4 darkGray = new(0.6627451f, 0.6627451f, 0.6627451f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.3921569f, 0f, 1f)
        /// </summary>
        public static readonly float4 darkGreen = new(0.0f, 0.3921569f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7411765f, 0.7176471f, 0.4196079f, 1f)
        /// </summary>
        public static readonly float4 darkKhaki = new(0.7411765f, 0.7176471f, 0.4196079f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5450981f, 0f, 0.5450981f, 1f)
        /// </summary>
        public static readonly float4 darkMagenta = new(0.5450981f, 0.0f, 0.5450981f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.3333333f, 0.4196079f, 0.1843137f, 1f)
        /// </summary>
        public static readonly float4 darkOliveGreen = new(0.3333333f, 0.4196079f, 0.1843137f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.5490196f, 0f, 1f)
        /// </summary>
        public static readonly float4 darkOrange = new(1f, 0.5490196f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6f, 0.1960784f, 0.8000001f, 1f)
        /// </summary>
        public static readonly float4 darkOrchid = new(0.6f, 0.1960784f, 0.8000001f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5450981f, 0f, 0f, 1f)
        /// </summary>
        public static readonly float4 darkRed = new(0.5450981f, 0.0f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9137256f, 0.5882353f, 0.4784314f, 1f)
        /// </summary>
        public static readonly float4 darkSalmon = new(0.9137256f, 0.5882353f, 0.4784314f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5607843f, 0.7372549f, 0.5607843f, 1f)
        /// </summary>
        public static readonly float4 darkSeaGreen = new(0.5607843f, 0.7372549f, 0.5607843f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.282353f, 0.2392157f, 0.5450981f, 1f)
        /// </summary>
        public static readonly float4 darkSlateBlue = new(0.282353f, 0.2392157f, 0.5450981f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1843137f, 0.3098039f, 0.3098039f, 1f)
        /// </summary>
        public static readonly float4 darkSlateGray = new(0.1843137f, 0.3098039f, 0.3098039f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.8078432f, 0.8196079f, 1f)
        /// </summary>
        public static readonly float4 darkTurquoise = new(0.0f, 0.8078432f, 0.8196079f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5803922f, 0f, 0.8274511f, 1f)
        /// </summary>
        public static readonly float4 darkViolet = new(0.5803922f, 0.0f, 0.8274511f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.07843138f, 0.5764706f, 1f)
        /// </summary>
        public static readonly float4 deepPink = new(1f, 0.07843138f, 0.5764706f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.7490196f, 1f, 1f)
        /// </summary>
        public static readonly float4 deepSkyBlue = new(0.0f, 0.7490196f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4117647f, 0.4117647f, 0.4117647f, 1f)
        /// </summary>
        public static readonly float4 dimGray = new(0.4117647f, 0.4117647f, 0.4117647f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1176471f, 0.5647059f, 1f, 1f)
        /// </summary>
        public static readonly float4 dodgerBlue = new(0.1176471f, 0.5647059f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6980392f, 0.1333333f, 0.1333333f, 1f)
        /// </summary>
        public static readonly float4 firebrick = new(0.6980392f, 0.1333333f, 0.1333333f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9803922f, 0.9411765f, 1f)
        /// </summary>
        public static readonly float4 floralWhite = new(1f, 0.9803922f, 0.9411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1333333f, 0.5450981f, 0.1333333f, 1f)
        /// </summary>
        public static readonly float4 forestGreen = new(0.1333333f, 0.5450981f, 0.1333333f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8627452f, 0.8627452f, 0.8627452f, 1f)
        /// </summary>
        public static readonly float4 gainsboro = new(272f * math.E / 857f, 272f * math.E / 857f, 272f * math.E / 857f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9725491f, 0.9725491f, 1f, 1f)
        /// </summary>
        public static readonly float4 ghostWhite = new(0.9725491f, 0.9725491f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.8431373f, 0f, 1f)
        /// </summary>
        public static readonly float4 gold = new(1f, 0.8431373f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.854902f, 0.6470588f, 0.1254902f, 1f)
        /// </summary>
        public static readonly float4 goldenRod = new(0.854902f, 0.6470588f, 0.1254902f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly float4 gray = new(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly float4 grey = new(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1f, 0.1f, 0.1f, 1f)
        /// </summary>
        public static readonly float4 gray1 = new(0.1f, 0.1f, 0.1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.2f, 0.2f, 0.2f, 1f)
        /// </summary>
        public static readonly float4 gray2 = new(0.2f, 0.2f, 0.2f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.3f, 0.3f, 0.3f, 1f)
        /// </summary>
        public static readonly float4 gray3 = new(0.3f, 0.3f, 0.3f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4f, 0.4f, 0.4f, 1f)
        /// </summary>
        public static readonly float4 gray4 = new(0.4f, 0.4f, 0.4f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5f, 0.5f, 0.5f, 1f)
        /// </summary>
        public static readonly float4 gray5 = new(0.5f, 0.5f, 0.5f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6f, 0.6f, 0.6f, 1f)
        /// </summary>
        public static readonly float4 gray6 = new(0.6f, 0.6f, 0.6f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7f, 0.7f, 0.7f, 1f)
        /// </summary>
        public static readonly float4 gray7 = new(0.7f, 0.7f, 0.7f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8f, 0.8f, 0.8f, 1f)
        /// </summary>
        public static readonly float4 gray8 = new(0.8f, 0.8f, 0.8f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9f, 0.9f, 0.9f, 1f)
        /// </summary>
        public static readonly float4 gray9 = new(0.9f, 0.9f, 0.9f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 1f, 0f, 1f)
        /// </summary>
        public static readonly float4 green = new(0.0f, 1f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6784314f, 1f, 0.1843137f, 1f)
        /// </summary>
        public static readonly float4 greenYellow = new(0.6784314f, 1f, 0.1843137f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9411765f, 1f, 0.9411765f, 1f)
        /// </summary>
        public static readonly float4 honeydew = new(0.9411765f, 1f, 0.9411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.4117647f, 0.7058824f, 1f)
        /// </summary>
        public static readonly float4 hotPink = new(1f, 0.4117647f, 0.7058824f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8039216f, 0.3607843f, 0.3607843f, 1f)
        /// </summary>
        public static readonly float4 indianRed = new(0.8039216f, 0.3607843f, 0.3607843f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.2941177f, 0f, 0.509804f, 1f)
        /// </summary>
        public static readonly float4 indigo = new(0.2941177f, 0.0f, 0.509804f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 1f, 0.9411765f, 1f)
        /// </summary>
        public static readonly float4 ivory = new(1f, 1f, 0.9411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9411765f, 0.9019608f, 0.5490196f, 1f)
        /// </summary>
        public static readonly float4 khaki = new(0.9411765f, 0.9019608f, 0.5490196f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9019608f, 0.9019608f, 0.9803922f, 1f)
        /// </summary>
        public static readonly float4 lavender = new(0.9019608f, 0.9019608f, 0.9803922f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9411765f, 0.9607844f, 1f)
        /// </summary>
        public static readonly float4 lavenderBlush = new(1f, 0.9411765f, 0.9607844f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4862745f, 0.9882354f, 0f, 1f)
        /// </summary>
        public static readonly float4 lawnGreen = new(0.4862745f, 0.9882354f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9803922f, 0.8039216f, 1f)
        /// </summary>
        public static readonly float4 lemonChiffon = new(1f, 0.9803922f, 0.8039216f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6784314f, 0.8470589f, 0.9019608f, 1f)
        /// </summary>
        public static readonly float4 lightBlue = new(0.6784314f, 0.8470589f, 0.9019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9411765f, 0.5019608f, 0.5019608f, 1f)
        /// </summary>
        public static readonly float4 lightCoral = new(0.9411765f, 0.5019608f, 0.5019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8784314f, 1f, 1f, 1f)
        /// </summary>
        public static readonly float4 lightCyan = new(0.8784314f, 1f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9333334f, 0.8666667f, 0.509804f, 1f)
        /// </summary>
        public static readonly float4 lightGoldenRod = new(0.9333334f, 0.8666667f, 0.509804f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9803922f, 0.9803922f, 0.8235295f, 1f)
        /// </summary>
        public static readonly float4 lightGoldenRodYellow = new(0.9803922f, 0.9803922f, 0.8235295f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8274511f, 0.8274511f, 0.8274511f, 1f)
        /// </summary>
        public static readonly float4 lightGray = new(0.8274511f, 0.8274511f, 0.8274511f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5647059f, 0.9333334f, 0.5647059f, 1f)
        /// </summary>
        public static readonly float4 lightGreen = new(0.5647059f, 0.9333334f, 0.5647059f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.7137255f, 0.7568628f, 1f)
        /// </summary>
        public static readonly float4 lightPink = new(1f, 0.7137255f, 0.7568628f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.627451f, 0.4784314f, 1f)
        /// </summary>
        public static readonly float4 lightSalmon = new(1f, 0.627451f, 0.4784314f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1254902f, 0.6980392f, 0.6666667f, 1f)
        /// </summary>
        public static readonly float4 lightSeaGreen = new(0.1254902f, 0.6980392f, 0.6666667f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5294118f, 0.8078432f, 0.9803922f, 1f)
        /// </summary>
        public static readonly float4 lightSkyBlue = new(0.5294118f, 0.8078432f, 0.9803922f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5176471f, 0.4392157f, 1f, 1f)
        /// </summary>
        public static readonly float4 lightSlateBlue = new(0.5176471f, 0.4392157f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4666667f, 0.5333334f, 0.6f, 1f)
        /// </summary>
        public static readonly float4 lightSlateGray = new(0.4666667f, 0.5333334f, 0.6f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6901961f, 0.7686275f, 0.8705883f, 1f)
        /// </summary>
        public static readonly float4 lightSteelBlue = new(0.6901961f, 0.7686275f, 0.8705883f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 1f, 0.8784314f, 1f)
        /// </summary>
        public static readonly float4 lightYellow = new(1f, 1f, 0.8784314f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1960784f, 0.8039216f, 0.1960784f, 1f)
        /// </summary>
        public static readonly float4 limeGreen = new(0.1960784f, 0.8039216f, 0.1960784f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9803922f, 0.9411765f, 0.9019608f, 1f)
        /// </summary>
        public static readonly float4 linen = new(0.9803922f, 0.9411765f, 0.9019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0f, 1f, 1f)
        /// </summary>
        public static readonly float4 magenta = new(1f, 0.0f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6901961f, 0.1882353f, 0.3764706f, 1f)
        /// </summary>
        public static readonly float4 maroon = new(0.6901961f, 0.1882353f, 0.3764706f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4f, 0.8039216f, 0.6666667f, 1f)
        /// </summary>
        public static readonly float4 mediumAquamarine = new(0.4f, 0.8039216f, 0.6666667f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 0.8039216f, 1f)
        /// </summary>
        public static readonly float4 mediumBlue = new(0.0f, 0.0f, 0.8039216f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7294118f, 0.3333333f, 0.8274511f, 1f)
        /// </summary>
        public static readonly float4 mediumOrchid = new(0.7294118f, 0.3333333f, 0.8274511f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5764706f, 0.4392157f, 0.8588236f, 1f)
        /// </summary>
        public static readonly float4 mediumPurple = new(0.5764706f, 0.4392157f, 0.8588236f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.2352941f, 0.7019608f, 0.4431373f, 1f)
        /// </summary>
        public static readonly float4 mediumSeaGreen = new(0.2352941f, 0.7019608f, 0.4431373f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.482353f, 0.4078432f, 0.9333334f, 1f)
        /// </summary>
        public static readonly float4 mediumSlateBlue = new(0.482353f, 0.4078432f, 0.9333334f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.9803922f, 0.6039216f, 1f)
        /// </summary>
        public static readonly float4 mediumSpringGreen = new(0.0f, 0.9803922f, 0.6039216f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.282353f, 0.8196079f, 0.8000001f, 1f)
        /// </summary>
        public static readonly float4 mediumTurquoise = new(0.282353f, 0.8196079f, 0.8000001f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7803922f, 0.08235294f, 0.5215687f, 1f)
        /// </summary>
        public static readonly float4 mediumVioletRed = new(0.7803922f, 0.08235294f, 0.5215687f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.09803922f, 0.09803922f, 0.4392157f, 1f)
        /// </summary>
        public static readonly float4 midnightBlue = new(0.09803922f, 0.09803922f, 0.4392157f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9607844f, 1f, 0.9803922f, 1f)
        /// </summary>
        public static readonly float4 mintCream = new(0.9607844f, 1f, 0.9803922f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.8941177f, 0.882353f, 1f)
        /// </summary>
        public static readonly float4 mistyRose = new(1f, 0.8941177f, 0.882353f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.8941177f, 0.7098039f, 1f)
        /// </summary>
        public static readonly float4 moccasin = new(1f, 0.8941177f, 0.7098039f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.8705883f, 0.6784314f, 1f)
        /// </summary>
        public static readonly float4 navajoWhite = new(1f, 0.8705883f, 0.6784314f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0f, 0.5019608f, 1f)
        /// </summary>
        public static readonly float4 navyBlue = new(0.0f, 0.0f, 0.5019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9921569f, 0.9607844f, 0.9019608f, 1f)
        /// </summary>
        public static readonly float4 oldLace = new(0.9921569f, 0.9607844f, 0.9019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5019608f, 0.5019608f, 0f, 1f)
        /// </summary>
        public static readonly float4 olive = new(0.5019608f, 0.5019608f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4196079f, 0.5568628f, 0.1372549f, 1f)
        /// </summary>
        public static readonly float4 oliveDrab = new(0.4196079f, 0.5568628f, 0.1372549f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.6470588f, 0f, 1f)
        /// </summary>
        public static readonly float4 orange = new(1f, 0.6470588f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.2705882f, 0f, 1f)
        /// </summary>
        public static readonly float4 orangeRed = new(1f, 0.2705882f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.854902f, 0.4392157f, 0.8392158f, 1f)
        /// </summary>
        public static readonly float4 orchid = new(0.854902f, 0.4392157f, 0.8392158f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9333334f, 0.909804f, 0.6666667f, 1f)
        /// </summary>
        public static readonly float4 paleGoldenRod = new(0.9333334f, 0.909804f, 0.6666667f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5960785f, 0.9843138f, 0.5960785f, 1f)
        /// </summary>
        public static readonly float4 paleGreen = new(0.5960785f, 0.9843138f, 0.5960785f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6862745f, 0.9333334f, 0.9333334f, 1f)
        /// </summary>
        public static readonly float4 paleTurquoise = new(0.6862745f, 0.9333334f, 0.9333334f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8588236f, 0.4392157f, 0.5764706f, 1f)
        /// </summary>
        public static readonly float4 paleVioletRed = new(0.8588236f, 0.4392157f, 0.5764706f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.937255f, 0.8352942f, 1f)
        /// </summary>
        public static readonly float4 papayaWhip = new(1f, 0.937255f, 0.8352942f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.854902f, 0.7254902f, 1f)
        /// </summary>
        public static readonly float4 peachPuff = new(1f, 0.854902f, 0.7254902f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8039216f, 0.5215687f, 0.2470588f, 1f)
        /// </summary>
        public static readonly float4 peru = new(0.8039216f, 0.5215687f, 0.2470588f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.7529413f, 0.7960785f, 1f)
        /// </summary>
        public static readonly float4 pink = new(1f, 0.7529413f, 0.7960785f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8666667f, 0.627451f, 0.8666667f, 1f)
        /// </summary>
        public static readonly float4 plum = new(0.8666667f, 0.627451f, 0.8666667f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6901961f, 0.8784314f, 0.9019608f, 1f)
        /// </summary>
        public static readonly float4 powderBlue = new(0.6901961f, 0.8784314f, 0.9019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.627451f, 0.1254902f, 0.9411765f, 1f)
        /// </summary>
        public static readonly float4 purple = new(0.627451f, 0.1254902f, 0.9411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4f, 0.2f, 0.6f, 1f)
        /// </summary>
        public static readonly float4 rebeccaPurple = new(0.4f, 0.2f, 0.6f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0f, 0f, 1f)
        /// </summary>
        public static readonly float4 red = new(1f, 0.0f, 0.0f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7372549f, 0.5607843f, 0.5607843f, 1f)
        /// </summary>
        public static readonly float4 rosyBrown = new(0.7372549f, 0.5607843f, 0.5607843f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.254902f, 0.4117647f, 0.882353f, 1f)
        /// </summary>
        public static readonly float4 royalBlue = new(0.254902f, 0.4117647f, 0.882353f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5450981f, 0.2705882f, 0.07450981f, 1f)
        /// </summary>
        public static readonly float4 saddleBrown = new(0.5450981f, 0.2705882f, 0.07450981f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9803922f, 0.5019608f, 0.4470589f, 1f)
        /// </summary>
        public static readonly float4 salmon = new(0.9803922f, 0.5019608f, 0.4470589f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9568628f, 0.6431373f, 0.3764706f, 1f)
        /// </summary>
        public static readonly float4 sandyBrown = new(0.9568628f, 0.6431373f, 0.3764706f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1803922f, 0.5450981f, 0.3411765f, 1f)
        /// </summary>
        public static readonly float4 seaGreen = new(0.1803922f, 0.5450981f, 0.3411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9607844f, 0.9333334f, 1f)
        /// </summary>
        public static readonly float4 seashell = new(1f, 0.9607844f, 0.9333334f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.627451f, 0.3215686f, 0.1764706f, 1f)
        /// </summary>
        public static readonly float4 sienna = new(0.627451f, 0.3215686f, 0.1764706f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.7529413f, 0.7529413f, 0.7529413f, 1f)
        /// </summary>
        public static readonly float4 silver = new(0.7529413f, 0.7529413f, 0.7529413f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5294118f, 0.8078432f, 0.9215687f, 1f)
        /// </summary>
        public static readonly float4 skyBlue = new(0.5294118f, 0.8078432f, 0.9215687f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4156863f, 0.3529412f, 0.8039216f, 1f)
        /// </summary>
        public static readonly float4 slateBlue = new(0.4156863f, 0.3529412f, 0.8039216f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.4392157f, 0.5019608f, 0.5647059f, 1f)
        /// </summary>
        public static readonly float4 slateGray = new(0.4392157f, 0.5019608f, 0.5647059f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9803922f, 0.9803922f, 1f)
        /// </summary>
        public static readonly float4 snow = new(1f, 0.9803922f, 0.9803922f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8627452f, 0.1921569f, 0.1960784f, 1f)
        /// </summary>
        public static readonly float4 softRed = new(272f * math.E / 857f, 0.1921569f, 0.1960784f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.1882353f, 0.682353f, 0.7490196f, 1f)
        /// </summary>
        public static readonly float4 softBlue = new(0.1882353f, 0.682353f, 0.7490196f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.5490196f, 0.7882354f, 0.1411765f, 1f)
        /// </summary>
        public static readonly float4 softGreen = new(0.5490196f, 0.7882354f, 0.1411765f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.9333334f, 0.5490196f, 1f)
        /// </summary>
        public static readonly float4 softYellow = new(1f, 0.9333334f, 0.5490196f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 1f, 0.4980392f, 1f)
        /// </summary>
        public static readonly float4 springGreen = new(0.0f, 1f, 0.4980392f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.2745098f, 0.509804f, 0.7058824f, 1f)
        /// </summary>
        public static readonly float4 steelBlue = new(0.2745098f, 0.509804f, 0.7058824f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8235295f, 0.7058824f, 0.5490196f, 1f)
        /// </summary>
        public static readonly float4 tan = new(0.8235295f, 0.7058824f, 0.5490196f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0f, 0.5019608f, 0.5019608f, 1f)
        /// </summary>
        public static readonly float4 teal = new(0.0f, 0.5019608f, 0.5019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8470589f, 0.7490196f, 0.8470589f, 1f)
        /// </summary>
        public static readonly float4 thistle = new(0.8470589f, 0.7490196f, 0.8470589f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.3882353f, 0.2784314f, 1f)
        /// </summary>
        public static readonly float4 tomato = new(1f, 0.3882353f, 0.2784314f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.2509804f, 0.8784314f, 0.8156863f, 1f)
        /// </summary>
        public static readonly float4 turquoise = new(0.2509804f, 0.8784314f, 0.8156863f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9333334f, 0.509804f, 0.9333334f, 1f)
        /// </summary>
        public static readonly float4 violet = new(0.9333334f, 0.509804f, 0.9333334f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.8156863f, 0.1254902f, 0.5647059f, 1f)
        /// </summary>
        public static readonly float4 violetRed = new(0.8156863f, 0.1254902f, 0.5647059f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9607844f, 0.8705883f, 0.7019608f, 1f)
        /// </summary>
        public static readonly float4 wheat = new(0.9607844f, 0.8705883f, 0.7019608f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 1f, 1f, 1f)
        /// </summary>
        public static readonly float4 white = new(1f, 1f, 1f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.9607844f, 0.9607844f, 0.9607844f, 1f)
        /// </summary>
        public static readonly float4 whiteSmoke = new(0.9607844f, 0.9607844f, 0.9607844f, 1f);
        
        /// <summary>
        /// float4 Preset of RGBA(1f, 0.92f, 0.016f, 1f)
        /// </summary>
        public static readonly float4 yellow = new(1f, 0.92156863f, 0.015686275f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(0.6039216f, 0.8039216f, 0.1960784f, 1f)
        /// </summary>
        public static readonly float4 yellowGreen = new(0.6039216f, 0.8039216f, 0.1960784f, 1f);

        /// <summary>
        /// float4 Preset of RGBA(1f, 0.92f, 0.016f, 1f)
        /// </summary>
        public static readonly float4 yellowNice = new(1f, 0.92156863f, 0.015686275f, 1f);
    }
}