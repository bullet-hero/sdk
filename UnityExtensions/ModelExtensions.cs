using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using UnityEngine;

namespace BH.SDK
{
    /// <summary> The model questions that need an engine to answer - resolving a framerate target against the
    /// screen it will actually run on. </summary>
    public static class ModelExtensions
    {
        /// <summary> The framerate this actually runs at, resolving Default against its parent and then against the
        /// screen. </summary>
        public static int GetFramerate(this IFrameable frameable, IFrameable parentFrameable = null)
        {
            while (true)
            {
                switch (frameable.FpsTarget)
                {
                    case FramerateTarget.Default:
                    {
                        if (parentFrameable != null)
                        {
                            frameable = parentFrameable;
                            parentFrameable = null;
                            continue;
                        }

                        return (int)Math.Round(GetScreenHz());
                    }
                    case FramerateTarget.ScreenHz:
                    {
                        return (int)Math.Round(GetScreenHz());
                    }
                    case FramerateTarget.Fixed:
                    {
                        return frameable.FpsFixed;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        /// <summary> One frame of that framerate, in seconds. </summary>
        public static float GetDeltaTime(this IFrameable frameable, IFrameable parentFrameable = null)
        {
            while (true)
            {
                switch (frameable.FpsTarget)
                {
                    case FramerateTarget.Default:
                    {
                        if (parentFrameable != null)
                        {
                            frameable = parentFrameable;
                            parentFrameable = null;
                            continue;
                        }

                        return 1f / (float)GetScreenHz();
                    }
                    case FramerateTarget.ScreenHz:
                    {
                        return 1f / (float)GetScreenHz();
                    }
                    case FramerateTarget.Fixed:
                    {
                        return 1f / frameable.FpsFixed;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        // ON A PHONE THE CURRENT MODE IS AN ANSWER TO THIS APP, NOT A PROPERTY OF THE PANEL. Android
        // picks the refresh rate from what the foreground app asks for, and Unity asks through
        // targetFrameRate - so reading Screen.currentResolution at boot, before anything was asked,
        // got 60 on a 120 Hz phone, set targetFrameRate to 60, and the OS kept the panel at 60 for
        // good. A phone therefore takes the fastest mode the display reports; a desktop keeps the
        // current one, since there the mode is the player's own choice of monitor setting.

        /// <summary> The refresh rate ScreenHz means: the current mode on a desktop, the fastest supported
        /// one on a mobile platform (never below the current). </summary>
        private static double GetScreenHz()
        {
            var hz = Screen.currentResolution.refreshRateRatio.value;
            if (!Application.isMobilePlatform) return hz;

            foreach (var resolution in Screen.resolutions)
                hz = Math.Max(hz, resolution.refreshRateRatio.value);

            return hz;
        }
    }
}