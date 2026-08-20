# CLAUDE.md — bullet-hero-sdk

This is `bullet-hero-sdk` (root namespace `BH.SDK`), a standalone C# library defining the level/save
file format for the game Bullet Hero. It's vendored into the main Unity project as a git submodule at
`Assets/Plugins/BulletHeroSDK` (own repo: `vertoker/bullet-hero-sdk`, MIT license) — see the main
project's root `CLAUDE.md` (Rule 4, "Open SDK") for *why* it's split out this way (player-data
longevity, external interop with other rhythm games, third-party modding tools). **This file is
scoped to the SDK repo itself.** Prefer editing here directly over patching a vendored copy; the
Unity project's own `CLAUDE.md`s document the *consumer* side (`Core`'s "SDK model vs runtime state"
split, `GameEditor`'s `TargetScope`/Prefab Mode).

The main `BulletHeroSDK` asmdef is deliberately Unity-independent: `noEngineReferences: true`,
depends only on Newtonsoft.Json/Newtonsoft.Json.Bson (NuGet). Everything that genuinely needs
`UnityEngine` lives in separate, opt-in asmdefs (`UnityExtensions/`, `UnityIntegration/`) so the core
model/serialization/rules code can be reused standalone (e.g. by external tooling, a future
companion website) without dragging Unity along.

## Mental model

A level on disk is **three independent files**, each its own serialization root:
- `level.json`/`.bson` — a `Level` (`Models/Level.cs`): `Settings`/`Game`/`Audio`/`Resources`, plus
  `Hints` — the one aggregate holding nothing authored (see `Models/Hints/` below).
- `metadata.json`/`.bson` — a `LevelMeta` (`Models/LevelMeta.cs`): name/description/authors/license/
  per-resource UGC metadata. **Not a field of `Level`** — a wholly separate aggregate, easy to
  assume otherwise since both describe "the same level."
- `settings.json` — a `UserSettings` (device-wide player options, not per-level).

Every serialization root is a class carrying `[DataVersion(domain, major, minor)]` — see
"Serialization & versioning" below; everything else is a plain nested model versioned only as part
of its containing envelope. `Models/FileNames.cs` names the on-disk files; `PathUtils.FindDataFile`
resolves the actual extension (json vs bson chosen per-level at creation).

Almost every "value-shaped" field in the model tree is **polymorphic**: a keyframeable float,
vector, or color can be a literal `Value`, `RandomMinMax[Step]`, `RandomCircle`/`RandomRect[Step]`,
or (for colors) a `ThemeRef` — see "The polymorphic Value system" below. This is the SDK-side half
of what the Unity project's `Core/CLAUDE.md` calls "two parallel data shapes": the SDK model keeps
values tagged with their *kind*; Unity's runtime `*KeyState`/`*State` structs keep that same tag
alive so `GamePlayer`'s jobs can re-roll randomness every frame instead of freezing it once at load.

## Layer map (folders)

