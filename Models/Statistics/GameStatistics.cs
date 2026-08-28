using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // THE PLAYER, ACROSS EVERYTHING: stats/statistics.json, read once at launch and written back on
    // a slow cadence. The per-level files answer "this level"; this answers "this person", and it
    // exists as its own document rather than as a sum computed on demand because deriving it would
    // mean opening every file in stats/ to draw one screen.
    //
    // SEVEN GROUPS, none of them carrying its own [DataVersion] - one envelope per FILE, exactly as
    // UserSettings does with its own sub-groups. A group is a heading on a screen and a place to add
    // a field without touching the root, not an independently versioned document.
    //
    // Additive by construction: every group builds its own defaults in its constructor, so a file
    // written before a group existed simply has no key for it and reads back as zeroes. That is what
    // keeps this domain at (1, 0) as the format grows.

    /// <summary> Everything one player has done, across every level and every screen. </summary>
    [RuleContainer]
    [DataVersion(DataDomains.GameStatistics, 1, 0)]
    public class GameStatistics : IModel<GameStatistics>
    {
        /// <summary> Since when, and how much. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Profile)]
        public ProfileStatistics Profile { get; set; }

        /// <summary> Where the time went, per kind of screen. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Screens)]
        public ScreenTimeStatistics Screens { get; set; }

        /// <summary> Every per-level counter, summed. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Totals)]
        public TotalsStatistics Totals { get; set; }

        /// <summary> The parts of a history a running total cannot express. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Streaks)]
        public StreakStatistics Streaks { get; set; }

        /// <summary> What the avatar itself has done. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Avatar)]
        public AvatarStatistics Avatar { get; set; }

        /// <summary> What has been authored on this device. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Editor)]
        public EditorTotalsStatistics Editor { get; set; }

        /// <summary> Which control device actually steers. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Devices)]
        public DeviceTimeStatistics Devices { get; set; }

        // TODO achievements. When they arrive they belong here as an eighth group, keyed by a stable
        // achievement id. Deliberately not scaffolded now: an empty aggregate cannot be told apart
        // from "this build has no achievements", and every reader would have to handle both anyway.

        public GameStatistics() => Reset();

        public void Reset()
        {
            Profile = new ProfileStatistics();
            Screens = new ScreenTimeStatistics();
            Totals = new TotalsStatistics();
            Streaks = new StreakStatistics();
            Avatar = new AvatarStatistics();
            Editor = new EditorTotalsStatistics();
            Devices = new DeviceTimeStatistics();
        }

        public object Clone() => Copy();

        public GameStatistics Copy() =>
            new()
            {
                Profile = Profile.Copy(),
                Screens = Screens.Copy(),
                Totals = Totals.Copy(),
                Streaks = Streaks.Copy(),
                Avatar = Avatar.Copy(),
                Editor = Editor.Copy(),
                Devices = Devices.Copy(),
            };

        public void Update(GameStatistics src)
        {
            Profile = src.Profile.Copy();
            Screens = src.Screens.Copy();
            Totals = src.Totals.Copy();
            Streaks = src.Streaks.Copy();
            Avatar = src.Avatar.Copy();
            Editor = src.Editor.Copy();
            Devices = src.Devices.Copy();
        }

        // Every group instance is kept, unlike Update above. This is the object the whole app holds
        // a reference to and hands out one group at a time, so replacing a group here would leave
        // every holder pointing at a copy nothing writes to any more - the same contract the
        // UserSettings groups have, and the reason IMoveable exists at all.
        public void Pull(GameStatistics source)
        {
            if (ReferenceEquals(this, source)) return;

            Profile.Pull(source.Profile);
            Screens.Pull(source.Screens);
            Totals.Pull(source.Totals);
            Streaks.Pull(source.Streaks);
            Avatar.Pull(source.Avatar);
            Editor.Pull(source.Editor);
            Devices.Pull(source.Devices);
        }

        public override bool Equals(object obj) => obj is GameStatistics value && Equals(value);

        public bool Equals(GameStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Profile.Equals(other.Profile)
                   && Screens.Equals(other.Screens)
                   && Totals.Equals(other.Totals)
                   && Streaks.Equals(other.Streaks)
                   && Avatar.Equals(other.Avatar)
                   && Editor.Equals(other.Editor)
                   && Devices.Equals(other.Devices);
        }

        public override int GetHashCode() =>
            HashCode.Combine(Profile, Screens, Totals, Streaks, Avatar, Editor, Devices);
    }
}
