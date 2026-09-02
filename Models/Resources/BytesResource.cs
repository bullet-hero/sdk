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
    /// An arbitrary binary file the level ships with. The catch-all Resource subtype - nothing in
    /// the object model references it yet, it exists so payloads can be carried without a format bump.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class BytesResource : Resource, IModel<BytesResource>
    {
        /// <summary> Identity of this blob, capped to the user-defined range like every level
        /// resource. </summary>
        [RuleIPrimitiveIntMax(BytesResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.ByteResourceId)]
        public BytesResourceId ByteResourceId { get; set; }

        public override ResourceType Type => ResourceType.Bytes;

        public BytesResource()
        {
            ByteResourceId = BytesResourceId.Null;
        }
        public BytesResource(BytesResourceId byteResourceId, List<ResourceKey> sources) : base(sources)
        {
            ByteResourceId = byteResourceId;
        }
    }
}