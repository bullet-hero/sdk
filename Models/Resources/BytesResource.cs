using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// An arbitrary binary file the level ships with. The catch-all Resource subtype - nothing in
    /// the object model references it yet, it exists so payloads can be carried without a format bump.
    /// </summary>
    [RuleContainer]
    public class BytesResource : Resource, IModel<BytesResource>
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
        public override void Reset()
        {
            base.Reset();
            ByteResourceId = BytesResourceId.Null;
        }
        
        public override object Clone() => CopyImpl();
        public override Resource Copy() => CopyImpl();
        BytesResource ICopyable<BytesResource>.Copy() => CopyImpl();
        
        private BytesResource CopyImpl() => new(ByteResourceId, Sources.CopyList());

        public void Update(BytesResource src)
        {
            base.Update(src);

            ByteResourceId = src.ByteResourceId;
        }

        public void Pull(BytesResource src)
        {
            base.Pull(src);

            ByteResourceId = src.ByteResourceId;
        }

        public override bool Equals(object obj) => obj is BytesResource value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), ByteResourceId);

        public bool Equals(BytesResource other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) 
                         && ByteResourceId.Equals(other.ByteResourceId);
            return result;
        }
    }
}