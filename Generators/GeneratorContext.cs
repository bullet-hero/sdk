using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Audio;
using BH.SDK.Models.Game;
using BH.SDK.Models.Hints;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Resources;
using BH.SDK.Models.SettingGroups;
using BH.SDK.Rules;

namespace BH.SDK.Generators
{
    // A generator mutates the model THROUGH this and nowhere else: every mutator journals itself, which
    // is the whole undo story. Scope and Counter are a pair, never inferred from each other (a Prefab is
    // both, at level scope Game is the scope and Settings the counter). Docs/GENERATORS.md.

    /// <summary>
    /// The sandbox a Content/Modifier generator runs in: what it may read, what it may write, and
    /// the journal of everything it wrote.
    /// </summary>
    public sealed class GeneratorContext
    {
        /// <summary> Where created objects land - Level.Game normally, a Prefab's own template while
        /// Prefab Mode is active. </summary>
        public IObjectScope Scope { get; }

        /// <summary> Whoever owns Scope's ObjectId namespace - Level.Settings for a level,
        /// the Prefab itself (the same instance as Scope) for a template. </summary>
        public IObjectIdCounter Counter { get; }

        /// <summary> Timeline shape of the hosting level. Present even in Prefab Mode: a template's
        /// content is still authored against the level's framerate. </summary>
        public LevelSettings Settings { get; }

        /// <summary> The hosting level's resources - present in Prefab Mode too, since a template
        /// may reference textures, themes and colliders like anything else. </summary>
        public LevelResources Resources { get; }

        /// <summary> Level-global event tracks. NULL while a Prefab template is the active scope -
        /// declare GeneratorRequirements.LevelScope if you touch this. </summary>
        public GameLevel Game { get; }

        /// <summary> Scheduled audio. NULL in Prefab Mode, same reason as Game. </summary>
        public AudioLevel Audio { get; }

        /// <summary> The level's advisory measurements. NULL in Prefab Mode, same reason as Game -
        /// a hint describes the file a player loads, and a template is not one. </summary>
        public LevelHints Hints { get; }

        /// <summary> Half-open window the run should write into. Nothing a generator creates may
        /// fall outside it - see BaseSpawnGenerator.ClampSpan. </summary>
        public FrameSpan Span { get; }

        // Grouping lives here: every generator parents to context.Parent, so a lazily created container
        // behind it needs no generator changes. Lazy, so a run that creates nothing leaves no container.

        /// <summary> Parent every created object should attach to. ObjectId.Null means scope root.
        /// With grouping on, reading this creates (once) the container object and returns it. </summary>
        public ObjectId Parent => _groupName == null ? _parent : EnsureGroup();

        /// <summary> Whether this run wraps everything it creates in one container object. </summary>
        public bool IsGrouping => _groupName != null;

        // Layer splitting is a run-wide option, not a generator's: done as a pass over the journal AFTER
        // Generate, it also wins over a generator that writes Layer itself.

        /// <summary> Whether every object this run creates gets its own Layer, stepping up from
        /// Layer, instead of all of them sharing one. </summary>
        public bool IsSplittingLayers { get; }

        private readonly ObjectId _parent;
        private readonly string _groupName;
        private ObjectId _group;
        private bool _groupCreated;

        /// <summary> Base layer the author asked for, as an EFFECTIVE (parent-chain-summed) value -
        /// that is what a layer means to the person typing it. Write LocalLayer onto an object, not
        /// this. </summary>
        public int Layer { get; }

        // Layer is parent-relative: writing the author's number under a non-zero parent would offset the
        // whole run, so the parent chain is subtracted once here.

        /// <summary> Layer to actually write on a created object: the author's Layer expressed
        /// relative to whatever this run parents to. </summary>
        public int LocalLayer => Layer - ParentLayerSum();

        /// <summary> Seed for GeneratorRandom - same seed, same output, always. </summary>
        public uint Seed { get; }

        /// <summary> What the host currently has selected. Empty unless the generator declared
        /// GeneratorRequirements.Selection. </summary>
        public IReadOnlyList<ObjectId> Selection { get; }

        /// <summary> Read-only view of the target scope. Writing goes through Create/Edit/Delete -
        /// this is for looking around (finding a free layer, measuring existing content). </summary>
        public IReadOnlyDictionary<ObjectId, RectObject> Objects => Scope.Objects;

        /// <summary> Everything this run has changed so far. </summary>
        public GeneratorChangeLog Log { get; } = new();

        /// <summary> Level-scope run. Objects land in level.Game, ids come from level.Settings. </summary>
        public GeneratorContext(Level level, FrameSpan span,
            ObjectId parent = default, int layer = 0, uint seed = 0, IReadOnlyList<ObjectId> selection = null,
            string groupName = null, bool splitLayers = false)
            : this(level.Game, level.Settings, level.Settings, level.Resources, level.Game, level.Audio,
                level.Hints, span, parent, layer, seed, selection, groupName, splitLayers)
        {
        }

