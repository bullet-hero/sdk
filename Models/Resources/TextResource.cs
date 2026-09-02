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

        public override ResourceType Type => ResourceType.Text;

        public TextResource()
        {
            TextResourceId = TextResourceId.Null;
        }
        public TextResource(TextResourceId textResourceId, List<ResourceKey> sources) : base(sources)
        {
            TextResourceId = textResourceId;
        }
    }
}