# Serialization: the reasoning behind the codecs and converters

Contributor-facing. `Serialization/CLAUDE.md` carries the folder map and the traps, compressed, and
links here; `Docs/VERSIONING.md` carries the envelope and the migration rules. This file holds the
measured `.blob` / JSON costs, the entry points, the retired enum numbers, the design reasoning the code
used to carry in comments, and the verified negatives.

## Two formats, and what each costs

**A level has two formats and `.blob` is the fast one.** `level.json` stays the readable, diffable,
portable one and is what the project's longevity promise is about; `level.blob` is the same data
through the generated codec. `.blob` is a first-class level format, equal to `.json` rather than a
cache beside it - a level may be saved only as `.blob`, and `LevelArchiveBuilder` carries it inside an
archive the same way. What `.json` keeps is the promise the project actually makes about it: readable,
diffable, openable by somebody else's tool in ten years. `.blob` deliberately does not make that
promise, and it is the default nowhere.

`.blob` removes the reflection instead of re-encoding around it - the codec is generated onto the
models themselves - and the difference is not incremental. Measured on the real corpus, three passes,
minimum:

| level | `level.json` | read | `.blob` | read | speedup | size |
|---|---:|---:|---:|---:|---:|---:|
| smoke | 2 KB | 1 ms | | 0 ms | 28x | 0.41x |
| **volcano** | **15.7 MB** | **9 946 ms** | **5.1 MB** | **203 ms** | **49x** | **0.32x** |
| weathergirl | 3.2 MB | 2 176 ms | 1.1 MB | 39 ms | 55x | 0.33x |

Writing volcano went 9.6 s to 147 ms; it occupies a third of the bytes. All three round-trip
`Level.Equals`-identical.

**The `.json` path is generated too, and its acceptance test is that the format did not change by one
byte** - checked against the real corpus, volcano included, by `Core.Tests`'
`CorpusLoadCostTests.EveryCorpusLevel_IsWrittenAndReadIdenticallyByBothJsonPaths` and by
`JsonParityTests` here. What changed is the cost of READING one: volcano goes 11 249 ms to 1 837 ms,
six times faster, because nothing binds members by reflection any more. **Writing did not move** (924
ms against 921 on volcano), and that is worth knowing before anyone tries: reflection dominates
reading, while writing is `JsonTextWriter` turning numbers into text, which both paths do identically.

**BSON is gone, and `.blob` is what it was pretending to be.** BSON's role here was speed and it never
delivered any: on a 4.7k-object level it read ~5% faster than JSON while writing a file ~30% *larger*,
because what dominates is Newtonsoft binding members by reflection and both formats pay that
identically. The level cache it stood in for is gone too: a side-car has to decide when it is stale,
while a format cannot disagree with the file because it IS the file.

**Validation**, two facts measured rather than assumed: **it runs on the EDITOR's load path only**
(`Core`'s `LevelLoaderService` defaults `LoadLevel` to `LevelValidation.Skip` and `LoadLevelProtected`
to `Report`), and the generated validation walk's speedup is **1.9x, not the order of magnitude `.blob`
bought** - three quarters of it came from hoisting `rule.IsValidType` off the per-node path rather than
from generating anything.

## Entry points (`SerializationService`)

- `SerializationService.SerializeData<T>`/`DeserializeData<T>` are the plain string-JSON entry points -
  both throw `ArgumentException` if `T` has no `[ModelGeneration]` (only aggregate roots may go through
  this API). They take no mode: **text is always compact JSON**, and which FORMAT a file is written in
  is `SerializeEnvelope`'s question, since only bytes can answer it.
- `GetDataSerializer(type)` returns an `IDataSerializer` (`SerializeEnvelope`/`DeserializeEnvelope` over
  raw `byte[]` + `EnvelopeData` - version tag + untyped payload): `JsonDataSerializer` on
  `BaseNewtonsoftDataSerializer`, and `BlobDataSerializer`, which shares none of that machinery because
  it goes through no Newtonsoft at all.
- `SerializationType` is `byte`-backed and holds **two** members, `Json = 0` and `Blob = 3`;
  `SerializationTypeExtensions.ToFileExtension`/`TryFromFileExtension` map `.json`/`.blob`.
