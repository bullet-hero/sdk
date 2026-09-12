using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Utils
{
    // THE ONE PLACE AN ObjectId BECOMES A RectObject, and it exists because a template's root is not
    // an entry in Objects and never may be (see Prefab.Root). Every other object in the format is
    // addressed by a dictionary lookup; the root is addressed by ObjectId.PrefabRoot and answered by
    // a field, so anything that turns an id into an object has to come through here or the root is
    // invisible to it - which for an editor means a row nobody can select, inspect or keyframe.
    //
    // Deliberately NOT applied to the many loops that ENUMERATE a scope's contents. Those ask a
    // different question - what does this template hold - and the root is not content, it is what
    // the content hangs off. Materialization, packing, depth and the graph pass all walk
    // Objects.Values and must keep walking exactly that.
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
