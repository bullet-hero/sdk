using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;

namespace BH.SDK.Utils
{
    // WHAT A SELECTION NEEDS TO STAND ON ITS OWN. A prefab picked out of a level or a collection is
    // useless without the custom shapes it draws, the textures and fonts they use, the effects it
    // emits and the templates it nests - and until this existed, every one of those was left behind
    // and discovered as an unresolved reference after the import. The closure is transitive over
    // ResourceGraph's edges; only prefabs and effects have outgoing edges, so it is shallow except
    // through nested templates.
    //
    // ONLY WHAT THE SOURCE OWNS IS COLLECTED. A reference the source has no entry for is either
    // game-defined (every build ships it, nothing to carry) or already dangling (nothing to carry
    // either); in both cases the answer is to leave it out, and the caller's validation reports the
    // dangling kind the same way it always has.

    /// <summary> The user resources a set of roots reaches, across every family. </summary>
    public static class ResourceClosure
    {
        /// <summary> Every resource of <paramref name="source"/> the roots reach, roots included. A root
        /// the source does not own is dropped. Reads the models, never writes them. </summary>
        public static ResourceSet Collect(LevelResources source, IEnumerable<ResourceRef> roots)
        {
            var result = new ResourceSet();
            if (source == null || roots == null) return result;

            var pending = new Queue<ResourceRef>(roots);
            while (pending.Count > 0)
            {
                var reference = pending.Dequeue();
                if (result.Contains(reference) || !Owns(source, reference)) continue;

                result.Add(reference);
                Expand(source, reference, pending);
            }

            return result;
        }

        /// <summary> Whether the source holds an entry for this reference. </summary>
        public static bool Owns(LevelResources source, ResourceRef reference)
        {
            if (source == null || reference.IsNull) return false;

            return reference.Type switch
            {
                ResourceType.Texture => source.Textures.ContainsKey(reference.AsTexture),
                ResourceType.Font => source.Fonts.ContainsKey(reference.AsFont),
                ResourceType.Audio => source.Audios.ContainsKey(reference.AsAudio),
                ResourceType.Shape => source.CompositeShapes.ContainsKey(reference.AsShape),
                ResourceType.Theme => source.Themes.ContainsKey(reference.AsTheme),
                ResourceType.Effect => source.Effects.ContainsKey(reference.AsEffect),
                ResourceType.Prefab => source.Prefabs.ContainsKey(reference.AsPrefab),
                _ => false,
            };
        }

        private static void Expand(LevelResources source, ResourceRef reference, Queue<ResourceRef> pending)
        {
            ResourceRef Enqueue(ResourceRef edge)
            {
                pending.Enqueue(edge);
                return edge;
            }

            switch (reference.Type)
            {
                case ResourceType.Prefab:
                    ResourceGraph.Walk(source.Prefabs[reference.AsPrefab], Enqueue, modifications: true);
                    break;

                case ResourceType.Effect:
                    ResourceGraph.Walk(source.Effects[reference.AsEffect], Enqueue);
                    break;
            }
        }
    }
}
