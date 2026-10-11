using System;
using System.Collections.Generic;

namespace BH.SDK.Agent
{
    // The guide is never a third copy of the docs: a topic names public pages, rendered in the Unity Editor
    // into StreamingAssets and served by guide.get. index and context are generated, not pages.

    /// <summary> The topics of the AI's guide, each mapped to public doc pages (paths under the docs'
    /// <c>en/</c> folder). </summary>
    public static class AgentGuideManifest
    {
        /// <summary> The folder under StreamingAssets the rendered topics live in. </summary>
        public const string Folder = "mcp-guide";

        /// <summary> The topic file's extension. </summary>
        public const string Extension = ".md";

        /// <summary> Lists every topic; generated. </summary>
        public const string Index = "index";

        /// <summary> The <c>CONTEXT.md</c> skeleton, from <see cref="AiContextDocument"/>. </summary>
        public const string Context = "context";

        /// <summary> The bridge itself: connecting, the AI's rights, author priority, the <c>ai/</c> folder.
        /// </summary>
        public const string Mcp = "mcp";

        /// <summary> One topic: its name, what it answers, and its pages in reading order. </summary>
        public sealed class Topic
        {
            public string Name { get; }
            public string Summary { get; }
            public IReadOnlyList<string> Pages { get; }

            public Topic(string name, string summary, params string[] pages)
            {
                Name = name;
                Summary = summary;
                Pages = pages;
            }
        }

        /// <summary> Every topic, in the order the index lists them. </summary>
        public static readonly IReadOnlyList<Topic> Topics = new[]
        {
            new Topic(Index, "this list"),
            new Topic("coordinates", "objects, parents, layers, the camera and the screen",
                "docs/2_editor/1_basics/3_how-the-editor-thinks.md",
                "docs/2_editor/4_craft/4_composition-and-camera.md",
                "docs/2_editor/7_reference/5_object-properties.md"),
            new Topic("time", "frames, spans, the level's length and fps, keyframes and easing",
                "docs/2_editor/1_basics/4_frames-and-time.md",
                "docs/2_editor/1_basics/5_keyframes-and-easing.md"),
            new Topic("randomness", "seeds and random values: what plays the same everywhere",
                "docs/1_game/8_determinism.md",
                "docs/2_editor/1_basics/5_keyframes-and-easing.md"),
            new Topic("prefabs", "templates, placements, overrides and Prefab Mode",
                "docs/2_editor/5_workflow/2_reuse.md",
                "docs/2_editor/7_reference/12_prefab-mode.md"),
            new Topic("shapes", "built-in and level shapes, colliders, images",
                "docs/2_editor/7_reference/11_level-resources.md",
                "docs/2_editor/2_resources/3_images-and-fonts.md"),
            new Topic("avatar", "the player's speed, dash, hitbox and damage, in world units",
                "docs/1_game/6_avatar.md",
                "docs/1_game/7_damage.md"),
            new Topic("generators", "generators and modifiers: content from a few parameters",
                "docs/2_editor/7_reference/8_generators.md",
                "docs/2_editor/7_reference/9_modifiers.md"),
            new Topic("design", "readability, rhythm, difficulty, colour, order of work, budget",
                "docs/2_editor/4_craft/1_readability-and-fairness.md",
                "docs/2_editor/4_craft/2_rhythm-and-structure.md",
                "docs/2_editor/4_craft/3_difficulty-curve.md",
                "docs/2_editor/4_craft/5_color-and-postprocessing.md",
                "docs/2_editor/5_workflow/1_order-of-work.md",
                "docs/2_editor/6_performance/1_level-budget.md"),
            new Topic(Context, "what the level's ai/CONTEXT.md holds, as an empty skeleton"),
            new Topic(Mcp, "how the AI connects and what it may do",
                "docs/2_editor/5_workflow/7_ai-assistant.md"),
        };

        /// <summary> The topic with this name, compared exactly; false for an unknown one. </summary>
        public static bool TryGet(string name, out Topic topic)
        {
            foreach (var candidate in Topics)
            {
                if (!string.Equals(candidate.Name, name, StringComparison.Ordinal)) continue;

                topic = candidate;
                return true;
            }

            topic = null;
            return false;
        }

        /// <summary> Every topic name, for an error that lists them. </summary>
        public static string Names()
        {
            var names = new string[Topics.Count];
            for (var i = 0; i < names.Length; i++) names[i] = Topics[i].Name;
            return string.Join(", ", names);
        }

        /// <summary> The rendered file of a topic, relative to StreamingAssets, with <c>/</c>. </summary>
        public static string FilePath(string topic) => Folder + "/" + topic + Extension;
    }
}