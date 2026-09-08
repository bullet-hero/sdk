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
    /// A text file shipped with the level, for copy too long to keep inline in a TextObject
    /// (credits, story text). Kept out of the level file so editing it does not rewrite the level.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TextResource : Resource, IModel<TextResource>
    {
        /// <summary> Identity of this text file within the level. </summary>
        [RuleIPrimitiveIntMax(TextResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.TextResourceId)]
        public TextResourceId TextResourceId { get; set; }

        /// <summary> Which kind of resource this is. </summary>
        [JsonProperty(Names.Type)]
        public override ResourceType Type => ResourceType.Text;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public TextResource()
        {
            TextResourceId = TextResourceId.Null;
        }
        /// <summary> Built from its resource id and sources. </summary>
        public TextResource(TextResourceId textResourceId, List<ResourceKey> sources) : base(sources)
        {
            TextResourceId = textResourceId;
        }
    }
}