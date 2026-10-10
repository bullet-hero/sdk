# Rules: why each limit and each attribute is shaped the way it is

Contributor reference for `Rules/` (constants and `[RuleXxx]` attributes) and the rule engine in
`Validations/`. Constants name their own values; this file holds the reasoning that the code
comments used to carry. What a player or an author needs to know is in `DocsPublic/`.

## Attribute contracts

- **Two base contracts.** `BasePropertyRuleAttribute` sees ONE value plus a `RuleContext`;
  `BaseObjectRuleAttribute` sees the whole object and is declared on the class, because a property
  rule structurally cannot compare siblings (Min/Max pairs). An object rule runs once per visited
  object before its properties, so a finding lands on the object a `Fix` has to write into. Anything
  needing more than one object (id uniqueness, dangling references, cycles) is a graph invariant
  (`Validations/Graph`), never a rule.
- **Rules see the world only through `RuleContext`** (immutable; entering a nested scope makes a new
  one, so a `RuleIssue` replays its fix against the exact context its rule saw). Validating a
  `Prefab`, `LevelMeta` or `UserSettings` standalone works because no rule assumes `context is
  Level`. A standalone template matches on the concrete `Prefab` type (its timeline is its root's
  span), not on an interface.
- **The rule name is abstract, not a default `GetType().Name`**, so a new rule cannot reach a UI as
  an unnamed issue. Same contract as `LevelEditorOperation.OperationNameKey` in the game.
- **`RuleOptional`** is the counterpart of `RuleNotNull`: every property rule reports null (the
  safety net for a forgotten `RuleNotNull`), so a member that is deliberately born null needs the
  permission written down. It suppresses only the null case. See `NAMING.md` (game `Docs/`) for when
  a member should be nullable at all: a third state, or a default block big enough that not writing
  it pays.
- **`RuleEnumValid`** covers single-choice enum FIELDS (`"ease": 200` otherwise surfaces much later
  as a switch falling through). It does NOT cover discriminator enums (`ObjectType`, `ColorType`,
  `VectorType`, `ScreenLimitType`, `LicenseType`, `Color4X4KeyType`, the `EffectShape*` family):
  those are the first element of a two-element JSON array and a converter throws on an unknown one.
  An unknown discriminator means the file cannot be read; an unknown field means one value is wrong.
  `[Flags]` enums go to `RuleEnumFlagsValid`, which masks unknown bits off instead of resetting to a
  default, since a combination from a newer file is mostly meaningful.
- **`RuleFiniteNumber`** closes the gap one-sided bounds leave: NaN sorts below every number, so
  `RuleMaxValue` accepts it and `+Infinity` satisfies `RuleMinValue`. `RuleInRange` rejects both for
  free. One NaN position poisons every derived transform, bound and collision for the frame. Prefer a
  two-sided range where one exists.
- **`RuleLevelFrame`** bounds the LEFT edge only (`FrameRules.MinFrame`): the level's end is a number
  the author drags, so a key parked past it is content waiting for the level to grow. It is a Warning
  because the only repair is a clamp onto the first frame, which loses what the author meant. It
  needs nothing from `RuleContext`, hence it is not under `Contextual/`.
- **`RulePropertyOrder`** is the one attribute for every "these two fields are a range" invariant
  (Min/Max of the MinMax value types, per-component pairs of every Rect, `ScreenLimitBounds` aspect
  pair, camera size pair). `AllowMultiple`: `Color4MinMax` has four pairs.
- **`RuleIVector2Ordered`** guards `IVector2` properties that are ranges (`LifetimeBounds`, every
  `*CurvesBySpeed.SpeedRange`, the two limit pairs of `ShadowsMidtonesHighlightsKey`). The random
  variants are checked strictly: the rule holds only if EVERY rollable value is ordered (highest X
  against lowest Y), so an overlapping rect fails. A `RandomLerp` segment is checked exactly by its
  two ends, because `x - y` is linear in the one shared roll; an ordered segment passes even when the
  axis spans overlap.
