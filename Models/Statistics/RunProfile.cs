using System;
using BH.SDK.Models.Enums.Settings;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // THE CONDITIONS A RUN WAS PLAYED UNDER, AS A DICTIONARY KEY. A record only means something
    // against runs it can be compared with: three lives at half speed with a bot helping is not the
    // same achievement as one life at double speed, and folding them into one "best" would make the
    // number meaningless in exactly the cases a player cares about. So the profile is the key and
    // BestRun carries none of these fields - see BestRun's own header.
    //
    // SPEED IS QUANTIZED TO HUNDREDTHS RATHER THAN STORED AS A FLOAT, and that is a requirement of
    // being a key rather than a preference. The speed control is a continuous slider whose readout
    // shows two decimals, and a float key compares bit for bit: 1f and 0.9999999f would open two
    // separate records for what the player and the screen both call "1.00", and both would sit in
    // the file forever. Hundredths are exactly the precision the player is shown.
    //
    // A struct, so no [RuleContainer]: that attribute is class-only, and RuleAnalyzer's walk
    // deliberately never descends into value types (see the SDK CLAUDE.md, "The walk is on a level's
    // load path"). What bounds this is the collection cap on Records, not a per-property rule.

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

        [JsonIgnore]
        public float Speed => SpeedCenti / 100f;

        /// <summary> Whether the run had no life limit. </summary>
        [JsonIgnore]
        public bool Immortality => LifeCount == 0;

        // [JsonConstructor] because a readonly struct has no property-setter path for Newtonsoft to
        // fill: the members are get-only on purpose (a key that can be mutated after it has been
        // hashed into a dictionary is a bug waiting to happen), so construction is the only way in.
        [JsonConstructor]
        public RunProfile(int lifeCount, int speedCenti, bool useCheckpoints, BotKind bot)
        {
            LifeCount = lifeCount;
            SpeedCenti = speedCenti;
            UseCheckpoints = useCheckpoints;
            Bot = bot;
        }

        /// <summary> Builds a profile from a launch's own numbers, quantizing the speed. </summary>
        public static RunProfile FromLaunch(int lifeCount, float speed, bool useCheckpoints, BotKind bot)
            => new(lifeCount, ToCenti(speed), useCheckpoints, bot);

        /// <summary> Rounds a speed to the hundredths this key is filed under. </summary>
        public static int ToCenti(float speed) => (int)Math.Round(speed * 100.0, MidpointRounding.AwayFromZero);

        public bool Equals(RunProfile other)
            => LifeCount == other.LifeCount
               && SpeedCenti == other.SpeedCenti
               && UseCheckpoints == other.UseCheckpoints
               && Bot == other.Bot;

        public override bool Equals(object obj) => obj is RunProfile other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(LifeCount, SpeedCenti, UseCheckpoints, Bot);

        // Ordering exists so a UI listing several profiles is stable across sessions, and so a test
        // can compare two sets without depending on dictionary order.
        public int CompareTo(RunProfile other)
        {
            var cmp = LifeCount.CompareTo(other.LifeCount);
            if (cmp != 0) return cmp;
            cmp = SpeedCenti.CompareTo(other.SpeedCenti);
            if (cmp != 0) return cmp;
            cmp = UseCheckpoints.CompareTo(other.UseCheckpoints);
            return cmp != 0 ? cmp : ((byte)Bot).CompareTo((byte)other.Bot);
        }

        public static bool operator ==(RunProfile left, RunProfile right) => left.Equals(right);
        public static bool operator !=(RunProfile left, RunProfile right) => !left.Equals(right);

        public override string ToString()
            => $"Lives:{LifeCount}, Speed:{Speed:0.00}, Checkpoints:{UseCheckpoints}, Bot:{Bot}";
    }
}
