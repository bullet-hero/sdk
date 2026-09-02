using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // HOW MUCH WORK WENT INTO THIS LEVEL, as opposed to how well it was played. It lives in the same
    // file as the play statistics because it is the same subject - one level, one document - but it
    // is the only part of that file a run launched from the editor may write to.
    //
    // That split is the whole reason this is a separate aggregate: an author testing a ten-second
    // passage will start it hundreds of times in an evening, and folding those into Attempts would
    // make the level's clear rate meaningless for everyone, the author included. Time spent editing,
    // saves and operations are the honest measure of that same evening, and they are here.

    /// <summary> What the author did to a level, as opposed to how it was played. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LevelEditorStatistics : IModel<LevelEditorStatistics>
    {
        /// <summary> How many times the level was opened for editing. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Opens)]
        public int EditorOpens { get; set; }

        /// <summary> Real seconds spent with the level open in the editor. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.EditSeconds)]
        public double TotalEditSeconds { get; set; }

        /// <summary> When the level was last edited. Always UTC. </summary>
        [JsonProperty(Names.LastEditedUtc)]
        public DateTime LastEditedUtc { get; set; }

        /// <summary> Saves the author asked for. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Saves)]
        public int Saves { get; set; }

        /// <summary> Saves the autosave timer made on its own. Kept apart from the above: one is a
        /// decision, the other is a policy. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Autosaves)]
        public int Autosaves { get; set; }

        /// <summary> Editor operations applied - every undoable edit, counted once. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Operations)]
        public int Operations { get; set; }

        /// <summary> Whether the level was ever opened for editing on this device. </summary>
        [JsonIgnore]
        public bool HasValue => EditorOpens > 0;

        public LevelEditorStatistics()
        {
            EditorOpens = 0;
            TotalEditSeconds = 0.0;
            LastEditedUtc = default;
            Saves = 0;
            Autosaves = 0;
            Operations = 0;
        }
    }
}