- **`RuleObjectIdValid`** accepts user-space ids (`ObjectId.IsValid()`); two of the three reserved
  negative ids are parent targets only (an object may attach to the camera, never BE it). The one
  exception is `PrefabRoot`: `Prefab.Root`'s identity IS `ObjectId.PrefabRoot`. What stops an inner
  object claiming it is `Prefab.Objects`' `RuleDictionaryKeyMatches` (nothing mints -3 as a key). With
  no scope resolved, a reserved id is accepted, as `RuleParentObjectIdValid` does.
  `RuleParentObjectIdValid` additionally allows Null (level space); `PrefabRoot` is legal only inside
  a template, while Camera and LocalPlayer are unreachable from a template's inner object.
- **`RuleReferenceExists`** checks only the int-backed id family (`TypedResourceId` and wrappers):
  negative = defined by this level, positive = the game's registries, which the SDK cannot see.
  Guid-backed ids (`ShapeId`, `ThemeId`, `EffectId`, `PrefabId`) would report every game-defined
  preset as dangling, so they belong to the graph pass where a consumer supplies its registry. With
  no level in context the rule stands down. It is a Warning with no `Fix`: every repair invents
  content (another resource shows the wrong asset; Null hides the object or changes what Null means).
  `RuleModificationKeyValid` declines a fix for the same reason (dropping the entry belongs to the
  dictionary's owner).
- **`RuleModificationKeyValid`**: `ModificationKey` is a struct dictionary key, so it is in the
  analyzer's blind spot twice (keys are not walked, a struct is never a `[RuleContainer]`). It asks
  whether the field id is registered in `ModificationTable` and whether an index is legal on it. It
  does NOT check the field exists on the addressed object's type (needs placement, `PrefabId` and
  template: the graph pass). Addressable objects: an ordinary user-space id (any field of the
  materialized copy) or `ObjectId.PrefabRoot` (only `ModificationFields.IsPrefabRootField`). `Span`,
  `Active`, `Layer` and `PlacementOffset` are the placement's own authored fields and nothing copies
  them off the root, so an override there would be a second way to say the same thing that also wins
  (`ApplyModifications` runs last). `PlacementDuration` is the exception: the duration IS copied off
  the root, so an override is the only way a placement keeps its own length (a derived property, not a
  stored member). Camera and avatar ids are never addressable.
- **`RulePrefabRootFixed`** is a class rule because the pinned fields live on `RectObject`, shared by
  every object. A placement owns `Active` (gates rendering and collision down the hierarchy) and
  `Layer` (summed up the chain), so a root that set either would shift every placement; `PrefabRootUtils`
  never copies them, so a value there reaches nothing. The identity is pinned here because
  `RuleObjectIdValid` sees one value and no path. `Root.Span` is half pinned: duration is the
  template's whole timeline (the author's one number); start is `FrameRules.MinFrame`; anchors are
  none (the root has no parent in the template). `ApplyRoot` copies the duration and nothing else.
- **`RuleResourceMetaId`**: a record fills exactly the id slot its `ResourceType` uses - a negative
  user-range int for a file resource (texture, font, audio, bytes, text), a guid for a data resource
  (theme, effect, shape, prefab) - and neither for the level cover (`ResourceType.LevelLogo`, one per
  level). The invariant lives on the object because both slots and the type are visible there.
- **`RuleShapeGeometry`** is a class rule because an index means nothing without its vertex list.
  Every check has a silent failure mode: a point outside the box samples past its atlas cell, a
  flipped triangle is culled, a degenerate one collides with nothing, an unreferenced point is
  invisible, an index past the end throws inside a bake. `Fix` is `ShapeGeometryUtils.Sanitize`, the
  same call the in-game shape editor makes on Save, so editor-accepted equals validation-accepted.
- **`RuleAnyDeviceActive`**: with every control device inactive nothing can move the avatar and
  nothing in the game says why. The UI refuses to clear the last checkbox and settings load falls
  back to platform defaults, but a hand-edited file needs this. A class rule: it spans four groups.
