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

        public override ResourceType Type => ResourceType.Font;

        public FontResource()
        {
            FontResourceId = FontResourceId.Null;
        }
        public FontResource(FontResourceId fontResourceId, List<ResourceKey> sources) : base(sources)
        {
            FontResourceId = fontResourceId;
        }
    }
}