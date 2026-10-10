# Serialization: the reasoning behind the codecs and converters

Contributor-facing. `Serialization/CLAUDE.md` carries the map, the measured `.blob` / JSON speedups and
the retired enum numbers; `Docs/VERSIONING.md` carries the envelope and the migration rules. This file
holds the design reasoning the code used to carry in comments, and the verified negatives.

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
  "damaged" from "from a newer build".
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
  writes the second reader.
- **Every read is bounds-checked and every failure is the same `BlobFormatException`**, caught in
  exactly one place (the envelope), which turns it into "this file could not be read" and lets the
  caller fall back to the `.json` beside it; generated bodies carry no error handling.
- **A count the file chose is refused before it is believed** (`Remaining` is what every collection
  checks before allocating): `new List<T>(count)` on a file-chosen length is an allocation of the
  file's choosing. This is the one attack a format has that a self-produced cache did not.
- **`BlobPrimitives` holds the structs the generator cannot write**: `FrameSpan` packs two ints with
  the anchor flags in their sign bits, so writing "its fields" would write the packing - the blob
  writes start, duration, anchors honestly (the JSON side DOES pack, because a two-number array was the
  shipped form); `ModificationKey` and `RunProfile` are readonly structs with get-only properties, so a
  member-driven generator has nothing to assign on read; `Pixel` is four bytes seen as one int, which
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
  through the top-level envelope, which `VersionedEnvelopeConverter` still owns because only it resolves
  an old version to its snapshot type and walks the migrations.
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
- **The mode reaches the writer, not the shared serializer**; `useGeneratedCodecs` exists only so the
  parity test can read the same bytes through both stacks - a withdrawn reader that passed 4494 tests
  and could not open a level is what that rule bought.

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
  `RuleEnumValid` repairs to `Json`. Why they went: `Serialization/CLAUDE.md`.
- **`SerializationTypeExtensions` probe order is NOT `Enum.GetValues`'** (numeric order, where a number is
  a retirement record, not a preference): `Json` first because it is the default and what a hand-made
  folder carries. Every probe goes through it, so a third format is one edit rather than a hunt for
  hand-written branch pairs, one of which being missed would resolve a real level to "no file".
- **No indented JSON anywhere**: nothing could recover the choice from a file, so it described the
  person saving rather than the level. `SerializationSettings` carries no `formatting`.