- **Collection rules.** `RuleCollectionMinCount` never pads with null (an entry that exists but is
  null is worse than a missing one; the analyzer walks past nulls); if the element type cannot be
  built the fix declines. `RuleCollectionNoNullItems` exists because a null element is invisible to
  the analyzer and `RuleCollectionCount`'s fix pads with nulls; it matters most for `ThemeData.Matrix`
  (64 index-addressed slots). Its fix REMOVES (a fresh default would invent content and shift every
  later slot); the paired count rule restores the length. `RuleCollectionSorted` is Advice: unsorted
  tracks are legal (frames must be unique, `LevelStateBuilder` sorts at load), but sorted files read
  and diff better.
- **`RuleDictionaryKeyMatches`**: every id-keyed dictionary stores its id twice (key and value).
  `DictionaryAsListConverter` hides that on disk, but code can re-id an object without re-keying. It
  sits on the dictionary property because a key is not a property and has no `RulePath`. The fix
  re-keys (the value's id is the authored intent), by rebuilding the dictionary; entries colliding
  after re-keying collapse into one.
- **`RuleShortcutBindings`** checks SHAPE only (plausible id, legal binding in canonical form); the
  catalog and the engine's key table live in consumers. Canonical form is required because downstream
  equality is string equality (`Shift+Ctrl+P` would resolve and still not match `ctrl+shift+p`).
  `Fix` drops the entry (a dropped override falls back to the default). `allowModifierOnly` is true
  because only the catalog knows a held shortcut from a pressed one.
- **`RuleStringMax`** counts code units, so a cut can split a surrogate pair; `SurrogateUtils`
  returns the shorter answer. It is a Warning because the repair destroys text either way.
  **`RuleStringPattern`** is for strings that are keys (language tag, URI scheme): a malformed one
  never matches and degrades silently. The regex compiles once per attribute instance and the
  analyzer caches instances.
- **`RuleIPrimitiveIntNotNull`**: "is set" for any primitive-int struct (value is not the reserved
  `NullValue`, always 0), for properties that may reference either the game or the user range.

## Limits (`Rules/*.cs`)

### Frames

- **The timeline counts from one**; the three kinds of number and `NoFrame` are in the root
  `CLAUDE.md` ("Frames, spans and the timeline"). `NoFrame` is below `MinFrame`, so it never collides
  with a real frame and is what a default-initialised int holds. It is not an index sentinel.
- `FrameRules.MaxFrameDuration` is chosen from what a level can plausibly be, not `int.MaxValue`:
  about 4.6 hours at 60 fps, about 17 minutes at the 1000 fps ceiling. Without it a hostile file
  claims a two-billion-frame timeline.
- `PlacementOffset` has an outer cap only; the real bound is per placement and cannot be a property
  rule: it may not exceed `Span.StartFrame - MinFrame`, or a materialized span start falls below the
  first frame, where the `FrameSpan` constructor clamps silently and the phase is lost.
- The time-to-frame epsilon absorbs float32 round trip error of `ToTime` (18.999998f instead of 19f
  would floor a frame early). It is tiny against 1/framerate.
- `LastFrameOf`/`CountOf`/`EndBoundaryOf` exist so nobody spells the origin inline again ("last frame
  of N" was `N - 1` in about twenty places). Level progress divides by the span between the first and
  last reachable frames, not the length: the playhead never reaches the end boundary, so
  `elapsed / frameCount` topped out at (N-1)/N and a survivor was told 99.9%.

### Level and prefab

- **`MaxObjects`** is capped because a total count is what a LOADER pays (every object is
  deserialized, id-mapped and parent-linked); 2^18 is far above any authored level and far below what
  exhausts a phone.
- **`MaxObjectIds` is not `MaxObjects`.** Objects is how many may EXIST; ids is how many have ever
  been MINTED, and ids are never reused (`LevelSettings.ObjectIdCounter`). A generator run, a paste, a
  prefab materialize and an undone creation all consume permanently, so the two diverge with editing
  time. Binding the counter to the collection cap made a long-lived level fail validation while
  holding almost nothing, destructively: `RuleInRange` is an Error whose `Fix` clamps, so the repair
  wrote the counter down and the next object took an id already live. The ceiling is the `int`'s
  (2.1 billion creates); at the wall minting refuses (never a wrap, never a negative: those are
  game-space objects and the three reserved parents), and the compaction generator is designed in
  `Docs/Issues/LEVEL_MODEL_ANALYSIS.md` D2. The warning threshold is three quarters of the range, so
  the author is told long before the refusal. The remaining-ids helper returns `long` because the
  `int` spelling overflows exactly at the end of the range it describes and would report an exhausted
  counter as having room. A template's counter has the same meaning, and Prefab Mode spends it fastest
  (every create/undo/branch inside it consumes from the template's own counter).
- Track keyframe caps: the theme track is 512 rather than 128 because it is animated and
  interpolated, so a colour change costs a PAIR of keyframes (a real imported level needed 146). The
  general per-track cap is 32-to-larger because 1 948 authored tracks across five community levels
  exceeded the old 32 (worst: 189 keyframes over 123 s, truncation froze the object for 100 of them).
  Every consumer is a validation clamp or import truncation; nothing sizes a buffer by it (a blob
  count is always `Int32`). It matches the camera and player tracks. Raising a cap invalidates nothing
  authored.
- **`MaxPrefabs`** was raised from 64 after a workshop level of 72 templates lost eight of them and
  63 placements (524 of 3 690 materialized objects). Cost is proportional to template content;
  nothing sizes a buffer by it.
- **Parent depth** is the player's `LevelPlayerSettings.MaxChildInherit` (16, a fixed `stackalloc`)
  MINUS ONE: the editor parents its overlays (selection outline segments, gizmo handles) one level
  under the selected object, so a level authored at the runtime cap would render correctly and lose
  its selection border. Past 16 the player composes against a mid-chain ancestor. Cycles are a graph
  invariant. Prefab nesting depth is a property of the FORMAT (no consumer can materialize deeper);
  `ResourceSettings.Prefabs_MaxInheritanceLevel` defaults from the constant.
- Beat segments: the tempo range covers any real song with room; the offset is bounded by the
  timeline, not by one beat, since a legal phase depends on `Bpm` and a property rule sees one
  property (`BeatMath` normalizes it). The BeatMath collection cap guards the whole-level form: the
  grid is computed, never stored, and a fast tempo over a long segment is millions of beats.
- `LevelHints.Limits` bounds are a sanity clamp so a hostile file cannot make a device preallocate
  gigabytes; the hint is advisory and the real ceiling is per device at runtime.
- **Seeds.** 0 is `NullSeed`, the absence of a seed, shared by all three resolution tiers; it is
  valid authored data (an unpinned level), so `IsValidSeed` is a predicate, not a rule attribute.
  `[MinSeed, MaxValidSeed]` is what the FIELD accepts, `NullSeed` included; `[MinValidSeed,
  MaxValidSeed]` is what a REAL seed lives in and what every generator must draw from, or it would
  occasionally produce a run nobody can reproduce.

### Values

- `MaxSize` equals the position range: a size is in the same world units. The old +-100 was a tenth
  of that, and imported levels carry sizes to 820 (5% of their objects broke the bound). Derived, so
  a size cannot outgrow what a coordinate can address.
- Rotation (radians) is capped in turns: a spinner is one keyframe pair whose end angle keeps
  growing; 1000 turns is about 8 minutes at 2 rev/s.
- **Shapes.** `MaxShapeTriangles` is 128: an inverted 32-sided ring is 94 triangles (rim, outer rim,
  inner disc) and six more built-in shapes sat at exactly the old 64. Vertices are capped apart from
  triangles because indexed geometry shares corners (64 triangles need 192 unwelded, about a third
  welded); the cap bounds the worst case. A shape needs at least one triangle (an empty shape draws
  and collides with nothing, worse than a Null `ShapeId`). Shape points live in the object's own
  rect: UV is read from `positionOS.xy + 0.5`, collision would extend past what is drawn, both
  silently. The built-in library is centred on its bounding box (the circumcentre put 31 of 78 presets
  at 0.577), each form's longer axis still measuring 1.
- **Collection minimums.** A gradient needs one stop (a constant colour, which every consumer
  evaluates without a special case); a curve needs two keys.
- The text length is the fixed slot length of the player's per-frame text buffers, a power of two so
  slot addressing is two shifts; raising it costs (slot x text capacity x 2) bytes twice over,
  lowering truncates existing levels.
- `StatisticsRules` histogram: a fixed bucket count so levels of different length compare (a bucket
  is a percentage); 64 puts a three-minute level near 2.8 s per bucket. The profile cap is above
  anything a player produces by hand; eviction is oldest-first by `TimeUtc`.
- `ResourceRules`: `Resource.MaxSourcesCount` (fallback URIs for ONE asset, each a load attempt the
  player waits through) and `MaxMetaSources` (attribution entries, never fetched) are different
  limits. Hash and permission lists are plural because one resource can be several files (per-platform
  re-encodes) and need several rights holders. Per-category dictionary caps bound load time.
- `TextRules`: an `AutoFontSizeKey`'s max is the size drawn while the text fits (matches
  `FontSize_Fallback`), the min is a floor where 0 means none. The font character cap bounds a builder
  hint, not a reader (a consumer ignoring it still renders everything); 512 covers Latin, Cyrillic
  and Greek, a CJK level warms a prefix. The two track fallbacks keep every text written before the
  tracks existed looking identical (empty = effect off = fully written, nothing hidden); inverting
  either makes old text vanish. The censor character set is bounded by a fixed per-text slot and the
  glyph-atlas warm-up of every member.
- `AudioRules`: the track speed range equals `FrameRules.MinSpeed/MaxSpeed` but is a different
  thing; they multiply, so pitch reaches +-4 (Unity's +-3 is an inspector slider, not an API bound).
  The discontinuity threshold floor is one audio buffer (below it, `dspTime` quantization alone would
  trigger re-seeks, audible as stutter). The rate-correction ceiling is about 0.85 of a semitone; 0
  leaves hard resync as the only correction. Volume and StereoPan bounds cannot be `[RuleInRange]`
  because both live in the shared `FloatKey`; the editor clamps against the constants.
- `EffectRules`: effect lifetime frames count from the emitter's start, not the level timeline.
  The simulation-step bounds bound a DEVICE setting (`EffectsGraphicsSettings`): one per effect (how a
  replay LOOKS; a replay rebuilds from empty, each step is a spawn cohort) and one per frame across
  the pool (worst-frame COST). The particle maximum is the VFX graph `capacity` in BOTH
  `UniversalVFX_Local/World` assets: the graph clamps silently above it. It was 32 768 and the pairing
  was the cost, since per-frame passes dispatch over capacity, not live count; the heaviest corpus
  effect asks 500, medians are tens; both moved to 1024 (measurement in `GamePlayer`'s CLAUDE.md).
  `ParticleCount` is a spawn RATE, `capacity` a population; they are equal by decision, not
  arithmetic. Lowering a bound never corrupts a level (`RuleInRange` clamps). Lifetime is bounded
  because emitter cost is about rate x lifetime and a one-shot burst scales with it. The ellipse
  ratio defaults to a circle so files written before it existed do not read back as ellipses. An
  effect's world/local mode is not authored (always local); the constant is the host's initial state.
- `PostProcessingRules`: the bloom maximum was raised from 10 (39% of one community level's bloom
  keys sat above it, up to 50, clamped to a fifth); it feeds URP's unbounded `Bloom.intensity` and
  nothing divides by it. The ColorCurves range is one for all eight curves because URP samples each
  from a 128x1 texture over 0..1; the YRGB curves are neutral at IDENTITY, the hue/saturation ones at
  a flat `CurveNeutral`.
- `ControlsRules`: angles and travel are TILT/SCREEN quantities (degrees, pixels); sizes, offsets and
  pad areas are NORMALIZED (fraction of screen height or camera rect) so a thumb control lands in the
  same place on 4:3 and 21:9. Nothing clamps at runtime: the limits bound the UI and the report.
  `KeybindingsRules` likewise: an unrecognised override resolves to the catalog default.
- `PrefabRules` max timeline: the template's length is a `FrameSpan` duration; nothing validates
  against it, it is the bound the editor's length field clamps to.

### Layers and depth bands

- `LayerCoefficient` puts whole layers exactly 1.0 apart, so two objects on one layer are coplanar:
  the opaque path shades overlaps twice (early-Z rejects only what differs; `ZTest LEqual` gives the
  pixel to whichever drew last) and the transparent path ties its depth sort on an index that moves
  between frames. The per-object tie-break offset times its count must stay strictly below 1.0 or an
  object bleeds into the next layer; 512 leaves half a layer spare. It is a different concern from
  `MinLayerDelta` (spaces the editor's own overlay pieces); do not merge them. Inframe objects stack
  themselves.
- **The player line**: the avatar owns no whole layer and renders inside the (0, 1) band, so the
  default layer an object is created on draws BEHIND it. Putting something in front is an explicit
  act (type 1). Only `AvatarInitData` and `InframeCursorService` draw inside the band; anything that
  must know the line cites the two constants, not 0 and 1.
- **Several avatars share the band** (reserved for multiplayer, nothing reads it yet): each gets one
  slot of width 1 / (avatars present), not 1 / `MaxAvatars`, so one avatar keeps the whole band the
  `AvatarInitData` numbers are written against. 128 and not 100: 1/128 is exact in binary floating
  point, and the camera is orthographic over near -2000 / far 2000 (4000 units linear depth, about
  2.4e-4 per 24-bit step), so a slot of 0.0078 is some 32 steps; around a thousand avatars slots fall
  to about 4 steps and z-fight, hence an eightfold margin. Inside a slot the parts of one avatar
  overlap in a fixed draw order (as the shipping 0.0001 offsets in `AvatarSettings` already do); budget
  about 32 planes per avatar (about 14 used), a 1/4096 step at the cap. The cursor keeps the bottom
  tenth of its OWN owner's slot.
- **Editor bands**, bottom to top: content, the grid (backdrop for content, but under the handles a
  gizmo is dragged across), collider fills, bot diagnostics, selection, gizmos. The selection band
  grows upward from `MinLayerSelection` by `MinLayerDelta` per line (about 25k lines before it could
  meet the gizmos at 1500). Collider fills and bot grid cells take one layer per drawn item because
  overlapping translucent fills at an equal z pick their own draw order and flicker (10k slots at
  `MinLayerDelta`). The grid size is a user preference with a floor only: the overlay stops drawing
  once cells are indistinguishable.
- A player size modifier is a MULTIPLIER of the size the player already is (neutral 1, see
  `PlayerEvents.Sizes`), with no maximum: a player scaled to nothing is still there and hittable.

## Avatar balance (`AvatarRules`)

Frozen because it used to be serialized fields with a ScriptableObject overriding them and the two
copies drifted: the asset played `dashTime` 0.15 against 0.2 in code, `dashCooldown` 0.25 against 0.5,
`damageTime` 0.2 against 0.3, `collisionScale` 0.4 against 0.5, `knockoutSpeed` 50 against 2;
`sizeSpeedInfluence` was added after the asset was last written, so it rested on a field initializer.
The values are the ones MEASURED OUT OF THE ASSET, because that is what players played. They may not
move: `Docs/Bots/README.md` promises a bot has the same speed, dash, hitbox and damage as a player,
and the PlayMode corpus compares runs across sessions - a run that moved because a field was dragged
is indistinguishable from a worse bot. Levels are authored against them too (a dash covers
`DashSpeed * DashTime` = 7.5 world units). `AvatarRulesTests` spells every value out a second time.
`Docs/Issues/MOVEMENT_HISTORY.md` is the tuning record.

- **Speed**: one and a half default camera heights per second (`DefaultZoom` is the full height), so
  the avatar crosses the screen in two thirds of a second. It was exactly one screen a second and a
  cursor chasing at that speed felt sluggish. Dash and knockback speeds are 3.3 times it.
- **Dash**: the reach (three quarters of the screen height in 0.15 s) is the number levels are
  authored against; changing either factor changes it, changing both to keep the product changes how
  long the avatar is uncontrollable. A dash aimed nearer than its reach is the same dash scaled down
  (`AvatarMovement.DashFraction`); the minimum fraction is `ArrivedDistance` of reach, effectively no
  floor (a body-sized floor read as a dash button that silently did nothing). It is a fraction because
  the reach scales with the level's Player Size and Speed tracks. Cost: a cursor resting beside the
  avatar can dash on alternate frames (a trail, a sound, a statistic each); the only limiter is the
  observed touchable frame (`AvatarMovement.ExposedSinceDash`), and releasing the mouse stops the
  avatar on its own position by default (`CursorReturn`, per device; touch walks on).
- **Dash cooldown equals the i-frames duration**, measured from the launch. The vulnerability window is
  exactly ONE SAMPLED FRAME at any frame rate. The order across three files is the guarantee:
  `BaseAvatarService.DriveAvatar` launches the dash BEFORE `AvatarController.UpdateAvatar`,
  `UpdateAvatar` calls `AvatarMovement.Observe` after the step, `GameAvatarService` sizes the collider
  afterwards. On the frame the i-frames lapse the dash is refused (`ExposedSinceDash` still false),
  `Observe` sets it, the collider is sized REAL, and the dash is taken on the next frame. Launching
  after `Observe` makes the window zero. The trade: how often a dash returns depends on frame rate (it
  costs the player, never pays them). It must not become `DashTime`: the i-frames have to outlast the
  dash for dashing through a solid to work (`IFrames_OutlastTheDash`), and a shorter gap makes the dash
  tail touchable while travelling at `DashSpeed` (tunnelling).
- **I-frames** are a rule of the game (levels cross solid obstacles by dashing), its own number rather
  than `DashTime` ("how far" and "how long safe" are two feel decisions); longer than the dash is a
  landing grace; 0 is the global off switch. They may not exceed the cooldown: then the next dash is
  available while the last still protects, no frame is ever touchable, and that is literal immunity
  (`AvatarRulesTests` forbids it).
- **Knockback**: speed equals the dash's, over `DamageTime` it travels 7.5 units (one full dash). It
  was 10 units at 1.0.0 (threw the player too far to recover) and 30 (6 units) read as a nudge, so the
  distance came down through `DamageTime`, keeping the snap. `DamageTime` sells the hit (control is
  gone for that window); this number only decides where the avatar lands.
- **`DamageTimeout`** is how long the avatar CANNOT BE HIT (nearly seven times the knockback): one
  long collision costs one life, else parking in a wall drains a run in a few frames. Do not confuse
  it with `DamageTime` (not steering). The warm bot's route verifier counts a replayed hit with it.
- **Presence animation** (arrival/departure) uses one duration; the asymmetry is the EASE (`Core`'s
  `AvatarPresenceRamp`: OutBack in, OutQuad out). It sits under 0.5 s because it must nest inside the
  checkpoint rewind (`CheckpointRamp`'s 0.5 s slowdown and speed-up). Above it the departure is still
  playing at the seek: `GameAvatarService.ApplyRespawn` calls Reset, the arrival replaces it midway,
  the avatar never reaches nothing and `IsDespawned` never becomes true (tried at 0.8: a shrink that
  changed its mind). 0.2 was a dash's length; 0.3 leaves room to read the ease.
- **Hitbox** is smaller than the drawing by well over a third (a bullet clipping the outline without
  killing reads generous, the reverse reads broken). Just Shapes & Beats runs 0.4 of its body's
  half-extent; at `AvatarScale` 0.5 this lands the radius at 0.15 world units against their 0.11
  and the 0.2 it carried before.
