using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // Joined the unshipped generation V2_SimplifyEntrance, like InterfaceSettings.LevelsLayout: a V1
    // file reads back without the group, i.e. the mode off and every colour at its default.
    //
    // NOT a BaseGraphicsSettings, for AntiAliasingGraphicsSettings' reason: an inherited Render would
    // read as "is this drawn", which is the opposite of what switching a MODE on means. Active is the
    // one switch, and it is off by default.
    //
    // THE HUE USED TO BE A HARD-CODED CONSTANT ON PURPOSE ("a colour field is a way for it to end up
    // invisible"), on the editor's collider overlay. It is a setting now at the author's request,
    // shared by that overlay and this mode. The guard against an invisible fill is the consumer's
    // CollidersOnlyMath.MinAlpha plus the settings Reset, not the absence of the field.
    //
    // THE ALPHA IS READ ONLY WHILE UseAlpha IS ON. Off, the fill is opaque and the stored alpha is kept
    // untouched for when it is turned back on.
    //
    // THE BACKGROUND IS NOT BLACK on purpose: the letterbox around the camera's rect is black, and a
    // black clear colour would hide where the level's own frame ends.

    /// <summary>
    /// Colliders Only - a practice mode for the game screen: every level shape is drawn as its
    /// collider in one flat colour over a plain background, and nothing else that is only visual.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class CollidersModeGraphicsSettings : IModel<CollidersModeGraphicsSettings>,
        IMoveable<CollidersModeGraphicsSettings>
    {
        /// <summary> Whether the mode is on. </summary>
        [JsonProperty(Names.Active)]
        public bool Active { get; set; }

        /// <summary> The fill colour; its RGB also colours the editor's collider overlays. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorWord)]
        public Color4Value Color { get; set; }

        /// <summary> Whether the fill uses <see cref="Color"/>'s alpha. Off, the fill is opaque. </summary>
        [JsonProperty(Names.UseAlpha)]
        public bool UseAlpha { get; set; }

        /// <summary> What the camera clears to while the mode is on, in place of the theme background. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Background)]
        public Color3Value Background { get; set; }

        /// <summary> What the fill's alpha is worth before a player touches it. </summary>
        public const float DefaultAlpha = 0.6f;

        /// <summary> The grey the default background is, on every channel. </summary>
        public const float DefaultBackgroundGrey = 0.15f;

        /// <summary> The fill colour before a player touches it. A method rather than a static field,
        /// because <see cref="Color4Value"/> is a mutable class. </summary>
        public static Color4Value DefaultColor() => new(1f, 0.25f, 0.2f, DefaultAlpha);

        /// <summary> The background before a player touches it - dark grey, see the header. </summary>
        public static Color3Value DefaultBackground() =>
            new(DefaultBackgroundGrey, DefaultBackgroundGrey, DefaultBackgroundGrey);

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CollidersModeGraphicsSettings()
        {
            Active = false;
            Color = DefaultColor();
            UseAlpha = true;
            Background = DefaultBackground();
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public CollidersModeGraphicsSettings(bool active, Color4Value color, bool useAlpha, Color3Value background)
        {
            Active = active;
            Color = color;
            UseAlpha = useAlpha;
            Background = background;
        }
    }
}
