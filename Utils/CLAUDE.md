# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Utils

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Utils/

`BHSDKMath` (Unity-independent math, since the core assembly can't reference
  `UnityEngine.Mathf`), `DimensionalIndexer2` (flat-array↔2D-grid indexing), `LevelUtils`
  (`RectObject` id/parent/bounds setters), `LevelCapacityUtils` (sweep-line "peak simultaneous
  objects per type" measurement backing `LevelHints.Limits` — see below), `LevelStatsUtils`
  (`LevelStats`/`LevelObjectStats`/`LevelResourceStats` — what a level *holds*, one O(n) allocation-free
  pass, for showing rather than for sizing; don't reach for it when you need `LevelCapacityUtils`'
  peak-simultaneous answer, and don't reach for that one to fill a readout), `ModelUtils`
  (deep-copy/equality/hash helpers for `List`/`Dictionary`/`Array` of `ICopyable<T>`),
  `ModificationUtils`/`TypeExtensions`,
  **`PrefabVirtualizationUtils`** (+`PrefabExpandReport`) — the pair that keeps a prefab placement's
  materialized copies OUT of the file: `Thin` drops them before a write, `Expand` rebuilds them from
  `pfid` + `ids` + `mod` after a read, and the level every other system sees is flat either way. Its
  header carries why; `Docs/Issues/PREFAB_VIRTUALIZATION_HISTORY.md` is the record. Two things about it
  are load-bearing rather than incidental: **it never mints an outer id** (ids are born at edit time,
  in the consumer's `PrefabMaterializer`, and a missing one is reported), and **`Expand` writes by
  INDEXER**, which is what lets a level saved before virtualization — copies AND table both present —
  open without doubling them, per Rule 11's no-migration stance.
  **`ObjectDepthUtils`** — parent-chain depth math over an `IObjectScope`, bounded by
  `LevelRules.MaxObjectDepth`. It lives here rather than beside the edit-time materializer because
  the expander above has to refuse the same chains and cannot reach `BH.Core`; the consumer's
  `BH.Core.Utils.HierarchyMath` is a forwarding facade over it.
  `ShapeLoopUtils` (radial loops - the two emitters every
  built-in shape is built out of, a fan and a resampled annulus, plus sector clipping; its header
  explains why angles are always measured RELATIVE to a reference and why the annulus resamples both
  rims instead of walking them in lockstep, both of which were real bugs), `ShapeSynthUtils`
  (procedural geometry for shapes the built-in library cannot name - **the Afterbeat importer's
  rounded custom polygons and its two arrows, and nothing else** now that the library covers its own
  parameter space; it reasons in the OPPOSITE angular convention to `ShapeLoopUtils`, measuring from
  straight down, so the two must never share a phase predicate), `ShapeGeometryUtils`
  (+`ShapeGeometryReport`) — the single
  implementation of "what a valid shape is" and "how to make an invalid one valid", shared by
  `RuleShapeGeometry.Fix` and the consumer's in-game shape editor on Save. Its `Sanitize` order is
  load-bearing (clamp → weld → drop malformed → drop degenerate → trim → drop orphans → fix
  winding): every step can reintroduce a problem an earlier one fixed, and this is the order in
  which none does.