- Two `JsonSerializerSettings` are built: one with the full converter list, one bare
  ("`settingsDefault`", the escape hatch every `IRequiresDefaultSerializer` converter needs).
  `ObjectCreationHandling = Replace` is load-bearing (documented inline in `SerializationService.cs`;
  below). `ContractResolver` only forces `MemberSerialization` (`OptIn` by default) onto every contract.
- **Id/primitive wrapper structs** (`ObjectId`, `ThemeId`, `AudioId`, `PrefabId`, `ShapeId`,
  `EffectId`, any `IPrimitiveGuid`/`IPrimitiveInt`/`IPrimitiveFloat`) serialize as a **bare scalar**, not
  `{"Value": ...}`, via `PrimitiveGuidConverter`/`PrimitiveIntConverter`/`PrimitiveFloatConverter` - all
  reconstruct via `Activator.CreateInstance(type, value)`, so every such wrapper needs a public
  single-arg constructor.
- **Nothing on the read path materializes a `JToken` tree, and that is a rule, not an implementation
  detail.** A version has to be known before the payload can be typed, and reading it by loading the
  whole document into a `JObject` and walking that tree a second time cost a clone per domain - and
  domains nest, so a `Level`'s tree was cloned again for `GameLevel`, again for each of the four event
  aggregates, and again for **every** `Prefab` in its resources. `DeserializeEnvelope` makes two
  streaming passes (the first stops at the version property, only the second reads content) and
  `VersionedEnvelopeConverter.ReadJson` reads envelope properties straight off the reader. Since
  `WriteJson` emits the version first, the ordinary document buffers nothing at all; a document whose
  value happens to come first (hand-edited, or written by another tool) is still read correctly, by
  buffering that one subtree until the version that types it arrives.
  `Tests/SerializationPerformanceTests` pins both formats.

## Retired `SerializationType` numbers

- **`Bson = 1` is RETIRED, not reused**: an old `settings.json` holding 1 lands on an undefined value
  that `RuleEnumValid` repairs to `Json`, rather than silently meaning something new. `Blob = 3` is
  appended, which is why `SerializationModeUtils`' dropdown index is no longer the enum's own number -
  order is what that class promises, and equality was a coincidence of the numbers happening to be 0,
  1, 2.
- **`JsonPretty` was member 2 and is gone, and there is no `Formatting` question left anywhere.** It
  wrote the same document as `Json` with indentation, shared its `.json` extension and was read by the
  same reader - so nothing could ever recover the choice from a file, which made it a property of
  whoever happened to save rather than of the level. What it was FOR is reading a level file by eye,
  and that is what an editor's own formatter does, on demand, without a second shape of the format
  existing to be tested, migrated and explained. Every JSON document this project writes is compact,
  `ToFormatting` is deleted rather than moved, and `SerializationSettings` carries no `formatting`
  field (it applied to the one shared `JsonSerializer`, so one screen's "write this readable"
  re-indented every file written afterwards).
- Number 2 is retired like number 1 and is never reissued - and the reason nothing may test
  `== Bson`-style two-branch ternaries survives it: a third member reads as `Json` silently, which is
  what the Unity project's `Core/Utils/SerializationModeUtils` exists to prevent at the five UI call
  sites that did.

## Keys (`Names.cs`)

- **`Names.cs` constants, never string literals, for `[JsonProperty]`.** Deliberately short/abbreviated
  (`"f"` for Frame, `"v"` for Value, `"t"` reused for both TimeShort and TangentShort - disambiguated
  only by never co-occurring on the same model) to keep the wire format compact. The naming rule
  itself is the consuming project's `Docs/NAMING.md`.
- Historical `Versions/V<n>/` snapshot classes intentionally use their own frozen literals/`NamesV<n>`
  instead, so renaming a *current* key can never silently corrupt what an old snapshot deserializes.

## Two different "generations"

