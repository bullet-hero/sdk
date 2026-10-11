# Generators: the reasoning behind the contract and the shipped set

Contributor-facing. `Generators/README.md` is how to write one and the roster; `Generators/CLAUDE.md` is
the map. This file holds the design reasoning the code used to carry in comments, per piece.

## The contract

- **`IGenerator` is non-generic on purpose.** A CRTP-style constraint on the parameters class made a
  common root impossible, so a registry was impossible, so every host hardcoded every generator by hand.
  Everything a host needs to LIST, LABEL and build a FORM for is on the interface; running needs one of
  the two sub-interfaces, picked by `Kind`.
- **Sort order is a property of the generator.** Rank first, `NameKey` second: everything at the default
  rank stays alphabetical and stable, negative pulls forward (the obvious starting points), positive
  pushes back (an import is nobody's first answer to "make a level"). The level-creation list is a first
  impression, and a list kept in the UI would mean editing a screen that has never heard of the generator.
- **`ILevelGenerator`** builds a whole level from nothing, before any level is open: no `GeneratorContext`,
  no change log (undo is not creating the level). **`IScopeGenerator`** serves Content and Modifier
  because they differ in intent and Requirements, not in mechanism. A host refuses a run whose whole
  estimate is zero (a button that silently does nothing is worse than a refusal), so a generator whose
  output `GeneratorCost` cannot express (a bare advisory field) declares so on the interface instead of
  the host guessing from `Kind`.
- **`GeneratedLevel` is one value** (not a `GenerateLevel`/`GenerateMeta` pair): `Level` and `LevelMeta`
  are two independent roots describing one thing, and two passes can disagree about, say, its length.
- **`GeneratorHints`**: a parameters class carries NO attributes (the core would define UI vocabulary it
  cannot render). Whatever a host derives from a field's type it reflects; the rest is plain data here,
  keyed by field name through `nameof` (a rename is a compile error, not an ignored hint). `Order` is
  mandatory because `Type.GetFields()` order is unspecified by the CLI. `Section` is `Order` plus a
  header, so grouping cannot disagree with ordering; Main first. `EmptyLevelGenerator` is the reference
  implementation (if it needs a special case, the contract is wrong); `Parameters` are public mutable
  fields because a form binds by reflection and a preset serializes from them - the one place in the SDK
  where that is intended.
- **`GeneratorRandom` is xorshift32**: `System.Random`'s sequence is not contractually stable (.NET Core
  reimplemented it once) and "same seed, same level" is a promise to authors; `UnityEngine.Random` is
  unreachable (`noEngineReferences`). Fully specified by its source, adequate for scattering bullets,
  not cryptographic (`CryptographyService`), not statistical.
- **`GeneratorRegistry`** is a one-time reflection scan in a static constructor, the shape
  `VersionedTypeRegistry` proved under IL2CPP stripping, so no new `link.xml` entry. Only this assembly is
  scanned: generators are format-level content and belong next to the format; widening means deciding
  which assemblies, and nothing asks for it.
- **`GeneratorResult` does not run validation**: it costs a full reflective walk and what to do about
  findings is host policy (`ValidationFacade` makes the same point); a host that wants a report runs it and
  calls `WithReport`.
- **`GeneratorCost.Estimate()` must agree with `Run()`**, covered by a test, because a host shows it before
  running and refuses past `LevelRules.MaxObjects`. It counts what a run ADDS: keys `ClearRange` removes
  are not subtracted ("8 keys, and by the way 5 disappear" is two questions). `BeatFlashGenerator`'s
  estimate replays `Generate` counting instead of adding: the key count depends on how beats collide with
  each other and with existing keys, so multiplying beats by four would be wrong.
- **`BaseModifier` defaults Requirements to Selection**: a modifier that works on the whole scope
  regardless of selection is the exception and says so. A modifier that works on the whole scope
  (`mod_content_remover`, `mod_span_fit`, `mod_prefab_flatten`) does so because "leave nothing here" is a
  statement about the scope, and limiting it to the selection would leave the rest while reporting
  success. They are not `LevelScope` either: the window comes from the context, so they are as meaningful
  inside a Prefab template.
