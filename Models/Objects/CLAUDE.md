# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Objects

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.

`RectObject` is the base of every placeable scene object (`ObjectId`, `ParentObjectId`, `Name`,
`Active`, `Span`, `Layer`, the seven shared keyframe tracks); subclasses `ShapeObject`,
`EffectObject`, `TextObject`, `PrefabObject`, each overriding `GetModelType() : ObjectType`. Every
design fact, in full: `Docs/OBJECTS.md`.

## Traps

- **A new `RectObject` subtype extends `ObjectConverter.GetType`'s switch by hand** - no attribute
  discovery, no `$type`; an unknown tag reads back as a plain `RectObject` (reported).
- **`ShapeId` (drawn) and `ColliderId` (hit) are independent**; "not drawn but solid" is a Null
  `ShapeId` with a real `ColliderId`. `Active` gates both paths, down the hierarchy.
- **A built-in `ShapeId` is its parameters packed**; `Models/Primitives/ShapeId.g.cs` is written only
  by `Services/Shapes/ShapeCatalogService`. Form code 0 is reserved and never issued.
- **`IObjectScope` and `IObjectIdCounter` are split at level scope** (`Level.Game` +
  `Level.Settings`) and joined on `Prefab` - never assume symmetry.
- **`ObjectId` reserves `Camera = -1`, `LocalPlayer = -2`, `PrefabRoot = -3`.** `PrefabRoot` is an
  ADDRESS (`Prefab.Root` carries it as its identity), not a sentinel; both id rules are context-checked.
- **`Modification.Value` is an untyped `object`** - hand-written copy/equality/codecs; integrals widen
  to `long`, floats to `double`, non-scalars compare with `JToken.DeepEquals`.
- **`PlacementOffset` moves everything the template contributes**; a placement's length is an
  override applied by `PrefabRootUtils.ApplyRoot`, serialized by neither codec.
- **`RectObject.Layer` is static on purpose** (a layer track would reroll every random value under it).
- **Override field ids belong to the DECLARATION** (`ModificationFields`, one band per declaring
  type, a new type takes the next FREE band); a root-keyed override may address only what the
  template owns.
- **`TextRules`' Fillment/Appearing fallbacks mean "effect off"**; `AppearingMask`'s cap counts CODE
  UNITS.
