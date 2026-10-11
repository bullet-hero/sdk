# CLAUDE.md - Assets/Plugins/BH.SDK/Generators

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

Authoring automation: a generator produces level content from a few parameters. `IGenerator` splits
into `ILevelGenerator` (a whole `Level`+`LevelMeta`) and `IScopeGenerator` (`Content`/`Modifier`);
derive from `BaseLevelGenerator<T>`/`BaseContentGenerator<T>`/`BaseModifier<T>`, or
`BaseSpawnGenerator<T>` for placement math. Subfolders: `Spawn/`, `External/`, `Modifiers/`,
`Import/`, and the concrete generators in `Geometry/`, `Bullets/`, `Audio/`, `Textures/`, `Utility/`.

Why everything is shaped as it is: `Docs/GENERATORS.md` ("The folder at a glance" is this folder's
former text, whole) and this folder's own `README.md`, the design record.

## Traps

- **All mutation goes through `GeneratorContext`** - its `GeneratorChangeLog` journal IS undo;
  writing the model directly silently breaks it.
- **Changing an object's TYPE is `Replace<T>`**, never `Delete` + `Create<T>` (a fresh id orphans
  every child). `GeneratorChangeLog.HasEdit` is keyed by scope AND id - a `Prefab` owns its id space.
- **Parent to `context.Parent`** to get grouping for free; write `LocalLayer`, not the effective
  `Layer`, under a non-zero parent. `ApplyLayerSplit` runs after `Generate` and wins.
- **A keyframe's `Frame` is LOCAL to its object** (`BaseSpawnGenerator`'s `Add*` helpers convert).
- **Rotation is STORED IN RADIANS** - `AddRotation` converts from degrees.
- **A staggered generator must not spawn past its window** (`CanSpawn`); a one-frame lifetime gets one
  key per track (`CanAnimate`, and `Estimate` applies the same clamp).
- **The last legal frame is `FrameRules.LastFrameOf`**, never written by hand - validation will not
  catch content past the end.
- **A parameters class must never shadow an inherited field** - everything is keyed by field NAME.
  Host inputs are matched by interface (`External/`), not by name.
- **`GeneratorRandom` (xorshift32) only** - `System.Random` is not stable, `UnityEngine.Random` is not
  reachable.
- **`mod_prefab_flatten` declines in Prefab Mode** and runs level-then-templates.