- **`ExternalInputs`**: `ExternalAnalysis` says "some parameters come from the host"; the interfaces say
  WHICH, so a host that finds one offers the matching control (picker, "detect beats") and writes the
  result back before `Run`. Matching by field name would make a rename a silent cross-assembly breakage;
  an interface makes it a compile error. The SDK implements none of the analysis (no decoder, FFT or
  image loader: it would drag a platform dependency into a library whose point is having none). The inputs
  are FIELDS with the interface implemented explicitly on top, since a property is invisible to the
  form builder (it reflects over public fields) and `Hints.Hidden` on it would mean nothing. The framerate
  input is the odd one out: a generator reads the framerate off `GeneratorContext.Settings`, never from
  that field - it exists so a FORM can show the number next to the target, and a host that never filled it
  must still produce a correct run.
- **`ILevelArchiveInput` carries BYTES**, not a store and not a path, because parameters are bound by a
  reflection-built form and SERIALIZED INTO A PRESET FILE: a store handle would be a live resource on disk
  as a field, a passphrase a password on disk. The passphrase is absent entirely: the host opens the
  archive, asks if it needs one, and hands plaintext. The format travels beside the bytes (a document is
  Json or Blob by the NAME it was stored under).

## The journal and the context

- **A journal, not a `Level` copy**, because a snapshot costs memory proportional to the LEVEL on every
  run, forever (near `LevelRules.MaxObjects` that is a multi-megabyte copy per click, and the undo stack
  holds many); a journal costs what CHANGED. The consequence every author must internalise: mutating the
  model outside `GeneratorContext` compiles, runs, and silently breaks undo, and this layer cannot detect it.
- `HasEdit` is keyed by scope AND id: a Prefab template owns its `ObjectId` namespace, so id 1 in a
  template and id 1 in the level differ. Id counters are NOT journaled: `ObjectIdCounter` only grows and
  ids of deleted objects are never reused, so rolling one back would hand out an id a stale reference
  still points at; `Reapply` re-inserts the SAME instance under the SAME id, so redo stays exact. Edits
  keep a whole before-copy of the object (a run may rewrite any part, tracks included); the after-copy is
  taken lazily on the first `Revert`. Level-global tracks (game, camera, post-processing, player events)
  are plain lists with no owner to snapshot, so they have their own change types; index is preserved on
  both sides, because these lists carry no enforced sort order and re-inserting at the end would reorder a
  track the author never touched. Their add/remove neither sorts nor deduplicates: `Frame` uniqueness is a
  per-generator decision (`mod_quantize_keyframes` alone has three answers to a collision).
- **`GeneratorContext`**: a generator mutates THROUGH it and nowhere else. `Scope` and `Counter` are a
  pair and neither is inferred from the other: a Prefab implements both `IObjectScope` and
  `IObjectIdCounter` on one class, but at level scope `Level.Game` is the scope and `Level.Settings` the
  counter (the SDK `CLAUDE.md`, "the split every consumer must get right"). `Create` deliberately does NOT
  set Span/Layer/ParentObjectId - only the generator knows whether it means "spread across the range" or
  "all on one frame" - and cannot use `LevelUtils.SetObjectId` (it takes `LevelSettings`, the wrong counter
  in a template).
  - **Grouping** lives in the context, not in each generator's parameters (every generator that creates
    anything wants it): every generator already parents to `context.Parent`, so a lazily created container
    behind it needs no generator changes. Lazy so a run that creates nothing (`gen_audio_waveform` with no
    peaks) leaves no empty container; `Estimate` adds its object only when the run produces objects.
  - **Layer splitting** is a pass over the journal AFTER `Generate` (so it also wins over a generator that
    writes `Layer` itself, and the layers are contiguous in creation order); a generator knows its own
    creation order and nothing else. It runs after, not inside `Create`, because "the Nth object of this
    run" exists only once the run is over; the container keeps Layer 0 (it is the parent).
  - **`Layer` is parent-relative**: writing the author's number onto a child under a non-zero parent would
    offset the whole run, so the parent chain is subtracted once (`LocalLayer`).
  - **`Replace<T>`** changes an object's type in place because `Edit<T>` refuses a mismatched subtype and
    `Delete` + `Create<T>` mints a fresh id, orphaning children, dropping selection and breaking operations
    holding it. `ObjectEdited` already keeps whole instances and assigns them back by id, so no journal
    entry of its own is needed.
  - A settings edit goes through a delegate because the context should not know which settings fields a
    future generator needs; a journal entry only needs "put it back the way it was".

