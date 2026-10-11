# Prefab placements in the SDK: root ownership, flatten, thin/expand, depth

The rules every consumer of a placement shares. Two consumers exist - the edit-time `PrefabMaterializer`
(Materialize/Resync, in the game project) and the load-time `PrefabVirtualizationUtils.Expand` - and they
stay correct only together, which is why the rules live here: Core references the SDK, never the reverse.

The data shape and the per-instance overrides were split out of `Models/Resources/CLAUDE.md`, which keeps
the summary and links here.

## The template and the placement (data shape)

- `Prefab` (`Models/Objects/Prefab.cs`, a `Level.Resources.Prefabs` entry) is the *template*: its own
  `Objects`/`ObjectIdCounter` and its **`Root`**, which carries its name, its transform and its timeline.
  `PrefabObject` (a `RectObject` subclass) is the *placement*: `PrefabId` (which template) +
  `Dictionary<ObjectId, ObjectId> ObjectIds` (template-inner id -> this placement's own materialized
  outer id) + `Dictionary<ModificationKey, Modification> Modifications` (per-instance field overrides,
  below). Placements - whether at level scope or nested inside another `Prefab`'s own `Objects` - live
  in the **same** `Objects` dictionary as everything else, discriminated only by
  `GetModelType() == ObjectType.PrefabObject`; there's no separate placement list.
- **Nesting is bounded by `PrefabRules.MaxInheritanceLevel`** (8; the consumer's
  `ResourceSettings.PrefabsMaxInheritanceLevel` may only lower it) - a template may hold a placement of
  another template, and `PrefabMaterializer.Resync` re-propagates through every level of that up to the
  limit.
- **The device-wide prefab library is the only cross-level sharing mechanism a prefab has**, since
  prefabs have no game-defined preset tier. The editor's prefab picker offers it as its own tier
  (`SearchOrigin.Library`); choosing an entry imports the resource, creates the placement and
  materializes the template as ONE undo step.
- **A TEMPLATE OWNS ITS ROOT, and a placement is that root's materialization.** `Prefab.Root` is a
  `RectObject` that always exists, addressed by `ObjectId.PrefabRoot` - which is its own `ObjectId`, not
  a sentinel - and is what every inner object with an unset/`PrefabRoot` parent hangs off. It is a FIELD
  and never an entry in `Objects`, which is the one condition the whole design rests on: one in that
  dictionary would be minted as an ordinary object UNDER the placement, costing a transform and a level
  of depth per placement. `Prefab` has **no `Name` of its own** - `Root.Name` is the template's name, so
  renaming a template reaches every placement of it.
- **THE TEMPLATE'S TIMELINE IS `Root.Span`, and there is no second number for it.** A `FrameDuration`
  field of `Prefab`'s own was read by nothing - two spellings of one fact, free to drift - so it is gone,
  `IFrameScope` with it, and `Prefab` is a plain `IObjectScope`. The span's **duration** is the authored
  half; its **start** is `FrameRules.MinFrame` and its anchors none, both pinned by `RulePrefabRootFixed`
  along with the root's identity, `Active` and `Layer`. `BH.SDK.Utils.PrefabRootUtils` is the ownership
  table and the one writer of the length (`SetTemplateLength`).
- **`PlacementOffset` (`off`) is the fourth thing a placement carries**, and the only one that is a plain
  authored field rather than bookkeeping: how many frames into the template this placement starts
  playing. It moves everything the template contributes - every materialized copy's span (both
  `ApplyPlacementFrameOffset` twins subtract it) and the placement's own instance origin
  (`PlacementTrimMath.InstanceSpan`) - so `Span.StartFrame - PlacementOffset` is the number every frame a
  placement contributes is measured from. That is what lets the timeline's Edges and Scissors tools trim
  and cut a placement with the level playing identically afterwards, and it is bounded by
  `Span.StartFrame - MinFrame`. `Docs/Issues/PREFAB_ROOT_HISTORY.md` (game repo) is the record for the
  root, the length and the offset.

## `Prefab.Root` and who owns what (`PrefabRootUtils`)

