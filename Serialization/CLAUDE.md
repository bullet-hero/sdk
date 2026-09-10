# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Serialization

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Serialization/

`Serializers/` (`SerializationService`, the JSON/BSON entry point),
  `Converters/Base/` + `Converters/CustomTypes/` + `Converters/Dict/` (the polymorphism/id/dictionary
  JsonConverters — see "Serialization pipeline" below).

## Serialization pipeline (`SerializationService`)

**A level has two formats and `.blob` is the fast one.** `level.json` stays the readable, diffable,
portable one and is what the project's longevity promise is about; `level.blob` is the same data
through the generated codec. Measured on the real corpus rather than estimated — volcano (15.7 MB,
19 341 objects) reads in **203 ms against JSON's 9 946 ms**, writes in 147 ms against 9.6 s, and
occupies a third of the bytes, round-tripping `Level.Equals`-identical.

The `.json` path is generated too, and its acceptance test is that the format did not change by one
byte. Reading one is six times faster (11 249 ms to 1 837 ms); writing is unchanged, because
reflection dominates reading while writing is the text writer, which both paths do identically.

**BSON is gone** (it was never the fast format — 5% faster to read, 30% larger to write) and so is
the level cache it stood in for: a side-car has to decide when it is stale, while a format cannot
disagree with the file because it IS the file.

Two facts about the generated validation walk, both measured rather than assumed: **validation runs
on the EDITOR's load path only** (`Core`'s `LevelLoaderService` defaults `LoadLevel` to
`LevelValidation.Skip` and `LoadLevelProtected` to `Report`), and the speedup is **1.9x, not the
order of magnitude `.blob` bought** — three quarters of it came from hoisting `rule.IsValidType` off
the per-node path rather than from generating anything.

**The Roslyn half is compiled separately and must be rebuilt by hand.** Unity loads the analyzers and
generators only as the built `.dll` in the SDK root; editing a source under `Roslyn/` and refreshing
changes nothing. `Tools/BH.SDK.Roslyn/Build Analyzer` does.
`Assets/Plugins/BulletHeroSDK/Roslyn/README.md` is the record.

`SerializationService.SerializeData<T>`/`DeserializeData<T>` are the plain string-JSON entry points —
both throw `ArgumentException` if `T` has no `[ModelGeneration]` (only aggregate roots may go through
this API). They take no mode: **text is always compact JSON**, and which FORMAT a file is written in
is `SerializeEnvelope`'s question, since only bytes can answer it. `GetDataSerializer(type)` returns
an `IDataSerializer` (`SerializeEnvelope`/`DeserializeEnvelope` over raw `byte[]` + `EnvelopeData` —
version tag + untyped payload): `JsonDataSerializer` on `BaseNewtonsoftDataSerializer`, and
`BlobDataSerializer`, which shares none of that machinery because it goes through no Newtonsoft at
all. `SerializationType` is `byte`-backed and holds **two** members, `Json = 0` and `Blob = 3`;
`SerializationTypeExtensions.ToFileExtension`/`TryFromFileExtension` map `.json`/`.blob`.

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

**BSON IS GONE, AND `.blob` IS WHAT IT WAS PRETENDING TO BE.** BSON's role here was speed and it
never delivered any: on a 4.7k-object level it read ~5% faster than JSON while writing a file ~30%
*larger*, because what dominates is Newtonsoft binding members by reflection and both formats pay
that identically. `.blob` removes the reflection instead of re-encoding around it - the codec is
generated onto the models themselves - and the difference is not incremental. Measured on the real
corpus, three passes, minimum:

| level | `level.json` | read | `.blob` | read | speedup | size |
|---|---:|---:|---:|---:|---:|---:|
| smoke | 2 KB | 1 ms | | 0 ms | 28x | 0.41x |
| **volcano** | **15.7 MB** | **9 946 ms** | **5.1 MB** | **203 ms** | **49x** | **0.32x** |
| weathergirl | 3.2 MB | 2 176 ms | 1.1 MB | 39 ms | 55x | 0.33x |