        /// <summary> Prefab-template run: Game/Audio stay null, so a LevelScope generator can't be
        /// handed one of these by accident. </summary>
        public GeneratorContext(IObjectScope scope, IObjectIdCounter counter, LevelSettings settings,
            LevelResources resources, FrameSpan span,
            ObjectId parent = default, int layer = 0, uint seed = 0, IReadOnlyList<ObjectId> selection = null,
            string groupName = null, bool splitLayers = false)
            : this(scope, counter, settings, resources, null, null, null,
                span, parent, layer, seed, selection, groupName, splitLayers)
        {
        }

        private GeneratorContext(IObjectScope scope, IObjectIdCounter counter, LevelSettings settings,
            LevelResources resources, GameLevel game, AudioLevel audio, LevelHints hints, FrameSpan span,
            ObjectId parent, int layer, uint seed, IReadOnlyList<ObjectId> selection, string groupName,
            bool splitLayers)
        {
            Scope = scope ?? throw new ArgumentNullException(nameof(scope));
            Counter = counter ?? throw new ArgumentNullException(nameof(counter));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Resources = resources ?? throw new ArgumentNullException(nameof(resources));
            Game = game;
            Audio = audio;
            Hints = hints;

            Span = span;
            _parent = parent;
            Layer = layer;
            Seed = seed;
            Selection = selection ?? Array.Empty<ObjectId>();
            _groupName = string.IsNullOrEmpty(groupName) ? null : groupName;
            IsSplittingLayers = splitLayers;
        }

        // After Generate, not inside Create: "the Nth object of this run" exists only once the run is
        // over, and a spawning generator writes Layer itself. The container keeps Layer 0 (the parent).

        /// <summary> Gives every object this run created its own Layer, stepping up from Layer in
        /// creation order. Called once by the base generator after Generate; a no-op when splitting
        /// is off. </summary>
        public void ApplyLayerSplit()
        {
            if (!IsSplittingLayers) return;

            var baseLayer = LocalLayer;
            var step = 0;
            foreach (var id in Log.GetCreatedIds())
            {
                if (id == _group) continue;
                if (!Scope.Objects.TryGetValue(id, out var obj)) continue;

                var layer = baseLayer + step++;
                obj.Layer = layer > ValueRules.MaxLayer ? ValueRules.MaxLayer : layer;
            }
        }

        // The group container is excluded deliberately: it is created with Layer 0 precisely so it
        // adds nothing to its children's effective layer. A malformed parent chain (a cycle) is
        // bounded rather than trusted - this walks author data.
        private int ParentLayerSum()
        {
            var sum = 0;
            var id = _parent;
            for (var guard = 0; guard < LevelRules.MaxObjectDepth; guard++)
            {
                if (!id.IsNotNull() || !Scope.Objects.TryGetValue(id, out var parent)) break;
                sum += parent.Layer;
                id = parent.ParentObjectId;
            }

            return sum;
        }

        // The container carries Layer ZERO, not this context's Layer: Layer is parent-relative
        // everywhere in the format, and the generator already puts context.Layer on every child - a
        // group repeating it would double the whole run's draw order.
        private ObjectId EnsureGroup()
        {
            if (_groupCreated) return _group;
            _groupCreated = true;

            var group = Create<RectObject>();
            group.ParentObjectId = _parent;
            group.Name = _groupName;
            group.Span = Span;
            group.Layer = 0;

            _group = group.ObjectId;
            return _group;
        }

        /// <summary> A fresh RNG on this context's seed. Take one per generator run, not per object,
        /// or every object gets the same number. </summary>
        public GeneratorRandom CreateRandom() => new(Seed);

        // Does NOT set Span/Layer/ParentObjectId: only the generator knows what it means by them. Nor can
        // it use LevelUtils.SetObjectId (that takes LevelSettings, the wrong counter in a template).

        /// <summary> Create a new object in the target scope with a freshly minted id. </summary>
        public T Create<T>() where T : RectObject, new()
        {
            var obj = new T { ObjectId = Counter.GetNextObjectId() };
            Scope.Objects.Add(obj.ObjectId, obj);
            Log.Add(new ObjectCreated(Scope, obj.ObjectId, obj));
            return obj;
        }

