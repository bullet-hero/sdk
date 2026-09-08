using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// A typeface the level brings with it, so text renders the same on a device that has never
    /// seen that font. Referenced by TextObject.FontResourceId.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FontResource : Resource, IModel<FontResource>
    {
        /// <summary> Identity of this font within the level. </summary>
        [RuleIPrimitiveIntMax(FontResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.FontResourceId)]
        public FontResourceId FontResourceId { get; set; }

        /// <summary> Which kind of resource this is. </summary>
        [JsonProperty(Names.Type)]
        public override ResourceType Type => ResourceType.Font;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public FontResource()
        {
            FontResourceId = FontResourceId.Null;
        }
        /// <summary> Built from its resource id and sources. </summary>
        public FontResource(FontResourceId fontResourceId, List<ResourceKey> sources) : base(sources)
        {
            FontResourceId = fontResourceId;
        }
    }
}