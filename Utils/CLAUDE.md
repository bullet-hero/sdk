# CLAUDE.md - Assets/Plugins/BH.SDK/Utils

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

Every helper here, and why: `Docs/UTILS_DESIGN.md` ("The folder at a glance" is the catalogue;
model copy/merge, overrides, level measurement, hashing, text, Interop above it) and
`Docs/RESOURCE_GRAPH.md` (references, closure, remap, naming).

## Traps

- **`ObjectScopeExtensions.TryGetObject`/`GetObject` is the ONE way an `ObjectId` becomes a
  `RectObject`** - `scope.Objects` cannot see `Prefab.Root`. Enumerating loops deliberately skip it.
- **`PrefabRootUtils` and `PrefabVirtualizationUtils.Expand` stay correct only together** with the
  consumer's `PrefabMaterializer`; `Expand` never mints an outer id and writes by INDEXER.
  `SetTemplateLength` is the one writer of a template's duration.
- **A root-keyed `Modification` lands only through `TryGetModificationTarget`.**
- **`ObjectDepthUtils`: the template root costs no depth** only while `Prefab.Root` is a field.
- **`ShapeGeometryUtils.Sanitize`'s step order is load-bearing** - don't reorder it.
- **`ResourceGraph` is the ONE list of reference edges** - a new reference field goes there, or
  closure, remap and the delete count all miss it. `ResourceReferenceUtils` walks every scope,
  `Prefab.Root` included, and counts no NULL id.
- **Cut strings through `SurrogateUtils`** - every length cap counts UTF-16 code units; it is not
  grapheme clustering.
- **`LevelCapacityUtils` (peak simultaneous) and `LevelStatsUtils` (what a level holds) are not
  interchangeable.**
