using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Game
{
    // The two force tracks are carried by the FORMAT and read by nobody yet: the player does not
    // apply them. They are here rather than waiting because a level converted from another engine
    // already has them - Afterbeat's own player-force track lands on Velocities - and a value the
    // format cannot hold is one that has to be dropped on import and can never come back.
    //
    // The version stays 1.0: both are additive and default to empty, so a document written before
    // they existed reads exactly as it did, and one written after them is ignored key by key by a
    // build that has not heard of them. A minor bump would need a frozen snapshot and a migrator to
    // say nothing at all.

    /// <summary>
    /// Level-driven overrides of the player's own state. Three independent switch tracks, so a level
    /// can e.g. keep the player visible but take control away during a cutscene section, plus two
    /// tracks of force the level applies to the player.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.PlayerEvents, 1, 0)]
    public class PlayerEvents : IModel<PlayerEvents>
    {
        /// <summary> A force pushing the player in a direction. Zero, the default, leaves them
        /// alone. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(Velocity.Frame))]
        [JsonProperty(Names.Velocity)]
        public List<Velocity> Velocities { get; set; }

        /// <summary> Points that push the player away or pull them in, as opposed to the flat
        /// direction <see cref="Velocities"/> carries. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(VelocityPoint.Frame))]
        [JsonProperty(Names.VelocityPoint)]
        public List<VelocityPoint> VelocityPoints { get; set; }


        /// <summary> Whether the player avatar is drawn. Hiding it does not make it safe - see
        /// Collisions. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(BoolKey.Frame))]
        [JsonProperty(Names.Visibles)]
        public List<BoolKey> Visibles { get; set; } // player can see himself

        /// <summary> Whether input moves the player. Off freezes them in place while the level keeps
        /// running. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(BoolKey.Frame))]
        [JsonProperty(Names.Controls)]
        public List<BoolKey> Controls { get; set; } // player can control himself

        /// <summary> Whether the player can be hit at all - the authored equivalent of invulnerability
        /// during a transition. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(BoolKey.Frame))]
        [JsonProperty(Names.Collisions)]
        public List<BoolKey> Collisions { get; set; } // active collision detection system for player

        public PlayerEvents()
        {
            Velocities = new List<Velocity>();
            VelocityPoints = new List<VelocityPoint>();
            Visibles = new List<BoolKey>();
            Controls = new List<BoolKey>();
            Collisions = new List<BoolKey>();
        }
        public PlayerEvents(List<Velocity> velocities, List<VelocityPoint> velocityPoints,
            List<BoolKey> visibles, List<BoolKey> controls, List<BoolKey> collisions)
        {
            Velocities = velocities;
            VelocityPoints = velocityPoints;
            Visibles = visibles;
            Controls = controls;
            Collisions = collisions;
        }
        public void Reset()
        {
            Velocities.Clear();
            VelocityPoints.Clear();
            Visibles.Clear();
            Controls.Clear();
            Collisions.Clear();
        }

        public object Clone() => Copy();
        public PlayerEvents Copy() => new(Velocities.CopyList(), VelocityPoints.CopyList(),
            Visibles.CopyList(), Controls.CopyList(), Collisions.CopyList());

        public override bool Equals(object obj) => obj is PlayerEvents value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Velocities.GetListHashCode(),
            VelocityPoints.GetListHashCode(), Visibles.GetListHashCode(),
            Controls.GetListHashCode(), Collisions.GetListHashCode());

        public bool Equals(PlayerEvents other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Velocities.ListEquals(other.Velocities)
                         && VelocityPoints.ListEquals(other.VelocityPoints)
                         && Visibles.ListEquals(other.Visibles)
                         && Controls.ListEquals(other.Controls)
                         && Collisions.ListEquals(other.Collisions);
            return result;
        }
    }
}