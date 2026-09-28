using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Models.Values;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Only Records changed at generation 2 - its key gained NoCollision - so only its key is retyped
    // to a snapshot (RunProfileV1). BestRun, DifficultyStatistics and LevelEditorStatistics are
    // TODAY's classes; one that changes later freezes its own leaf next to this file. Keys are
    // literal: a snapshot spells them the way its own generation did.

    /// <summary> Generation 1 (Release) of the level-statistics domain - the shape 1.0.0 shipped,
    /// before a record could be filed under "no collision". A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [ModelGeneration(ModelDomains.LevelStatistics, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class LevelStatisticsV1 : IModel<LevelStatisticsV1>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("level_id")]
        public LevelId LevelId { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("name")]
        public IString LevelName { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("vrs")]
        public Version LevelVersion { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("first_played_utc")]
        public DateTime FirstPlayedUtc { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("last_played_utc")]
        public DateTime LastPlayedUtc { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("real_seconds")]
        public double TotalRealSeconds { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("sessions")]
        public int SessionCount { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("attempts")]
        public int Attempts { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("clears")]
        public int Clears { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("deaths")]
        public int Deaths { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("hits")]
        public int Hits { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("dashes")]
        public int Dashes { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("checkpoint_restarts")]
        public int CheckpointRestarts { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("quits")]
        public int Quits { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("best_frame")]
        public int BestFrame { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("best_progress")]
        public float BestProgress { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("first_cleared_utc")]
        public DateTime FirstClearUtc { get; set; }

        /// <summary> Changed at this bump: filed under the four-number key. </summary>
        [JsonProperty("records")]
        public Dictionary<RunProfileV1, BestRun> Records { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("difficulty")]
        public DifficultyStatistics Difficulty { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("editor")]
        public LevelEditorStatistics Editor { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public LevelStatisticsV1()
        {
            LevelId = LevelId.Null;
            LevelName = new StringValue(string.Empty);
            LevelVersion = new Version(1, 0);
            FirstPlayedUtc = default;
            LastPlayedUtc = default;
            TotalRealSeconds = 0.0;
            SessionCount = 0;

            Attempts = 0;
            Clears = 0;
            Deaths = 0;
            Hits = 0;
            Dashes = 0;
            CheckpointRestarts = 0;
            Quits = 0;

            BestFrame = 0;
            BestProgress = 0f;
            FirstClearUtc = default;

            Records = new Dictionary<RunProfileV1, BestRun>();
            Difficulty = new DifficultyStatistics();
            Editor = new LevelEditorStatistics();
        }
    }
}
