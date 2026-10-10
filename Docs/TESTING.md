# The SDK test suite: why the fixtures are shaped the way they are

Contributor reference for `Tests/`. The folder map and the three mandatory attributes are in
`Tests/CLAUDE.md`; a test file keeps one line on why a case exists and the reasoning lives here.

## Async tests and the main-thread deadlock

- **NUnit's `Assert.ThrowsAsync` freezes the Unity Editor** at zero CPU, silently, and the run has to be
  killed from Task Manager (unsaved work lost; it happened twice). It blocks the calling thread on the
  task; EditMode tests run on the main thread, the only thread Unity's `SynchronizationContext` posts
  continuations to, so the moment the awaited work really suspends (`GZipStream.WriteAsync`, a tar entry
  read) its continuation is queued for a thread waiting on it. An early round of tests survived only
  because the exceptions were thrown before the first await. Await the failure instead
  (`AsyncAssert`); every caller is an `async Task` test.
- **`AsyncAssert.WithTimeout` is not a cure for that deadlock**: a timeout is itself a continuation and
  a blocked main thread swallows it. It catches the OTHER hang, an await on something that never
  completes (a completion source nobody sets). It uses `Task.WhenAny`, not a `CancellationToken`: the
  work worth guarding is the work that stopped honouring tokens. A lost race is reported and the task
  abandoned (it cannot be killed).
- **`AsyncDisciplineTests` is the only thing that prevents the blocking kind**, by reading test sources
  for `Assert.ThrowsAsync`, `.Result`, `.Wait()`, `.GetAwaiter().GetResult()`. It is a test, not a
  Roslyn analyzer, because the analyzer assembly is `#if BHSDK_ROSLYN`-gated and compiles to nothing in
  this project, and a test runs in every suite. Scope is `Tests/` folders only (`FileLoaderService`
  blocks inside a thread-pool delegate on purpose). Its roots are project-relative: under `dotnet test`
  (cwd `Tests/bin~/Debug`) none existed, a missing root is skipped, and the check passed by examining
  nothing, so the count of scanned files is asserted. A folder called `Tests` with no `.cs` (the two
  under `Assets/Code/Architecture` holding AssemblyBuilder descriptors) is not a test folder.

## Reading and writing: parity is the proof

- **A withdrawn reader bought the rule**: anything that changes how a level is READ is locked by a test
  comparing the SAME BYTES through the old path and the new one. The change that shipped a game unable
  to open a level passed 4 494 tests, all self-consistent, because a reader that writes and reads its own
  mistakes agrees with itself. `JsonParityTests` compares WRITING byte for byte against the reflective
  path and READING through `Level.Equals` on bytes neither path produced.
- **A degraded read is bytes neither path would write**, which the corpus cannot supply (every corpus
  file sits at one generation with no unknown tag). That is how a real divergence survived: the same
  nested `LevelSettings` read back fps=60 one way and fps=61 the other. Both stacks must degrade to the
  same model and report the same substitutions; the report is the stronger half, since two readers can
  reach identical defaults for opposite reasons. Retagging every keyframe value (`"v":[0,`, the family's
  lowest tag, `[tag, payload]` form) to a number nothing claims leaves the payloads in place, so the
  model is identical and the report is the only evidence anything happened.
- **The id-remap table** is the one member whose key a reader cannot recover from the value, so it is
  an array of `{k,v}` pairs; the generated codec and the reflective converter once disagreed about the
  CASE of those two letters, which no self-consistent test can see.
- **`BlobCodecTests`**: shapes are the level cache's (round trip, truncation sweep, wrong generation,
  garbage) plus what a FORMAT answers for: a payload damaged mid-way with a plausible header, and a
  count the file chose (`new List<T>(count)`; the reader compares a count against the bytes left, so it
  costs an exception, not a gigabyte). The round trip proves correctness through `Level.Equals`, not
  bytes. A future is fabricated from FILES, never from types (a type the SDK knows is not a future
  type); the generation offset is found by reading the header back. Damage one level down opens instead
  of throwing: recovery happens at the ENCLOSING root (a reader past its declared end lost its place,
  the root above knows where its content ends); here that root is `Level`, which comes back nearly
  empty - the guarantee working. A positional format cannot skip one value it does not understand, only
  the root that carried it; an unknown tag therefore needs no framing, because rule two (a new variant
  bumps the generation) means it only arrives inside a root whose generation already moved. XxHash64 is
  pinned to its published vectors (empty input, "abc" at seed 0) so another implementation reads the
  same file.
