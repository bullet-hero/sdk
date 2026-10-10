using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;

namespace BH.SDK.Utils
{
    // What a selection needs to stand alone, transitive over ResourceGraph. Only what the SOURCE OWNS is
    // collected: game-defined and dangling references are left out (Docs/RESOURCE_GRAPH.md).

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
