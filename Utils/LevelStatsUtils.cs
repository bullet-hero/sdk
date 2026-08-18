using BH.SDK.Models;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Resources;

namespace BH.SDK.Utils
{
    /// <summary> What one object scope holds, counted by kind. <see cref="Total"/> is the object
    /// count itself; the four kind counters partition it. </summary>
    public readonly struct LevelObjectStats
    {
        /// <summary> Every object in the scope, placed prefab contents included - a materialized
        /// child is an ordinary object living in the same dictionary. </summary>
        public readonly int Total;

        /// <summary> Objects carrying no payload of their own - parents, anchors, pivots. </summary>
        public readonly int Transforms;
        public readonly int Shapes;
        public readonly int Texts;
        public readonly int Effects;

        /// <summary> Prefab PLACEMENTS. Their materialized contents are counted as the ordinary
        /// objects they are, not here. </summary>
        public readonly int Prefabs;

        /// <summary> Keyframes across every track of every object in the scope. </summary>
        public readonly int Keyframes;

        public LevelObjectStats(int total, int transforms, int shapes, int texts, int effects,
            int prefabs, int keyframes)
        {
            Total = total;
            Transforms = transforms;
            Shapes = shapes;
            Texts = texts;
            Effects = effects;
            Prefabs = prefabs;
            Keyframes = keyframes;
        }
    }

    /// <summary> How many user-defined resources a level carries, per collection. </summary>
    public readonly struct LevelResourceStats
    {
        public readonly int Textures;
        public readonly int Fonts;
        public readonly int Audios;
        public readonly int CompositeShapes;
        public readonly int Themes;
        public readonly int Effects;
        public readonly int Prefabs;

        /// <summary> Every collection summed - what "this level carries N resources" means. </summary>
        public int Total => Textures + Fonts + Audios + CompositeShapes + Themes + Effects + Prefabs;

        public LevelResourceStats(int textures, int fonts, int audios, int compositeShapes,
            int themes, int effects, int prefabs)
        {
            Textures = textures;
            Fonts = fonts;
            Audios = audios;
            CompositeShapes = compositeShapes;
            Themes = themes;
            Effects = effects;
            Prefabs = prefabs;
        }
    }

    /// <summary> A whole level's authored size: its objects, its scheduled audio, its resources. </summary>
    public readonly struct LevelStats
    {
        public readonly LevelObjectStats Objects;
        public readonly LevelResourceStats Resources;

        /// <summary> Scheduled audio tracks (Level.Audio), unrelated to how many play at once. </summary>
        public readonly int AudioTracks;

        public LevelStats(LevelObjectStats objects, LevelResourceStats resources, int audioTracks)
        {
            Objects = objects;
            Resources = resources;
            AudioTracks = audioTracks;
        }
    }

    // Counts what a level HOLDS, which is a different question from what it plays: LevelCapacityUtils
    // answers "how many objects are alive at the heaviest single frame" with a sweep over every span,
    // and is O(n log n) because of it. Everything here is one O(n) pass over the same dictionary with
    // no allocation, so a caller polling several times a second (a diagnostics readout) can afford it
    // and a caller sizing buffers still has to use LevelCapacityUtils instead.
    //
    // Null tolerance is deliberate throughout: this measures a level for display, and a half-built or
    // hand-edited one must produce a number rather than an exception.

    /// <summary>
    /// Measures how much a level (or one object scope inside it) actually contains - objects by kind,
    /// keyframes, audio tracks, resources. Advisory in the same sense LevelHints is: nothing decides
    /// anything from these numbers, they exist to be shown.
    /// </summary>
    public static class LevelStatsUtils
    {
        /// <summary> Measures a whole level. A null level, or any missing aggregate inside it, reads
        /// as zero rather than throwing. </summary>
        public static LevelStats Collect(Level level)
        {
            if (level == null) return new LevelStats();

            return new LevelStats(CollectObjects(level.Game), CollectResources(level.Resources),
                level.Audio?.Tracks?.Count ?? 0);
        }

        /// <summary> Measures one object scope - a level's own Game, or a Prefab's template. </summary>
        public static LevelObjectStats CollectObjects(IObjectScope scope)
        {
            var objects = scope?.Objects;
            if (objects == null) return new LevelObjectStats();

            var total = 0;
            var transforms = 0;
            var shapes = 0;
            var texts = 0;
            var effects = 0;
            var prefabs = 0;
            var keyframes = 0;

            foreach (var pair in objects)
            {
                var levelObject = pair.Value;
                if (levelObject == null) continue;

                total++;
                keyframes += CountKeyframes(levelObject);

                switch (levelObject.GetModelType())
                {
                    case ObjectType.ShapeObject: shapes++; break;
                    case ObjectType.TextObject: texts++; break;
                    case ObjectType.EffectObject: effects++; break;
                    case ObjectType.PrefabObject: prefabs++; break;
                    default: transforms++; break;
                }
            }

            return new LevelObjectStats(total, transforms, shapes, texts, effects, prefabs, keyframes);
        }

        /// <summary> Counts every collection of a level's resources. Null reads as zero. </summary>
        public static LevelResourceStats CollectResources(LevelResources resources)
        {
            if (resources == null) return new LevelResourceStats();

            return new LevelResourceStats(
                resources.Textures?.Count ?? 0,
                resources.Fonts?.Count ?? 0,
                resources.Audios?.Count ?? 0,
                resources.CompositeShapes?.Count ?? 0,
                resources.Themes?.Count ?? 0,
                resources.Effects?.Count ?? 0,
                resources.Prefabs?.Count ?? 0);
        }

        /// <summary> Keyframes on one object, across the seven shared tracks plus whatever its own
        /// type adds. An empty track is valid data (see RectObject), so this is genuinely zero for a
        /// static object rather than a sign of anything missing. </summary>
        public static int CountKeyframes(RectObject levelObject)
        {
            if (levelObject == null) return 0;

            var count = (levelObject.Positions?.Count ?? 0)
                        + (levelObject.Rotations?.Count ?? 0)
                        + (levelObject.Scales?.Count ?? 0)
                        + (levelObject.Sizes?.Count ?? 0)
                        + (levelObject.AnchorsMin?.Count ?? 0)
                        + (levelObject.AnchorsMax?.Count ?? 0)
                        + (levelObject.Pivots?.Count ?? 0);

            switch (levelObject)
            {
                case ShapeObject shape:
                    count += (shape.Colors?.Count ?? 0) + (shape.UVs?.Count ?? 0);
                    break;
                case TextObject text:
                    count += (text.Colors?.Count ?? 0) + (text.FontSizes?.Count ?? 0)
                             + (text.Fillments?.Count ?? 0) + (text.Appearings?.Count ?? 0);
                    break;
            }

            return count;
        }
    }
}
