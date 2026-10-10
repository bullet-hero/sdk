using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Game
{
    // The two force tracks are carried by the format and applied by nobody yet. Why, and why the domain
    // stays at 1.0: Models/Game/CLAUDE.md

    /// <summary>
    /// Level-driven overrides of the player's own state: three independent switch tracks (e.g. visible
    /// but uncontrollable in a cutscene) plus two tracks of force the level applies to the player.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.PlayerEvents, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class PlayerEvents : IModel<PlayerEvents>
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
        public List<BoolKey> Visibilities { get; set; } // player can see himself

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

        // A MULTIPLIER of the avatar's own size, not world units: neutral and empty-track are 1, the floor
        // (ValueRules.MinPlayerSize) is enforced on read, there is no maximum. Models/Game/CLAUDE.md

        /// <summary> How large the player is drawn and hit, as a multiple of its ordinary size.
        /// Applied every frame; 1 leaves it alone. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.Size)]
        public List<FloatKey> Sizes { get; set; }

        // The one multiplier on walking, dashing and knockback alike. Same shape as Sizes: neutral is 1,
        // the floor (ValueRules.MinPlayerSpeed) is enforced on read, there is no maximum.

        /// <summary> How fast the player moves, as a multiple of its ordinary speed. Applied every
        /// frame to every speed it has; 1 leaves it alone. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxPlayerKeys)]
        [RuleCollectionUnique(nameof(FloatKey.Frame))]
        [JsonProperty(Names.Speed)]
        public List<FloatKey> Speeds { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public PlayerEvents()
        {
            Velocities = new List<Velocity>();
            VelocityPoints = new List<VelocityPoint>();
            Visibilities = new List<BoolKey>();
            Controls = new List<BoolKey>();
            Collisions = new List<BoolKey>();
            Sizes = new List<FloatKey>();
            Speeds = new List<FloatKey>();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public PlayerEvents(List<Velocity> velocities, List<VelocityPoint> velocityPoints,
            List<BoolKey> visibles, List<BoolKey> controls, List<BoolKey> collisions,
            List<FloatKey> sizes, List<FloatKey> speeds)
        {
            Velocities = velocities;
            VelocityPoints = velocityPoints;
            Visibilities = visibles;
            Controls = controls;
            Collisions = collisions;
            Sizes = sizes;
            Speeds = speeds;
        }
    }
}