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
                        
                        return (int)Math.Round(Screen.currentResolution.refreshRateRatio.value);
                    }
                    case FramerateTarget.ScreenHz:
                    {
                        return (int)Math.Round(Screen.currentResolution.refreshRateRatio.value);
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
                        
                        return 1f / (float)Screen.currentResolution.refreshRateRatio.value;
                    }
                    case FramerateTarget.ScreenHz:
                    {
                        return 1f / (float)Screen.currentResolution.refreshRateRatio.value;
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
    }
}