## The shipped set

- **Audio file level**: the commonest way a level begins. The SDK cannot measure a duration (no decoder),
  so `DurationSeconds` is an `ExternalAnalysis` input. Zero is not an error: it falls back to a fixed length
  to start authoring in (an `.mp3` costs a frame-by-frame walk, so it is deliberately not read). The track
  is the song and the level is the song plus a tail: both start on the first frame, the track stops where
  the music does, and a span is a COUNT, so a track of `SongDuration` frames ends on frame `SongDuration`
  while the level's last frame is `FrameDuration`.
- **Audio waveform**: peaks come from the host; with none, it produces nothing, deliberately (invented
  numbers look like it worked). **Beat flash**: the first `LevelScope` and destructive generator; it clears
  THROUGH the context so undo restores what it removed (wiping a `List` directly compiles, runs and makes
  the run un-undoable).
- **`BaseLevelGenerator`**: a level with no screen limit plays at the device's aspect, so a level built from
  nothing offers a pin to 16:9 on its first frame (a KEYFRAME, not a runtime default, so it shows on the
  Events timeline and can be deleted). An import offers none: it keeps its source's limit.
- **Bullets**: homing is baked into position keys at author time (a level is data), so `Steps` is capped,
  and the cap is the generator's OWN number: the format ceiling (`LevelRules.MaxObjectKeys`) says how many
  keys a track may STORE, this says how many a bullet should COST (a burst multiplies it), and the two only
  ever coincided by accident. Rounding can land two steps on one frame, and `Frame` is unique within a
  track, compared in object-LOCAL space. Laser sweep is two objects because `ColliderId` is a static field
  and cannot go from harmless to lethal: the warning beam is a collider-less object that dies as the firing
  beam is born. Rain proves determinism (same seed, same rain, from `CreateRandom()`, never `System.Random`).
  Wave: an object's lifetime is `[spawn, spawn + travel]`, not the whole window.
- **Fractal** depth is capped at 6: growth is constant-factor (Koch is 3 * 4^depth, so depth 8 is 196 608
  objects, near `MaxObjects` from one click), so the cap keeps the estimate meaningful.
- **`BaseSpawnGenerator`**: the bookkeeping (mint, parent, bound to the window, template's texture, size,
  colour, collider) is what concrete generators share, leaving pure placement math.
  - Placement math is in DEGREES but the format STORES RADIANS (`AddRotation` is the one conversion; a
    hand-written `AngleKey` must convert or 45 means 45 radians, ~2578 degrees).
  - Frames come in ABSOLUTE and are stored LOCAL (the runtime reads `obj.Span.StartFrame + key.Frame`);
    storing an absolute frame is invisible: the object spawns and never moves. Every `Add*`/`SetSize`
    converts and the sweep test checks it.
  - Frames are clamped to the context's window, not the level's; `protected` so an animated generator
    applies the same clamp in `Estimate` (a one-frame bullet carries one position key, not two).
  - `CanSpawn`: a MOVING object that would start at or past the end of the window is not spawned (clamping
    the overflow onto the last frame left a pile of one-frame ghosts flashing after the pattern); `Estimate`
    must apply the same check.
  - `SpawnParameters` is a shared base so the object-template fields are not copy-pasted and renamed;
    reflection returns inherited public fields. `Hints` must still name inherited fields, so the two arrays
    are spliced into each generator's `Section` calls, split by section: WHAT is drawn, its paint and its
    collider are Main resources; size and tint tune the look (Additional).
- **`gen_level_archive`** is a LEVEL generator rather than a menu command: importing a level is "build a
  whole Level and LevelMeta from a few parameters", and the form, presets, estimate and labels come off
  the contract. The reverse (export) is a host service, not a generator (a generator produces content).
  It deserializes through the ordinary service, so `DeserializeEnvelope` migrates an old archive on the way
  in; a NEWER document is refused unwrapped so the host tells the player to update instead of reporting a
  broken archive. A fresh `LevelId` is minted by default (importing under the same id would point two
  entries at one folder); keeping it is the deliberate choice for restoring a level. Handed nothing it
  produces an empty level and says why.