Writing volcano went 9.6 s to 147 ms. All three round-trip `Level.Equals`-identical.

The enum member `Bson = 1` is RETIRED, not reused: an old `settings.json` holding 1 lands on an
undefined value that `RuleEnumValid` repairs to `Json`, rather than silently meaning something new.
`Blob = 3` is appended, which is why `SerializationModeUtils`' dropdown index is no longer the
enum's own number - order is what that class promises, and equality was a coincidence of the
numbers happening to be 0, 1, 2.

**`JsonPretty` WAS MEMBER 2 AND IS GONE, and there is no `Formatting` question left anywhere.** It
wrote the same document as `Json` with indentation, shared its `.json` extension and was read by the
same reader — so nothing could ever recover the choice from a file, which made it a property of
whoever happened to save rather than of the level. What it was FOR is reading a level file by eye,
and that is what an editor's own formatter does, on demand, without a second shape of the format
existing to be tested, migrated and explained. Every JSON document this project writes is now
compact, `ToFormatting` is deleted rather than moved, and `SerializationSettings` carries no
`formatting` field (it applied to the one shared `JsonSerializer`, so one screen's "write this
readable" re-indented every file written afterwards). Number 2 is retired like number 1 and is never
reissued — and the reason nothing may test `== Bson`-style two-branch ternaries survives it: a third
member reads as `Json` silently, which is what the Unity project's `Core/Utils/SerializationModeUtils`
exists to prevent at the five UI call sites that did.

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

## The JSON codec (`Serialization/Json/`)

The same models write and read their own JSON now. **The format did not change by one byte** — that
is the whole acceptance test, and it is checked against the real corpus, volcano included, by
`Core.Tests`' `CorpusLoadCostTests.EveryCorpusLevel_IsWrittenAndReadIdenticallyByBothJsonPaths` and
by `JsonParityTests` here. What changed is the cost of READING one: volcano goes 11 249 ms to
1 837 ms, six times faster, because nothing binds members by reflection any more.

**Writing did not move** (924 ms against 921 on volcano), and that is worth knowing before anyone
tries: reflection dominates reading, while writing is `JsonTextWriter` turning numbers into text,
which both paths do identically.

- **`SerializationSettings.useGeneratedCodecs`** is the switch, on by default, and it exists for
  exactly one caller: the parity test, through `SerializationService.CreateWithoutGeneratedCodecs()`.
  Anything that changes how a level is READ is locked by a test comparing the same bytes through
  both paths — the rule a withdrawn reader bought this project after it passed 4 494 tests and
  shipped a game that could not open a level.
- **`GeneratedModelConverter` goes LAST in the converter list**, and that placement is the whole of
  its wiring: the router takes the first match by runtime type, so a `Vector2Value` still reaches
  `Vector2Converter` and still comes out `[0,{...}]`, and this one writes the payload inside it.
- **The top-level `{version, value}` envelope is still `VersionedEnvelopeConverter`'s**, because it
  is the only thing that resolves an OLD version to its snapshot type and walks the migrations. A
  versioned MEMBER is wrapped by whoever holds it instead.
- **`[GenerateModelKeyed]`** names the property a dictionary's key is recovered from, which is what
  lets the collection write as a bare array of values. It records the same knowledge the twelve
  `DictionaryAsListConverter` subclasses hold; those stay, because they are the reflective path the
  parity test compares against.
- The traps this reproduces are all in `docs/issues/MODEL_CODEGEN_HISTORY.md` §5, and every one of
  them is a way the format is not what it looks like: the contract is OptOut in practice so
  `Resource.Type` IS written, members are ordered derived-first, an abstract property is written by
  its override, `[JsonIgnore]` is inherited, and a CONCRETE member of a value family is still tagged.

## The binary format (`Serialization/Blob/`)

`.blob` is a first-class level format, equal to `.json` rather than a cache beside it - a level may
be saved only as `.blob`, and `LevelPackageBuilder` carries it inside a package the same way. What
`.json` keeps is the promise the project actually makes about it: readable, diffable, openable by
somebody else's tool in ten years. `.blob` deliberately does not make that promise, and it is the
default nowhere.

- **`BlobWriter`/`BlobReader` are ref structs over a byte buffer**, not `Stream` wrappers: writing a
  value is a bounds check and a store, with no allocation and no virtual call. Passed by `ref`
  because the writer's buffer grows.
- **The encoding is the level cache's**, which was written out by hand for this same model and
  proved the shape works: little-endian, fixed width, no varints; a length prefix of `-1` for null
  (an empty keyframe list and a missing one are different states and a round trip has to keep them
  apart); a one-byte tag for a polymorphic value with `0xFF` reserved for null. **The tag is the
  model's own `GetModelType()`** - the generator reads the enum member that method names - so the
  blob and the JSON `[tag, payload]` carry the same discriminator and cannot drift apart.
- **Every `[ModelGeneration]` aggregate writes its own envelope**: domain as text, `major`, `minor`, and
  a byte length the reader checks the content against. The version tags are written so that the day
  a domain bumps there is somewhere to attach a migration; today an unknown version is REFUSED,
  which is honest rather than a gap - no build has ever written a `.blob`, so none of an older
  generation can exist, and the `.json` beside it is the recovery path.
- **The file header is checked in one order and nothing is allocated before it passes**: magic,
  codec generation, declared length against the real one, then an xxHash64 of the payload. Four
  distinct refusals, because "this file is damaged" and "this file is from a newer build" ask
  completely different things of a player. The hash is non-cryptographic on purpose - what is being
  checked is corruption, and forgery is already answered by the OpenPGP layer that protects a level.
- **A count the FILE chose is refused before it is believed** (`BlobReader.ReadCount`). That is the
  one attack surface a format has and a cache did not: its payload was self-produced.
- `BlobPrimitives` holds the four structs the generator cannot write for itself - `FrameSpan`
  (packed, so writing its fields would write the packing), `ModificationKey` and `RunProfile`
  (readonly, get-only), `Pixel` (four bytes seen as one int, on image-sized arrays).

## Model versioning (`Versions/`)

**`Assets/Plugins/BulletHeroSDK/Docs/VERSIONING.md` is the design record and `Versions/README.md` the folder convention**
(generation-first, e.g. `V0/` + `V0/Migrations/`, plus the "nested envelope always resolves to the
domain's *current* type" rule). Read those first; this section only adds what they don't cover.

- **A GENERATION IS ONE INTEGER, NOT `major.minor`.** `[ModelGeneration(domain, generation)]` marks
  an aggregate-root boundary that gets its own envelope and migrates as one unit. The second number
  never had anything to say — a shape change either needs a migration or does not, and there is no
  intermediate grade a minor could express; what it did instead was invite a bump nobody migrated.
  **20 types carry the attribute and every one of them is at generation 1
  (`ModelGenerations.Release`).** Two had bumped and both were put back when their snapshots were
  deleted - the game is pre-release, so the format changes in place and nothing migrates; root
  `CLAUDE.md` Rule 11 is the record. The twenty: `Level`, `LevelMeta`,
  `UserSettings`, `Prefab`, `EffectData`, `ThemeData`, `CompositeShape`, `ClipboardData` (SDK-repo
  "core" tier); `PublishProfile` (`Publishing/`); `GameStatistics`, `LevelStatistics`
  (`Models/Statistics/`, two roots rather than one — see that section); `LevelSettings`, `GameLevel`,
  `AudioLevel`, `LevelResources`, `LevelHints` (nested under `Level`); `GameEvents`, `CameraEvents`,
  `PostProcessingEvents`, `PlayerEvents` (nested under `GameLevel`). `ModelDomains.cs` is the
  `nameof()`-based constant list; `ModelGenerations.cs` names the generations themselves, so no
  attribute carries a bare digit.
- **`ModelGenerations.Invalid` is `-1`, and everything negative is equally invalid.** Zero cannot be
  the sentinel: the frozen snapshots under `Versions/V0` are written at generation 0, so a reader
  treating zero as "no generation" would refuse exactly the files the migration path exists for.
  Every reader therefore asks "did we read one" with a flag rather than by comparing the value.
- `VersionedEnvelopeConverter` (`Serialization/Converters/`, not `Versions/`) writes/reads the
  `{"g": <int>, "v": ...}` wrapper, gated purely by `[ModelGeneration]` presence — a
  `_activeDomains` reentrancy guard lets member serialization fall through to plain fields while
  writing/reading that same domain's own payload, without special-casing "nested vs. top-level."
  On read, it always resolves + upgrades through `VersionedTypeRegistry` and returns the domain's
  **current-shape type**, never the historical snapshot type.
- **The envelope's key is `Names.Generation` (`"g"`), never `Names.Version` (`"vrs"`).** That second
  one is the AUTHOR's version of a level (`LevelMeta`/`BestRun`/`LevelStatistics`), a `System.Version`
  written as a string. While the envelope shared it, one `metadata.json` carried `"vrs"` twice at two
  depths meaning two different things - so nothing may rewrite that key mechanically.
- `VersionedTypeRegistry` populates itself via a **one-time reflection scan in a static constructor**
  (same pattern as the Unity project's `ReflectionUtils.GetImplementations<T>()`), indexing both
  every `[ModelGeneration]` type and every `IMigration` implementation. `UpgradeToLatest` walks
  `IMigration` step by step from a deserialized instance's generation to the domain's latest,
  throwing if a step is missing. `VersionedTypeRegistryTests` is its own fixture.
- **`V0` is a scaffold that exercises the machinery end-to-end, not real shipped format history** —
  its `Names` use placeholder JSON keys (`"test_settings"`, etc.) and its snapshot classes are
  structurally near-identical to current ones. **It is kept for exactly that reason**: the only two
  real snapshots this repo ever had were deleted along with the bumps that needed them, so this is
  the ONLY thing that still proves `VersionedTypeRegistry` and `IMigration` work at all, and they
  have to work the day the game ships. "Current" is the live, un-suffixed class carrying
  `[ModelGeneration(..., ModelGenerations.Release)]` directly, and a migrator filename like
  `LevelV0ToV1.cs` names that live class by convention rather than an actual file.
- **A NESTED DOMAIN IS REFUSED AT ANOTHER GENERATION, NOT MIGRATED.** The top-level envelope
  migrates (that is `VersionedEnvelopeConverter`'s job); `IJsonModel.ReadEnveloped<T>`, which the
  generated codec uses for every nested domain, checks the generation and throws on anything else -
  a generated codec reads only ITSELF, holds no `JsonSerializer`, and a snapshot is not a generated
  model. It used to `Skip()` the tag instead, which read an old payload by property name into today's
  class and returned CONSTRUCTOR DEFAULTS silently: a nested `LevelSettings` at generation 0 came back
  `fps=60` through the generated codec and `fps=61` through the reflective one, so
  `useGeneratedCodecs` - a switch that must change nothing - changed the answer, and the parity tests
  could not see it with the whole corpus at one generation. `.blob` always refused this case, so the
  two formats agree now. Migration for nested domains is still open by decision, and waits for a real
  second snapshot rather than being built against the `V0` scaffold - `Assets/Plugins/BulletHeroSDK/Docs/VERSIONING.md` carries the
  three options.
- Replaces an older `CompatibilityService`/`SaveData<T>`/`JsonConverterData<T>` design — those names
  are fully gone from the codebase (only survive in a comment explaining what replaced them); don't
  reintroduce or reference them as if live.
- Open per the SDK's own `TODO.md`: nested/optional aggregates below the current per-domain split,
  the first *real* migrator once a domain actually needs to bump past generation 1, Project
  Arrhythmya import.
