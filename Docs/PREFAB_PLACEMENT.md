# Prefab placements in the SDK: root ownership, flatten, thin/expand, depth

The rules every consumer of a placement shares. Two consumers exist - the edit-time `PrefabMaterializer`
(Materialize/Resync, in the game project) and the load-time `PrefabVirtualizationUtils.Expand` - and they
stay correct only together, which is why the rules live here: Core references the SDK, never the reverse.

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
  `OpLevelObjectSpan` records the divergence only when the written duration actually differs.
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
- `SetRootDuration` is the one writer of a template's length: start `FrameRules.MinFrame` (timeline counts
  from one), no anchors (the root has no parent to follow; `RulePrefabRootFixed` reports both).
- `ObjectScopeExtensions.TryGetObject` is the one place an `ObjectId` becomes a `RectObject`, because the
  root is a field, not an `Objects` entry. It is deliberately NOT applied to loops that ENUMERATE a
  scope (materialization, packing, depth, the graph pass): the root is not content, it is what the
  content hangs off.

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