- **`mod_content_remover`**: the one generator that only DELETES, and for which the window is an
  instruction rather than a boundary ("clean up everything the level can no longer play": whole level,
  Invert on; "wipe this section": the section, Invert off). `WindowSelection.Selects` is asymmetric on
  purpose: Invert deletes what shares NO frame with the window, its opposite only what lies WHOLLY inside,
  so an object hanging over the edge survives both modes (making both "overlaps" deletes it either way,
  which is a trap, not a mode); `mod_span_fit` selects through the same helper. Deleting is never
  per-object: a removed object may PARENT a staying one (a dangling `ParentObjectId`), so a survivor marks
  its whole ancestor chain, and a surviving placement keeps its materialized children (its `ObjectIds`
  table points at them). The two configurations that reach past what the author sees (Invert, or a window
  over the whole timeline) get a confirmation, not a refusal. Inside Prefab Mode the window is bounded by
  `Prefab.Root`'s span.
- **`mod_framerate_remap`**: changing `Framerate` alone reinterprets every frame number, so the numbers are
  remapped (`frame * to / from`); `FrameDuration` unconditionally, as it is the same statement. The three
  switches are about REACH (lowering framerate for a device usually wants objects moved but not keys lost
  from audio automation or camera events); a switch left off keeps old numbers and shifts timing, a
  visible outcome. Shrinking is lossy: distinct frames can collide and `Frame` must stay unique, so
  `MaxKeyShift` is the whole policy (0 drops on the first collision; the default 1 keeps a 2:1 downsample
  lossless for keys at least two frames apart; forward first, because keys are packed ascending and the
  slots below the ideal one are the taken ones). Level-global tracks re-add survivors as COPIES (the
  journal's removal entries hold the originals, and moving one would restore it at the new frame). **A frame
  is not a count here either**: scaling a frame directly scales the origin too, so what is scaled is the
  DISTANCE from the first frame, then the origin is added back (an exclusive end boundary takes the same
  path). A span is remapped by both edges and rebuilt, so rounding happens once per edge and no object
  drifts against its own keyframes; the end is clamped one above the start (exclusive).
- **`mod_prefab_flatten`**: the mass form of one placement's Flatten (`PrefabFlattenUtils` carries why it is
  bookkeeping removal and why it takes a whole chain); nothing materializes, resyncs or moves. **Order is
  load-bearing with templates included**: the active scope first, templates second, because a template
  edit propagates to every placement of it. Flattening is never dangerous (no content destroyed, one undo
  restores every link); removing templates is. Unused templates are swept to a FIXED POINT: a template is
  kept by any surviving reference, including one from another template, so one pass would keep what an
  already-removed template named; it also makes the result independent of dictionary order, and terminates
  because every round removes one or stops. Both switches need the WHOLE level, so `Game == null` (Prefab
  Mode) declines.
- **`mod_quantize_keyframes`**: everything goes through `context.Edit(id)`, snapshotting each object once,
  so quantizing forty objects is one undo step. Frames must stay unique and snapping is exactly what
  collides two keys, so the collision policy is the feature: keep the first, leave the second where it
  was. A key's `Frame` is LOCAL but the beat grid belongs to the LEVEL's timeline, so the work is done in
  global frames and converted back on write (else two objects starting a beat and a half apart land on two
  grids). The grid is anchored on the first FRAME (`MinFrame`), not on zero, so line zero is the frame the
  level starts on.
