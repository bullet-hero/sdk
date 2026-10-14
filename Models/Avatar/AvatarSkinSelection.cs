using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    // Its own file inside skins/, so the choice travels with the skins and UserSettings never changes.

    /// <summary> Which avatar skin is worn (<c>skins/selection.json</c>). </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.AvatarSkinSelection, ModelGenerations.V3_ImproveAppearance)]
    [GenerateModel]
    public sealed partial class AvatarSkinSelection : IModel<AvatarSkinSelection>
    {
        /// <summary> The worn skin's id: a file skin's or a built-in one. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.Skin)]
        public AvatarSkinId Skin { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarSkinSelection()
        {
            Skin = AvatarSkinId.Classic;
        }

        /// <summary> Built from the worn skin's id. </summary>
        public AvatarSkinSelection(AvatarSkinId skin)
        {
            Skin = skin;
        }
    }
}