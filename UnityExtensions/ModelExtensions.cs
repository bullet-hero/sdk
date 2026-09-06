using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using UnityEngine;

namespace BH.SDK
{
    public static class ModelExtensions
    {
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