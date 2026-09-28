using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Statistics;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Generation 2 only ADDED a group (Tutorial), so every group here is TODAY's class - none of them
    // changed shape. Keys are literal: a snapshot spells them the way its own generation did.

    /// <summary> Generation 1 (Release) of the device-wide statistics domain - the shape 1.0.0
    /// shipped, before the tutorial group. A frozen snapshot - never edit it to match today's shape. </summary>
    [ModelGeneration(ModelDomains.GameStatistics, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class GameStatisticsV1 : IModel<GameStatisticsV1>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("profile")]
        public ProfileStatistics Profile { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("screens")]
        public ScreenTimeStatistics Screens { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("totals")]
        public TotalsStatistics Totals { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("streaks")]
        public StreakStatistics Streaks { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("avatar")]
        public AvatarStatistics Avatar { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("editor")]
        public EditorTotalsStatistics Editor { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("devices")]
        public DeviceTimeStatistics Devices { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public GameStatisticsV1()
        {
            Profile = new ProfileStatistics();
            Screens = new ScreenTimeStatistics();
            Totals = new TotalsStatistics();
            Streaks = new StreakStatistics();
            Avatar = new AvatarStatistics();
            Editor = new EditorTotalsStatistics();
            Devices = new DeviceTimeStatistics();
        }
    }
}
