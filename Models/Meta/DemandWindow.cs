using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Meta
{
    /// <summary> One demand window: the mean of every <see cref="DemandDimension"/> over it, and where in
    /// the level it starts. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class DemandWindow : IModel<DemandWindow>
    {
        /// <summary> Seconds from the level's start; a merged window keeps the earliest of its sources.
        /// </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.Start)]
        public float Start { get; set; }

        /// <summary> One value per <see cref="DemandDimension"/>, in its order. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Values)]
        public float[] Values { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public DemandWindow()
        {
            Start = 0f;
            Values = new float[DemandDimensions.Count];
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public DemandWindow(float start, float[] values)
        {
            Start = start;
            Values = values;
        }

        /// <summary> The value of one dimension; zero when the array is shorter than this build's. </summary>
        public float Get(DemandDimension dimension)
            => Values != null && (int)dimension < Values.Length ? Values[(int)dimension] : 0f;
    }
}
