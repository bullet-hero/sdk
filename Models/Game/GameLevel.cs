using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// Everything a level shows and does: its objects plus four aggregates of level-global events.
    /// This is the object scope at level scope - note the id counter that feeds it lives on
    /// LevelSettings instead, so the pair has to be carried together by anything creating objects.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.GameLevel, 1, 0)]
    [GenerateModel]
    public sealed partial class GameLevel : IObjectScope, IModel<GameLevel>
    {
        /// <summary> Markers, checkpoints, screen limits, background and theme tracks. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Events)]
        public GameEvents Events { get; set; }

        /// <summary> The camera's own transform tracks - it is animated like an object but is not
        /// one, so it lives here rather than in Objects. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CameraEvents)]
        public CameraEvents CameraEvents { get; set; }

        /// <summary> The screen-effect stack over time. </summary>
        [RuleNotNull]
        [JsonProperty(Names.PostProcessingEvents)]
        public PostProcessingEvents PostProcessingEvents { get; set; }

        /// <summary> Player-state switches over time (visible / controllable / collidable). </summary>
        [RuleNotNull]
        [JsonProperty(Names.PlayerEvents)]
        public PlayerEvents PlayerEvents { get; set; }
        
        // TODO add a contextual Rule validating this whole dictionary (key must equal value's own id)
        /// <summary> Every object in the level, flat and keyed by id - hierarchy is expressed through
        /// each object's ParentObjectId, not by nesting. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [GenerateModelMerge]
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjects)]
        [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
        [JsonProperty(Names.Objects)]
        public Dictionary<ObjectId, RectObject> Objects { get; set; }

        public GameLevel()
        {
            Events = new GameEvents();
            CameraEvents = new CameraEvents();
            PostProcessingEvents = new PostProcessingEvents();
            PlayerEvents = new PlayerEvents();

            Objects = new Dictionary<ObjectId, RectObject>();
        }
        public GameLevel(GameEvents events, CameraEvents cameraEvents, PostProcessingEvents postProcessingEvents,
            PlayerEvents playerEvents, Dictionary<ObjectId, RectObject> objects)
        {
            Events = events;
            CameraEvents = cameraEvents;
            PostProcessingEvents = postProcessingEvents;
            PlayerEvents = playerEvents;

            Objects = objects;
        }
    }
}