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
        /// <summary> How many shape objects the level holds. </summary>
        public readonly int Shapes;
        /// <summary> How many text objects. </summary>
        public readonly int Texts;
        /// <summary> How many effect objects the level holds. </summary>
        public readonly int Effects;

        /// <summary> Prefab PLACEMENTS. Their materialized contents are counted as the ordinary
        /// objects they are, not here. </summary>
        public readonly int Prefabs;

        /// <summary> Keyframes across every track of every object in the scope. </summary>
        public readonly int Keyframes;

        /// <summary> Every member at once, in declaration order. </summary>
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
        /// <summary> How many images. </summary>
        public readonly int Textures;
        /// <summary> How many fonts. </summary>
        public readonly int Fonts;
        /// <summary> How many audio files. </summary>
        public readonly int Audios;
        /// <summary> How many shapes the level authored itself. </summary>
        public readonly int CompositeShapes;
        /// <summary> How many themes. </summary>
        public readonly int Themes;
        /// <summary> How many effect resources it carries. </summary>
        public readonly int Effects;
        /// <summary> How many prefab templates the level carries. </summary>
        public readonly int Prefabs;

        /// <summary> Every collection summed - what "this level carries N resources" means. </summary>
        public int Total => Textures + Fonts + Audios + CompositeShapes + Themes + Effects + Prefabs;

        /// <summary> Every member at once, in declaration order. </summary>
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
        /// <summary> What the level's objects add up to. </summary>
        public readonly LevelObjectStats Objects;
        /// <summary> What its resources add up to. </summary>
        public readonly LevelResourceStats Resources;

        /// <summary> Scheduled audio tracks (Level.Audio), unrelated to how many play at once. </summary>
        public readonly int AudioTracks;

        /// <summary> Built from its objects, resources and tracks. </summary>
        public LevelStats(LevelObjectStats objects, LevelResourceStats resources, int audioTracks)
        {
            Objects = objects;
            Resources = resources;
            AudioTracks = audioTracks;
        }
    }

    // What a level HOLDS, not what it plays: one O(n) pass with no allocation, cheap enough to poll
    // (LevelCapacityUtils is the O(n log n) sweep for sizing). Null-tolerant on purpose: a half-built level
    // must produce a number.

    /// <summary>
    /// How much a level (or one object scope) contains: objects by kind, keyframes, audio, resources.
    /// Advisory like LevelHints: nothing decides from these numbers.
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
