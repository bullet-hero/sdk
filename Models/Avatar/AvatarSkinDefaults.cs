using BH.SDK.Models.Values;

namespace BH.SDK.Models
{
    // THE ONE OWNER of the classic avatar's colours. Methods, not fields: Color4Value is a mutable class.

    /// <summary> The classic avatar's colours - what every skin starts from. </summary>
    public static class AvatarSkinDefaults
    {
        /// <summary> The body, in every slot. </summary>
        public static Color4Value Body() => new(0.5137255f, 0.7607844f, 0.9568628f, 1f);

        /// <summary> The eyes. </summary>
        public static Color4Value Eyes() => new(1f, 1f, 1f, 1f);

        /// <summary> The hands. </summary>
        public static Color4Value Hands() => new(0.8039216f, 0.9254903f, 0.9529412f, 1f);

        /// <summary> The trail left while moving. </summary>
        public static Color4Value MoveEffect() => new(0f, 0.6f, 0.9568628f, 0.6f);

        /// <summary> The trail left by a dash. </summary>
        public static Color4Value DashEffect() => new(0.162f, 0.422f, 1f, 0.6f);

        /// <summary> The burst a hit throws off - a signal, kept red. </summary>
        public static Color4Value DamageEffect() => new(1f, 0.269f, 0.209f, 0.8f);

        /// <summary> The shadow under the body. </summary>
        public static Color4Value Shadow() => new(0f, 0f, 0f, 0.5f);

        /// <summary> What the body is tinted towards while dashing. </summary>
        public static Color4Value DashBody() => new(0.4708f, 0.6752f, 0.9773f, 1f);
    }
}
