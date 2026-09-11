using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // WHAT THE AVATAR COSTS TO DRAW, WHICH IS ONE DECISION TODAY. The body is a grid of 64 cells
    // that health takes apart (Services.Shared.Avatars' ShatterGridMath); off, that grid collapses
    // to ONE square and health becomes transparency instead. Same reading, a sixty-fourth of the
    // objects, and nothing for a weak device to overdraw.
    //
    // TRUE EVERYWHERE, including phones. 64 inframe squares is not a lot next to what a level draws,
    // and the effect is the game's main piece of readable feedback about how a run is going - so it
    // ships on and the switch is there for the device that disagrees, not as a platform default.
    //
    // Inherited Render is that switch. It is not a "draw the avatar" flag: an avatar that is not
    // drawn is not a graphics option, it is a broken run.

    /// <summary>
    /// How the player avatar is drawn: whether its body comes apart cell by cell as health falls.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AvatarGraphicsSettings : BaseGraphicsSettings,
        IModel<AvatarGraphicsSettings>, IMoveable<AvatarGraphicsSettings>
    {
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarGraphicsSettings()
        {
            Render = true;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public AvatarGraphicsSettings(bool render) : base(render)
        {
        }
    }
}
