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

        // Objects holds per-instance polymorphic RectObject variants (ObjectConverter's job, not
        // domain versioning) - never independently versioned, same type on both sides. The keyed
        // attribute is what keeps the generated writer spelling the collection the way the
        // reflective one does; the two have to agree byte for byte here as everywhere else.

        /// <summary> Never independently versioned, so the type is the same on both sides. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [JsonProperty("test_objects")]
        public Dictionary<ObjectId, RectObject> Objects { get; set; }

        // A SNAPSHOT NEEDS ITS DEFAULTS CONSTRUCTED NOW THAT IT IS A GENERATED MODEL. The constructor
        // is the single source of every default the generated Reset/Copy/Update read from, so a
        // member left null here is a NullReferenceException in bodies a free-form deserialization
        // target never had. That is the price of carrying codecs, and it is the whole of it.

        /// <summary> Every member at the value generation 0 would have read into it. </summary>
        public GameLevelV0()
        {
            GameEvents = new GameEvents();
            Objects = new Dictionary<ObjectId, RectObject>();
        }
    }
}
