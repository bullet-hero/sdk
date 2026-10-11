# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Resources

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.

`LevelResources` (`Level.Resources`) and the `Resource` subtypes. The prefab template/placement data
shape is summarised here too, since a `Prefab` is a `Level.Resources.Prefabs` entry (its class lives in
`Models/Objects/`).

## Resources: invariants and traps

Full statement: `Docs/LEVEL_RESOURCES.md`.

- **Every `LevelResources` dictionary holds only user-defined (negative-id) resources**; game-defined
  ones live in the game's registries and never appear in a level. `TypedResourceId`: `0`=Null,
  `[1,MaxInt]` game-defined, `[MinInt,-1]` user-defined (needs `Resource.Sources`, at most
  `Resource.MaxSourcesCount`). `Guid` ids have no sign split - which collection an id is found in
  decides; `Guid.Empty` is Null.
- **Null is an authored state for `ShapeObject.ShapeId`, `ShapeObject.ColliderId` and
  `PrefabObject.PrefabId` - never add `[RuleIPrimitiveGuidNotNull]`**: its `Fix` mints a random `Guid`
  (a nonexistent shape, a dangling prefab), and it already broke `ValidatorTests` once. Likewise
  `ShapeObject.TextureResourceId`/`EffectObjectCore.TextureResourceId` take `allowNull: true` and no
  `[RuleIPrimitiveIntNotNull]`, defaulting to `Null`.
- **`TextureResource` carries six independent author axes** (`Kind`, `Alpha`, `Sampling`,
  `Compression`, `WrapU`, `WrapV`) - deliberately not one richer enum, and none of them a format, a
  size or a memory budget (that is `UserSettings.Graphics.Textures`). A kind SEEDS sampling and
  compression; the consumer's `TextureLoadPlanner` decides the device half.
- **`FontCharacters` is not a resource** - it is in `Level.Hints`.
- **Credits cover all seven kinds since `LevelMeta` generation 2** (`ResourceMeta.ResourceGuid` for the
  guid kinds, `RuleResourceMetaId` keeps the slots exclusive). `ResourceCollection` is a new domain,
  not a bump: no level refers to a collection.

## Prefabs: template and placement

Full statement: `Docs/PREFAB_PLACEMENT.md` ("The template and the placement", "Per-instance
overrides").

- **`Prefab.Root` is a FIELD, never an `Objects` entry**, addressed by `ObjectId.PrefabRoot`; one in
  `Objects` would be minted under every placement. `Root.Name` is the template's name and `Root.Span`
  its timeline - there is no second number; `PrefabRootUtils.SetTemplateLength` is the one writer.
- **Placements share the scope's `Objects` dictionary** (`ObjectType.PrefabObject`), nest up to
  `PrefabRules.MaxInheritanceLevel`, and carry `PlacementOffset` (`off`): `Span.StartFrame -
  PlacementOffset` is what every frame they contribute is measured from.
- **Materialized copies are not in the file**: `Utils/PrefabVirtualizationUtils` Thin/Expand rebuilds them
  from `pfid` + `ids` + `mod`. An outer id is BORN at edit time in `PrefabMaterializer`; the expander
  refuses rather than mints.
- **An override is keyed by the template's INNER id** (or `ObjectId.PrefabRoot`, legal only for
  `ModificationFields.IsPrefabRootField`) + a stable `ModificationFields` number + an index
  (`WholeField` = -1) - never a JSON key spelling. `Modification.Value` normalizes to `long`/`double`;
  the generated `ModificationTable` applies it; overrides are re-applied after every materialize/resync.

## Docs

| Question | File |
|---|---|
| resource dictionaries, id ranges, texture fields, nullable references, credits | `Docs/LEVEL_RESOURCES.md` |
| template/placement shape, root ownership, overrides, thin/expand, flatten, depth | `Docs/PREFAB_PLACEMENT.md` |
| `Guid` vs `int`, and the sign table | `Docs/IDENTIFIERS.md` |
| the texture enums' own reasoning | `Docs/ENUMS.md`, "Texture axes" |
| reference counting, closure, remap | `Docs/RESOURCE_GRAPH.md` |