- **Fabricated future numbers** (`MockData.FabricatedGeneration`, the fabricated tag) are derived, not
  literal: a literal 2 turns every forward fixture into a backward one the day a domain takes it, and
  the tag is the highest a family may legally reach (one byte, 0xFF is null, the generator refuses above
  0xFE) so an `int` that big is not truncated by the reflective path only (a divergence the parity
  fixtures exist to catch). `ModelGenerationsTests` guards both and decides LIVE versus FROZEN by
  namespace, not by number. `MockData.CreateTestLevelV0Json` serializes each nested domain standalone
  from its `V0` snapshot and splices it in, because `VersionedEnvelopeConverter` tags a whole current
  object with the current generation (the "gotcha" in `VERSIONING.md`).
- `EnvelopeShapeTests`: the envelope is the one part no model owns; two one-character keys, generation a
  NUMBER. Zero is read with a presence flag, since frozen snapshots are generation 0 and a reader
  treating zero as absent would refuse exactly the files migration exists for. A nested domain at
  another generation reports instead of reading silently into today's class (see
  `Docs/Issues/FORWARD_COMPATIBILITY_HISTORY.md`).

## Models: sweeps over the contract

- **`ModelContractTests`** sweeps all implementations (180-odd): `Update` leaves nothing aliased to its
  source, `Pull` keeps every nested instance. Default-constructed pairs suffice, since it asks about
  references. It cannot catch a `Copy()` that loses a value field or shares a list, which is why models
  whose `Copy` rides an object initializer carry their own test.
- **`ModelResetContractTests`**: Reset means "back to what the constructor built", so the constructor is
  the single source of defaults (and a precondition of the generated `Reset`, which assigns from `new
  T()`). Collections compare by value (Reset clears in place, the constructor allocates). Exemptions are
  facts about C#: a struct has no parameterless constructor (`ModificationKey` normalizes `Path` to
  empty, better than the default null), and a non-deterministic member cannot be compared
  (`LevelMeta` draws `LevelId.NewId()`); such members are FOUND by building two fresh instances, which is
  also why the generated `Reset` builds a fresh instance rather than copying a shared prototype. The
  other half: a reset object is the SAME object (nested models and collections survive); polymorphic
  fields are the exception.
- **`ModelHierarchyTests`**: `Update`/`Pull` are overloads down a hierarchy while `Copy()` is virtual, so
  `Update` through a base reference writes the base half only. Polymorphic value interfaces' `Update`
  are no-ops on a sibling implementation; `ModelUtils.PullFrom` is the correct path. Abstract-`Copy`
  bases (`Resource`, `BaseGraphicsSettings`, `BaseDeviceControlsSettings`) reach their base half only via
  `base.Update/Pull`.
- **Additive defaults**: a settings group or field added without a generation bump works because an
  absent key leaves the constructor's value (Newtonsoft). That only works for a default that is read
  from the constructor; a zero-value default cannot prove the mechanism (use a neighbour that is present).
  Several settings fixtures pin exactly this (`AntiAliasing`, `Keybindings`, `Interface`, `Textures`,
  `DisplayGraphics`, `EffectsGraphics`, `LevelSettings.Seed/Orientation`, `LevelTrack` fader, the three
  `LevelMeta` browser fields, `TextureResource`). `LevelDuration` zero means UNKNOWN, never "instant".
  `MinGeneration`'s unknown cannot be zero (zero is a real generation). Options carried by an object
  initializer in `Copy()` rather than the constructor are the shape a copy-paste drops silently.
