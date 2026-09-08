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

        /// <summary> Which kind of resource this is. </summary>
        [JsonProperty(Names.Type)]
        public override ResourceType Type => ResourceType.Bytes;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public BytesResource()
        {
            ByteResourceId = BytesResourceId.Null;
        }
        /// <summary> Built from its resource id and sources. </summary>
        public BytesResource(BytesResourceId byteResourceId, List<ResourceKey> sources) : base(sources)
        {
            ByteResourceId = byteResourceId;
        }
    }
}