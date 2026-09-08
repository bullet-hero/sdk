using System.Collections.Generic;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Models.Interfaces
{
    // PrefabObject placements (whether hand-placed in a level or nested inside another Prefab
    // template) live directly in Objects, identified by GetModelType() == ObjectType.PrefabObject -
    // there is no separate list, unlike the SDK's original design.

    /// <summary> A place objects live: a level's game half, or a prefab template. </summary>
    public interface IObjectScope
    {
        /// <summary> Every object in this scope, flat - the parent chain lives on the objects themselves. </summary>
        public Dictionary<ObjectId, RectObject> Objects { get; set; }
    }
}