- **Settings defaults that are decisions**: `AudioSettings` UI starts at half (interface sounds are
  feedback on a press already made, music is what they came for) and `EditorUI` is a separate channel at
  half (the shell sounds every press, the editor only outcomes); EditorUI's key is absent from every
  older file, so the constructor answers. `DisplayGraphicsSettings` defaults are what the game already
  does (Windowed, native resolution, no scaling, vsync off); the window default went from FullScreenWindow
  to Windowed with `fullscreenMode` because the game may never put ITSELF full screen, so the fixture's
  authored value is FullScreenWindow (an authored value equal to the default proves nothing);
  `NativeResolution` is a zero SENTINEL read only via `HasResolution`. `EffectsGraphicsSettings` step
  budgets default to the shipped values, not zero (zero stalls every replay). `InterfaceSettings`:
  `OpenMenuOnLose` is FALSE (a lost run rewinds to the last checkpoint); the screen orientation default
  is Unlock because screens lay out from `.portrait` (a level's `LevelOrientation` still outranks it, so
  it frees only the menu, the browser and settings); the three HUD switches default TRUE with zero FALSE,
  so an older file shows the HUD, and ride a `Copy()` object initializer; the memory stats block was
  the one drawn whatever the file said. `GameEditorSettings` was restructured (sixteen flat properties to
  nine nested groups), so tests pin the NESTING (a `Copy` sharing a group instance, an `Equals` comparing
  references); the grid starts off (it draws behind every object), the effects preview ON, BotControl and
  the debug master off with every debug PART on (the parts exist to take pieces away), both expansion
  surfaces open on Partial but stay two settings.
- A settings enum's stored value is its number, so a rung may only ever be appended
  (`TexturesGraphicsSettings.SizeLimit`). `MenuBackgroundKind` has a zero of None and a default of Bot:
  an absent key is never written, so the constructor's Bot survives.
- `ObjectTypeMask`: `ToMask` shifts by the member's ordinal, so a kind added to `ObjectType` and
  forgotten lands on whatever bit sits there. `BotKind.None` MUST be zero: every other default
  (`LevelPlayerInfo` never filled in, a launch naming no bot) reads as "the player steers" through
  `default(BotKind)`. The two nested `iface` keys reuse a name across models that never co-occur
  (`Names.cs` allows it); `GameEditorSettingsTests` keeps that honest.

## Rules fixtures

- Each `[RuleXxx]` has its own file with throwaway `[RuleContainer]` fixtures, deliberately not
  `MockData`, so a failure names one rule. `ValidatorTests` over `MockData` is the integration layer.
  `RuleCoverageTests` keeps a hand-written registry (some files cover a family) so a rule without a test
  file fails; `RuleSeverityTests` makes each rule state its `Group` or be named in `IntentionallyError`
  (the `Group` criterion is in `RuleGroup.cs`). `RuleGroupFilterTests` keeps its Advice rule in the test
  assembly because `RuleCoverageTests` scans only the SDK assembly.
- `RuleWalkSeamTests` is the executable specification of what `ValidationGenerator` must emit: property
  order is `GetProperties` order (declaration order per level, derived-first across levels); phase A
  (all property rules) is total before phase B (descent); the descent is gated by `Check`'s return
  unless `analyzeAllRecursiveRules`; the rule array comes from reflection with reflection's own
  instances. Details: `VALIDATIONS.md`. `ValidationParityTests` states three small reports in full so a
  change of ORDER is read, not detected; the 4 750-object fixture is the only one big enough to exercise
  the pooled buffer, the boxed-index cache and a template scope rebase (count plus first and last line).
  Its twin with the real corpus is `BH.Core.Tests.ValidationParityTests`.
- `RuleContainerCoverageTests`: `[RuleContainer]` is INHERITED (`GetCustomAttribute<T>()` searches the
  base chain), so `Resource`, `BaseDeviceControlsSettings` and `BaseGraphicsSettings` opt whole families
  in; reading sources for the declared attribute wrongly reported `AudioGraphicsSettings` and
  `PostProcessingGraphicsSettings` as unvalidated. The same inheritance is the generator's blind spot
  (`ForAttributeWithMetadataName` matches declared attributes only), so every container must DECLARE it.
  The "every model has a generated walk" claim is scoped to the SDK assembly: the analyzer BHS1101
  ("must be partial") was withdrawn because it cannot tell a model from a test fixture, and the
  private nested containers here are deliberately non-partial (the only coverage of `RuleWalk.Node`'s
  reflective branch). **Measured, refuted: transitive pruning** (skip subtrees with no rules) buys
  almost nothing - `Keyframe.Frame` is `[RuleLevelFrame]`, `Keyframe.Ease` is `[RuleEnumValid]`, every
  value model is range-checked, so a least fixed point leaves a handful of leaves and no subtree
  (`Docs/Plans/VALIDATION_GENERATOR_PLAN.md` expected a second order of magnitude). Worth taking in the
  generator (a compiler-folded const), not in the reflective walk (the lookup costs what it saves).
