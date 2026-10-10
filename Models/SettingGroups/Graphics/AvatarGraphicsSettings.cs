using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The body is 64 cells health takes apart; Render off collapses it to ONE square, health becoming
    // transparency. True everywhere: it is the main readable feedback of a run. Docs/SETTING_GROUPS.md

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
