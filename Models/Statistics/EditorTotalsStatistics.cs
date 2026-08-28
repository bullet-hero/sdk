using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // THE AUTHORING HALF OF THE PROFILE, and the reason it is worth having at all: this project is
    // an engine as much as a game, so how much someone has BUILT is as much a part of their profile
    // as how much they have played. LevelEditorStatistics answers the same question per level; this
    // is the device-wide sum, plus the few counters that belong to no single level.
    //
    // LevelsDeleted is kept beside LevelsCreated rather than netted off against it: the two answer
    // different questions, and a difference cannot be recovered from a single number afterwards.
    //
    // TotalResources counts resources added to levels - images, audio, fonts. It replaced a
    // narrower "packages imported and exported" pair, which measured one workflow rather than the
    // work.

    /// <summary> What has been authored on this device, summed across every level. </summary>
    [RuleContainer]
    public class EditorTotalsStatistics : IModel<EditorTotalsStatistics>
    {
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LevelsCreated)]
        public int LevelsCreated { get; set; }

        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LevelsDeleted)]
        public int LevelsDeleted { get; set; }

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

        public EditorTotalsStatistics() => Reset();

        public void Reset()
        {
            LevelsCreated = 0;
            LevelsDeleted = 0;
            ObjectsCreated = 0;
            OperationsExecuted = 0;
            GeneratorsRun = 0;
            TotalResources = 0;
        }

        public object Clone() => Copy();

        public EditorTotalsStatistics Copy()
        {
            var copy = new EditorTotalsStatistics();
            copy.Update(this);
            return copy;
        }

        public void Update(EditorTotalsStatistics src)
        {
            LevelsCreated = src.LevelsCreated;
            LevelsDeleted = src.LevelsDeleted;
            ObjectsCreated = src.ObjectsCreated;
            OperationsExecuted = src.OperationsExecuted;
            GeneratorsRun = src.GeneratorsRun;
            TotalResources = src.TotalResources;
        }

        public void Pull(EditorTotalsStatistics source) => Update(source);

        public override bool Equals(object obj) => obj is EditorTotalsStatistics value && Equals(value);

        public bool Equals(EditorTotalsStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LevelsCreated == other.LevelsCreated
                   && LevelsDeleted == other.LevelsDeleted
                   && ObjectsCreated == other.ObjectsCreated
                   && OperationsExecuted == other.OperationsExecuted
                   && GeneratorsRun == other.GeneratorsRun
                   && TotalResources == other.TotalResources;
        }

        public override int GetHashCode() =>
            HashCode.Combine(LevelsCreated, LevelsDeleted, ObjectsCreated, OperationsExecuted,
                GeneratorsRun, TotalResources);
    }
}