The **blob codec generation** (`BlobFormat`) says which byte layout a `.blob` is written in. A
**domain generation** (`ModelGenerationAttribute.Generation`) says what shape a domain's model was and
travels inside each envelope. A domain moving does not move the codec generation; the codec moving
invalidates every `.blob` whatever its domains say. The codec generation cannot be folded into the
domain one: that one lives INSIDE an envelope, and an envelope can only be found by a reader that
already knows the byte layout. It also seeds the payload hash, so a file from another codec is refused
by the header rather than misread deeper in.

## The blob format

- **The header is checked in this order, and the order is the design**: magic, codec generation, the
  declared length against the real one, the hash - and only then is a byte decoded. Nothing is
  allocated on the strength of anything the file says until the file is shown to be whole (a format's
  payload is a stranger's; a cache's was its own). Four refusals, not one, so the UI can tell
  "damaged" from "from a newer build". The hash is an xxHash64 of the payload, non-cryptographic on
  purpose - what is being checked is corruption, and forgery is already answered by the OpenPGP layer
  that protects a level.
- **Degrading at domain granularity is free.** A root already writes `domain + generation + length +
  content`, so the framing that makes a skip safe is paid at every root. A root this build cannot
  parse is stepped over whole, leaving siblings intact.
  - A root from the FUTURE is not skipped but refused (`NewerGenerationException`), before a byte of
    its content is read.
  - Anything that throws INSIDE a root's content is a version problem, not damage: damage was
    answered by the header, so a byte that survived all four checks and then fails to parse is a shape
    this build does not know (an unknown polymorphic tag, a later enum member). One catch site stands
    in for a per-value fallback the binary format cannot have.
  - Content that does not end EXACTLY at its declared length, in either direction, is damage: that
    reader lost its place. (A newer build bumps the generation for an appended member, so no build this
    one can read writes short content.)
- **`IBinaryEnvelope` exists because `IBinaryModel.Read` is the wrong entry point for a snapshot.** An
  aggregate root's generated `Read` reads the envelope first; a migrating reader has already consumed
  it (the generation told it to migrate), so handing the snapshot `Read` would make it look for a
  second one. JSON needs nothing like it: there the envelope is written by whoever HOLDS the member
  (`JsonModels.WriteEnvelope`) and a generated `ReadJson` is already the plain payload reader.
  `BlobEnvelopes` passes the ref struct by `ref` to the interface method; it may not be captured or
  boxed, and nothing does either.
- **`IBinaryModel` is separate from `IModel<T>`**: `IModel` is what a model IS to the program (copy,
  compare, reset, become another instance) and has no opinion about bytes; a codec that changes never
  touches those five. Keeping them apart also lets a type opt out of one and not the other.
- **`BlobWriter`/`BlobReader` are ref structs passed by `ref`**: a ref struct never escapes to the heap
  (a store plus a bounds check, no allocation, no virtual call - the whole reason it is not
  `BinaryWriter`), and by-ref because the buffer grows and a by-value copy would leave the caller
  writing into the old array. Little-endian, fixed width, no varints: a level's numbers are mostly
  floats and ids, which a varint does not shrink, and the format has to be explainable to whoever
  writes the second reader. They are not `Stream` wrappers.
- **Null and polymorphism in the encoding**: a length prefix of `-1` is null (an empty keyframe list
  and a missing one are different states and a round trip has to keep them apart); a polymorphic
  value carries a one-byte tag with `0xFF` reserved for null. **The tag is the model's own
  `GetModelType()`** - the generator reads the enum member that method names - so the blob and the
  JSON `[tag, payload]` carry the same discriminator and cannot drift apart.
- **Every read is bounds-checked and every failure is the same `BlobFormatException`**, caught in
  exactly one place (the envelope), which turns it into "this file could not be read" and lets the
  caller fall back to the `.json` beside it; generated bodies carry no error handling.
- **A count the file chose is refused before it is believed** (`BlobReader.ReadCount`; `Remaining` is
  what every collection checks before allocating): `new List<T>(count)` on a file-chosen length is an
  allocation of the file's choosing. This is the one attack a format has that a self-produced cache did not.