        /// <summary> Take an existing object for modification, snapshotting it first. The returned
        /// instance is the live one - mutate it freely, including its keyframe tracks. </summary>
        public T Edit<T>(ObjectId id) where T : RectObject
        {
            if (!Scope.Objects.TryGetValue(id, out var obj))
                throw new KeyNotFoundException($"Object {id.value} is not in the target scope");
            if (obj is not T typed)
                throw new InvalidCastException($"Object {id.value} is {obj.GetType().Name}, not {typeof(T).Name}");

            // A second Edit of the same object must not overwrite the ORIGINAL before-copy with an
            // already-modified one, or undo restores a half-generated state.
            if (!Log.HasEdit(Scope, id)) Log.Add(new ObjectEdited(Scope, id, obj.Copy()));
            return typed;
        }

        /// <summary> Same as Edit&lt;T&gt; when the concrete subtype doesn't matter. </summary>
        public RectObject Edit(ObjectId id) => Edit<RectObject>(id);

        // Changing an object's TYPE in place is its own primitive: Edit<T> refuses a mismatched subtype
        // and Delete + Create<T> mints a fresh ObjectId, orphaning children. ObjectEdited already keeps
        // whole instances by id, so no journal entry of its own.

        /// <summary> Replace an object with a fresh one of another type, keeping its id and every
        /// member RectObject declares. Returns the new instance, live in the scope. </summary>
        public T Replace<T>(ObjectId id) where T : RectObject, new() => Replace<T>(Scope, id);

        // The scope is a parameter rather than always this run's own because a run may legitimately
        // reach outside it: Level.Resources.Prefabs holds templates that are IObjectScopes in their
        // own right, and a sweep over them is one run, not one per template.

        /// <summary> Same, in a scope this run does not target - a Prefab template, say. </summary>
        public T Replace<T>(IObjectScope scope, ObjectId id) where T : RectObject, new()
        {
            if (scope?.Objects == null) throw new ArgumentNullException(nameof(scope));
            if (!scope.Objects.TryGetValue(id, out var obj))
                throw new KeyNotFoundException($"Object {id.value} is not in the given scope");

            if (!Log.HasEdit(scope, id)) Log.Add(new ObjectEdited(scope, id, obj.Copy()));

            var replacement = new T();
            replacement.Update(obj);
            scope.Objects[id] = replacement;
            return replacement;
        }

        /// <summary> Remove an object from the target scope. Children are NOT removed - reparenting
        /// or cascading is a content decision the generator makes explicitly. </summary>
        public void Delete(ObjectId id)
        {
            if (!Scope.Objects.TryGetValue(id, out var obj)) return;
            Scope.Objects.Remove(id);
            Log.Add(new ObjectDeleted(Scope, id, obj));
        }

        // For what is neither object, resource nor track (LevelSettings.Framerate/FrameDuration): a
        // delegate, since a journal entry only needs "put it back the way it was".

        /// <summary> Journalled write of one plain value. Reads the current value first, so undo
        /// restores exactly what was there. </summary>
        public void SetValue<T>(Func<T> read, Action<T> write, T value)
        {
            if (read == null || write == null) return;

            var before = read();
            write(value);
            Log.Add(new ValueChanged<T>(write, before, value));
        }

        /// <summary> Add a level resource to one of LevelResources' dictionaries. </summary>
        public void AddResource<TId, TResource>(Dictionary<TId, TResource> target, TId id, TResource resource)
        {
            target[id] = resource;
            Log.Add(new ResourceAdded<TId, TResource>(target, id, resource));
        }

        /// <summary> Remove one entry from a level-owned dictionary - a resource, or a scheduled
        /// audio track, which is shaped the same way. The removed value lives on in the journal, so
        /// undo puts it back under the same id. </summary>
        public void RemoveResource<TId, TResource>(Dictionary<TId, TResource> target, TId id)
        {
            if (target == null || !target.TryGetValue(id, out var resource)) return;
            target.Remove(id);
            Log.Add(new ResourceRemoved<TId, TResource>(target, id, resource));
        }

        // For the level-global tracks (game, camera, post-processing, player events), which have no owner
        // to snapshot. Neither sorts nor deduplicates: Frame uniqueness is a per-generator decision.

        /// <summary> Append a keyframe to a level-global track. </summary>
        public void AddLevelKey<TKey>(List<TKey> track, TKey key)
        {
            track.Add(key);
            Log.Add(new LevelKeyAdded<TKey>(track, track.Count - 1, key));
        }

        /// <summary> Remove every matching keyframe from a level-global track. This is what keeps a
        /// destructive generator undoable: the removed keys live on in the journal, so undo puts
        /// them back at the exact indices they were removed from. </summary>
        public int RemoveLevelKeys<TKey>(List<TKey> track, Predicate<TKey> match)
        {
            var removed = 0;
            for (var i = track.Count - 1; i >= 0; i--)
            {
                if (!match(track[i])) continue;
                Log.Add(new LevelKeyRemoved<TKey>(track, i, track[i]));
                track.RemoveAt(i);
                removed++;
            }

            return removed;
        }
    }
}