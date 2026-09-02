using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // THE DESKTOP HALF OF GRAPHICS: where the game's pixels go, rather than what is drawn into them.
    // A phone has one window and it is the screen, so every field here is inert on mobile and the
    // whole group is disabled rather than hidden there - the same call PathUtils.CanOpenFolder gets
    // on the General tab, and for the same reason: a control that vanishes per platform makes a
    // screenshot of the settings screen unreadable to whoever is reading it.
    //
    // RESOLUTION IS TWO INTS WITH A ZERO SENTINEL, not a nullable pair and not a string: zero means
    // "not set, take the display's own", which is what a fresh install and a new monitor both need,
    // and it is the same never-a-literal sentinel discipline LevelRules.NullSeed keeps. The two are
    // also the axis pair that decides the game's ASPECT, and therefore - on desktop, where nothing
    // rotates - the orientation the whole UI lays itself out for. That is the desktop equivalent of
    // turning a phone, and it is why this group and ScreenOrientationLock are two halves of one
    // feature rather than neighbours.
    //
    // THE REFRESH RATE IS DELIBERATELY NOT STORED. Screen.SetResolution without one keeps the
    // current rate, whereas a stored rate is wrong the moment the player moves the window to another
    // monitor - a whole class of bugs bought for a setting nobody asks for. The framerate cap that
    // players DO ask for already lives one level up, on GraphicsSettings.
    //
    // Nothing here resolves per platform the way TexturesGraphicsSettings' three Auto values do,
    // because there is nothing to resolve: a desktop honours all three and a phone honours none.

    /// <summary>
    /// How this device presents the game - window mode, window resolution, and how many pixels are
    /// actually rendered before being scaled to it. Desktop only.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class DisplayGraphicsSettings : IModel<DisplayGraphicsSettings>,
        IMoveable<DisplayGraphicsSettings>
    {
        /// <summary> Smallest render scale offered - a quarter of the pixels. </summary>
        public const float MinRenderScale = 0.5f;

        /// <summary> Largest render scale offered - supersampling. </summary>
        public const float MaxRenderScale = 2f;

        /// <summary> Resolution value meaning "not set": the display's own is used instead. </summary>
        public const int NativeResolution = 0;

        /// <summary> How the window occupies the display. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.WindowMode)]
        public WindowMode WindowMode { get; set; }

        /// <summary> Window width in pixels. <see cref="NativeResolution"/> = the display's own. </summary>
        [RuleMinValue(NativeResolution)]
        [JsonProperty(Names.ResolutionWidth)]
        public int ResolutionWidth { get; set; }

        /// <summary> Window height in pixels. <see cref="NativeResolution"/> = the display's own. </summary>
        [RuleMinValue(NativeResolution)]
        [JsonProperty(Names.ResolutionHeight)]
        public int ResolutionHeight { get; set; }

        /// <summary> How large the render target is relative to the window. 1 is native; below it
        /// trades sharpness for speed, above it supersamples. </summary>
        [RuleInRange(MinRenderScale, MaxRenderScale)]
        [JsonProperty(Names.RenderScale)]
        public float RenderScale { get; set; }

        /// <summary> Whether a resolution was authored at all, or the display's own is to be used. </summary>
        public bool HasResolution() =>
            ResolutionWidth > NativeResolution && ResolutionHeight > NativeResolution;

        public DisplayGraphicsSettings()
        {
            WindowMode = WindowMode.FullScreenWindow;
            ResolutionWidth = NativeResolution;
            ResolutionHeight = NativeResolution;
            RenderScale = 1f;
        }
        public DisplayGraphicsSettings(WindowMode windowMode, int resolutionWidth,
            int resolutionHeight, float renderScale)
        {
            WindowMode = windowMode;
            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;
            RenderScale = renderScale;
        }
    }
}
