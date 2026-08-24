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

        // A MULTIPLIER of the size the player already has, not a size in world units - the avatar's
        // own scale is a device/skin setting, and a level that stated an absolute size would fight
        // it. So the neutral value is 1 and an empty track means 1, which is why a level written
        // before this existed reads exactly as it did.
        //
        // ValueRules.MinPlayerSize is 0 and there is no maximum: 0 is a player that is there,
        // controllable and hittable at a point, which is a legitimate thing to author, and a level
        // that wants a giant player is not this format's business to argue with. The floor is
        // enforced where it matters rather than by an attribute - FloatKey is shared by every float
        // track in the format, so a rule on its Value would bind all of them.

        /// <summary> How large the player is drawn and hit, as a multiple of its ordinary size.
        /// Applied every frame; 1 leaves it alone. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.Size)]
        public List<FloatKey> Sizes { get; set; }

        // The one multiplier every speed the avatar has is scaled by - walking, dashing and the
        // knockback a hit gives it - rather than a walking speed of its own. A level that only wanted
        // to slow a section down would otherwise have to know which of the three the player is in.
        //
        // Same shape as Sizes and for the same reasons: neutral is 1, an empty track is 1, the floor
        // (ValueRules.MinPlayerSpeed) is enforced where the value is read rather than by an attribute
        // on FloatKey, and there is no maximum.

        /// <summary> How fast the player moves, as a multiple of its ordinary speed. Applied every
        /// frame to every speed it has; 1 leaves it alone. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.Speed)]
        public List<FloatKey> Speeds { get; set; }

        public PlayerEvents()
        {
            Velocities = new List<Velocity>();
            VelocityPoints = new List<VelocityPoint>();
            Visibles = new List<BoolKey>();
            Controls = new List<BoolKey>();
            Collisions = new List<BoolKey>();
            Sizes = new List<FloatKey>();
            Speeds = new List<FloatKey>();
        }
        public PlayerEvents(List<Velocity> velocities, List<VelocityPoint> velocityPoints,
            List<BoolKey> visibles, List<BoolKey> controls, List<BoolKey> collisions,
            List<FloatKey> sizes, List<FloatKey> speeds)
        {
            Velocities = velocities;
            VelocityPoints = velocityPoints;
            Visibles = visibles;
            Controls = controls;
            Collisions = collisions;
            Sizes = sizes;
            Speeds = speeds;
        }
        public void Reset()
        {
            Velocities.Clear();
            VelocityPoints.Clear();
            Visibles.Clear();
            Controls.Clear();
            Collisions.Clear();
            Sizes.Clear();
            Speeds.Clear();
        }

        public object Clone() => Copy();
        public PlayerEvents Copy() => new(Velocities.CopyList(), VelocityPoints.CopyList(),
            Visibles.CopyList(), Controls.CopyList(), Collisions.CopyList(), Sizes.CopyList(), Speeds.CopyList());

        public void Update(PlayerEvents src)
        {
            Velocities = src.Velocities.CopyList();
            VelocityPoints = src.VelocityPoints.CopyList();
            Visibles = src.Visibles.CopyList();
            Controls = src.Controls.CopyList();
            Collisions = src.Collisions.CopyList();
            Sizes = src.Sizes.CopyList();
            Speeds = src.Speeds.CopyList();
        }

        public void Pull(PlayerEvents src)
        {
            Velocities = src.Velocities.CopyList();
            VelocityPoints = src.VelocityPoints.CopyList();
            Visibles = src.Visibles.CopyList();
            Controls = src.Controls.CopyList();
            Collisions = src.Collisions.CopyList();
            Sizes = src.Sizes.CopyList();
            Speeds = src.Speeds.CopyList();
        }

        public override bool Equals(object obj) => obj is PlayerEvents value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Velocities.GetListHashCode(),
            VelocityPoints.GetListHashCode(), Visibles.GetListHashCode(),
            Controls.GetListHashCode(), Collisions.GetListHashCode(), Sizes.GetListHashCode(),
            Speeds.GetListHashCode());

        public bool Equals(PlayerEvents other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Velocities.ListEquals(other.Velocities)
                         && VelocityPoints.ListEquals(other.VelocityPoints)
                         && Visibles.ListEquals(other.Visibles)
                         && Controls.ListEquals(other.Controls)
                         && Collisions.ListEquals(other.Collisions)
                         && Sizes.ListEquals(other.Sizes)
                         && Speeds.ListEquals(other.Speeds);
            return result;
        }
    }
}