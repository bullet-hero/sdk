using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    /// <summary> Many skins in one file - the presets a build ships, never a player's own. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.AvatarSkinPack, ModelGenerations.V3_ImproveAppearance)]
    [GenerateModel]
    public sealed partial class AvatarSkinPack : IModel<AvatarSkinPack>
    {
        /// <summary> The skins, in the order they are offered. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Skins)]
        public List<AvatarSkin> Skins { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarSkinPack()
        {
            Skins = new List<AvatarSkin>();
        }
    }
}
