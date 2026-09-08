using BH.SDK.Models.Primitives;

namespace BH.SDK.Models.Interfaces
{
    // Implemented by whichever model owns an ObjectId namespace of its own (LevelSettings for a
    // level's own objects, Prefab for a template's objects) - lets BH.Core.Services.PrefabMaterializer
    // mint fresh, permanent object ids for a materialized prefab instance without caring whether the
    // instance is hosted directly by a level or nested inside another prefab's template.

    /// <summary> Mints object ids for one scope's own id namespace. </summary>
    public interface IObjectIdCounter
    {
        /// <summary> The next unused id in this scope, consuming it. </summary>
        public ObjectId GetNextObjectId();
    }
}