- `RuleAnalyzerPoolTests` pin the rental contract (returned on success, on throw; the prewarmed count
  is an optimization): the misapplied-rule throw is by design and once leaked a buffer each, failing the
  whole folder past the prewarmed count, across runs (a static analyzer outlives one).
- `RulesConsistencyTests`: the limit tables are hundreds of hand-written constants, many Min/Max/Default
  triples copied from Unity audio and URP ranges; a transposed pair compiles and validates. Read
  reflectively. `ObjectIdCounterTests` pin that the counter is not the collection and that the fix does
  not clamp it down (see `RULES.md`, "MaxObjectIds").
- Gaps pinned as behaviour, not endorsed: the I* range rules check outer edges and leave ordering to
  `RulePropertyOrder`/the model's own bounds; a null inner `StringValue` throws in `RuleIStringMax`; a
  `Nullable<T>` with `RuleNotNull` reports forever (its Fix returns null); `RuleCollectionCount` pads
  reference types with nulls; `RuleIPrimitiveGuidNotNull`'s Fix mints a new Guid (so
  `ShapeObject.ColliderId` and `PrefabObject.PrefabId`, where Null is real, must never carry it); an
  open generic answers `IsList` TRUE (property types are always closed). The `IVector3` rules share one
  file because they are their `IVector2` siblings with a Z axis; none is on a live model yet.
- `ThemeColorNamesRuleTests`: `RuleWalk` returns on the optional check BEFORE the rule loop, so
  `[RuleOptional]` suppresses every rule on the property, not just the null safety net.

## Modifications

- `ModificationApplyTests` drive one field of each kind through `Apply`, in memory and after a round trip.
  They found that an int override never applied (`Modification.Value` widens integrals to `long` so an
  override equals itself after a round trip, and `Apply` guarded its write with `IsAssignableFrom`,
  false for int from long) and that an enum and an id stopped applying once serialized (they arrive as
  numbers). `ModificationValues.TryConvert` converts back through the serializer. A scalar whose
  DECLARED type is an interface is the shape the list cases miss (`TextObject.Text`). Values are taken
  out of a serialized-and-read-back `Modification`, never hand-built.
- `ModificationFieldsTests` checks the number table from both sides (the Roslyn compile-time check only
  sees members it is handed; a constant marking nothing is invisible to it); modelled on Core's
  `RandomTracksTests`. Reflection, not a hand-written list, because the list is what failed to notice.
  A marked member must be serialized, except a derived view (`[GenerateModelIgnore]`, e.g.
  `PrefabObject.PlacementDuration` over `Span.FrameDuration`: a key of its own would store the length
  twice) - which still owes a SETTER, since `ApplyModifications` writes through it.
- `ModificationCheckedWriteTests`: a per-instance override is the one write reaching a model with nothing
  to judge it; the checked write refuses, the PLAIN write still does not, which keeps opting in explicit.

## Generators

- `GeneratorSweepTests` loop over the registry (not `[TestCaseSource]`) so they run under a plain
  reflective runner; a new generator is covered on registration. The estimate is measured as ADDITIONS
  (ids reported created, plus the keys of new objects and the level-global camera tracks), since a
  Modifier legitimately adds nothing and removers go negative; it cannot see keys added to a pre-existing
  object (none does). The window bounds what a run CREATES, not what it touches. A keyframe's `Frame` is
  LOCAL to its object; every animated generator once shipped an absolute frame, invisible to every other
  check. A created level must be horizontal (`LevelSettings` constructors set it; an object initializer
  or a foreign file's numbers would opt into portrait); the importer is excluded (a file's own answer).
