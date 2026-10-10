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
    /// Base of every external asset a level carries. Holds only where to fetch it; the bytes live outside
    /// the level file, which keeps a level a folder of files rather than one blob.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public abstract partial class Resource : IModel<Resource>
    {
        /// <summary> How many fallback places ONE asset may be fetched from, tried in order. Small on purpose -
        /// each is a load attempt the player waits through. </summary>
        public const int MaxSourcesCount = 4;

        /// <summary> Where to look for the asset, in order - a level path, an absolute path, a URL, a StreamingAssets path.
        /// Several entries are fallbacks for one and the same asset, not several assets. </summary>
        [RuleNotNull, RuleCollectionMaxCount(MaxSourcesCount)]
        [JsonProperty(Names.Src)]
        public List<ResourceKey> Sources { get; set; }

        // A member with no [JsonProperty] is still serialized under its C# name; every member declares a
        // key (Docs/NAMING.md).

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