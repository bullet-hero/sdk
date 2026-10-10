using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Game;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Generation 0 of the game domain. A frozen snapshot - never edit it to match today's shape. </summary>
    [ModelGeneration(ModelDomains.GameLevel, ModelGenerations.Test)]
    [GenerateModel]
    public sealed partial class GameLevelV0 : IModel<GameLevelV0>
    {
        // GameEvents is its own independently-versioned domain - must be typed as the CURRENT
        // GameEvents class, same reasoning as LevelV0.Settings/.Game/.Resources.

        /// <summary> Its own versioned domain, so this is typed as the CURRENT class - the envelope upgrades it before this container is read. </summary>
        [JsonProperty("test_game_events")]
        public GameEvents GameEvents { get; set; }

        // Objects holds polymorphic RectObject variants (ObjectConverter's job), never versioned on
        // their own. The keyed attribute keeps the generated writer byte for byte equal to the
        // reflective one.

        /// <summary> Never independently versioned, so the type is the same on both sides. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [JsonProperty("test_objects")]
        public Dictionary<ObjectId, RectObject> Objects { get; set; }

        // A generated snapshot needs its defaults constructed: the constructor feeds the generated
        // Reset/Copy/Update, so a member left null here throws inside them.

        /// <summary> Every member at the value generation 0 would have read into it. </summary>
        public GameLevelV0()
        {
            GameEvents = new GameEvents();
            Objects = new Dictionary<ObjectId, RectObject>();
        }
    }
}
