namespace BH.SDK.Rules
{
    public static class PostProcessingRules
    {
        public const bool ActiveDefault = true;
        
        public static class Bloom
        {
            public const float IntensityMin = 0f;
            public const float IntensityMax = 10f;
            
            public const float ScatterMin = 0f;
            public const float ScatterMax = 1f;
        }
        public static class ChromaticAberration
        {
            public const float IntensityMin = 0f;
            public const float IntensityMax = 1f;
        }
        public static class Vignette
        {
            public const float CenterMin = 0f;
            public const float CenterMax = 1f;
            
            public const float IntensityMin = 0f;
            public const float IntensityMax = 1f;

            public const float SmoothnessMin = 0.01f;
            public const float SmoothnessMax = 1f;

            // The neutrals the model used to spell out in its constructor. They moved here when the
            // two sub-models started being born null: absent has to read back as these, and a
            // default that lives in two places is a default that can disagree with itself.
            public const float ColorR_Default = 0f;
            public const float ColorG_Default = 0f;
            public const float ColorB_Default = 0f;
            public const float CenterX_Default = 0.5f;
            public const float CenterY_Default = 0.5f;
        }
        public static class LensDistortion
        {
            public const float IntensityMin = -1f;
            public const float IntensityMax = 1f;
            
            public const float MultiplierMin = 0f;
            public const float MultiplierMax = 1f;
            
            public const float CenterMin = 0f;
            public const float CenterMax = 1f;
            
            public const float ScaleMin = 0.01f;
            public const float ScaleMax = 5f;

            public const float MultiplierX_Default = 1f;
            public const float MultiplierY_Default = 1f;
            public const float CenterX_Default = 0.5f;
            public const float CenterY_Default = 0.5f;
        }
        public static class FilmGrain
        {
            public const float IntensityMin = 0f;
            public const float IntensityMax = 1f;
        }
        public static class MotionBlur
        {
            public const float IntensityMin = 0f;
            public const float IntensityMax = 1f;
        }
        // One range for all eight curves rather than one per curve, because URP gives them one:
        // every ColorCurves curve is sampled out of a 128x1 texture whose axes are both 0..1, so a
        // key outside it is not a stronger effect, it is a value the texture clamps away. The two
        // neutrals differ though, and that is a real distinction rather than a naming one - the
        // YRGB curves are an absolute mapping and are neutral at IDENTITY, the hue/saturation ones
        // are an offset/multiplier and are neutral at a flat CurveNeutral.
        public static class ColorCurves
        {
            public const float CurveMin = 0f;
            public const float CurveMax = 1f;

            /// <summary> What HueVsHue/HueVsSat/SatVsSat/LumVsSat read as when nothing is authored:
            /// URP subtracts it from the hue curve and doubles the saturation ones, so a flat curve
            /// here shifts nothing and multiplies by one. </summary>
            public const float CurveNeutral = 0.5f;
        }
        public static class ShadowsMidtonesHighlights
        {
            public const float ShadowLimitMin = 0f;
            public const float ShadowLimitMax = 1f;
            
            public const float HighlightLimitMin = 0f;
            public const float HighlightLimitMax = 1f;

            public const float ColorR_Default = 1f;
            public const float ColorG_Default = 1f;
            public const float ColorB_Default = 1f;
            public const float ShadowLimitX_Default = 0f;
            public const float ShadowLimitY_Default = 0.3f;
            public const float HighlightLimitX_Default = 0.55f;
            public const float HighlightLimitY_Default = 1f;
        }
        public static class WhiteBalance
        {
            public const float TemperatureMin = -100f;
            public const float TemperatureMax = 100f;
            
            public const float TintMin = -100f;
            public const float TintMax = 100f;
        }
        public static class AnalogGlitch
        {
            public const float ScanLineJitterMin = 0f;
            public const float ScanLineJitterMax = 1f;
            
            public const float VerticalJumpMin = 0f;
            public const float VerticalJumpMax = 1f;
            
            public const float HorizontalShakeMin = 0f;
            public const float HorizontalShakeMax = 1f;
            
            public const float ColorDriftMin = 0f;
            public const float ColorDriftMax = 1f;
        }
        public static class DigitalGlitch
        {
            public const float IntensityMin = 0f;
            public const float IntensityMax = 1f;
        }
    }
}