- **Models/** — the level format itself (see "Object model" / "Value system" below for the
  architecturally important parts). Subfolders: `Objects/` (scene-object hierarchy), `Values/`
  (polymorphic Value implementations), `Keyframes/` (keyframe wrapper types), `Effects/`+`Audio/`+
  `AudioEffects/`+`PostProcessing/` (per-domain payload data), `Game/` (`GameLevel` + level-global
  event tracks), `Events/` (one-shot markers/checkpoints, not animated tracks), `Data/` (`ThemeData`/
  `EffectData`/`CompositeShape` — resource-level aggregate payloads), `Resources/` (level resource
  dictionaries + `TypedResourceId` family), `SettingGroups/` (⚠️ two unrelated aggregates share this
  folder — `LevelSettings`, per-level, vs. `UserSettings`' sub-groups, per-device — see below),
  `Hints/` (`LevelHints` + `LimitHints` — everything advisory, see its own section below),
  `Meta/` (`Author`, `ResourceMeta` — consumed by `LevelMeta`, itself NOT in this folder; note
  `ResourceMeta` carries licensing/attribution only — **age rating and content descriptors live on
  `LevelMeta` alone**, since a rating describes the finished experience, not an asset in isolation),
  `Interfaces/`, `Enum/`, `Primitives/` (id structs). `Models/Names.cs` is the single source of truth
  for every `[JsonProperty]` name (short/abbreviated on purpose — see Serialization).
- **Serialization/** — `Serializers/` (`SerializationService`, the JSON/BSON entry point),
  `Converters/Base/` + `Converters/CustomTypes/` + `Converters/Dict/` (the polymorphism/id/dictionary
  JsonConverters — see "Serialization pipeline" below).
- **Versions/** — the model-versioning/migration system (`[DataVersion]`, `VersionedTypeRegistry`,
  `IMigration`) plus one subfolder per historical format generation (`V0_0/`). Has its own detailed
  `README.md` — read it first, this file only adds what it doesn't cover.
- **Rules/** — `public const` numeric/enum clamp tables (`FrameRules`, `ValueRules`, `LevelRules`,
  `AudioRules`, `EffectRules`, `PostProcessingRules`, `ResourceRules`, `TextRules`) plus
  `Rules/Attributes/` (declarative `[RuleXxx]` property attributes consumed by `Validations/`).
- **Validations/** — the rule engine, in two halves. *Declarative*: `RuleAnalyzer`/`RuleFixer`
  (+`RuleIssue`/`RulePath`) walk `[RuleContainer]`-marked object graphs and check/auto-fix every
  `[RuleXxx]`-attributed property, one property at a time. *Relational*: `LevelGraphAnalyzer`
  (+`GraphRule`/`GraphIssue`) checks the cross-object invariants a per-property attribute
  structurally cannot see. `ValidationFacade`/`ValidationReport` run both and are what a consumer
  should call. **Opt-in tooling, not wired into save/load anywhere** — see "Rules & validation" below.
- **Utils/** — `BHSDKMath` (Unity-independent math, since the core assembly can't reference
  `UnityEngine.Mathf`), `DimensionalIndexer2` (flat-array↔2D-grid indexing), `LevelUtils`
  (`RectObject` id/parent/bounds setters), `LevelCapacityUtils` (sweep-line "peak simultaneous
  objects per type" measurement backing `LevelHints.Limits` — see below), `LevelStatsUtils`
  (`LevelStats`/`LevelObjectStats`/`LevelResourceStats` — what a level *holds*, one O(n) allocation-free
  pass, for showing rather than for sizing; don't reach for it when you need `LevelCapacityUtils`'
  peak-simultaneous answer, and don't reach for that one to fill a readout), `ModelUtils`
  (deep-copy/equality/hash helpers for `List`/`Dictionary`/`Array` of `ICopyable<T>`),
  `ModificationUtils`/`TypeExtensions`, `ShapeLoopUtils` (radial loops - the two emitters every
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
- **Services/** — `SerializationService`-adjacent but SDK-root-level: `CryptographyService`
  (AES-256-CBC), `ModificationService` (reflection path-based get/set, see "Modification system"
  below), `TextFormatService` (`{variable}` string templating), `FontCharacterService` (builds
  `LevelHints.FontCharacters`, see below), and **`Shapes/ShapeCatalogService`** + `ShapeParameters`
  — the game's own built-in shape library, which lives HERE rather than in the consumer because it
  is what a `ShapeId` means: 497 shapes as the cross product of a form, a sector, a thickness rung
  and an invert flag, an id that IS those parameters packed one axis per nibble
  (`Encode`/`TryDecode`), and `Build` producing the geometry in pure C#. The consuming project only
  bakes what this enumerates. Read its header before touching the id layout — the six rules there
  are what make a future axis free and an inserted side count harmless.
- **Generators/** — authoring automation: a generator produces level content from a few parameters.
  Non-generic `IGenerator` root (so `GeneratorRegistry`'s reflection scan and a reflection-built
  form are possible) split into `ILevelGenerator` (builds a whole `Level`+`LevelMeta`) and
  `IScopeGenerator` (`Content`/`Modifier`, writes into an existing scope), with
  `BaseLevelGenerator<T>`/`BaseContentGenerator<T>`/`BaseModifier<T>` as the bases anyone actually
  derives from. **All mutation goes through `GeneratorContext`**, which journals it into a
  `GeneratorChangeLog` — that journal *is* undo, and writing to the model directly silently breaks
  it. The context also owns **grouping**: given a `groupName`, `context.Parent` lazily creates one
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
  `Geometry/`, `Bullets/`, `Audio/`, `Textures/`, `Utility/` (the concrete generators — 20 of them,
  the roster the design document calls complete plus `mod_content_remover`/`mod_framerate_remap` and
  `mod_span_fit`, which fits every child's lifetime to its parent's and is what replaced the removed
  `GraphRule.ChildSpanOutsideParent` and its auto-repair). Three rules a spawning generator must get right:
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
  format-wide: **`FrameDuration` is a count**, so the last legal frame is `FrameDuration - 1`
  (`RuleLevelFrame`'s upper bound is exclusive).
  Has its own `README.md`; full design in the consuming project's
  `docs/superpowers/specs/2026-08-05-sdk-generators-design.md`.
- **Roslyn/** — a *separate* asmdef (`BulletHeroSDK.Roslyn`, Editor-only, `autoReferenced: false`),
  entirely `#if BHSDK_ROSLYN`-gated (that define is never set inside the Unity project, so this
  compiles to an empty assembly here — it's meant for a standalone analyzer-package build of the SDK
  repo). `RuleContainerAnalyzer.cs` — see "Rules & validation" below.
- **UnityExtensions/** — Unity-type conversion glue (`Pixel`↔`Color32`, `PixelTexture`↔`Texture2D`,
  `IFrameable` framerate resolution reading `Screen.currentResolution`). Own asmdef, unconditionally
  requires `UnityEngine` (unlike the core SDK).
- **UnityIntegration/** — `Cat.cs`, a tiny `Debug.Log`-style logging façade (`Meow`/`MeowWarn`/
  `MeowError`/...) gated by `#if BHSDK_UNITY` per call, falling back to `Console.WriteLine`. Own
  asmdef, distinct purpose from `UnityExtensions/` (logging, not data conversion) — don't conflate
  the two folders.
- **Tests/** — `BulletHeroSDK.Tests.asmdef`, NUnit. `MockData.cs` is the shared fixture factory and
  `Metadata.cs` the author/category constants (neither is a test) — read `MockData.cs`'s header
  comment before writing new tests that need a `Level`/`Prefab`/etc. Root-level files cover
  serialization (`SerializationTests`, `SerializationTypeExtensionsTests`), modification
  (`ModificationTests`), validation (`ValidatorTests`), capacity (`LevelCapacityUtilsTests`),
  cryptography, text formatting, `ShapeIdTests` and `ShapeGeometryUtilsTests`, plus
`Tests/Services/ShapeCatalogServiceTests` (the built-in shape library — id round trip, retired and
future-axis ids refused, and the two geometric invariants a person cannot eyeball across five
hundred entries: a shape and its inverse tile the sector they were cut from, and slices tile the
whole). **`Tests/Rules/` is the bulk** — 42 files,
  roughly one per `[RuleXxx]` attribute on top of `BaseRuleTests` (the shared analyze/fix harness),
  `RuleCoverageTests` (fails if a rule has no test file), `RuleContextTests`, `RulesConsistencyTests`,
  `LevelGraphAnalyzerTests`, `ValidationFacadeTests`, `ModificationCheckedWriteTests`.

## Object model (`Models/Objects/`)

**`TextObject` carries two per-character effect tracks** beyond the usual transform ones:
`Fillments` (how much of the text is written) and `Appearings` (how much of it hides behind
`AppearingMask`), both plain `List<FloatKey>`, plus the non-keyframed `FillDirection`
(`Forward`/`Backward`/`FromCenter`/`ToCenter`), `AppearingMode` (`Random`=0/`Forward`/`Backward`) and
`AppearingMask` (an author-set string, default `"X"`, capped by `TextRules.MaxAppearingMask`). They
are resolved over the string itself by the consumer's text job rather than by the keyframe→transform
path. **Both fallbacks in `TextRules` mean "effect off"** (`Fillment_Fallback` = 1,
`Appearing_Fallback` = 0) — an empty track has to read as unchanged, or every text authored before
these existed would vanish. `Services/FontCharacterService` folds the mask into the font's character
set, since a mask character needs a glyph exactly like the text it replaces.

`RectObject` is the base of every placeable scene object: `ObjectId`, `ParentObjectId`, `Name`,
`Active`, `Span` (a half-open `FrameSpan`), `Layer`, plus the shared keyframe tracks
(`Positions`/`Rotations`/`Scales`/`Sizes`/`AnchorsMin`/`AnchorsMax`/`Pivots`). Empty keyframe lists
are valid (mirrors Unity project's `defaults.xxx` fallback convention). Subclasses, each overriding
`GetModelType() : ObjectType`: `ShapeObject`, `EffectObject` (thin — just an `EffectId` pointing
into `Level.Resources.Effects`, the actual payload lives in `EffectData`), `TextObject`,
`PrefabObject` (see "Prefab system" below).

**`ShapeObject` carries TWO `ShapeId` fields and neither derives from the other**: `ShapeId` is what
is drawn, `ColliderId` is what is hit, and a level routinely wants them to disagree — a telegraph
beam that is drawn but harmless, a hitbox simpler than the art it guards, an invisible wall. One id
type serves both because a shape and a hitbox are the same data: triangles inside `[-0.5, 0.5]`.
Both resolve against the same two collections (the game's own shape presets, or
`Level.Resources.CompositeShapes`), so a user-authored shape is usable for either or both.

**A built-in `ShapeId` is its parameters packed**, and the constants naming them live in the
generated half of the type (`Models/Primitives/ShapeId.g.cs`, one nested class per form —
`ShapeId.Hexagon.S4_2_T8_I`). `Services/Shapes/ShapeCatalogService` owns the layout and is the only
thing that may write that file. The previous library numbered its 78 shapes 1..78 by their position
in the consumer's array, so inserting a form renumbered everything after it; form code 0 is now
reserved and never issued, which is what makes every one of those retired ids decode to nothing
rather than to some other shape.

**`RectObject.Active` replaced `Visible`, and the change is semantic, not cosmetic.** `Visible`
gated rendering only, so an invisible object still hit the player — a trap, since nothing in the
name said so. `Active` gates *both* paths and applies down the hierarchy. "Not drawn but still
solid" moved to where it belongs: a Null `ShapeId` with a real `ColliderId`.

`ShapeObject.ShaderType` (`Models/Enum/ShaderType.cs`, byte: `Auto = 0`/`Opaque`/`Transparent`) is
authored intent about the render path, not a shader id — the format deliberately has no
user-defined shaders. `Auto = 0` is what an older file deserializes to, so adding it needed no
migration and the domain stayed at `(1, 0)`. What `Auto` actually resolves to is a *consumer*
question and lives in the Unity project (`Core`'s `ShapeShaderResolver`); the format only stores the
three-way choice. It is one of the hand-written-boilerplate fields, so it must appear in
`CopyImpl`/`Update`/`EqualsShapeObject`/`GetHashCode` alike.

**Polymorphism mechanism** (applies throughout the whole model tree, not just objects — see "Value
system"): `ObjectType` (byte enum) is resolved by `Serialization/Converters/CustomTypes/
ObjectConverter.cs`, registered globally in `SerializationService`, **not** via a `[JsonConverter]`
attribute on `RectObject` itself and **not** via Newtonsoft `TypeNameHandling`/`$type`. Adding a new
`RectObject` subtype means extending `ObjectConverter.GetType`'s switch (throws
`ArgumentOutOfRangeException` otherwise) — there's no attribute-based auto-discovery.

## `IObjectScope` / `IObjectIdCounter` — the split every consumer must get right

`IObjectScope` (`Dictionary<ObjectId, RectObject> Objects`) and `IObjectIdCounter`
(`ObjectId GetNextObjectId()`) are two separate, narrow interfaces. **`Prefab` implements both on
one class** (its own `Objects` + its own int counter). **At level scope the two are split across
different classes**: `Level.Game` (`GameLevel`) is the `IObjectScope`, but `Level.Settings`
(`LevelSettings`) is the `IObjectIdCounter` — `GameLevel` does *not* implement `IObjectIdCounter`,
`LevelSettings` does *not* implement `IObjectScope`. Anything that needs "the scope+counter pair"
generically (the Unity project's `PrefabMaterializer`, `LevelEditorOperation.TargetScope`/
`TargetCounter`) must combine `Level.Game`+`Level.Settings` explicitly for the level case, unlike the
`Prefab` case where one object satisfies both. Don't assume symmetry here.

`ObjectId` has reserved negative constants beyond plain user ids: `Camera = -1` (player-runtime-only,
invalid as an actual `ObjectId`), `LocalPlayer = -2` (a valid parent target), `PrefabRoot = -3`
(only meaningful *inside* a `Prefab` template — same effect as an unset/`Null` `ParentObjectId`
there). `RuleParentObjectIdValidAttribute` accepts all three everywhere, even where semantically
meaningless (e.g. `PrefabRoot` at level scope) — a known leniency gap, not yet context-checked.

## Prefab system

`Prefab` (`Models/Objects/Prefab.cs`, a `Level.Resources.Prefabs` entry) is the *template*: its own
`Objects`/`ObjectIdCounter`, plus its own authored `Name`/`FrameDuration`. `PrefabObject` (a
`RectObject` subclass) is the *placement*: `PrefabId` (which template) +
`Dictionary<ObjectId, ObjectId> ObjectIds` (template-inner id → this placement's own materialized
outer id) + `Dictionary<ModificationKey, Modification> Modifications` (per-instance field overrides,
below). Placements — whether at level scope or nested inside another `Prefab`'s own `Objects` — live
in the **same** `Objects` dictionary as everything else, discriminated only by
`GetModelType() == ObjectType.PrefabObject`; there's no separate placement list.

**Per-instance overrides (`PrefabObject.Modifications`) are live and load-bearing** — this is how a
placement diverges from its template without breaking the link. Three pieces:

- `ModificationKey` (`Models/Primitives/`) — the *address*: `ObjectId` (the **template's inner** id,
  not the materialized outer one, so the key survives re-materialization) + `string Path` (dotted/
  indexed field path like `"pos[0].v"`, resolved through each property's `[JsonProperty]` name).
  Being the dictionary key is what makes "one override per (object, field) pair" a structural
  guarantee rather than a rule to enforce.
- `Modification` (`Models/Objects/`) — `Key` + an untyped `object Value`. The `Value` setter
  **normalizes integrals to `long` and floating-point to `double`** on assignment, deliberately
  matching what Newtonsoft always produces when deserializing a raw JSON number into an `object`
  property — without it an override built in code with a plain `int` stops `Equals`-ing itself after
  a round trip. Its file header also lists the design limits still in force: only `RectObject`/
  `Prefab` targets, no parenting a `RectObject` *into* a prefab's inner objects (only the reverse),
  and no deep inheritance — an override applies only within the prefab scope it lives in.
- `Services/ModificationService.cs` — the generic reflection get/set by path string that resolves a
  `Key.Path` against a live model instance.

Overrides are **re-applied on top of a fresh template copy after every materialize/resync**
(Unity-side: `Core`'s `PrefabMaterializer.ApplyModifications`; recorded by `GameEditor`'s
`ModificationRecorder`/`EditObjectOperation.RecordModification` — see those folders' `CLAUDE.md`s).
`Modifications` serializes through its own `Serialization/Converters/Dict/
DictionaryModificationsConverter` (the key is recoverable from the value's own `Key` property, so it
writes as a plain array — same family as `DictionaryAsListConverter`, see "Value system" below).

## Clipboard (`Models/Clipboard/`)

`ClipboardData` (`[DataVersion(DataDomains.ClipboardData, 1, 0)]`) is one copied editor selection,
split into **one section per editor timeline** — the consumer keeps a single instance as the backing
store of all its per-timeline buffers, and the same instance is what leaves the process as JSON when
the author exports it. It is a **partial level**: every section is a collection type the format
already owns (`Dictionary<ObjectId, RectObject>` ×3, `Dictionary<AudioId, LevelTrack>` ×2, the four
`GameLevel` event aggregates), so it rides on the existing converters and introduces no new
polymorphism of its own.

Two things about its shape are decisions rather than accidents:
- **`Objects` and `KeyObjects` hold the same value type and are separate sections.** The first means
  "create these objects", the second "add these keyframes to an object that already exists" — a
  copied keyframe travels inside a *stripped copy of its owner* because which track a keyframe
  belongs to has no representation in the format other than which property of which object its list
  hangs off. One dictionary cannot express both intents.
- **No anchor frame is stored.** Where a paste lands is derived from the section's own contents at
  paste time; a stored anchor is one more thing that can disagree with what it describes after a
  partial edit or a clear.

`ClipboardContent` (`Models/Enums/`, `[Flags] byte`) records which sections carry something and is
validated by `RuleEnumFlagsValid`, not `RuleEnumValid` (see Rules below). Covered by
`Tests/ClipboardDataTests` — a round trip is the feature here, not a nicety.

## The polymorphic Value system

Every "authorable value" field goes through one of these interfaces (`Models/Interfaces/Values/`),
each `: IModel<TSelf>` plus a single discriminator method returning a `*Type` enum:

| Interface | discriminator enum | concrete variants |
|---|---|---|
| `IFloat` / `IInt` | `FloatType`/`IntType` | Value / RandomMinMax / RandomMinMaxStep |
| `IVector2/3/4` | `VectorType` (shared) | Value / RandomRect / RandomRectStep / RandomCircle |
| `IColor3` (RGB) / `IColor4` (RGBA) | `ColorType` (shared) | Value / ThemeRef / RandomMinMax |
| `IString` | `StringType` | Value / Localized (`List<StringLanguage>`) |
| `IScreenLimit` | `ScreenLimitType` | None / Fixed / Bounds |
| `ILicense` | `LicenseType` | NoSpecified / Typical / Custom |

**Discriminator mechanism — a 2-element JSON array, not Newtonsoft `$type`/`TypeNameHandling`.** Each
interface has its own `JsonConverter<TInterface, TType> : JsonConverterCustomType<T, TType>`
(`Serialization/Converters/CustomTypes/*.cs`) writing `[typeEnum, payload]`. The base class
(`Serialization/Converters/Base/JsonConverterCustomType.cs`) needs a *second*, private "default"
serializer (containing every other converter except itself) to deserialize the resolved concrete
type's own plain members without recursively re-wrapping them — see `IRequiresDefaultSerializer`
(`Serialization/Converters/Base/`, **not** under `Models/Interfaces` despite the SDK's own TODO.md
implying otherwise). `SerializationService.GetConverters` auto-wires this for any converter
implementing the interface — adding a new polymorphic-value converter needs no other bookkeeping.

**What a serializer actually holds is two converters, not thirty-five.** Newtonsoft resolves a
settings-level converter by walking `JsonSerializer.Converters` and calling `CanConvert` on each,
once per **value**, caching nothing — so a long list is paid for on every value in the file, and
`JsonConverter<T>.CanConvert` is `sealed`, so a converter cannot memoize its own answer. The list in
`GetConverters` is therefore handed to a `ConverterRouter` (`Converters/Base/`), which resolves
`Type → converter` once and answers from a cache afterwards; first match wins over the same list in
the same order, so **order still decides which converter handles a type, and adding one works
exactly as before**. `VersionedEnvelopeConverter` is the one that cannot be routed and sits in the
list beside the router: its `CanConvert` answers differently depending on which domain is currently
being written (the `_activeDomains` guard, which is what stops it re-wrapping its own payload), and a
per-type cache cannot express that. Each `IRequiresDefaultSerializer`'s private serializer gets its
own router, since each excludes a different converter.

Same 2-element-array mechanism backs several other polymorphic families beyond the table above:
`EffectAngleConverter`/`EffectColorConverter`/`EffectScaleConverter`/`EffectShapeConverter`/
`EffectShapeSpreadConverter` (effect emitter sub-shapes, see "Effects" below), `Color4X4KeyConverter`
(4-corner keyframe color — `Color4X4KeyType.Value/Horizontal/Vertical/BariCentrical`), and
`ObjectConverter` (`RectObject` hierarchy, see "Object model" above).

**Dictionaries with a self-describing value** (`ObjectId→RectObject`, `AudioId→LevelTrack`,
`ThemeId→ThemeData`, `EffectId→EffectData`, `PrefabId→Prefab`,
`ModificationKey→Modification`, resource-id dicts) serialize as a
**plain array with the key dropped** (`Serialization/Converters/Dict/DictionaryAsListConverter`,
key recovered from the value on read). Dictionaries where the key *can't* be derived from the value
(id→id remap tables) use `DictionaryAsPairListConverter` instead (array of `{K,V}` structs) — plain
Newtonsoft dictionary serialization needs a `TypeConverter` on the key type to use it as a JSON
property name, which value-type ids like `ObjectId` don't have; this sidesteps that entirely.

## Keyframes

`Keyframe` (`Models/Keyframes/Keyframe.cs`) is the shared base: `Frame` (int) + `Ease` (`EaseType`,
29-value enum, stored **per-keyframe**, not per-track). Concrete keyframe types wrap one payload
field each (`FloatKey`, `Vector2/3/4Key`, `Color3Key`, `Color4Key`, `AngleKey`, `ScaKey`,
`AlignmentKey`, `UVKey`, `ZoomKey`, `ShakeKey`, `ScreenLimitKey`, `VelocityPoint`, ...). **`BoolKey`
is the one outlier** — implements bare `IFrame` (no `Ease`), since a toggle has nothing to
interpolate. **4-corner texture color** is its own polymorphic keyframe family, separate from the
`IColor4` value system: `IColor4X4Key` → `Color4Key` (all 4 corners same) / `ColorHorizontalKey`
(Left/Right) / `ColorVerticalKey` (Bottom/Top) / `Color4X4Key` (all 4 independent — `BL`/`BR`/`TL`/
`TR`), resolved via `Color4X4KeyConverter`.

**No enforced sort order.** `[RuleCollectionUnique(nameof(XKey.Frame))]` validates Frame values are
*unique* within a track's `List<TKeyframe>` — it does not enforce them being sorted. Consumers must
sort/search themselves (Unity's `LevelPlayerMath.FindMatchIndexes` assumes sorted input at that
layer, converted once at load via `LevelStateBuilder`).

## Effects, Audio, PostProcessing, Theme

- **Effects**: `EffectObject.EffectId` points at an `EffectData` resource (`Models/Data/
  EffectData.cs`, `Level.Resources.Effects`) — the SDK-side analog of Unity's `EffectGameStatic`
  index table. `EffectData` groups: `Core` (render/loop/particle-count/lifetime/texture/pivot),
  `Forces` (gravity/velocity/orbital/linear-force), plus 4 independently-polymorphic sub-shapes:
  `IEffectAngle`/`IEffectColor`/`IEffectScale` (each: Value / CurvesOverLife / CurvesBySpeed /
  RandomUniform / RandomPerComponent — the Random pair is structurally identical, differs only in
  evaluation semantics) and `IEffectShape` (Point/Circle/Rectangle/Line/Cone/Torus, with
  Circle/Line/Cone/Torus additionally nesting an `IEffectShapeSpread`: Random/Loop/PingPong/Sine —
  `Sine` and `Point` carry zero fields, pure enum-selected behavior).
- **Audio**: `AudioLevel.Tracks : Dictionary<AudioId, LevelTrack>` (one flat dict per level;
  `AudioId` bans negative values — no game/user-defined split unlike other ids). `LevelTrackEffects`
  is a **flat class with one always-present field per DSP effect** (Lowpass/Highpass/Echo/Reverb/
  Chorus/PitchShifter/Distortion/Flange/Compressor/Normalize/ParamEQ), not a dictionary/flags/list.
  Each effect's "enabled" state is encoded as `MixLevel > -80dB` (`AudioRules.IsActiveMixLevel`) —
  **no explicit bool per effect**. `LevelTrackEffects.Active` (track-level, default `false`) *is* an
  explicit bool — don't confuse the two.
  **`LevelTrack.Volume` (`[0, 1]`, default `1`) is the track's own fader and the SECOND thing on it
  called volume** - the first being `Effects.Volumes`, the keyframed curve. They MULTIPLY at playback
  rather than compete (the consumer does it in `BuildAudioJob`): the fader is what the whole track
  sits behind, the curve is what fades it in and out inside that, and an author wanting "this track,
  quieter" should not have to rewrite every key on it. Additive with a constructor default, so it
  needed no migration and `AudioLevel` stays at `(1, 0)` - a pre-fader file reads back at full
  volume, which is exactly how it used to sound. Note this is the opposite call from `Speed`
  below, whose pre-Speed default deserialized to a silent `0`.
  `LevelTrack.Speed` (`[-2, 2]`, default `1`) is the track's own resample rate — faster is also
  higher-pitched, negative **reverses** the track (it starts at the clip's END and plays back to its
  start, with `OffsetTime` skipping the tail rather than the head), `0` freezes it silent — and it
  is deliberately **not** keyframed: an animated rate would make the clip position the integral of
  that curve, which no consumer can evaluate from one frame's data. It shipped **without a migration**
  on purpose (`AudioLevel` stays at `(1, 0)`): a pre-Speed file deserializes to `0f`, i.e. silent
  tracks, and the levels that existed at the time were the author's own to re-save. Don't add a
  `NullSpeed`-style sentinel after the fact — `0` is a legal authored value here.
- **PostProcessing**: `GameLevel.PostProcessingEvents` — top-level `Active` (default `true`,
  opposite default from audio's `Active`) + 12 keyframe-track lists, one per URP effect (Bloom,
  ChromaticAberration, Vignette, LensDistortion, FilmGrain, MotionBlur, ColorCurves, LiftGammaGain,
  ShadowsMidtonesHighlights, WhiteBalance, AnalogGlitch, DigitalGlitch — matches Unity's
  `Core/Models/Groups/PostProcessingGroup.cs` field-for-field). `PostProcessingKeyframe` implements
  `IKeyframe` directly (not `: Keyframe`) and adds its own per-keyframe `Active` bool — every effect
  is independently toggleable per-keyframe *in addition to* the track-level switch. Several fields
  are commented `HEAVY, PHONES DON'T LIKE IT` (Bloom, MotionBlur, AnalogGlitch, DigitalGlitch).
- **Theme**: `ThemeData` (`Level.Resources.Themes`) holds a fixed `Color4Value[64] Matrix` (an
  "8×8 grid", index layout documented in-file, mirrors *Project Arrhythmya*'s convention).
  `ThemeKeyframe` (a real animated track, unlike `Marker`/`Checkpoint`) selects which `ThemeId` is
  **active** over time. `ColorType.ThemeRef` (`Color3ThemeRef`/`Color4ThemeRef`) stores only a raw
  `int ThemeColorIndex` (0-63) — **not** a `ThemeId` — indexing into whichever theme is currently
  active. Two-level indirection: `ThemeKeyframe` picks the theme, `ThemeRef` picks a slot within it.

## `Models/Game/`, `Models/Events/`: level-global vs. per-object

`GameLevel` (`Level.Game`) = `Objects` (the `IObjectScope`) + four `[DataVersion]`-tagged event
aggregates: `GameEvents` (Markers — editor-only annotation, no gameplay effect — **Beats**, Checkpoints,
ScreenLimits, Backgrounds [`Color3Key`, themeable], Themes [`ThemeKeyframe`]), `CameraEvents`
(Positions/Rotations/Pivots like a `RectObject`, but `Zooms` instead of 2-axis `Sizes` and an added
`Shakes` track — no Layer/Anchors/Sizes, the camera has no parent/isn't rendered), `PostProcessingEvents`
(above), `PlayerEvents` (`Visibles`/`Controls`/`Collisions`, each `List<BoolKey>`). `Checkpoint`/
`Marker` (`Models/Events/`) are flat one-shot lists, not animated keyframe tracks, despite living
alongside `ThemeKeyframe` (which *is* a real track) in the same `GameEvents` class.

**`GameEvents.Beats`** (`List<BeatSegment>`, `Models/Events/BeatSegment.cs`) is a third shape again —
neither a track nor a list of points, but a list of **non-overlapping `FrameSpan` segments**, one per
stretch of constant tempo (`Bpm` + `Offset`, the phase, in **fractional** frames + `BeatsPerBar`,
plus `Marker`-style `Name`/`Color4`). Spans rather than tempo points because a point track cannot
express a HOLE: an intro with no percussion, a break, the tail after the song ends. Editor-only in
exactly the sense `Markers` is — saved, read back, consumed by generators and by the editor's own
snapping, never by playback. `Span`'s setter strips `FrameAnchor`s: anchors mean "follow the parent's
edge" and a segment has no parent. Adding it needed no migration — the domain stays at `(1, 0)` and
an older file deserializes to an empty list.

**Where the beats fall is computed, never stored**: `Utils/BeatMath` resolves
`Start + round(Offset + i * framesPerBeat)` — rounded from the segment's own start rather than
accumulated, so the error stays at half a frame instead of growing with every beat — and every
collection is bounded by `LevelRules.MaxBeatGridPoints`, since a fast tempo over a long span is
millions of lines. It lives here rather than in the consumer because both halves need the SAME grid:
a generator resolves beats with no Unity around it, the editor draws and snaps with the result.
Non-overlap is `LevelGraphAnalyzer`'s (`GraphRule.BeatSegmentsOverlap`), not a `[RuleXxx]`'s — an
attribute sees one property at a time — and like every graph finding it carries no repair.

## `Models/SettingGroups/` — one folder, two unrelated aggregates

`LevelSettings` (`Level.Settings`, per-level: `Framerate`, `FrameDuration`, `ObjectIdCounter`,
`AudioIdCounter` — the `IObjectIdCounter` implementation — and `Seed`) has nothing to do with the
rest of this folder (`GeneralSettings`/`ControlsSettings`/`AudioSettings`/`GraphicsSettings`/
`GameEditorSettings`/`InterfaceSettings`), which are all sub-groups of `UserSettings` (per-device,
`settings.json`).

`InterfaceSettings` is the newest of them (the game's own overlays — today the diagnostics readout's
`StatsActive` + `StatsAlignmentX`/`Y`) and shipped **without bumping the `UserSettings` domain**: an
additive property whose constructor supplies a default needs no snapshot and no migrator, exactly like
`LevelSettings.Seed` and `GameEvents.Beats`. Its alignment pair is two free `[0,1]` floats rather than
a nine-value enum, because it is the same convention level content is authored in (`0,0` lower-left) —
the settings screen offers the nine presets, a hand-edited value between them is legal data.
`GameEditorSettings.GridSize`/`GridOpacity` (the editor's viewport grid — one world unit per cell
and a quarter opacity by default, floored at `ValueRules.MinGridSize` and ranged `[0,1]`) shipped the
same way and are worth reading as the worked example of what belongs here at all: how the grid LOOKS
is how the author works and is remembered, while whether it is currently drawn is the current view
and stays in the editor's session (`Services.GameEditor`'s `GridModeService`) — the same split the
active gizmo has. Opacity is the only part of its colour anyone authors; the hue is derived from the
camera background live, which is why there is no grid colour here.

`AntiAliasingGraphicsSettings` (`Type`/`Msaa`/`Hdr`) is the one graphics sub-group that does **not**
derive from `BaseGraphicsSettings`, and the omission is deliberate: an inherited `Render` would mean
"is anti-aliasing on", which is exactly what `Type = None` already says, and two switches for one
decision can disagree. It shipped additively like everything else here — the domain stays at
`(1, 0)`, and a settings file written before it deserializes to the constructor's defaults (MSAA,
x2, no HDR) rather than to a zeroed pair that would read as "off". `MsaaType`'s value **is** its
sample count, except `None = 0`, which every graphics API states as 1 — convert with
`MsaaTypeExtensions.ToSampleCount`, never a cast.

The `UserSettings` sub-groups additionally implement `IMoveable<T>` (`Pull(source)` — an in-place
merge, distinct from `IModel<T>`'s `Copy`/`Reset`).

`LevelSettings.Seed` is the level's own random seed, and **`LevelRules.NullSeed` (0) is its
default and means "not authored"**, not seed number zero — test it with `LevelRules.IsValidSeed`,
never with a literal (same shape as `AudioRules.IsActiveMixLevel`). `LevelRules` carries **two
ranges, and conflating them is the mistake to avoid**: `[MinValidSeed, MaxValidSeed]` = `[1,
int.MaxValue]` is what a REAL seed is and what every generator must draw from (`IsValidSeed`/
`AssertSeed`), while `[MinSeed, MaxValidSeed]` is what a seed *field* may hold, `NullSeed` included
(`IsSeedInput`/`AssertSeedInput`/`ClampSeed`, and what `[RuleMin]` validates). A generator that
could return 0 would occasionally produce a run nobody can reproduce, since 0 reads as "unseeded"
one level load later. A level ships without one and the
consumer generates a fresh seed on every load, which is the ordinary case; an author sets it only to
pin a run down, and a host may still override it per-launch. The consumer side of that three-tier
ladder lives in the Unity project (`Core`'s `SettingsGroup`, see its CLAUDE.md "Determinism") — the
format only stores the middle tier. Adding the field needed no migration: the domain stays at
`(1, 0)` and an older file simply deserializes to 0.

## `Models/Hints/` — the level's advisory aggregate

`LevelHints` (`Level.Hints`, `[DataVersion(LevelHints, 1, 0)]`) is the fifth aggregate on `Level`
and the only one carrying **nothing an author wrote**. Everything in it is DERIVED from the other
four, written by whoever saves the level, and safe to drop: a consumer that ignores the whole object
plays the identical level, only paying at load (or mid-playback) for work the hint front-loads.
**That is the membership test** — a field belongs here when it is recomputable from the rest of the
level *and* nothing looks different when it is missing, wrong or stale. Anything an author decides
belongs in the aggregate it describes, however small and however editor-only: a `Marker`, a
`BeatSegment`, an object's `Name` and a `FrameSpan`'s anchors are all *authored* editor-only data,
unrecoverable once dropped, and none of them is a hint.

It exists as its own aggregate rather than as fields on what it measures (`Limits` used to sit on
`LevelSettings`, `FontCharacters` on `LevelResources`) because those neighbourhoods are
authoritative: a reader there cannot tell "the author decided this" from "a tool measured this last
save". Behind one property the distinction is structural, and "recompute every hint" / "drop every
hint" become one call each instead of a list someone has to keep current.

Two members today:

- **`Limits`** (`LimitHints`) — six peak-simultaneous-object counts (instances/shapes-opaque/
  shapes-transparent/effects/texts/tracks) an editor writes on save from
  `LevelCapacityUtils.GetPeakUsage`, so a player can preallocate its per-frame buffers instead of
  growing them mid-level. Never authoritative — the file may be hand-edited, foreign or stale, so a
  consumer treats it as a lower bound at most, measures/grows on its own anyway, and clamps it
  against what the device allows. All zeroes (the default, and what an older level deserializes to)
  means "no hint", not "no objects". `LevelRules.Min/MaxCapacityHint` bound it purely so a hostile
  file can't request a gigabyte-scale preallocation.
- **`FontCharacters`** (`Dictionary<FontResourceId, CachedFontText>`) — each font's distinct-character
  set, a glyph-atlas warm-up hint. Keyed by **any** `FontResourceId`, game-defined ids included
  (unlike every dictionary in `LevelResources`, which is user-defined-only), because a level's
  most-used font is usually the game's own and needs warming just as much as a shipped one. Text
  renders identically without it; an empty or absent entry means "warm nothing for this font", not
  "this font has no text", and no consumer recomputes it at load. The value is a `CachedFontText`
  (`Models/Data/`) carrying its own `FontResourceId` plus the characters as an **`IString`**, not a
  bare `string`, so a localized level warms per language through the very same resolution its text
  goes through. Carrying the key is what lets it serialize like every other keyed collection — a
  plain array with the key dropped and recovered on read (`DictionaryCachedFontTextsConverter :
  DictionaryAsListConverter`) — instead of the `{k, v}` pair form a bare `id -> IString` map needs.
  Build it with `Services/FontCharacterService` (`Build`/`BuildAll` are pure and return values;
  `Apply` writes into the model) or run the `gen_font_cache` generator, which does the same through
  `GeneratorContext` so the run stays undoable (`Generators/Utility/FontCacheGenerator.cs`).

Both reach a generator through `GeneratorContext.Hints`, which is **null in Prefab Mode** exactly
like `Game`/`Audio`: a hint describes the file a player loads, and a template is not one.

**No migration, and the domains stay at `(1, 0)`.** A level written before this existed deserializes
to an all-empty `Hints`, which is precisely "no hint"; one whose hints were written under the old
layout simply loses them at the next save. That is what advisory means, and it is why the move needed
nothing else.

## `Models/Resources/`

`LevelResources` (`Level.Resources`): seven dictionaries — `Textures`/`Fonts`/`Audios` (by their
typed resource id), `CompositeShapes` (by `ShapeId`), `Themes`/`Effects`/`Prefabs` (by their own
Guid-based id). **Every dictionary here can only ever contain user-defined (negative-id) resources**
— each concrete `Resource` subtype's id property is rule-capped to the negative range; game-defined
resources are baked into the game/its own registries and never appear in a level's own `Resources`.
`TypedResourceId` (`Models/Primitives/Resources/`) is the shared convention: `0`=Null,
`[1,MaxInt]`=game-defined (permanent), `[MinInt,-1]`=user-defined (needs `Resource.Sources`, up to
`Resource.MaxSourcesCount`=4 fallback URIs per resource). `TextureResourceId`/`FontResourceId`/
`AudioResourceId`/`BytesResourceId`/`TextResourceId` are narrow per-category wrappers sharing this
range, freely convertible to/from the untyped `TypedResourceId`.

**`FontCharacters` used to live here and no longer does** — it was never a resource, only a fact
*about* the resources, so it moved to `Level.Hints` with the rest of the advisory data. Don't look
for it under `Level.Resources`.

`Guid`-based ids (`ShapeId`/`EffectId`/`LevelId`/`PrefabId`/`ThemeId`) deliberately have **no**
positive/negative split — a `Guid` has no natural sign, so game-defined vs. user-defined is
determined by *which collection* an id is found in (game registry vs. level `Resources`), not by the
id's own value, unlike the int-based `TypedResourceId` family. `Guid.Empty` is the reserved Null for
all of them.

**Three `IPrimitiveGuid` properties deliberately allow Null and must NOT carry
`[RuleIPrimitiveGuidNotNull]`** — for them Null is a real authored state, not an unset reference:
`ShapeObject.ShapeId` (Null = drawn as nothing, which combined with a real `ColliderId` is how an
invisible hitbox is authored), `ShapeObject.ColliderId` (Null = decoration, drawn but never collided
with — the runtime collision jobs skip on `!IsEnabled()` and the editor's collision toggle writes
Null) and `PrefabObject.PrefabId` (Null = empty placement, materializes nothing —
`OpLevelCreatePrefabObject` creates every placement this way before the author picks a template).
`ColliderId`/`PrefabId` default to Null straight from their constructors, so adding the rule makes a
freshly-constructed object fail validation; worse, its `Fix` assigns a random `Guid`, silently
inventing a nonexistent shape (giving decoration real damage) or a dangling prefab reference. This
bit the SDK once already — the rule was on `ColliderId` and broke
`ValidatorTests`/`SerializationTests` against `MockData`. Don't re-add any of the three.

**Two int-backed ids join them, for one reason: `ShapeObject.TextureResourceId` and
`EffectObjectCore.TextureResourceId`.** Both carry `[RuleReferenceExists(Texture, allowNull: true)]`
and **no** `[RuleIPrimitiveIntNotNull]`, and both default to `Null` rather than `Square`/`Circle`.
Geometry moved out of the texture and into `ShapeId` — an object's own, and (since
`EffectObjectCore.ParticleShapeId`) a particle's too — so the ordinary object and the ordinary
particle are both a bare tinted silhouette, and an image painted on top of one is the exception. On
the object side the old default also cost every freshly created object the opaque render path, since
a texture that exists cannot be *proven* alpha-1 while one that does not exist trivially can
(`Core`'s `ShapeShaderResolver`); on the effect side it is the graph's own authored state, so the
common effect pushes no texture property at all. The other two int-backed references
(`LevelTrack.AudioResourceId`, `TextObject.FontResourceId`) keep the rule: nothing plays or renders
without them.

## `IModel<T>` pattern

`IModel<T> : ICopyable<T>, IEquatable<T>, IResetable` — every live domain model implements: `Copy()`
(new instance), `Equals(T)`/`Equals(object)`/`GetHashCode()` (hand-written, not generated — this is
exactly the kind of boilerplate a copy-paste mistake hides in, see "Conventions" below), `Reset()`
(back to defaults, in place, no allocation). Some additionally implement `IUpdatable<T>`
(`Update(src)` — in-place field copy from another instance, distinct from both `Copy` and `Reset`;
used by `RectObject`+subclasses, `EffectData`+its sub-groups). **Frozen historical snapshot classes
under `Versions/VX_Y/` deliberately skip all of this** — they're one-shot deserialization targets,
not domain objects; don't expect every `[JsonProperty]`-bearing class in this codebase to implement
`IModel<T>`.

## Serialization pipeline (`SerializationService`)

`SerializationService.SerializeData<T>`/`DeserializeData<T>` are the plain string-JSON entry points —
both throw `ArgumentException` if `T` has no `[DataVersion]` (only aggregate roots may go through
this API). `GetDataSerializer(SerializationType.Json/Bson)` returns an `IDataSerializer`
(`SerializeEnvelope`/`DeserializeEnvelope` operating on raw `byte[]` + `EnvelopeData` — version tag +
untyped payload) — `JsonDataSerializer`/`BsonDataSerializer` share all envelope logic in
`BaseNewtonsoftDataSerializer`, differing only in `JsonTextWriter/Reader` vs. `BsonDataWriter/Reader`.
`SerializationType` is `byte`-backed (`Json=0, Bson=1, JsonPretty=2`);
`SerializationTypeExtensions.ToFileExtension`/`TryFromFileExtension` map `.json`/`.bson`.

**Nothing on the read path materializes a `JToken` tree, and that is a rule, not an implementation
detail.** A version has to be known before the payload can be typed, and reading it used to mean
loading the whole document into a `JObject` and walking that tree a second time to deserialize — per
domain, and domains nest, so a `Level`'s tree was cloned again for `GameLevel`, again for each of the
four event aggregates, and again for **every** `Prefab` in its resources. `DeserializeEnvelope` now
makes two streaming passes (the first stops at the version property, only the second reads content)
and `VersionedEnvelopeConverter.ReadJson` reads envelope properties straight off the reader. Since
`WriteJson` emits the version first, the ordinary document buffers nothing at all; a document whose
value happens to come first (hand-edited, or written by another tool) is still read correctly, by
buffering that one subtree until the version that types it arrives. `Tests/SerializationPerformance
Tests` pins both formats.

**BSON is not the fast format here, and it was measured.** On a 4.7k-object level it reads ~5% faster
than JSON while writing a file ~30% *larger* — what dominates is Newtonsoft binding members by
reflection, which both formats pay identically, not tokenizing. Choose between them for what they are
(one is readable and diffable, the other is not), not for speed.

**`JsonPretty` is a write-only distinction and `Formatting` lives on the MODE, not on the settings.**
It writes the same document as `Json` with indentation, shares its `.json` extension, and is read by
the same reader — so `TryFromFileExtension` resolves `.json` to `Json` alone, deliberately: nothing
can recover the choice from a file, and it belongs to whoever is saving. `SerializationSettings` has
no `formatting` field any more (it applied to the one shared `JsonSerializer`, so one screen's "write
this readable" re-indented every file written afterwards); `SerializationTypeExtensions.ToFormatting`
is the single conversion, applied per `JsonTextWriter` by `JsonDataSerializer` and by
`SerializeData<T>(value, type)`. Adding a third member is also why nothing may test `== Bson` any
more — a two-branch ternary reads `JsonPretty` as `Json` silently, which is what the Unity project's
`Core/Utils/SerializationModeUtils` exists to prevent at the five UI call sites that did.

Two `JsonSerializerSettings` are built: one with the full converter list, one bare ("`settingsDefault`",
the escape hatch every `IRequiresDefaultSerializer` converter needs — see "Value system" above).
`ObjectCreationHandling = Replace` is load-bearing (documented inline in `SerializationService.cs`):
without it, deserializing into a non-null nested object/list left by a parameterless constructor
(e.g. a default 2-key curve) *populates into* the existing instance instead of replacing it, breaking
round-trip equality. `ContractResolver` only forces `MemberSerialization` (`OptIn` by default) onto
every contract.

**Id/primitive wrapper structs** (`ObjectId`, `ThemeId`, `AudioId`, `PrefabId`, `ShapeId`,
`EffectId`, any `IPrimitiveGuid`/`IPrimitiveInt`/`IPrimitiveFloat`) serialize as a **bare scalar**, not
`{"Value": ...}`, via `PrimitiveGuidConverter`/`PrimitiveIntConverter`/`PrimitiveFloatConverter` — all
reconstruct via `Activator.CreateInstance(type, value)`, so every such wrapper needs a public
single-arg constructor. `PrimitiveGuidConverter` specifically handles Guid surfacing as a `string`
under JSON but an already-boxed `Guid` under BSON (BSON's native UUID subtype).

## Model versioning (`Versions/`)

**Read `Versions/README.md` first** — it documents the folder convention (generation-first, e.g.
`V0_0/` + `V0_0/Migrations/`) and the "nested envelope always resolves to the domain's *current*
type" rule in detail; this section only adds what it doesn't cover.

- `[DataVersion(domain, major, minor)]` marks an aggregate-root boundary that gets its own envelope
  and migrates as one unit. **Every live domain today is `(1, 0)`** — nothing has bumped yet. 18
  types currently carry it: `Level`, `LevelMeta`, `UserSettings`, `Prefab`, `EffectData`, `ThemeData`,
  `CompositeShape`, `ClipboardData` (SDK-repo "core" tier); `PublishProfile` (`Publishing/`);
  `LevelSettings`, `GameLevel`, `AudioLevel`, `LevelResources`, `LevelHints` (nested under `Level`);
  `GameEvents`, `CameraEvents`, `PostProcessingEvents`, `PlayerEvents` (nested under `GameLevel`). `DataDomains.cs` is the `nameof()`-based constant list.
- `VersionedEnvelopeConverter` (`Serialization/Converters/`, not `Versions/`) writes/reads the
  `{"version": "major.minor", "value": ...}` wrapper, gated purely by `[DataVersion]` presence — a
  `_activeDomains` reentrancy guard lets member serialization fall through to plain fields while
  writing/reading that same domain's own payload, without special-casing "nested vs. top-level."
  On read, it always resolves + upgrades through `VersionedTypeRegistry` and returns the domain's
  **current-shape type**, never the historical snapshot type.
- `VersionedTypeRegistry` populates itself via a **one-time reflection scan in a static constructor**
  (same pattern as the Unity project's `ReflectionUtils.GetImplementations<T>()`), indexing both
  every `[DataVersion]` type and every `IMigration` implementation. `UpgradeToLatest` walks
  `IMigration` step by step from a deserialized instance's version to the domain's latest, throwing
  if a step is missing.
- **`V0_0` is a scaffold that exercises the machinery end-to-end, not real shipped format history** —
  its `Names` use placeholder JSON keys (`"test_settings"`, etc.) and its snapshot classes are
  structurally near-identical to current ones. There is no `V1_0` folder or class anywhere — "current"
  is just the live, un-suffixed model class carrying `[DataVersion(..., 1, 0)]` directly; migrator
  filenames like `LevelV0_0ToV1_0.cs` reference that live class by convention, not an actual file.
- Replaces an older `CompatibilityService`/`SaveData<T>`/`JsonConverterData<T>` design — those names
  are fully gone from the codebase (only survive in a comment explaining what replaced them); don't
  reintroduce or reference them as if live.
- **Dangling cross-reference**: both `Versions/README.md` and this SDK's `TODO.md` say "see
  `VERSION-UPDATE.md` at the SDK root" — that file does not exist. Likely renamed to
  `Versions/README.md` without updating the pointer, or lost; don't try to find it.
- Open per the SDK's own `TODO.md`: nested/optional aggregates below the current per-domain split,
  the first *real* migrator once a domain actually needs to bump past 1.0, Project Arrhythmya import.

## Rules & validation

`Rules/` classes are mostly pure `public const` numeric tables with zero `Models/` dependency
(`FrameRules`, `ValueRules`, `LevelRules`, `AudioRules`, `PostProcessingRules`, `ResourceRules`,
`TextRules`) — `EffectRules` is the one exception, constructing default `CurveValue`/`GradientValue`
model instances. `RuleGroup` (`None/Error/Warning/Advice`) is a severity enum that exists but is
**never actually set away from its `Error` default** by any current rule attribute.

**`ValueRules.MaxShapeTriangles` is 128, and it was 64 until the game's own shapes outgrew it** —
an inverted 32-sided ring is the box's rim, the ring's outer rim and its inner disc, which is 94
triangles, and six more built-in shapes sat at exactly 64 with no room at all. Raising a cap can
invalidate nothing (it only lets a hand-written file carry more than it could before), and
`MaxShapeVertices` is derived from it rather than restated.

**`CompositeShape`'s geometry carries no per-property collection rule beyond `[RuleNotNull]`, and
that is deliberate.** Every generic collection fix is index-destructive on indexed geometry:
`RuleCollectionNoNullItems` would *remove* a null vertex and shift every index after it onto the
wrong point, `RuleCollectionMaxCount` would truncate the vertex list out from under the triangles
still referencing its tail. Both look local and corrupt the shape silently. The class-level
`RuleShapeGeometry` owns all of it instead — only a rule seeing both lists can fix one without
breaking the other. Don't "helpfully" add a collection rule to `Vertices`/`Indices`.

`RuleEnumValid` covers single-choice enums only; `[Flags]` enums (today: `ContentDescriptor` on
`LevelMeta`) go through `RuleEnumFlagsValid`, which asks "does this carry an undeclared bit" and
whose `Fix` masks the unknown bits off instead of falling back to a default. Don't loosen
`RuleEnumValid` to cover both — `Enum.IsDefined` rejects every legitimate flag combination.

`Rules/Attributes/` are declarative `[RuleXxx]` property attributes (`[AttributeUsage(Property)]`
only — never fields), all `: BaseRuleAttribute` (`IsValidType`/`IsValid`/`Fix`). `[RuleContainer]`
(a bare class-level marker) opts a type into the reflective walk — applied broadly across `Models/`
(156+ files), not just a handful of aggregate roots. `Rules/Attributes/Contextual/` need the root
`Level` as context (`RuleLevelFrameAttribute` checks against `Level.Settings.FrameDuration`,
`RuleObjectIdValidAttribute`/`RuleParentObjectIdValidAttribute` check `ObjectId` validity/parent
rules) — both still carry a `// TODO add complex check for parenting and ids uniqueness`, because a
property attribute only ever sees one property at a time. **Cross-object invariants are implemented,
just not here** — `Validations/LevelGraphAnalyzer` owns them (duplicate `ObjectId`s, missing or
cyclic parents, dangling/self-referencing prefab placements, stale id counters, broken remap tables),
and `ValidationFacade` is what runs the two passes together. Don't write a graph check as a
`[RuleXxx]` attribute. `Rules/Attributes/Values/` are
typed against the polymorphic `IFloat`/`IVector2-4`/`IString`/`IPrimitiveInt`/`IPrimitiveGuid`
interfaces, switching per concrete variant to check/clamp.

`Validations/RuleAnalyzer` walks any `[RuleContainer]`-typed object graph (generic over the root, not
hardcoded to `Level`) and returns `List<RuleIssue>`; `RuleFixer` applies fixes **in reverse trace
order** deliberately (fixing may invalidate/shift deeper issues). `RuleIssue`/`RulePath` carry the
full trace from root to the failing property (including list index / dict key) so a fix knows exactly
where to write. **The analyzer logs nothing** — an issue used to go to the console the moment it was
found, on top of whatever the caller did with the returned list, so a level breaking one rule on
every object paid for each finding twice in Editor stack traces; what to do about a report is
`ValidationFacade`'s caller's policy.

**The walk is on a level's load path**, so its per-node cost is that level's load time: the Unity
project's `LevelLoaderService.LoadLevel` runs `ValidationFacade.Validate` on every level it reads
(reporting only — it never refuses or repairs). Two things it therefore must not do per node, both
removed after measuring ~1.3 s on a 4.7k-object level: query `[RuleContainer]` uncached (a Mono
custom-attribute lookup allocates a fresh attribute instance every call), and read a property whose
value can lead nowhere. `RuleContainerAttribute` is `AttributeTargets.Class`, so a value type — or a
collection of value types, since the walk only descends into items/values — is a proven dead end and
is never fetched at all. `Tests/RuleAnalyzerPerformanceTests` pins the result.

`Roslyn/RuleContainerAnalyzer.cs` (separate `BulletHeroSDK.Roslyn` asmdef, `#if BHSDK_ROSLYN`-gated,
so inert inside the Unity project) enforces at compile time that every `[RuleContainer]` class is
non-static, non-abstract, and has a public parameterless constructor — because several `Fix*` paths
(`RuleNotNullAttribute`, the `RuleIPrimitiveXxx` family) call `Activator.CreateInstance` on property
*types* at runtime, and a violation here would otherwise only surface as a rare, hard-to-place
`MissingMethodException` deep inside an editor "auto-fix my level" flow.

## Conventions

- **`Names.cs` constants, never string literals, for `[JsonProperty]`.** Deliberately short/
  abbreviated (`"f"` for Frame, `"v"` for Value, `"t"` reused for both TypeShort and TimeShort —
  disambiguated only by never co-occurring on the same model) to keep the wire format compact.
  Historical `Versions/VX_Y/` snapshot classes intentionally use their own frozen literals/`NamesVX_Y`
  instead, so renaming a *current* key can never silently corrupt what an old snapshot deserializes.
- **New polymorphic value/effect variant** = new enum case + concrete class + a case in the matching
  `JsonConverterCustomType<T,TType>` subclass's `GetType`/`GetCustomType` switch. No attribute-based
  auto-discovery anywhere in this system (unlike `[RuleContainer]`'s reflection scan or
  `VersionedTypeRegistry`'s reflection scan) — every converter is a manual, explicit mapping.
  Same applies to `RectObject` subtypes via `ObjectConverter`.
- **New `[DataVersion]` aggregate** = add the attribute at `(1, 0)` if genuinely new, or bump + write
  a `VX_Y/` snapshot + `IMigration` pair if changing an existing domain's shape — see `Versions/
  README.md`'s folder-convention rules in full before doing this, several easy-to-miss subtleties
  (nested property must stay typed as the *current* class, snapshot classes skip `IModel<T>`, a
  domain with no independent envelope yet at some generation gets a snapshot with no `[DataVersion]`
  at all).
- **`ValueRules`' layer constants define reserved draw-order bands, not just a clamp.** Authored
  content is capped to `[MinLayer, MaxLayer]` = `[-1000, 1000]`; everything above that is reserved
  for the Unity project's editor-only overlays (`MinLayerSelection` = `MaxLayer + MinLayerDelta` for
  selection outlines, `MinLayerGrid` = 1250 for the viewport grid, `MinLayerColliders` = 1400 for the
  collider fills, `MinLayerGizmos` = 1500 for viewport gizmo handles, each overlay piece
  stepping by `MinLayerDelta` so same-set pieces never z-fight — see `Services.GameEditor`'s
  `LayerPolicy`), and `[MinCameraLayer, MaxCameraLayer]` bounds the camera itself. Don't widen the
  authored range without moving those bands too. Layer is **parent-relative** (a child's effective
  layer is the sum up its parent chain), so these are limits on one object's own contribution.
  `LayerZOffsetStep` (0.001) × `LayerZOffsetCount` (512) is a **different** concern living next to
  them: the depth tie-break the consumer applies to coplanar *opaque* shapes so they can't z-fight
  (`GamePlayer`'s `BuildInstancesParentingJob`). Their product must stay below 1.0, since that is
  how far apart the layer coefficient spaces two layers — don't merge the two families of constant.
- **Level-wide/track-wide numeric collection caps live in `Rules/LevelRules.cs`/`AudioRules.cs`**
  (max markers/checkpoints/keys/prefabs/audio-layers/...) — check there before hardcoding a magic
  cap elsewhere; `Level.Objects` itself is deliberately uncapped (see the commented-out
  `MaxObjects` in `LevelRules.cs`).
- **`IModel<T>`'s `Equals(object obj) => obj is T value && Equals(value);` boilerplate is hand-written
  per class, not generated** — when adding a new `IModel<T>` type, double-check the `is T` matches
  the enclosing class exactly. This exact one-line pattern is the single easiest place to introduce a
  silent bug in this codebase: it looks identical across every model, so pasting it from a sibling
  class without updating the type name compiles fine and just makes `Equals(object)` always return
  `false` for real instances (the strongly-typed `Equals(T)` overload keeps working, so the bug only
  shows up wherever something compares through the non-generic `object.Equals`/`==` boxed path).

## Testing

`Tests/` (`BulletHeroSDK.Tests.asmdef`, NUnit): `MockData.cs` is the shared fixture builder (moved out
of `SerializationTests` specifically so every test file can reuse it) — `CreateTestXxx`/
`CreateValidXxx` factories deliberately touch as much field surface as possible while staying
rule-valid; `CreateInvalidXxx` factories are deliberately minimal, each encoding exactly one rule
violation `RuleFixer` must detect and fix. A `#region Version v0.0` half builds equivalents against
`V0_0` snapshot types for exercising the migration path, including a hand-spliced JSON envelope
builder (`CreateTestLevelV0_0Json`) — needed because `VersionedEnvelopeConverter` always tags a
*whole* current-shape object with the *current* version when serializing, so each historical fragment
has to be serialized standalone from its own real `VX_Y` type and spliced in by hand.

**Every test method carries three attributes**, no exceptions — `[Author(Metadata.Author.Vertoker)]`,
`[Category(Metadata.Category.Self)]` (`"BH.SDK"`, this namespace minus its `.Tests` suffix), and
exactly one difficulty out of `Metadata.Category.VeryEasy`/`Easy`/`Normal`/`Hard`/`Extreme`. They sit
between the `[Test]`/`[TestCase]` attribute(s) and the signature. `Metadata.cs` holds the constants;
their string values are ordinal-prefixed (`"1_very_easy"` … `"5_extreme"`) so a category dropdown
sorts cheapest-first. The consuming Unity project states this as a hard rule and applies the same
convention to its own test assemblies — see its root `CLAUDE.md`.

`ModificationTests` covers only `ModificationService`'s path resolution (`TestGet`/`TestSet`/
`TestJToken`, against two local throwaway models) — **not** `Modification.Value`'s long/double
normalization, nor a `PrefabObject.Modifications` round trip through
`DictionaryModificationsConverter`; both are worth adding, since a path that fails to resolve
degrades to "the override silently doesn't apply" rather than throwing.

Per the SDK's own `TODO.md`: `SerializationService`'s stability across all keyframe/value/effect type
combinations (round-trip + real saved level files) is explicitly flagged as not yet fully verified,
especially after the `IRequiresDefaultSerializer` refactor — treat serialization-adjacent changes
here as higher-risk than they might look.