- **`BlobPrimitives` holds the structs the generator cannot write**: `FrameSpan` packs two ints with
  the anchor flags in their sign bits, so writing "its fields" would write the packing - the blob
  writes start, duration, anchors honestly (the JSON side DOES pack, because a two-number array was the
  shipped form); `ModificationKey` and `RunProfile` (and its `RunProfileV1` snapshot) are readonly
  structs with get-only properties, so a member-driven generator has nothing to assign on read; `Pixel` is four bytes seen as one int, which
  costs a quarter of the calls on an image-sized array. An id wrapper is NOT here: one int or Guid
  behind a public single-argument constructor, which the generator emits directly.
- **A trailing index is written last and never moves** (`NAMING.md`'s positional rule): the blob writes
  no names, so inserting a member anywhere but the end makes every trailing byte mean something else.

## The JSON stack

- **`ConverterRouter` fronts the converter list** because Newtonsoft asks every converter whether it
  can convert, once per value and cached nowhere; order still decides the winner and the router keeps
  it. `VersionedEnvelopeConverter` cannot be routed: its `CanConvert` depends on which domain is being
  written right now, which a per-type cache cannot express.
- **`GeneratedModelConverter` is one converter for every generated model** (a single `CanConvert` on
  `IJsonModel`; two hundred entries would undo the router) and goes LAST: first match wins by runtime
  type, so a `Vector2Value` still reaches `Vector2Converter` and comes out `[0,{...}]`, while a model
  with no polymorphic wrapper reaches this one directly. First in the list, every tagged value would
  silently lose its tag. A snapshot is a generated model too, but it never reaches this converter
  through the top-level `{version, value}` envelope, which `VersionedEnvelopeConverter` still owns
  because only it resolves an old version to its snapshot type and walks the migrations. A versioned
  MEMBER is wrapped by whoever holds it instead.
- **`JsonConverterCustomType` writes its type tag directly** (the enum is an integer on the wire - no
  `StringEnumConverter` is registered - verified by re-serializing a real level byte for byte).
  Measured on a 3.2 MB level: `Resources` 672 -> 470 ms, and nothing on its OBJECTS tree. The cost
  there is the per-value DISPATCH (`ConverterRouter` plus Newtonsoft's converter-invocation path), not
  the work in the converters: the objects tree costs ~1420 ms through the stack and 295 ms through the
  same contract resolver with no converters. Hand-writing the members for `Vector2Value`, `FloatValue`
  and `Color4Value` was measured at nothing (eight passes: min 1398 / max 1437 / mean 1419 ms against
  1421) and rejected - a hand-kept duplicate of the contract changes the wire format silently when a
  field is added to the model and not to its writer. Anything aimed at this number should measure the
  dispatch first.
- **An unknown tag falls back to the family's LOWEST tag**, which is every family's plain literal form
  (`FloatType.Value` is `FloatValue`, `ObjectType.RectObject` is `RectObject`,
  `LicenseType.NoSpecified` is `NoSpecifiedLicense`). An unknown OBJECT is a visible hole, and the inert
  `RectObject` is preferred to skipping the entry because it keeps the object's children parented where
  they were - skipping orphans them and the runtime does not draw an orphan. An unknown VALUE is
  invisible, which is why the report is not decoration.
- **`TType` is an enum on all seventeen converters built on it**, but nothing in the signature can say
  so without constraining the parameter on all of them, so the conversion goes through `Convert`,
  boxing once per value (negligible beside the string being built). A converter that writes enough
  values to care can override with a direct `(int)` cast.
- **`VersionedEnvelopeConverter`**:
  - the envelope key is `Names.Generation`, never `Names.Version` (the author's `System.Version`
    string): sharing it would put `{"vrs":1}` and `{"vrs":"1.0"}` in one file;
  - `_activeDomains` suppresses `CanConvert` only for the domain being written or read one level up the
    stack, so the inner serialize call falls through to plain members while a differently-domained
    nested aggregate stays convertible and recurses normally;
  - the payload is handed STRAIGHT to its own writer when it has one - going back through the serializer
    for the same instance trips Newtonsoft's circular-reference check (the value is already on its
    serialize stack). The `_activeDomains` guard covers anything reaching the converter without a
    generated codec;
  - the guard is released in a `finally`: a throw mid-payload would otherwise leave the domain in the
    set for the life of the converter, turning `CanConvert` off for it and reading every later envelope
    of that domain as plain fields;
  - it reads straight off the reader, one envelope property at a time, never into a `JObject` (domains
    nest, so a materialized tree is cloned again per nested domain, down to every prefab). `WriteJson`
    emits the generation first; a document carrying the value first is read by buffering that one
    subtree;
  - **nothing resolved means no chain to walk**: the payload was already read into today's shape by
    property name (the lossy half working), and walking from there would find no step, report a second
    finding and discard what the name match landed while the generated codec kept it - the two stacks
    would disagree about a degraded file, which they may never do.
- **`IRequiresDefaultSerializer`** marks converters that resolve a concrete type of a polymorphic family
  and need a private serializer carrying every other converter (so a nested `IVector2` inside a
  `Color4Key` still works) but not the one that would re-wrap the concrete payload as another
  `[type, value]` array. `SerializationService.GetConverters` wires it for every converter that
  implements it, each with its own router (each excludes a different converter, and a router caches
  the answers for the exact set it was built with).
- **`DictionaryAsPairListConverter`** is for a dictionary whose key cannot be derived from its value
  (`DictionaryAsListConverter` assumes the value embeds its key): an array of `{k, v}` objects instead
  of Newtonsoft's string-key form, which throws for value-type keys with no `TypeConverter`. `BestRun`
  uses it because a record is filed under conditions (`RunProfile`) it does not itself carry, and
  repeating them would be a second copy with nothing keeping the two in agreement.
- **`PrimitiveGuidConverter`** reads a Guid that is a `string` under the JSON reader and an
  already-boxed `Guid` under a binary reader; the same chain must serve both.
- **`FrameSpanConverter` writes the two LOGICAL numbers, each negated when its edge is anchored.** The
  in-memory sign-bit packing must never reach the file (an open format: a start of -2147483548 would
  be unreadable to every other tool). No offset is needed because frames count from
  `FrameRules.MinFrame` and a duration is at least one, so neither is ever zero; a two-number array
  matches the compact wire format. `JsonPrimitives` reproduces the encodings that already exist on
  disk for `FrameSpan`, `ModificationKey`, `RunProfile` and `Pixel` rather than inventing new ones, and
  is as LENIENT as the converters it replaced: a corrupt file should cost one wrong object, not a level
  that refuses to open.
- **`ObjectCreationHandling.Replace` is load-bearing**: without it Newtonsoft appends into a non-null
  nested object/list left by a parameterless constructor (for example `EffectAngleCurvesBySpeed` seeds
  `Curve` with two keyframes), breaking round-trip equality.
- **The reflective writer omits a null SUB-MODEL whole, and nothing else**, to agree byte for byte with
  the generated one (`JsonParityTests`): the test is that the member's type is an `IModel<T>` (every
  concrete model and every polymorphic value interface is one; `List<T>` and `Dictionary<,>` are not,
  which keeps a null collection distinguishable from an empty one, as the blob does).
