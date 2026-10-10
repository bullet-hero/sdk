using System;
using BH.SDK.Models.Enums.Settings;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The conditions a run was played under, AS A DICTIONARY KEY. Speed is quantized to hundredths: a float
    // key compares bit for bit. A struct, so no [RuleContainer]; Records' cap bounds it. Docs/STATISTICS.md

    /// <summary> The launch conditions a record is filed under. </summary>
    public readonly struct RunProfile : IEquatable<RunProfile>, IComparable<RunProfile>
    {
        /// <summary> Lives the run was given. 0 is the immortal "Zen" mode, a real choice rather
        /// than an unset value. </summary>
        [JsonProperty(Names.Lives)]
        public int LifeCount { get; }

        /// <summary> Playback speed in hundredths - 100 is normal speed. </summary>
        [JsonProperty(Names.SpeedCenti)]
        public int SpeedCenti { get; }

        /// <summary> Whether checkpoints were armed for the run. </summary>
        [JsonProperty(Names.Checkpoints)]
        public bool UseCheckpoints { get; }

        /// <summary> Which bot played, if any. What the player CHOSE, never what a driver managed
        /// to do on the day. </summary>
        [JsonProperty(Names.Bot)]
        public BotKind Bot { get; }

        // Part of the key, not a reason to drop the run: nothing-can-hit is not Zen (hit, never killed).

        /// <summary> Whether the avatar had no collisions at all - the launch's "No collision"
        /// option. </summary>
        [JsonProperty(Names.NoCollision)]
        public bool NoCollision { get; }

        /// <summary> The stored hundredths read back as the number a player sees. </summary>
        [JsonIgnore]
        public float Speed => SpeedCenti / 100f;

        /// <summary> Whether the run had no life limit. </summary>
        [JsonIgnore]
        public bool Immortality => LifeCount == 0;

        // [JsonConstructor] because a readonly struct has no property-setter path for Newtonsoft to
        // fill: the members are get-only on purpose (a key that can be mutated after it has been
        // hashed into a dictionary is a bug waiting to happen), so construction is the only way in.

        /// <summary> Built from its count, centi, checkpoints, bot and collision switch. </summary>
        [JsonConstructor]
        public RunProfile(int lifeCount, int speedCenti, bool useCheckpoints, BotKind bot,
            bool noCollision = false)
        {
            LifeCount = lifeCount;
            SpeedCenti = speedCenti;
            UseCheckpoints = useCheckpoints;
            Bot = bot;
            NoCollision = noCollision;
        }

        /// <summary> Builds a profile from a launch's own numbers, quantizing the speed. </summary>
        public static RunProfile FromLaunch(int lifeCount, float speed, bool useCheckpoints, BotKind bot,
            bool noCollision = false)
            => new(lifeCount, ToCenti(speed), useCheckpoints, bot, noCollision);

        /// <summary> Rounds a speed to the hundredths this key is filed under. </summary>
        public static int ToCenti(float speed) => (int)Math.Round(speed * 100.0, MidpointRounding.AwayFromZero);

        /// <summary> Member by member. </summary>
        public bool Equals(RunProfile other)
            => LifeCount == other.LifeCount
               && SpeedCenti == other.SpeedCenti
               && UseCheckpoints == other.UseCheckpoints
               && Bot == other.Bot
               && NoCollision == other.NoCollision;

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is RunProfile other && Equals(other);

        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode()
            => HashCode.Combine(LifeCount, SpeedCenti, UseCheckpoints, Bot, NoCollision);

        // Ordering exists so a UI listing several profiles is stable across sessions, and so a test
        // can compare two sets without depending on dictionary order.

        /// <summary> Orders profiles so a list of records is stable across sessions. </summary>
        public int CompareTo(RunProfile other)
        {
            var cmp = LifeCount.CompareTo(other.LifeCount);
            if (cmp != 0) return cmp;
            cmp = SpeedCenti.CompareTo(other.SpeedCenti);
            if (cmp != 0) return cmp;
            cmp = UseCheckpoints.CompareTo(other.UseCheckpoints);
            if (cmp != 0) return cmp;
            cmp = ((byte)Bot).CompareTo((byte)other.Bot);
            return cmp != 0 ? cmp : NoCollision.CompareTo(other.NoCollision);
        }

        /// <summary> Value equality. </summary>
        public static bool operator ==(RunProfile left, RunProfile right) => left.Equals(right);
        /// <summary> Its negation. </summary>
        public static bool operator !=(RunProfile left, RunProfile right) => !left.Equals(right);

        /// <summary> One line, for a log. </summary>
        public override string ToString()
            => $"Lives:{LifeCount}, Speed:{Speed:0.00}, Checkpoints:{UseCheckpoints}, Bot:{Bot}, NoCollision:{NoCollision}";
    }
}
