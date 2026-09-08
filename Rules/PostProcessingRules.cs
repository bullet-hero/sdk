namespace BH.SDK.Rules
{
    /// <summary> Every bound a camera effect is authored within, one nested class per effect. </summary>
    public static class PostProcessingRules
    {
        /// <summary> The default for active, read by PostProcessingEvents, PostProcessingKeyframe. </summary>
        public const bool ActiveDefault = true;
        
        /// <summary> Bounds of Bloom. Heavy - phones do not like it. </summary>
        public static class Bloom
        {
            /// <summary> Lower bound of BloomKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of BloomKey.Intensity. </summary>
            public const float IntensityMax = 10f;
            
            /// <summary> Lower bound of BloomKey.Scatter. </summary>
            public const float ScatterMin = 0f;
            /// <summary> Upper bound of BloomKey.Scatter. </summary>
            public const float ScatterMax = 1f;
        }

        /// <summary> Bounds of chromatic aberration. </summary>
        public static class ChromaticAberration
        {
            /// <summary> Lower bound of ChromaticAberrationKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of ChromaticAberrationKey.Intensity. </summary>
            public const float IntensityMax = 1f;
        }

        /// <summary> Bounds of the vignette. </summary>
        public static class Vignette
        {
            /// <summary> Lower bound of VignetteKey.Center. </summary>
            public const float CenterMin = 0f;
            /// <summary> Upper bound of VignetteKey.Center. </summary>
            public const float CenterMax = 1f;
            
            /// <summary> Lower bound of VignetteKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of VignetteKey.Intensity. </summary>
            public const float IntensityMax = 1f;

            /// <summary> Lower bound of VignetteKey.Smoothness. </summary>
            public const float SmoothnessMin = 0.01f;
            /// <summary> Upper bound of VignetteKey.Smoothness. </summary>
            public const float SmoothnessMax = 1f;

            // The neutrals the model used to spell out in its constructor. They moved here when the
            // two sub-models started being born null: absent has to read back as these, and a
            // default that lives in two places is a default that can disagree with itself.

            /// <summary> The color R used when nothing says otherwise, read by PostProcessingColorTests. </summary>
            public const float ColorR_Default = 0f;
            /// <summary> The color G used when nothing says otherwise, read by PostProcessingColorTests. </summary>
            public const float ColorG_Default = 0f;
            /// <summary> The color B used when nothing says otherwise, read by PostProcessingColorTests. </summary>
            public const float ColorB_Default = 0f;
            /// <summary> The center X used when nothing says otherwise. </summary>
            public const float CenterX_Default = 0.5f;
            /// <summary> The center Y used when nothing says otherwise. </summary>
            public const float CenterY_Default = 0.5f;
        }

        /// <summary> Bounds of lens distortion. </summary>
        public static class LensDistortion
        {
            /// <summary> Lower bound of LensDistortionKey.Intensity. </summary>
            public const float IntensityMin = -1f;
            /// <summary> Upper bound of LensDistortionKey.Intensity. </summary>
            public const float IntensityMax = 1f;
            
            /// <summary> Lower bound of LensDistortionKey.Multiplier. </summary>
            public const float MultiplierMin = 0f;
            /// <summary> Upper bound of LensDistortionKey.Multiplier. </summary>
            public const float MultiplierMax = 1f;
            
            /// <summary> Lower bound of LensDistortionKey.Center. </summary>
            public const float CenterMin = 0f;
            /// <summary> Upper bound of LensDistortionKey.Center. </summary>
            public const float CenterMax = 1f;
            
            /// <summary> Lower bound of LensDistortionKey.Scale. </summary>
            public const float ScaleMin = 0.01f;
            /// <summary> Upper bound of LensDistortionKey.Scale. </summary>
            public const float ScaleMax = 5f;

            /// <summary> The multiplier X used when nothing says otherwise. </summary>
            public const float MultiplierX_Default = 1f;
            /// <summary> The multiplier Y used when nothing says otherwise. </summary>
            public const float MultiplierY_Default = 1f;
            /// <summary> The center X used when nothing says otherwise. </summary>
            public const float CenterX_Default = 0.5f;
            /// <summary> The center Y used when nothing says otherwise. </summary>
            public const float CenterY_Default = 0.5f;
        }

        /// <summary> Bounds of film grain. </summary>
        public static class FilmGrain
        {
            /// <summary> Lower bound of FilmGrainKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of FilmGrainKey.Intensity. </summary>
            public const float IntensityMax = 1f;
        }

        /// <summary> Bounds of motion blur. Heavy - phones do not like it. </summary>
        public static class MotionBlur
        {
            /// <summary> Lower bound of MotionBlurKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of MotionBlurKey.Intensity. </summary>
            public const float IntensityMax = 1f;
        }
        // One range for all eight curves rather than one per curve, because URP gives them one:
        // every ColorCurves curve is sampled out of a 128x1 texture whose axes are both 0..1, so a
        // key outside it is not a stronger effect, it is a value the texture clamps away. The two
        // neutrals differ though, and that is a real distinction rather than a naming one - the
        // YRGB curves are an absolute mapping and are neutral at IDENTITY, the hue/saturation ones
        // are an offset/multiplier and are neutral at a flat CurveNeutral.

        /// <summary> Bounds of the colour curves. </summary>
        public static class ColorCurves
        {
            /// <summary> Lowest curve allowed, read by ABPostProcessingMap, ColorCurvesKey, ColorCurvesKeyTests. </summary>
            public const float CurveMin = 0f;
            /// <summary> Highest curve allowed, read by ABPostProcessingMap, ColorCurvesKey, ColorCurvesKeyTests. </summary>
            public const float CurveMax = 1f;

            /// <summary> What HueVsHue/HueVsSat/SatVsSat/LumVsSat read as when nothing is authored:
            /// URP subtracts it from the hue curve and doubles the saturation ones, so a flat curve
            /// here shifts nothing and multiplies by one. </summary>
            public const float CurveNeutral = 0.5f;
        }

        /// <summary> Bounds of the shadows/midtones/highlights grade. </summary>
        public static class ShadowsMidtonesHighlights
        {
            /// <summary> Lower bound of ShadowsMidtonesHighlightsKey.ShadowLimits. </summary>
            public const float ShadowLimitMin = 0f;
            /// <summary> Upper bound of ShadowsMidtonesHighlightsKey.ShadowLimits. </summary>
            public const float ShadowLimitMax = 1f;
            
            /// <summary> Lower bound of ShadowsMidtonesHighlightsKey.HighlightLimits. </summary>
            public const float HighlightLimitMin = 0f;
            /// <summary> Upper bound of ShadowsMidtonesHighlightsKey.HighlightLimits. </summary>
            public const float HighlightLimitMax = 1f;

            /// <summary> The color R used when nothing says otherwise, read by PostProcessingColorTests. </summary>
            public const float ColorR_Default = 1f;
            /// <summary> The color G used when nothing says otherwise. </summary>
            public const float ColorG_Default = 1f;
            /// <summary> The color B used when nothing says otherwise. </summary>
            public const float ColorB_Default = 1f;
            /// <summary> The shadow limit X used when nothing says otherwise. </summary>
            public const float ShadowLimitX_Default = 0f;
            /// <summary> The shadow limit Y used when nothing says otherwise. </summary>
            public const float ShadowLimitY_Default = 0.3f;
            /// <summary> The highlight limit X used when nothing says otherwise. </summary>
            public const float HighlightLimitX_Default = 0.55f;
            /// <summary> The highlight limit Y used when nothing says otherwise. </summary>
            public const float HighlightLimitY_Default = 1f;
        }

        /// <summary> Bounds of white balance. </summary>
        public static class WhiteBalance
        {
            /// <summary> Lower bound of WhiteBalanceKey.Temperature. </summary>
            public const float TemperatureMin = -100f;
            /// <summary> Upper bound of WhiteBalanceKey.Temperature. </summary>
            public const float TemperatureMax = 100f;
            
            /// <summary> Lower bound of WhiteBalanceKey.Tint. </summary>
            public const float TintMin = -100f;
            /// <summary> Upper bound of WhiteBalanceKey.Tint. </summary>
            public const float TintMax = 100f;
        }

        /// <summary> Bounds of the analog glitch. Heavy - phones do not like it. </summary>
        public static class AnalogGlitch
        {
            /// <summary> Lower bound of AnalogGlitchKey.ScanLineJitter. </summary>
            public const float ScanLineJitterMin = 0f;
            /// <summary> Upper bound of AnalogGlitchKey.ScanLineJitter. </summary>
            public const float ScanLineJitterMax = 1f;
            
            /// <summary> Lower bound of AnalogGlitchKey.VerticalJump. </summary>
            public const float VerticalJumpMin = 0f;
            /// <summary> Upper bound of AnalogGlitchKey.VerticalJump. </summary>
            public const float VerticalJumpMax = 1f;
            
            /// <summary> Lower bound of AnalogGlitchKey.HorizontalShake. </summary>
            public const float HorizontalShakeMin = 0f;
            /// <summary> Upper bound of AnalogGlitchKey.HorizontalShake. </summary>
            public const float HorizontalShakeMax = 1f;
            
            /// <summary> Lower bound of AnalogGlitchKey.ColorDrift. </summary>
            public const float ColorDriftMin = 0f;
            /// <summary> Upper bound of AnalogGlitchKey.ColorDrift. </summary>
            public const float ColorDriftMax = 1f;
        }

        /// <summary> Bounds of the digital glitch. Heavy - phones do not like it. </summary>
        public static class DigitalGlitch
        {
            /// <summary> Lower bound of DigitalGlitchKey.Intensity. </summary>
            public const float IntensityMin = 0f;
            /// <summary> Upper bound of DigitalGlitchKey.Intensity. </summary>
            public const float IntensityMax = 1f;
        }
    }
}