- **The mode reaches the writer, not the shared serializer**; `SerializationSettings.useGeneratedCodecs`
  is the switch, on by default, and exists for exactly one caller: the parity test, through
  `SerializationService.CreateWithoutGeneratedCodecs()`, reading the same bytes through both stacks.
  Anything that changes how a level is READ is locked that way - a withdrawn reader that passed 4494
  tests and could not open a level is what that rule bought.
- **`[GenerateModelKeyed]`** names the property a dictionary's key is recovered from, which is what
  lets the collection write as a bare array of values. It records the same knowledge the twelve
  `DictionaryAsListConverter` subclasses hold; those stay, because they are the reflective path the
  parity test compares against.
- The traps the generated JSON codec reproduces are all in the consuming project's
  `docs/issues/MODEL_CODEGEN_HISTORY.md` §5, and every one of them is a way the format is not what it
  looks like: the contract is OptOut in practice so `Resource.Type` IS written, members are ordered
  derived-first, an abstract property is written by its override, `[JsonIgnore]` is inherited, and a
  CONCRETE member of a value family is still tagged.
- **A new polymorphic value/effect variant** = new enum case + concrete class + a case in the matching
  `JsonConverterCustomType<T,TType>` subclass's `GetType`/`GetCustomType` switch. No attribute-based
  auto-discovery anywhere in this system (unlike `[RuleContainer]`'s reflection scan or
  `VersionedTypeRegistry`'s reflection scan) - every converter is a manual, explicit mapping. Same
  applies to `RectObject` subtypes via `ObjectConverter`. Since 1.0.0 a new variant is also a
  generation bump (`VERSIONING.md`).

## `IJsonModel`

The third contract beside `IModel<T>` and `IBinaryModel`: a model can be copied without knowing about
bytes and written without knowing about `Copy`. **Reading is deliberately not the mirror of writing**:
a writer knows every member, a reader meets a document that may be hand-edited, written by another tool
or by a build that knew members this one does not, so it switches on the property NAME, skips what it
does not recognise, in whatever order - which is also what makes an added member free for an older file.

**`ReadEnveloped<T>` reads a nested domain as the top-level converter does**: a known generation
migrates, an unknown one degrades with a report, and the silence is what must not come back. It could
begin migrating because a snapshot carries `[GenerateModel]` and reads ITSELF with its own codec, so the
path still holds a bare `JsonReader` and no serializer - the whole of what makes it fast. Measured when
the silent `Skip()` was removed: a nested `LevelSettings` at generation 0 read back `fps=60` through the
generated codec and `fps=61` through the reflective one. Both stacks must degrade to the same value.
Migration needs the generation BEFORE the payload, which `WriteEnvelope` guarantees (`g` first); a
document carrying `v` first takes the lossy half instead of a buffered token tree, because nothing on
this path materializes one - the single documented exception is `VersionedEnvelopeConverter`. An absent
tag is reported and the payload read as-is. The out-of-date branch costs one `Activator.CreateInstance`
per envelope, which is free at this scale (envelopes are ~1.4% of a level's nodes: 266 of 19 341 in
volcano) and only runs for an old file.

## `SerializationReport`

The collector is ambient because the codecs hold a bare reader and nothing else; a report parameter
through every generated body would take that apart for a field that is null in every ordinary read. It
is `AsyncLocal`, not `[ThreadStatic]`: the consumer deserializes on the thread pool, so a thread-static
collector opened by the caller would be installed on a thread that does none of the reading and come
back empty. `AsyncLocal` flows into that hop only, and per logical call - which a server DLL needs, so
two concurrent reads never interleave findings. An entry carries NO tree path: a breadcrumb would be
pushed and popped on the ordinary path, which must not pay; the domain and the member name are what the
degradation site knows for free.

## Serializers

- **`BlobDataSerializer` is the third `IDataSerializer`, and it is not where versions are answered.**
  A JSON envelope resolves a generation to a historical TYPE because Newtonsoft binds by name; a blob
  payload is read by generated code that IS the type, so the same resolve happens one level down, inside
  the generated root read (`BlobEnvelopes`). The serializer checks the FILE (magic, codec generation,
  length, hash, nothing left over); every DOMAIN question (migrate, degrade, skip) belongs to the root.
- **`IDataSerializer` is format-agnostic**: only the envelope step is format-specific; resolving a tag
  to a type and walking the migrations live in `VersionedTypeRegistry` for every implementation.
  `BaseNewtonsoftDataSerializer` reads an envelope in two streaming passes (the first stops at the
  generation, only the second reads content) and hands back the value already current-shape.
- **`SerializationType` retires numbers instead of reusing them**: a member's NUMBER is what a settings
  file on somebody's disk holds. `1` (`Bson`) and `2` (`JsonPretty`) land on an undefined value that
  `RuleEnumValid` repairs to `Json`. Why they went: "Retired `SerializationType` numbers" above.
- **`SerializationTypeExtensions` probe order is NOT `Enum.GetValues`'** (numeric order, where a number is
  a retirement record, not a preference): `Json` first because it is the default and what a hand-made
  folder carries. Every probe goes through it, so a third format is one edit rather than a hunt for
  hand-written branch pairs, one of which being missed would resolve a real level to "no file".
