using System.Collections.Generic;
using BH.SDK.Models.Collections;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Collections
{
    // ONE COLLECTION IN MEMORY, SHAPED LIKE A LEVEL'S RESOURCES. Its data resources and the file
    // resources its manifest lists are gathered into a LevelResources, and that is the whole trick:
    // ResourceClosure, ResourceRemap and every planner above them already speak LevelResources, so a
    // collection is walked by exactly the code that walks a level. The three file dictionaries ARE the
    // manifest's (shared instances, not copies), so an edit through either reaches both.

    /// <summary> Why one file of a collection was left out of what was read. </summary>
    public enum CollectionEntryProblem
    {
        /// <summary> Written by a newer build - left out, never degraded. </summary>
        NewerGeneration = 0,

        /// <summary> Not a readable envelope of its kind. </summary>
        Damaged = 1,

        /// <summary> Its file name is not a guid, or not the id inside it. </summary>
        IdMismatch = 2,
    }

    /// <summary> A file that was skipped, and why. </summary>
    public readonly struct CollectionSkippedEntry
    {
        /// <summary> The store path of the skipped file. </summary>
        public readonly string Path;

        /// <summary> What was wrong with it. </summary>
        public readonly CollectionEntryProblem Problem;

        /// <summary> Built from its path and problem. </summary>
        public CollectionSkippedEntry(string path, CollectionEntryProblem problem)
        {
            Path = path;
            Problem = problem;
        }
    }

    /// <summary> A collection read into memory: its manifest, every resource it carries, and the
    /// store its media files are read from. </summary>
    public sealed class CollectionContent
    {
        /// <summary> The manifest. </summary>
        public ResourceCollection Manifest { get; }

        /// <summary> Every resource, data and file alike. The file dictionaries are the manifest's own. </summary>
        public LevelResources Resources { get; }

        /// <summary> The format the manifest was stored in - what a rewrite keeps. </summary>
        public SerializationType Format { get; }

        /// <summary> Where the files are - media paths in the manifest resolve against it. </summary>
        public IContentStore Store { get; }

        /// <summary> Files that were not read, and why. </summary>
        public IReadOnlyList<CollectionSkippedEntry> Skipped { get; }

        /// <summary> Built from its parts. The resources' file dictionaries are replaced by the
        /// manifest's, so there is one copy of each. </summary>
        public CollectionContent(ResourceCollection manifest, LevelResources resources, SerializationType format,
            IContentStore store, IReadOnlyList<CollectionSkippedEntry> skipped = null)
        {
            Manifest = manifest;
            Resources = resources ?? new LevelResources();
            Resources.Textures = manifest.Textures;
            Resources.Fonts = manifest.Fonts;
            Resources.Audios = manifest.Audios;
            Format = format;
            Store = store;
            Skipped = skipped ?? new List<CollectionSkippedEntry>();
        }

        /// <summary> A new, empty collection held in a store of the caller's choosing. </summary>
        public static CollectionContent CreateEmpty(IContentStore store, SerializationType format)
            => new(new ResourceCollection(), new LevelResources(), format, store);
    }
}
