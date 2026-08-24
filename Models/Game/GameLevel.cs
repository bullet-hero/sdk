using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Game
{
    /// <summary>
    /// Everything a level shows and does: its objects plus four aggregates of level-global events.
    /// This is the object scope at level scope - note the id counter that feeds it lives on
    /// LevelSettings instead, so the pair has to be carried together by anything creating objects.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.GameLevel, 1, 0)]
    public class GameLevel : IObjectScope, IModel<GameLevel>
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
        public void Reset()
        {
            Events.Reset();
            CameraEvents.Reset();
            PostProcessingEvents.Reset();
            PlayerEvents.Reset();

            Objects.Clear();
        }

        public object Clone() => Copy();
        public GameLevel Copy() => new(Events.Copy(), CameraEvents.Copy(), PostProcessingEvents.Copy(),
            PlayerEvents.Copy(), Objects.CopyDictionary());

        public void Update(GameLevel src)
        {
            Events = src.Events.Copy();
            CameraEvents = src.CameraEvents.Copy();
            PostProcessingEvents = src.PostProcessingEvents.Copy();
            PlayerEvents = src.PlayerEvents.Copy();
            Objects = src.Objects.CopyDictionary();
        }

        public void Pull(GameLevel src)
        {
            Events.Pull(src.Events);
            CameraEvents.Pull(src.CameraEvents);
            PostProcessingEvents.Pull(src.PostProcessingEvents);
            PlayerEvents.Pull(src.PlayerEvents);
            Objects.PullDictionary(src.Objects, LevelUtils.PullObject);
        }

        public override bool Equals(object obj) => obj is GameLevel value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Events, CameraEvents, PostProcessingEvents, PlayerEvents,
            Objects.GetDictionaryHashCode());

        public bool Equals(GameLevel other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Events.Equals(other.Events)
                         && CameraEvents.Equals(other.CameraEvents)
                         && PostProcessingEvents.Equals(other.PostProcessingEvents)
                         && PlayerEvents.Equals(other.PlayerEvents)
                         && Objects.DictionaryEquals(other.Objects);
            return result;
        }
    }
}