- **The template owns** the root's Name, all seven positional tracks and its span's DURATION. All are
  written onto the placement on every materialize and resync, so editing the root in Prefab Mode moves
  and re-lengthens every placement of the template. Positional tracks going to the template is a
  deliberate departure from Unity, which treats an instance root's transform as per-instance: a
  `PrefabObject` often has no parent or is pinned where its TRS keyframes ARE the content, so a template
  that dropped them would lose authored data. Divergence between placements is a `Modification`.
- **The duration is the template's by default and overridable on top**: `ApplyRoot` writes it
  unconditionally, `ApplyModifications` runs after it in both consumers, so a placement carrying
  `ModificationFields.PlacementDuration` (root-keyed) gets its own length back one step later.
  `OpLevelObjectSpan` records the divergence only when the written duration actually differs, so an
  ordinary MOVE records nothing and changing a template's length still moves every placement nobody has
  trimmed.
  `ModificationFields.Span` stays out of `IsPrefabRootField`: a whole-span override would restate the
  start and beat the next ordinary move.
- **The placement owns** the span START, Active, Layer and `PlacementOffset` (how far into the template
  it starts playing). Start plus in-point are the origin `ApplyPlacementFrameOffset` reads for everything
  inside it. The template's own Active and Layer are pinned by `RulePrefabRootFixed` because nothing
  copies them.
- **No frame offset is applied to the root.** An inner object's span is local to the template's timeline
  and gets the placement's start added back; the root has no span anybody reads and the placement's own
  span IS the anchor being added, so shifting anything there counts the origin twice.
- The root is not a key in `placement.ObjectIds` (it has no outer copy, the placement is it), so a
  modification addressed at it short-circuits the table lookup. Without that branch an override on the
  root is recorded, saved, listed as present and never applied
  (`Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md` in the game repo).
- `ObjectIds` is keyed inner -> outer (the direction every materialize walks); the reverse question is a
  scan, since a placement holds at most `PrefabRules.MaxObjectIdRemaps` entries and a second dictionary
  would have to follow every resync.
- `SetTemplateLength` is the one writer of a template's length: start `FrameRules.MinFrame` (timeline counts
  from one), no anchors (the root has no parent to follow; `RulePrefabRootFixed` reports both).
- `ObjectScopeExtensions.TryGetObject` is the one place an `ObjectId` becomes a `RectObject`, because the
  root is a field, not an `Objects` entry. It is deliberately NOT applied to loops that ENUMERATE a
  scope (materialization, packing, depth, the graph pass): the root is not content, it is what the
  content hangs off.

## Per-instance overrides (`PrefabObject.Modifications`)

**Live and load-bearing** - this is how a placement diverges from its template without breaking the
link. Three pieces:

- `ModificationKey` (`Models/Primitives/`) - the *address*: `ObjectId` (the **template's inner** id, not
  the materialized outer one, so the key survives re-materialization - or `ObjectId.PrefabRoot`, which
  addresses the placement ITSELF and is legal only for the fields the template owns,
  `ModificationFields.IsPrefabRootField`) + `int Field` (a stable `ModificationFields` number, NOT a JSON
  key spelling) + `int Index` (which element of a collection field, or `WholeField` = -1). Being the
  dictionary key is what makes "one override per (object, field) pair" a structural guarantee rather
  than a rule to enforce. It was a dotted string (`"pos[0].v"`) until a key rename's cost outgrew it -
  `Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md` (game repo) carries why, including the three
  spellings that never resolved at all.
- `Modification` (`Models/Objects/`) - `Key` + an untyped `object Value`. The `Value` setter
  **normalizes integrals to `long` and floating-point to `double`** on assignment, deliberately matching
  what Newtonsoft always produces when deserializing a raw JSON number into an `object` property -
  without it an override built in code with a plain `int` stops `Equals`-ing itself after a round trip.
  Its file header also lists the design limits still in force: only `RectObject`/`Prefab` targets, no
  parenting a `RectObject` *into* a prefab's inner objects (only the reverse), and no deep inheritance -
  an override applies only within the prefab scope it lives in.
