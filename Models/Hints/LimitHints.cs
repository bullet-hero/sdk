using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Hints
{
    /// <summary>
    /// Peak simultaneous objects of each kind at the level's heaviest frame, written by the editor on save
    /// so a game can preallocate. Strictly advisory (lower bound at most); all zeroes means "no hint".
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LimitHints : IModel<LimitHints>
    {
        /// <summary> Peak count of objects of every type at once - the widest of them all. </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.Instances)]
        public int Instances { get; set; }

        // Shapes are counted in two buckets (two pools); Auto counts as transparent unless a resolver
        // says otherwise. Why: Models/Hints/CLAUDE.md

        /// <summary> Peak simultaneous shape objects on the opaque path - a subset of Instances. </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.ShapesOpaque)]
        public int ShapesOpaque { get; set; }

        /// <summary> Peak simultaneous shape objects on the transparent path - a subset of Instances. </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.ShapesTransparent)]
        public int ShapesTransparent { get; set; }

        /// <summary> Peak simultaneous effect objects (emitters, not particles). </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.Effects)]
        public int Effects { get; set; }

        /// <summary> Peak simultaneous text objects. </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.Texts)]
        public int Texts { get; set; }

        /// <summary> Peak simultaneously sounding audio tracks - the one count that is not an object
        /// at all. </summary>
        [RuleInRange(LevelRules.MinCapacityHint, LevelRules.MaxCapacityHint)]
        [JsonProperty(Names.Tracks)]
        public int Tracks { get; set; }

        /// <summary> Was anything ever measured into this hint? </summary>
        [JsonIgnore]
        public bool HasValue => Instances > 0 || ShapesOpaque > 0 || ShapesTransparent > 0
                                || Effects > 0 || Texts > 0 || Tracks > 0;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LimitHints()
        {
            Instances = 0;
            ShapesOpaque = 0;
            ShapesTransparent = 0;
            Effects = 0;
            Texts = 0;
            Tracks = 0;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public LimitHints(int instances, int shapesOpaque, int shapesTransparent,
            int effects, int texts, int tracks)
        {
            Instances = instances;
            ShapesOpaque = shapesOpaque;
            ShapesTransparent = shapesTransparent;
            Effects = effects;
            Texts = texts;
            Tracks = tracks;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() =>
            $"instances:{Instances}, shapes_opaque:{ShapesOpaque}, " +
            $"shapes_transparent:{ShapesTransparent}, effects:{Effects}, " +
            $"texts:{Texts}, tracks:{Tracks}";
    }
}