- **`mod_span_fit`**: a child outside its parent is legal authored data resolved on READ by the consumer, so
  fitting is a generator the author asks for rather than a validation rule that follows them around (it
  replaced `GraphRule.ChildSpanOutsideParent` and its repair, now `ClampChildren`). The window is an
  instruction as in `mod_content_remover`. **The walk direction is not a detail**: clamping runs
  PARENT-FIRST (cutting a parent moves the boundary its children fit into), expanding runs CHILD-FIRST (a
  stretched parent is what its own parent must cover); reversing either leaves the far end of a chain
  outside after a run that reported success. Only children whose parent resolves are fitted (a root is
  bounded by nothing; a dangling `ParentObjectId` is `GraphRule.MissingParent`'s business), sorted by depth
  with the id as tie-breaker (`List.Sort` is unstable). A materialized prefab child is clamped into its
  parent instead of deleted while its placement survives (a broken remap table). The configurations that
  reach past the window (Invert, whole timeline, Delete) get a confirmation.
- **`mod_span_stagger`** turns a wall into a wave and pairs with every geometry generator. Shifting BOUNDS
  delays when an object exists, shifting KEYFRAMES delays what it does; both are optional and separately
  useful. `ObjectTracks` exists because every track is a `List<T>` of unrelated element types: generic work
  on "the object's tracks" otherwise needs a one-branch-per-track switch in every modifier. `IList<IFrame>`
  is unavailable (`List<PosKey>` is not one, and copying each track would defeat mutating in place).
- **`ShapeObjectsGenerator`** (key still `gen_texture_objects`: a PERSISTED id resolved by
  `GeneratorRegistry`, listed in the README and keyed in tests; the TextureObject-to-ShapeObject rename left
  it alone because renaming silently breaks every saved reference. Do not "fix" it). One object per pixel is
  unusable (512x512 is exactly `MaxObjects`), so the input is downsampled and run-length merging collapses
  flat areas into wide rects. The downsample is a box average in PREMULTIPLIED alpha (otherwise a
  transparent pixel's arbitrary colour bleeds into its neighbours).
- **`gen_capacity_hint`** and **`gen_font_cache`** refresh advisory `LevelHints` the editor already
  refreshes on every save; as generators they let an author SEE the number on demand (after importing a
  foreign level, editing by hand, or suspecting drift). The capacity one writes `Hints.Limits` directly
  (no journal entry shape for a bare field; undoing it leaves the refreshed hint, harmless because it is
  advisory and recomputed on save), so its estimate is permanently zero and it declares itself exempt from
  the host's zero-estimate refusal. The font one IS fully undoable (a keyed dictionary, covered by
  `AddResource`/`RemoveResource`): an existing entry is REMOVED before the new one is added, because
  `ResourceAdded.Revert` removes the key it added and overwriting would make undo delete an entry that
  existed before the run. It reports its estimate as Resources (entries written and dropped), honest and
  non-zero whenever there is anything to do.

## The folder at a glance (moved from `Generators/CLAUDE.md`)

The folder file keeps the traps; this is its former text, whole.

Authoring automation: a generator produces level content from a few parameters.
  Non-generic `IGenerator` root (so `GeneratorRegistry`'s reflection scan and a reflection-built
  form are possible) split into `ILevelGenerator` (builds a whole `Level`+`LevelMeta`) and
  `IScopeGenerator` (`Content`/`Modifier`, writes into an existing scope), with
  `BaseLevelGenerator<T>`/`BaseContentGenerator<T>`/`BaseModifier<T>` as the bases anyone actually
  derives from. **All mutation goes through `GeneratorContext`**, which journals it into a
  `GeneratorChangeLog` - that journal *is* undo, and writing to the model directly silently breaks
  it. **`Replace<T>` is the third write primitive beside `Create`/`Edit`/`Delete`**, and it exists
  because neither of the others can change an object's TYPE: `Edit<T>` refuses a mismatched subtype
  by contract, and `Delete` + `Create<T>` mints a fresh `ObjectId`, which orphans every child pointing
  at the old one. The id is exactly what has to survive, so the object is replaced under it - and
  `ObjectEdited` already keeps whole instances and assigns them back by id, so no journal entry of its
  own was needed. Its overload takes an `IObjectScope`, since a run may legitimately reach outside its
  own (`Level.Resources.Prefabs` holds templates that are scopes in their own right); that is also why
  `GeneratorChangeLog.HasEdit` is keyed by scope AND id rather than by id - a `Prefab` owns its
  `ObjectId` namespace, so id 1 in a template and id 1 in the level are different objects.
  The context also owns **grouping**: given a `groupName`, `context.Parent` lazily creates one
  container `RectObject` (Layer 0 - Layer is parent-relative, the children already carry
  `context.Layer`) and returns it, so every generator that parents to `context.Parent` gets
  "wrap this run in one object" for free, including future ones; `BaseScopeGenerator.Estimate` adds
  the container's object, and a run that creates nothing creates no container either. `Layer` is the
  author's **effective** number while `LocalLayer` is what an object actually stores (`Layer` minus
  the parent chain's sum) - Layer being parent-relative, writing the raw number under a non-zero
  parent offsets the whole run. **Layer
  splitting** is the same shape: `ApplyLayerSplit` runs over the journal after `Generate`
  (`BaseScopeGenerator.Run`), giving each created object its own `Layer` stepping up from
  `context.Layer` - after, so it also wins over a generator that writes `Layer` itself, and skipping
  the container for the same parent-relative reason. Also: `GeneratorHints` (form order/**sections**/ranges/units/visibility, since parameter
  classes carry no attributes - `Section(key, fields)` is `Order` plus a header, so grouping can't
  disagree with ordering; `GeneratorSections.Main`/`Additional` is the shipped vocabulary, and a
  parameters class must never shadow an inherited field, since everything here is keyed by field
  NAME), `GeneratorCost`/`GeneratorRequirements`, `GeneratorRandom` (deterministic xorshift32 -
  `System.Random`'s sequence isn't contractually stable and `UnityEngine.Random` isn't reachable).
  Subfolders: `Spawn/` (`SpawnParameters` + `BaseSpawnGenerator<T>` - the shared object template and
  the mint/parent/frame bookkeeping, so a concrete generator is only placement math; its
  `MainFields`/`AdditionalFields` are spliced into each generator's own `Section` calls), `External/`
  (the `IAudioFileInput`/`IWaveformInput`/`IBeatFramesInput`/`IPixelTextureInput` interfaces a
  generator implements to say "this parameter comes from the host" - matched by interface, not by
  field name, so a rename is a compile error), `Modifiers/` (`ObjectTrackMask`/`ObjectTracks` -
  generic enumeration of every keyframe track an object has, plus the modifiers themselves),
  `Import/` (`LevelArchiveGenerator`, `gen_level_archive` - importing this project's OWN package),
  `Geometry/`, `Bullets/`, `Audio/`, `Textures/`, `Utility/` (the concrete generators - 25 of them in all,
  the roster the design document calls complete plus `mod_content_remover`/`mod_framerate_remap`,
  `mod_span_fit`, which fits every child's lifetime to its parent's and is what replaced the removed
  `GraphRule.ChildSpanOutsideParent` and its auto-repair, and `mod_prefab_flatten`, which turns every
  prefab placement in a scope back into an ordinary object - see the consuming project's
  `Docs/Architecture/LEVEL_OPERATIONS.md`, "Flattening a placement", for when a flatten takes a whole chain, and note two things
  about the generator itself: its two switches need the WHOLE level and therefore decline in Prefab
  Mode, and its run order is level-then-templates, because a template edit propagates to placements
  and the other order leaves the level describing content its templates no longer hold).
  Three rules a spawning generator must get right:
  **a keyframe's `Frame` is LOCAL to its owning object** (the runtime reads `obj.Span.StartFrame + Frame`,
  so an absolute frame yields objects that spawn correctly and then never move - `BaseSpawnGenerator`'s
  `Add*` helpers convert, `mod_quantize_keyframes` converts the other way to snap against the level's
  own grid, and a sweep test pins it),
  **placement math is in degrees but rotation is STORED IN RADIANS**
  (`AddRotation` converts; a raw 45 becomes 45 radians ≈ 2578°, and the Unity project converts back
  to degrees only at its inspector boundary), **a staggered generator must not spawn past its
  window** (`CanSpawn` - clamping the overflow onto the last frame leaves one-frame ghosts), and
  **a lifetime clamped to one frame gets one key per track**, not two - see
  `BaseSpawnGenerator.CanAnimate`, and note that `Estimate` has to apply the same clamp. A third,
  format-wide: **`FrameDuration` is a count** and the timeline counts frames from
  `FrameRules.MinFrame`, so the last legal frame IS `FrameDuration` - spell it
  `FrameRules.LastFrameOf`, never by hand. Validation will not catch a generator that misses it:
  `RuleLevelFrame` bounds a frame on the left only, so content written past the end is legal data
  the author simply never sees.
  `GeneratorSchema` writes a generator's parameters (fields in `Hints.Order`, hints as `x-` extras) as JSON schema for the MCP bridge and reads a caller's values back with range clamps; `Visible` is stated for the defaults only, `HiddenFields` answers for given values.
  The folder has its own `README.md`, which is the design record: the spec it was written from is no
  longer present in the consuming project.
