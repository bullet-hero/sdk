using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// Base of every external asset a level carries. Holds no data itself - only where to fetch it
    /// from; the bytes live outside the level file, which is what keeps a level a folder of files
    /// rather than one huge blob.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public abstract partial class Resource : IModel<Resource>
    {
        /// <summary> How many fallback places ONE asset may be fetched from, tried in order. Small on purpose -
        /// each is a load attempt the player waits through. </summary>
        public const int MaxSourcesCount = 4;

        /// <summary> Where to look for the asset, in order - a local path, a URL, an addressable key.
        /// Several entries are fallbacks for one and the same asset, not several assets. </summary>
        [RuleNotNull, RuleCollectionMaxCount(MaxSourcesCount)]
        [JsonProperty(Names.Src)]
        public List<ResourceKey> Sources { get; set; }

        // A MEMBER WITH NO [JsonProperty] IS STILL SERIALIZED, under its C# name - this one wrote
        // "Type" with a capital T into every resource of every level, the only such key the naming
        // audit missed, because that audit read attributes and this member had none. Every
        // serialized member declares its key; see docs/NAMING.md.

        /// <summary> Which category this resource is, filled in by each subtype rather than stored -
        /// it is derivable from the type, so it never has to be kept in sync. </summary>
        [JsonProperty(Names.Type)]
        public abstract ResourceType Type { get; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        protected Resource()
        {
            Sources = new List<ResourceKey>();
        }
        /// <summary> Built from its sources. </summary>
        protected Resource(List<ResourceKey> sources)
        {
            Sources = sources;
        }
    }
}