using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Utils
{
    // The one place an ObjectId becomes a RectObject: a template's root is a field, never an Objects entry.
    // Not for loops that ENUMERATE a scope - the root is not content (Docs/PREFAB_PLACEMENT.md).
    public static class ObjectScopeExtensions
    {
        /// <summary> The object an id names in this scope, including a template's own root. False
        /// when nothing answers to it. </summary>
        public static bool TryGetObject(this IObjectScope scope, ObjectId objectId, out RectObject obj)
        {
            if (scope == null)
            {
                obj = null;
                return false;
            }

            if (objectId == ObjectId.PrefabRoot)
            {
                obj = scope is Prefab prefab ? prefab.Root : null;
                return obj != null;
            }

            return scope.Objects.TryGetValue(objectId, out obj);
        }

        // Keeps the throwing contract of the indexer it replaces: a caller reaching for an id it
        // has already established must not silently get null.

        /// <summary> The object an id names, root included. Throws on an id this scope has no object
        /// for, exactly as indexing <see cref="IObjectScope.Objects"/> does. </summary>
        public static RectObject GetObject(this IObjectScope scope, ObjectId objectId)
            => objectId == ObjectId.PrefabRoot && scope is Prefab prefab
                ? prefab.Root
                : scope.Objects[objectId];
    }
}
