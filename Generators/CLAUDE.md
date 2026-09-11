# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Generators

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Generators/

authoring automation: a generator produces level content from a few parameters.
  Non-generic `IGenerator` root (so `GeneratorRegistry`'s reflection scan and a reflection-built
  form are possible) split into `ILevelGenerator` (builds a whole `Level`+`LevelMeta`) and
  `IScopeGenerator` (`Content`/`Modifier`, writes into an existing scope), with
  `BaseLevelGenerator<T>`/`BaseContentGenerator<T>`/`BaseModifier<T>` as the bases anyone actually
  derives from. **All mutation goes through `GeneratorContext`**, which journals it into a
  `GeneratorChangeLog` — that journal *is* undo, and writing to the model directly silently breaks
  it. **`Replace<T>` is the third write primitive beside `Create`/`Edit`/`Delete`**, and it exists
  because neither of the others can change an object's TYPE: `Edit<T>` refuses a mismatched subtype
  by contract, and `Delete` + `Create<T>` mints a fresh `ObjectId`, which orphans every child pointing
  at the old one. The id is exactly what has to survive, so the object is replaced under it — and
  `ObjectEdited` already keeps whole instances and assigns them back by id, so no journal entry of its
  own was needed. Its overload takes an `IObjectScope`, since a run may legitimately reach outside its
  own (`Level.Resources.Prefabs` holds templates that are scopes in their own right); that is also why
  `GeneratorChangeLog.HasEdit` is keyed by scope AND id rather than by id — a `Prefab` owns its
  `ObjectId` namespace, so id 1 in a template and id 1 in the level are different objects.
  The context also owns **grouping**: given a `groupName`, `context.Parent` lazily creates one
  container `RectObject` (Layer 0 — Layer is parent-relative, the children already carry
  `context.Layer`) and returns it, so every generator that parents to `context.Parent` gets
  "wrap this run in one object" for free, including future ones; `BaseScopeGenerator.Estimate` adds
  the container's object, and a run that creates nothing creates no container either. `Layer` is the
  author's **effective** number while `LocalLayer` is what an object actually stores (`Layer` minus
  the parent chain's sum) — Layer being parent-relative, writing the raw number under a non-zero
  parent offsets the whole run. **Layer
  splitting** is the same shape: `ApplyLayerSplit` runs over the journal after `Generate`
  (`BaseScopeGenerator.Run`), giving each created object its own `Layer` stepping up from
  `context.Layer` — after, so it also wins over a generator that writes `Layer` itself, and skipping
  the container for the same parent-relative reason. Also: `GeneratorHints` (form order/**sections**/ranges/units/visibility, since parameter
  classes carry no attributes — `Section(key, fields)` is `Order` plus a header, so grouping can't
  disagree with ordering; `GeneratorSections.Main`/`Additional` is the shipped vocabulary, and a
  parameters class must never shadow an inherited field, since everything here is keyed by field
  NAME), `GeneratorCost`/`GeneratorRequirements`, `GeneratorRandom` (deterministic xorshift32
  — `System.Random`'s sequence isn't contractually stable and `UnityEngine.Random` isn't reachable).
  Subfolders: `Spawn/` (`SpawnParameters` + `BaseSpawnGenerator<T>` — the shared object template and
  the mint/parent/frame bookkeeping, so a concrete generator is only placement math; its
  `MainFields`/`AdditionalFields` are spliced into each generator's own `Section` calls), `External/`
  (the `IAudioFileInput`/`IWaveformInput`/`IBeatFramesInput`/`IPixelTextureInput` interfaces a
  generator implements to say "this parameter comes from the host" — matched by interface, not by
  field name, so a rename is a compile error), `Modifiers/` (`ObjectTrackMask`/`ObjectTracks` —
  generic enumeration of an object's ten keyframe tracks, plus the modifiers themselves),
  `Import/` (`LevelPackageGenerator`, `gen_level_package` - importing this project's OWN package, as opposed to `Interop/`'s foreign formats),
  `Geometry/`, `Bullets/`, `Audio/`, `Textures/`, `Utility/` (the concrete generators — 21 of them,
  the roster the design document calls complete plus `mod_content_remover`/`mod_framerate_remap`,
  `mod_span_fit`, which fits every child's lifetime to its parent's and is what replaced the removed
  `GraphRule.ChildSpanOutsideParent` and its auto-repair, and `mod_prefab_flatten`, which turns every
  prefab placement in a scope back into an ordinary object — see the consuming project's root
  `CLAUDE.md`, "Prefab system", for why a flatten always takes a whole chain, and note two things
  about the generator itself: its two switches need the WHOLE level and therefore decline in Prefab
  Mode, and its run order is level-then-templates, because a template edit propagates to placements
  and the other order leaves the level describing content its templates no longer hold).
  Three rules a spawning generator must get right:
  **a keyframe's `Frame` is LOCAL to its owning object** (the runtime reads `obj.Span.StartFrame + Frame`,
  so an absolute frame yields objects that spawn correctly and then never move — `BaseSpawnGenerator`'s
  `Add*` helpers convert, `mod_quantize_keyframes` converts the other way to snap against the level's
  own grid, and a sweep test pins it),
  **placement math is in degrees but rotation is STORED IN RADIANS**
  (`AddRotation` converts; a raw 45 becomes 45 radians ≈ 2578°, and the Unity project converts back
  to degrees only at its inspector boundary), **a staggered generator must not spawn past its
  window** (`CanSpawn` — the overflow used to clamp onto the last frame as one-frame ghosts), and
  **a lifetime clamped to one frame gets one key per track**, not two — see
  `BaseSpawnGenerator.CanAnimate`, and note that `Estimate` has to apply the same clamp. A third,
  format-wide: **`FrameDuration` is a count** and the timeline counts frames from
  `FrameRules.MinFrame`, so the last legal frame IS `FrameDuration` — spell it
  `FrameRules.LastFrameOf`, never by hand (`RuleLevelFrame`'s bounds are inclusive at both ends).
  Has its own `README.md`, and that README is now the design record: the spec it was written
  from is no longer present in the consuming project.
