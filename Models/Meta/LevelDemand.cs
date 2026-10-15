using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    // Computed at save, never typed. Only the Pareto front of the windows is kept, which loses nothing for
    // any monotone cost model: Docs/PERFORMANCE_RATING.md.

    /// <summary> What a level asks the device to do: the demand windows no other window outweighs in
    /// every dimension. Paired with a device profile it gives the level's performance score. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LevelDemand : IModel<LevelDemand>
    {
        /// <summary> The <see cref="PerformanceRules.AnalyzerVersion"/> that measured this; any other value
        /// reads as unknown. </summary>
        [JsonProperty(Names.AnalyzerVersion)]
        public int AnalyzerVersion { get; set; }

        /// <summary> False when windows were merged to fit <see cref="PerformanceRules.MaxDemandWindows"/>;
        /// a merged demand can only over-read. </summary>
        [JsonProperty(Names.Exact)]
        public bool Exact { get; set; }

        /// <summary> The kept windows, in level order. </summary>
        [RuleNotNull, RuleCollectionMaxCount(PerformanceRules.MaxDemandWindows), RuleCollectionNoNullItems]
        [JsonProperty(Names.Windows)]
        public List<DemandWindow> Windows { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LevelDemand()
        {
            AnalyzerVersion = PerformanceRules.AnalyzerVersion;
            Exact = true;
            Windows = new List<DemandWindow>();
        }

        /// <summary> Whether a demand can be scored by this build: present and measured the way it
        /// measures. </summary>
        public static bool IsCurrent(LevelDemand demand)
            => demand?.Windows != null && demand.AnalyzerVersion == PerformanceRules.AnalyzerVersion;
    }
}
