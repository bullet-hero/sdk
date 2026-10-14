using System.Collections.Generic;
using BH.SDK.Models;

namespace BH.SDK.Services.Skins
{
    // Curated by eye from contact sheets: the silhouettes first, they are the body shapes the eyes suit.
    // A patterned preset is an abstract body, eyes off. No two presets share a form (AvatarSkinPresetsTests).

    /// <summary> The shipped skin presets, built from one table: sliced outlines, then generator seeds.
    /// Pure and deterministic, so a pack built twice is the same pack. </summary>
    public static class AvatarSkinPresets
    {
        /// <summary> The silhouettes, as outline and grid side, in shown order. </summary>
        public static readonly (string Outline, int Side)[] Silhouettes =
        {
            ("circle", 8), ("triangle", 8), ("triangle-down", 8), ("diamond", 6), ("pentagon", 8), ("hexagon", 7),
            ("octagon", 8), ("dome", 8), ("rounded", 6), ("shield", 8), ("drop", 8),
        };

        /// <summary> The patterned presets, as <see cref="AvatarSkinGenerator"/> seeds, in shown order.
        /// </summary>
        public static readonly uint[] Seeds =
        {
            195, 82, 114, 56, 79, 175, 162, 231, 226, 192, 211, 65,
            55, 298, 156, 72, 155, 234, 84, 24, 305, 207, 28,
        };

        // Far above any seed, so a silhouette's id never meets a patterned preset's.
        private const uint SilhouetteIdBase = 0x5100_0000u;

        /// <summary> Every preset: <see cref="Silhouettes"/>, then <see cref="Seeds"/>. </summary>
        public static List<AvatarSkin> Build()
        {
            var presets = new List<AvatarSkin>(Silhouettes.Length + Seeds.Length);
            for (var i = 0; i < Silhouettes.Length; i++)
            {
                var (outline, side) = Silhouettes[i];
                var seed = SilhouetteIdBase + (uint)i;
                var skin = AvatarSkinSilhouettes.Build(outline, side, seed);
                skin.Id = AvatarSkinGenerator.PresetId(seed);
                presets.Add(skin);
            }

            foreach (var seed in Seeds)
            {
                var skin = AvatarSkinGenerator.Generate(seed);
                skin.Elements = AvatarSkinElements.Classic & ~AvatarSkinElements.Eyes;
                presets.Add(skin);
            }

            return presets;
        }
    }
}
