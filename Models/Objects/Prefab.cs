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
    [RulePrefabRootFixed]
    [ModelGeneration(ModelDomains.Prefab, ModelGenerations.Release)]
    [GenerateModel]
    public sealed partial class Prefab : IObjectScope, IObjectIdCounter, IModel<Prefab>
    {
        /// <summary> Identity of this template and the key of Level.Resources.Prefabs. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.PrefabId)]
        public PrefabId PrefabId { get; set; }
        
        /// <summary> The object a placement of this template IS - what everything parented to
        /// <see cref="ObjectId.PrefabRoot"/> hangs off, materialized onto the placement itself
        /// rather than beside it, and carrying that same id as its own. Holds the template's name,
        /// its positional tracks and - as its own Span's duration - the template's whole timeline;
        /// a placement diverges from the tracks through its Modifications, and from the length not
        /// at all. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Root)]
        public RectObject Root { get; set; }

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

        /// <summary> The next unused id in this scope, consuming it. </summary>
        public ObjectId GetNextObjectId() => new(ObjectIdCounter++);

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Prefab()
        {
            PrefabId = PrefabId.Null;
            Objects = new Dictionary<ObjectId, RectObject>();
            ObjectIdCounter = ObjectId.MinLevelValue;
            Root = new RectObject
            {
                ObjectId = ObjectId.PrefabRoot,
                Span = new FrameSpan(FrameRules.MinFrame, PrefabRules.DefaultFrameDuration),
            };
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Prefab(PrefabId prefabId, Dictionary<ObjectId, RectObject> objects, int objectIdCounter,
            RectObject root)
        {
            PrefabId = prefabId;
            Objects = objects;
            ObjectIdCounter = objectIdCounter;
            Root = root;
        }

        /// <summary> A copy sharing every member instance - see <see cref="Level.ShallowClone"/> for
        /// what it is for and why it is not <c>Copy</c>. </summary>
        internal Prefab ShallowClone() => (Prefab)MemberwiseClone();
    }
}