using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The desktop half: inert on mobile, so disabled there, not hidden. Resolution is two ints, zero meaning
    // "the display's own"; the refresh rate is NOT stored. Docs/SETTING_GROUPS.md

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

        // Off (the zero value) is what the game did unconditionally before there was a choice.

        /// <summary> Whether presentation waits for the display's refresh. Desktop only - Unity
        /// ignores vSyncCount on phones - and it OUTRANKS
        /// <see cref="GraphicsSettings.FpsTarget"/>: Unity ignores
        /// targetFrameRate whenever this is not Off. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.VSync)]
        public VSyncMode VSync { get; set; }

        /// <summary> Whether a resolution was authored at all, or the display's own is to be used. </summary>
        public bool HasResolution() =>
            ResolutionWidth > NativeResolution && ResolutionHeight > NativeResolution;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public DisplayGraphicsSettings()
        {
            WindowMode = WindowMode.Windowed;
            ResolutionWidth = NativeResolution;
            ResolutionHeight = NativeResolution;
            RenderScale = 1f;
            VSync = VSyncMode.Off;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public DisplayGraphicsSettings(WindowMode windowMode, int resolutionWidth,
            int resolutionHeight, float renderScale, VSyncMode vSync)
        {
            VSync = vSync;
            WindowMode = windowMode;
            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;
            RenderScale = renderScale;
        }
    }
}