- `Utils/ModificationUtils.cs` + the generated `ModificationTable` - applying one. The table is a flat
  switch written by `BH.SDK.Roslyn`'s `ModificationTableGenerator` from every member carrying
  `[ModificationField]`, so the apply path is reflection-free; `ModificationValues.TryConvert` converts
  the untyped value back through the serializer that shaped it, which is what makes an int, an enum and
  an id override land at all. `ModificationUtils` keeps the rule-checked write
  (`IsValueAllowed`/`SetValueChecked`), the one place a `PropertyInfo` is still needed - an override
  writes past every rule the target property carries, and nothing else can judge it
  (`UTILS_DESIGN.md`, "Overrides").

Overrides are **re-applied on top of a fresh template copy after every materialize/resync** (Unity-side:
`Core`'s `PrefabMaterializer.ApplyModifications`; recorded by `GameEditor`'s
`ModificationRecorder`/`EditObjectOperation.RecordModification` - see those folders' `CLAUDE.md`s).
`Modifications` serializes through its own `Serialization/Converters/Dict/DictionaryModificationsConverter`
(the key is recoverable from the value's own `Key` property, so it writes as a plain array - same family
as `DictionaryAsListConverter`).

## Flatten (`PrefabFlattenUtils`)

Flattening is the inverse of placing: it drops the bookkeeping tying a `PrefabObject` to its template and
leaves the copies it owns as ordinary objects. Nothing is created, moved or re-materialized; the copies
already are permanent positively-id'd objects, and Modifications were applied ONTO them, so dropping the
table loses no authored value.

- **A flatten must answer for every `PrefabObject` the placement owns.** A template holding a nested
  placement materializes FLAT: the nested one is an ordinary `PrefabObject`-typed entry whose own
  `ObjectIds` names ids in the TEMPLATE's scope. `FindPlacements` skips it only because its id appears as
  a VALUE in the outer table. Clear that table alone and the copy reads as a genuine placement with a
  table into a scope the host knows nothing about, and the next resync of the inner template writes over
  host ids belonging to something else.
- **Exactly two answers**: flatten the whole CHAIN (nothing keeps a table), or flatten the TOP ALONE with
  the nested copy staying a placement linked to its own template and its table REBASED into host ids
  first (`TryRebaseNested`: the outer table reads outer-template id -> host id, and a nested copy's values
  ARE outer-template ids, so one lookup per entry). The rebase must happen while the outer table is
  intact. A copy that cannot be rebased is not flattened on that copy and the caller is told; a
  half-rebased table is the one state neither answer survives.
- The sweep takes every `PrefabObject` in the scope; it deliberately does not draw the genuine /
  materialized-copy distinction `FindPlacements` draws.
- `Update` copies exactly the members `RectObject` declares (id, parent, name, active, span, layer, the
  seven tracks). `PrefabId`, `ObjectIds`, `Modifications` have nowhere to go. **`PlacementOffset` is not
  dropped**: it says WHEN, so dropping it would move the picture. A placement reads tracks from
  `Span.StartFrame - PlacementOffset` (`PlacementTrimMath.InstanceSpan`), a plain object from its own
  start, so keys are rebased by the difference. A key rebased outside the span is DROPPED - the one lossy
  thing a flatten does, the same call the cut makes (`ObjectSplitMath.MapKeyframe`). Children need
  nothing: their spans already carry the subtraction. A placement without offset is untouched.

## Thin and expand (`PrefabVirtualizationUtils`)

A placement's materialized copies are rebuildable from four things: `pfid` (the template), `ids` (every
template-inner id to the copy's permanent outer id), `mod` (overrides) and `off` (the in-point the spans
are pulled back by, `PlacementOffset`). The copies therefore stay out of the file and are rebuilt on
load. Measured: 62-82% of four corpus levels' bytes, 13% of volcano's, and gzip does not absorb it (each
copy carries its own id and span offset; deflate's window is 32 KB).
`Docs/Issues/PREFAB_VIRTUALIZATION_HISTORY.md` (game repo) is the record.

- **Nothing about the model changed and nothing else learns that a load-time step exists**: by the time
  the editor, the runtime or a validator sees a level, the scope is flat exactly as it always was. The
  one rule to keep straight is where an outer id is BORN - at edit time, in the consumer's
  `PrefabMaterializer`; the expander reads the table and refuses rather than minting.
- **`Level.ShallowClone` is what Thin writes through**, a reference-sharing copy (`MemberwiseClone`, used
  only by `PrefabVirtualizationUtils.Thin`): a write must replace two collections without touching the
  level the editor keeps editing through a save, and `Copy()` walks the whole graph (124 ms on the
  largest level when measured, paid on every autosave). `MemberwiseClone` over a member list, so a later
  member joins by itself; the shared instances are safe because the writer only reads.
- **The editor still materializes.** Thin and Expand bracket the FILE, not the model: once a level plays
  the same flat `GameLevel.Objects` exists, and `PrefabMaterializer` remains the only thing that MINTS an
  outer id. Ids are born at edit time, never at load, which is what makes the table authoritative.
- **Expansion writes by indexer, not `Add`**, so a level saved before virtualization (flat copies AND an
  id table) still opens: an indexer overwrites in place. No migration, no drop pass.
- **Thin builds a NEW dictionary** instead of removing from the old one: remove-and-add reshuffles entry
  slots and slot order is what both writers iterate, so bytes would depend on whether the level had been
  thinned before. It also keeps Thin non-destructive for a live level. Capacity is `objects.Count`, not
  the difference: a level straight out of an importer has every `ObjectIds` filled and no copies yet, so
  the difference goes negative and the constructor throws.
- **The shared predicate**: an object is a materialized copy exactly when its id appears as a VALUE in
  some placement's `ObjectIds` within the SAME scope (same as `FindPlacements`). A `PrefabObject` can be
  both (the nested-placement case above).
- **The root is written before, and independently of, the id table** (the placement IS its copy), so a
  template holding only a root still materializes that much. A placement with no table at all (import
  before its host resynced, or refused on depth) yields ONE finding, not one per template object.
- **Spans**: a template object's span is local to the prefab's own timeline, like a keyframe's `Frame` to
  its object's span start. Only the start moves; a duration is the same number in any coordinate system.
  Composition is `ToGlobalFrame`, never a bare addition: a local frame is a frame, the placement's first
  frame is local `FrameRules.MinFrame`, and adding outright puts every copy one frame late (the
  materializer carries the same formula).
- **Parents**: an inner object whose parent is unset (Null) or `ObjectId.PrefabRoot` auto-parents to the
  placement. One naming another inner object is remapped through the placement's table; a dangling
  reference stays unremapped for the rule system to flag.
- `PrefabExpandReport` caps its entry list but not its counts: one broken template is one finding per
  PLACEMENT (340 placements would be 340 stack captures for one mistake); the counts still say how big it
  is.

## Depth (`ObjectDepthUtils`)

Layer is summed up the parent chain, and the chain is bounded: `LevelRules.MaxObjectDepth` is one below
the player's `MaxChildInherit`, the size of `BuildInstancesParentingJob`'s stackalloc. Past the cap the
runtime composes an object against a mid-chain ancestor: no exception, no validation failure, just a draw
in the wrong place. Every edit that can deepen a chain (reparent, materialize, load-time expand) asks here.

- It lives in the SDK because the load-time expander is SDK code and cannot reach Core, while the editor's
  refusals are in Core; the game's `HierarchyMath` is a forwarding facade kept for IL2CPP options.
- Every walk is bounded by the cap itself: author data can be cyclic (`LevelGraphAnalyzer` catches cycles,
  the per-property rules do not).
- The subtree height is a PARAMETER because measuring it walks the whole scope while the parent's depth is
  at most `MaxObjectDepth` steps; a parent picker measures once and pays only the cheap half per row.
- A template's inner objects are copied under the placement, so its internal depth stacks on the
  placement's. The ROOT COSTS NO DEPTH only while `Prefab.Root` stays a field: the walk is over
  `Objects.Values` and stops at `PrefabRoot` without incrementing. Were the root an `Objects` entry it
  would be minted as an ordinary object and every cap would be off by one.
