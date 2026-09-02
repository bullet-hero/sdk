using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// A reusable group of objects with its own timeline - the template a PrefabObject places.
    /// The only type that is both an object scope and an id counter on one class; at level scope
    /// those two roles are split across GameLevel and LevelSettings.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.Prefab, 1, 0)]
    [GenerateModel]
    public sealed partial class Prefab : IFrameScope, IObjectIdCounter, IModel<Prefab>
    {
        /// <summary> Identity of this template and the key of Level.Resources.Prefabs. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.PrefabId)]
        public PrefabId PrefabId { get; set; }

        /// <summary> Editor-facing label of the template. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        // This template's own local timeline length - has no live placement to derive one from
        // (a template can be referenced by many/zero placements), so it's authored directly,
        // mirroring LevelSettings.FrameDuration. Used both as the recommended/default duration for a
        // newly-placed PrefabObject and as the Prefab Timeline's own editing bound.

        /// <summary> Length of the template's own timeline, in frames. </summary>
        [RuleInRange(FrameRules.MinFrameDuration, PrefabRules.MaxFrameDuration)]
        [JsonProperty(Names.FrameDurationShort)]
        public int FrameDuration { get; set; }
        
        // Nested PrefabObject placements (instances of OTHER prefabs, placed inside this template)
        // live directly in here too, already fully materialized - see IObjectScope's own comment.

        /// <summary> The template's own contents, keyed by ids local to this template - the same
        /// dictionary shape a level uses, which is why every editor operation works unchanged
        /// inside Prefab Mode. </summary>
        [GenerateModelKeyed(nameof(RectObject.ObjectId))]
        [GenerateModelMerge]
        [RuleNotNull, RuleCollectionMaxCount(PrefabRules.MaxObjects)]
        [RuleDictionaryKeyMatches(nameof(RectObject.ObjectId))]
        [JsonProperty(Names.Objects)]
        public Dictionary<ObjectId, RectObject> Objects { get; set; }

        // This prefab's own object-id namespace (mirrors LevelSettings.ObjectIdCounter) - used both
        // to author new objects directly inside this template, and to mint outer ids when this
        // prefab is itself materialized as a nested placement inside another prefab's template.

        /// <summary> Next free id in this template's own namespace. </summary>
        [RuleInRange(ObjectId.MinLevelValue, PrefabRules.MaxObjects)]
        [JsonProperty(Names.ObjectIdCounter)]
        public int ObjectIdCounter { get; set; }

        public ObjectId GetNextObjectId() => new(ObjectIdCounter++);

        public Prefab()
        {
            PrefabId = PrefabId.Null;
            Name = string.Empty;
            Objects = new Dictionary<ObjectId, RectObject>();
            ObjectIdCounter = ObjectId.MinLevelValue;
            FrameDuration = PrefabRules.DefaultFrameDuration;
        }
        public Prefab(PrefabId prefabId, string name, Dictionary<ObjectId, RectObject> objects, int objectIdCounter, int frameDuration)
        {
            PrefabId = prefabId;
            Name = name;
            Objects = objects;
            ObjectIdCounter = objectIdCounter;
            FrameDuration = frameDuration;
        }
    }
}