using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The authoring half of the profile (the device-wide sum of LevelEditorStatistics). LevelsDeleted sits
    // beside LevelsCreated: a difference cannot be recovered from one number. Docs/STATISTICS.md

    /// <summary> What has been authored on this device, summed across every level. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EditorTotalsStatistics : IModel<EditorTotalsStatistics>
    {
        /// <summary> How many levels this device has authored. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LevelsCreated)]
        public int LevelsCreated { get; set; }

        /// <summary> How many it has removed. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LevelsDeleted)]
        public int LevelsDeleted { get; set; }

        /// <summary> How many objects have been placed across all of them. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.ObjectsCreated)]
        public int ObjectsCreated { get; set; }

        /// <summary> Editor operations applied, across every level. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Operations)]
        public int OperationsExecuted { get; set; }

        /// <summary> Generator runs. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.GeneratorsRun)]
        public int GeneratorsRun { get; set; }

        /// <summary> Resources added to levels - images, audio, fonts. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.TotalResources)]
        public int TotalResources { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EditorTotalsStatistics()
        {
            LevelsCreated = 0;
            LevelsDeleted = 0;
            ObjectsCreated = 0;
            OperationsExecuted = 0;
            GeneratorsRun = 0;
            TotalResources = 0;
        }
    }
}