- `GeneratorRegistryTests`: unique keys, complete forms, every numeric field has a range (a host clamps
  against `Hints.Ranges`), no shadowed inherited field (`ShapeObjectsGenerator` once hid
  `SpawnParameters.Texture`). `TestGenerators` live in the test assembly because `GeneratorRegistry`
  scans only the SDK assembly. `GeneratorDeterminismTests`: `GeneratorRandom` is a hand-written
  xorshift32, not `System.Random`, so the same seed gives the same level across runtimes.
- `FramerateRemapGeneratorTests` reach every branch of `FindSlot` by name: 60 to 30 (frames 3 and 4
  both sample to 2), 60 to 10 (frames 0-2 to 0, no room below), 60 to 10 (frames 3-5 to 1, room below,
  backward nudge). Rounding is away from zero. Survivors are checked as "within the allowance of SOME
  original's sampled frame": rebuilding the mapping would re-implement the packer.
- `ModifierGeneratorsTests`: frames are OFFSETS from the object's first frame so a test says what a
  modifier does; a lifetime is clamped whole (a 91-frame object pushed past a 100-frame end lands at 9,
  still 91 long; clamping edges separately squashed it onto frame 99).
- `FontCacheGeneratorTests` clear `AppearingMask`: `TextObject`'s default is "X" and lands in every
  font's set (pinned by `Build_DefaultMaskIsIncluded`). `PrefabFlattenGeneratorTests`: the run reaches
  the LEVEL before the templates (a template edit propagates to placements).
  `AllowsEmptyRunTests`: a host disables a generator whose whole estimate is zero.

## Archives, crypto and the hostile-input fixtures

- **Hostile archives are built by hand** (`RawZip` for zip, raw 512-byte ustar headers for tar): a
  traversal or symlink entry is not something the writer produces, so asking it proves it refuses
  twice; a zip written by Explorer or `zip -r` is the import direction. `RawZip` is also the only way
  this assembly writes a zip: `BH.SDK.Tests.asmdef` sets `overrideReferences`, so SharpZipLib is not
  visible and a fixture on `ZipEntry` would compile under `dotnet test` and fail in Unity.
  `ZipServiceTests` mirror `TarGzServiceTests` case for case; the zip-only facts are a size that lies
  (the directory is a claim the archive makes about itself, so the limit is enforced twice: directory
  check, then a byte counter during the copy), two entries under one name, and an encrypted entry.
  A gzip bomb is a few kilobytes expanding to whatever the reader holds.
- **Zip determinism**: a zip written to a file and one written to a pipe must be the same archive or a
  backend cannot recognise a re-upload by digest; `ZipOutputStream` patches the header when it can seek
  and writes a data descriptor when it cannot, hence `NonSeekableWrite`.
- **Zip AES defect that shipped once**: SharpZipLib encodes a password in the machine's ANSI code page
  while AE-2 derives the key from UTF-8 bytes; a pack/unpack pair sharing the mistake round trips
  perfectly, so the archive opened only here. The verifier is recomputed from the spec (PBKDF2-HMAC-SHA1
  over the passphrase bytes and the archive's salt, 1000 iterations, two keys, then the two-byte
  verification value). An ASCII passphrase passes under either reading and proves nothing. AE-2 leaves
  entry NAMES readable, so an import preview can say "needs a password" rather than "unreadable".
