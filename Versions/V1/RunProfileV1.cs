using System;
using BH.SDK.Models.Enums.Settings;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: the key of LevelStatisticsV1.Records, frozen
    // because generation 2 added NoCollision to the live RunProfile. Like the live one it is a struct
    // BlobPrimitives and JsonPrimitives write by hand (Write/ReadRunProfileV1 there), so the
    // generator treats it as ValueKind.Struct and needs nothing more.

    /// <summary> A record's launch conditions as LevelStatistics generation 1 (Release) filed them -
    /// lives, speed, checkpoints and bot, before the collision switch. A frozen snapshot - never edit
    /// it to match today's shape. </summary>
    public readonly struct RunProfileV1 : IEquatable<RunProfileV1>
    {
        /// <summary> Lives the run was given; 0 is Zen. </summary>
        public int LifeCount { get; }

        /// <summary> Playback speed in hundredths. </summary>
        public int SpeedCenti { get; }

        /// <summary> Whether checkpoints were armed. </summary>
        public bool UseCheckpoints { get; }

        /// <summary> Which bot played, if any. </summary>
        public BotKind Bot { get; }

        /// <summary> Built from its four numbers. </summary>
        public RunProfileV1(int lifeCount, int speedCenti, bool useCheckpoints, BotKind bot)
        {
            LifeCount = lifeCount;
            SpeedCenti = speedCenti;
            UseCheckpoints = useCheckpoints;
            Bot = bot;
        }

        /// <summary> Member by member. </summary>
        public bool Equals(RunProfileV1 other)
            => LifeCount == other.LifeCount
               && SpeedCenti == other.SpeedCenti
               && UseCheckpoints == other.UseCheckpoints
               && Bot == other.Bot;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is RunProfileV1 other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode() => HashCode.Combine(LifeCount, SpeedCenti, UseCheckpoints, Bot);
    }
}