- **PGP**: BouncyCastle offers a byte-per-char and a `...Utf8` overload of every passphrase call; ASCII
  agrees under both, so only a Cyrillic round trip stops the wrong overload (a Russian-speaking
  author's export would fail in gpg, reported as a wrong password). A wrong passphrase and a damaged
  file must be two answers (the MDC gives that). `S2KWorkBytes` is turned to its floor in tests (one test
  runs the shipped default); BouncyCastle's `itCount` is the coded octet, one of 256 rungs, and a
  request in bytes rounds UP (never less stretching than asked).
- `ArchiveFormatSnifferTests`: three magic numbers are exact and the OpenPGP packet tag is loose enough
  to match a byte from anything, so it is asked last. `ContentStoreTests` run one case set against
  `MemoryContentStore` and the directory store (the memory one backs every pipeline test); the escape
  case is load-bearing since "rooted by construction" is what the tar-slip defence rests on; the
  `ValueTask` bargain is that the memory store completes synchronously. `LevelArchiveBuilderTests` have
  four routes out of one field (packed; outside the folder COPIED IN with the level's key rewritten, the
  common case, since a level started around a song picked from disk has `UriType = AbsolutePath`; URL
  left alone; missing reported); the live model is untouched (an export is not an edit).
  `LevelArchiveRoundTripTests` are the server's path (no disk): protected ARCHIVE is opaque, protected
  FOLDER encrypts the level document alone and leaves metadata and cover readable; the seven export
  modes are three independent choices; blob inside a protected folder is `level.blob.gpg` and the probe
  tries both formats behind the encrypted name; a passphrase alone means AES for zip and OpenPGP for
  `.tar.gz`. A level document is read in two steps (deserialize, then expand prefab placements), as
  `LevelArchiveGenerator` and `LevelLoaderService` do. `LevelExportModeTests`: the number a mode carries
  never moves (the dropdown order may), and the three passworded archives sit together.
- `ShapeCatalogServiceTests`: an id means one thing forever (the old library numbered shapes by array
  position, so inserting a form renumbered the rest); every retired id (1..78) has form code 0, reserved
  and never issued; a shape plus its inverse tiles the sector it was cut from (area counted twice or not
  at all catches every triangulation bug); the catalog order is form-major so inverted shapes do not
  split a block; fitting by BOUNDS makes every form measure 1 across its longer axis.
  `ShapeGeometryUtilsTests`: the repair pipeline is order-dependent (clamping, welding, dropping and
  winding each undo another), and `GetCentroidPivot` weights by area to agree with the per-form one.

## Other fixtures

- **`AvatarRulesTests`**: the balance was re-read against Just Shapes & Beats', measured from its binary
  (its field is 1280x720 units against a 10-unit camera height, so its numbers divide by 72). Only the
  hitbox moved, 0.4 to 0.3; dash i-frames went 0.2 to 0.3 to sit on the cooldown (documented on
  `DashCooldown`). The player-facing statement is the avatar page at `bullethero.space` and the
  `avatar.*` keys of `DocsPublic/values.yaml`, the source of truth; a number moves there first. The test
  restates every value so a change fails with the old number visible. If one fails the question is not
  how to make it pass (see `RULES.md`, "Avatar balance"). The spawn ceiling spans two assemblies
  (`CheckpointService.RampSeconds` is not visible), so the relation is pinned with the number restated.
  There is no duration to assert for the vulnerability window any more: it was `DashCooldown -
  DashInvulnerabilityTime >= 0.1 s` (one frame at 10 fps; 0.05 s fell between two samples on a phone);
  with the two equal the guarantee is the call order of three types. The i-frames-vs-cooldown relation is
  an inequality in one direction only.
- **Performance tripwires** (`RuleAnalyzerPerformanceTests`, `SerializationPerformanceTests`): validation
  is the one full-graph pass on the editor load path, so the walk must stay linear in node count. The
  blob-vs-reflection claim is a RATIO (`MinimumBlobSpeedup` is 3, far below measured) because
  milliseconds describe the machine. It takes the MINIMUM of several passes: one sample failed about one
  run in four on a busy machine, always on the ratio. The measured fixture is sized off a real imported
  level (about 4.7k objects, 30k keyframes, 33 templates), deliberately rule-valid and monotonous.
- **`MockData`**: `CreateTest*`/`CreateValid*` touch much of the field surface and must stay rule-valid
  (`ValidatorTests` asserts zero issues on several); `CreateInvalid*` are minimal and left alone (broaden
  one and it may introduce an issue `RuleFixer` cannot clear). Prefab references resolve to templates
  that exist and id counters sit past the highest id (both are graph invariants only
  `LevelGraphAnalyzer` sees). Every remap table describes a real copy (a placement's children are rebuilt
  from it on every read). The invalid prefab fixtures carry real ids: an unset one is fixed to a fresh
  Guid or to `ObjectId` 1, which then disagrees with its key or collides, a different violation. The
  uniform variants of each family ride the factory so no tag is left unchecked by a whole-Level round trip.
- `FrameOriginTests` is the one fixture that pins where the timeline starts (the convention spans
  `FrameRules`, `FrameSpan`, `Keyframe` and `FrameSpanConverter`); every other fixture speaks in OFFSETS,
  so moving the origin is a handful of failures here. `FrameSpanTests` check packing through the public
  surface only; a start of -2147483548 on the wire would be unreadable to third parties; `-0` exists in
  neither JSON nor BSON, which the origin move made moot.
- `PostProcessingColorTests`: URP reads the fourth component of lift/gamma/gain and shadows/midtones/
  highlights as a signed OFFSET, not opacity (`PrepareLiftGammaGain` adds it to every channel,
  `PrepareShadowsMidtonesHighlights` weights it x4 when positive), so a model storing it as an alpha
  defaulting to 1 pushed a tonal band four stops up. The vignette colour is three channels because URP
  declares it `hdr: false`, `showAlpha: false`. Older payloads carrying a fourth property are dropped by
  Newtonsoft. SMH colours and limits are born absent and read back as the neutrals `PostProcessingRules` names.
- `InteropReportTests`: a report keeps a bounded list of paths per code; the COUNT can exceed the paths
  (same place hit repeatedly) and the PATHS can be cut (more distinct places than the list keeps).
- `ResourceReferenceUtilsTests`: a zero count lets a delete through unasked, so a MISSED reference is the
  expensive failure. `ResourceGraphTests` touch every reference field once with its own id.
- `PrefabVirtualizationTests`: `Thin` drops exactly what `Expand` can rebuild (the Unity project's
  `PrefabExpandParityTests` pins `Expand` against the real materializer); a level is built thin and
  expanded, the direction a file travels. A local frame is a frame: the copy sits on the placement's first
  frame, not one past (adding both positions counts the origin twice). The `Limits` hint is swept on the
  LIVE expanded level before the writer thins it (thinned under-reports by every copy, and
  `PlayerService.EnsureLevelCapacity` treats it as a lower bound). A nested template is expanded before
  the placement copying it; no corpus level has one, so it has never met real content.
  `PrefabFlattenUtilsTests`: `CollectChain` must reach a NESTED placement copy or `FindPlacements` reads
  it as a genuine placement and the next resync writes over host objects sharing those ids.
- `StatisticsSerializationTests`: `MemberSerialization.OptIn` does not filter in this project (the
  resolver sets it after `CreateProperties` ran), so a public member with neither `[JsonProperty]` nor
  `[JsonIgnore]` enters the format under its C# name forever. `SurrogateUtilsTests` enumerate every cut
  so the guarantee is independent of the ceiling. `RunProfileTests`: speed is an int (`ToCenti`)
  because a float key filed "1.00" and "1.00" under two records.
- `ControlsSettingsTests` walk the six hand-written device groups generically; `GeneralMode` is a cast
  between two enums that line up only by convention. `CryptographyTests` pin two shipped defects: a
  constant-seeded `System.Random` gave every installation the same salt and IV (so equal text encrypted to
  equal bytes), and a short input computed a negative array length. `AuthorTests` and
  `ResourceMeta*Tests` alias the model types because NUnit's `[Author]` makes the name ambiguous.
  `LicenseTests`: `NoSpecifiedLicense` stopped being fieldless when it gained `NoLicenseSourceType`, so
  its equality is "same origin", not "always true". `DimensionalIndexer2Tests` use a non-square grid (a
  square one hides a transposed formula) and compare enumerators to the arithmetic, not to a list.
  `TypeExtensionsTests` pin what `IsList` refuses (an `IList`, an array, a `List<T>` subclass), since the
  walks' failure mode is a rule silently never running. `VersionedTypeRegistryTests` give the registry a
  fixture of its own; the gap fallback answers an OLDER generation only (a newer one is refused first,
  `ThrowIfNewer`), reported, never silent. `SerializationReportTests`: the collector is silent on an
  ordinary read, present on a degraded one, free when nobody asked. `ColorNames` is the first member that
  is both a collection and legally null: an unnamed palette writes no key.
