Bullet Hero carries **six** different numbers that a reader could reasonably call "the version", and
they belong to six different things. This file names all six, says where each is written, and says
who moves it. Three of them are the ones anyone normally means — the client, the SDK and the model
format — and those three are what the game shows the player on one line.

This is not a changelog and not a release process. It is the answer to "which number is this, and
what happens if I change it". **The step-by-step for actually raising one is the consuming project's
`Docs/VERSION-BUMP.md`** — every file to touch, in order, and how to know it worked. That one lives
outside this repo on purpose: two of the three axes (the client's own version, and the store build
numbers that move with it) are not the SDK's business at all.

### The three axes a player sees

The Settings screen shows them in this order, labelled, and clicking the line copies it:

```
gv 0.7.0, sv 0.7.0, mg 1
```

| Axis | What it versions | Where it lives | Runtime |
|---|---|---|---|
| `gv` | the client — the game as a product | `ProjectSettings.asset`' `bundleVersion` | `Application.version` |
| `sv` | `BH.SDK` as a library, semver over its public API | `SdkVersion.cs`, `package.json`, `BH.SDK.csproj`' `<Version>` | `SdkVersion.Value` |
| `mg` | the model format, one generation per domain | `[ModelGeneration]` on each aggregate root | `ModelGenerations.Current` |

**They start equal and are free to diverge.** `sv` began at 0.5.5 by duplicating `gv` once, because
a library that has shipped inside one client since 0.0.1 has no other honest origin. Nothing keeps
them in step from here, and nothing should: a breaking SDK API change is a major `sv` and possibly
no `gv` at all, while a story update is a major `gv` and no `sv`.

**The labels are load-bearing.** Two of the three are the same number today, so a line without them
is three digits nobody can act on in a bug report. `BH.Shared.Utils.VersionText` is the one place
that formats it.

### `gv` — the client

`major` is a global update (story, multiplayer), `minor` is ordinary features, `patch` is a hotfix.
Read at runtime by `ErrorReportService`, which puts it in every error report.

The repository carries a `gv<X.Y.Z>` tag on the commit where each value first appeared — fifteen of
them, `gv0.0.0` (the Unity default `1.0`, before the project had a version of its own) through
`gv0.5.5`. The gaps (`0.2.x`, `0.4.0`, `0.4.2`–`0.4.9`, `0.5.0`) never existed.

### `sv` — the SDK as a package

Semver over the **public API of `BH.SDK`**, for consumers that compile against the DLL rather than
read a file: the site, the server, external tools, modders.

- major — a breaking API change (a type removed, a signature changed, a member's meaning changed)
- minor — an addition nothing has to react to
- patch — a fix that moves no signature

**Three copies, and a test keeps them honest.** `SdkVersion.cs` is what a consumer reads at runtime,
`package.json` is what a package manager reads, and `<Version>` in `BH.SDK.csproj` is what a
NuGet-style build stamps on the assembly. None of the three can see the others, so
`Services.Shared.Tests`' `SdkVersionAgreementTests` compares them.

`SdkVersion.Value` is **`static readonly`, never `const`**: a `const` is inlined into each consumer
at *its* compile time, so a tool built against 0.6.0 would keep reporting 0.6.0 after being handed a
newer DLL — which is exactly the question the field exists to answer.

`<Version>` sits in `BH.SDK.csproj` rather than `Directory.Build.props`, which is shared by four
projects and would stamp the analyzer and both test assemblies too.

**`sv` tags live in THIS repository, not the consumer's.** They are pushed from here, which is why
tagging the game's repo never produces any — `gv*` and `sv*` are tags of two different repositories
that happen to share a working tree. `sv0.5.5` (the commit that introduced `SdkVersion.cs`)
through `sv0.7.0` exist; there is no history before that, because the library carried no version at
all until then.

Separately, and often confused with the above: what records which `sv` a given `gv` shipped against
is the consumer's **submodule pointer**, not a tag on either side. That pointer has been lost once
already - staging a file that lives inside this folder from the outer repository replaces it with
loose files, since a gitlink cannot coexist with tracked files under its own path - and the consuming
project's `Docs/VERSION-BUMP.md` carries the repair.

### `mg` — the model format's generation

**One integer per domain**, carried by `[ModelGeneration(domain, generation)]` on that domain's
aggregate root, and written into every envelope on disk.

**Why one number and not `major.minor`.** A change to the shape of a file either needs a migration or
does not; there is no intermediate grade a second component could express. What the second number did
instead was invite a bump nobody migrated — two domains had moved to `(2, 0)` and `(1, 1)` and both
were put back. Every domain now reads generation 1.

**A generation is assigned FORWARD and never reused.** A domain that changes shape takes the next
free number **for itself alone**; the twenty domains are free to diverge (`Level` at 3 while
`ThemeData` is still at 1), and only the one that moved needs a snapshot and a migrator.
`ModelGenerations.Current` is the maximum across the live domains, and `ModelGenerationsTests` is
what fails when it stops being.

The named constants are in `Versions/ModelGenerations.cs`:

- `Invalid = -1` — no generation at all: an unversioned leaf in the generator's specs, or an envelope
  nothing was read from. **Negative rather than zero**, because zero is a real generation the frozen
  snapshots are written at; every negative value is equally invalid and `-1` is merely canonical.
- `Test = 0` — the scaffold under `Versions/V0`, which exists to exercise the migration path end to
  end rather than to describe a format any build shipped.
- `Release = 1` — what the game writes today.
- `Current` — an alias for the newest. The only one UI and reports read.

### The envelope on disk

```json
{"g": 1, "v": { ... }}
```

`g` is the generation, `v` is the payload; `Names.Generation` and `Names.Value` are the constants.
Every `[ModelGeneration]` type writes one, including one nested inside another — a `Level` carries
`LevelSettings`, `GameLevel`, `AudioLevel`, `LevelResources` and `LevelHints`, each with its own.

**`g` is deliberately not `vrs`.** `Names.Version` (`"vrs"`) is the AUTHOR's version of a level, a
`System.Version` written as a string, and it appears in `LevelMeta`, `BestRun` and `LevelStatistics`.
While the envelope also used `"vrs"`, one `metadata.json` carried `"vrs"` twice at two depths meaning
two different things — and a mechanical rewrite of that file would silently destroy the author's
number. See "The other three axes" below.

The binary format writes the same two things: the domain as text, then the generation as one `int`,
then a length. That length is what lets a reader step over a root it cannot read, which is how the
binary format degrades at domain granularity - see `BlobEnvelopes`.

### A known generation migrates, an unknown one degrades

One sentence, and it holds in both formats and at both levels. "Known" means
`VersionedTypeRegistry` resolves the generation to a snapshot type **and** the migration chain from
it is complete; that is the whole of the backward direction and it is exact. Everything else is the
forward direction and is lossy by design.

| Site | What it does |
|---|---|
| `VersionedTypeRegistry.Resolve` | resolves, or falls back to the domain's CURRENT type and reports. An unknown DOMAIN still throws — it has nothing to fall back to |
| `VersionedTypeRegistry.TryResolve` | the same lookup answering `null`, for the read paths that must branch rather than be handed a substitute |
| `VersionedTypeRegistry.TryUpgradeToLatest` | walks what it can, says whether it arrived, and reports when it did not |
| `JsonModels.ReadEnveloped` (nested) | migrates through the snapshot's own generated codec when the generation resolves; otherwise reads the payload into today's class by property name and reports |
| the generated `.blob` root | migrates, or skips the whole root by its declared length and leaves the model at constructor defaults |

**A snapshot carries `[GenerateModel]` now, which is what made the nested half possible.** It reads
itself with its own generated codec, so `ReadEnveloped` still holds a bare `JsonReader` and no
`JsonSerializer` — the thing `IJsonModel`'s identity rests on. In `.blob` the snapshot is read
through `IBinaryEnvelope.ReadContent` rather than `IBinaryModel.Read`, because the caller has already
consumed the envelope that told it to migrate.

**Migration needs the generation BEFORE the payload**, which every writer this format has provides —
`WriteEnvelope` emits `g` first. A document carrying `v` first (hand-edited, or written by another
tool) takes the lossy half instead of a buffered `JToken`: nothing on the read path materializes a
token tree, and `VersionedEnvelopeConverter` is that rule's single documented exception.

**What must never come back is the SILENCE.** The refusal this replaced was itself replacing a
`Skip()`: the payload was matched by property name against today's class, every field missed, and the
object came back as **constructor defaults** with nothing thrown and nothing logged. Measured: a
nested `LevelSettings` written at generation 0 read back `fps=60` through the generated codec and
`fps=61` through the reflective one — the same file, two answers, and the quiet one was the default
path. The tolerant read is back; the silence is not. Every substitution goes to
`SerializationReport`, and **both codec stacks must degrade to the same value and report the same
substitutions**, or `useGeneratedCodecs` stops being a switch that changes nothing.

**An absent tag degrades too**: it is reported, and the payload is read as-is.

`EnvelopeShapeTests`, `SerializationReportTests`, `BlobCodecTests` and `JsonParityTests` pin all of
it. `Docs/Issues/FORWARD_COMPATIBILITY_HISTORY.md` in the consuming project is the design record.

### Changing a domain's shape

**Before release** — which is now — the format changes **in place**: change the model, change the key,
done. No snapshot, no migrator, no generation bump. The main project's `CLAUDE.md` Rule 11 is the
record, and the cost is stated there: a file written earlier reads back with defaults where the change
landed, silently, so whatever local content matters is re-saved.

**After release** that inverts. A domain that changes shape then:

1. takes the next free generation for itself,
2. gets a frozen snapshot class under `Versions/V<n>/` carrying `[ModelGeneration(domain, <n-1>)]`,
3. gets an `ModelMigration<From, To>` under that folder's `Migrations/`,
4. and leaves every other domain alone.

`Versions/README.md` carries the folder convention and the traps in full — a nested property must
stay typed as the CURRENT class, a snapshot is a `[GenerateModel] sealed partial` class with its
members constructed, and a domain that was not yet an envelope at some generation gets a snapshot
with no `[ModelGeneration]` at all. Read it before writing one.

### Two rules `.blob` takes on, both free now and frozen at release

They are the price of degrading a binary format at domain granularity, and both are cheap only while
nothing is on anyone's disk.

1. **Member order in the blob is append-only.** A new member is written LAST. The generator writes
   members in declaration order, so inserting one in the middle makes every trailing byte mean
   something else and a short payload stops being recoverable. `Docs/NAMING.md` carries the
   neighbouring half — never reorder a member while renaming it.
2. **A domain's generation rises for a new polymorphic VARIANT too**, not only for a shape change — a
   new `ObjectType`, a new `FloatType`, a new licence form. In JSON an unknown tag is survivable
   because the payload is skippable; in the blob it is not, because a positional format has no length
   to skip by. What the reader does instead is skip the whole ROOT that carried the tag, and this
   rule is what makes that sufficient: such a tag only ever arrives inside a root whose generation
   already moved.

**The limit, named rather than papered over:** `BlobFormat.Generation` is the byte codec's own
version. If it moves again there is nothing to degrade toward and the file is unreadable whole. No
design makes "it opens" true there; what the format promises for that case is a sentence instead of a
stack trace.

### The other three axes

They are listed here because a reader looking for "the version" will find them first, and each has
been mistaken for one of the three above at least once.

**`LevelMeta.LevelVersion`** — the AUTHOR's own version of their level, a `System.Version` under the
key `"vrs"`, duplicated onto `BestRun.LevelVersion` and `LevelStatistics.LevelVersion` so a record
says which version of a level it was set on. It has nothing to do with the format. **Nothing may
rewrite `"vrs"` mechanically**: in `metadata.json` the same key used to appear as both this and the
envelope's, and telling them apart needs the surrounding shape.

**`BlobFormat.Generation`** — the binary CODEC's generation: which byte layout a `.blob` is written
in. It moved 1 → 2 when the envelope stopped carrying two `ushort`s and started carrying one `int`.
It also seeds the payload hash, so a file from the other codec is refused by the header rather than
misread deeper in. A domain moving does not move this; this moving invalidates every `.blob` whatever
its domains say.

**`ReflexRingKey.LevelVersion`** — an `int` the reflex bot bumps to invalidate its own cache. Not a
version of anything on disk.

### Where the machinery lives

| Piece | File |
|---|---|
| the attribute | `Versions/ModelGenerationAttribute.cs` |
| the named generations | `Versions/ModelGenerations.cs` |
| the domain names | `Versions/ModelDomains.cs` |
| generation → type, and the migration walk | `Versions/VersionedTypeRegistry.cs` |
| one migration step | `Versions/ModelMigration.cs`, `Versions/IMigration.cs` |
| what an envelope read back holds | `Versions/EnvelopeData.cs` |
| the JSON envelope | `Serialization/Converters/VersionedEnvelopeConverter.cs` |
| what a degraded read substituted | `Serialization/SerializationReport.cs` |
| the blob envelope's degrade/migrate half | `Serialization/Blob/BlobEnvelopes.cs`, `Serialization/Blob/IBinaryEnvelope.cs` |
| what a level claims a client needs | `Versions/LevelGenerations.cs`, `LevelMeta.MinGeneration` |
| the generated codecs' half | `Serialization/Json/IJsonModel.cs`, `Serialization/Blob/` |
| the generator that emits them | `Roslyn/Generators/Model/` |

**The generator matches the attribute by its SIMPLE NAME** (`ModelSpecFactory.ResolveDomain`), because
it sees the user's source through Roslyn symbols and references this assembly not at all. Renaming
the attribute without renaming that literal compiles perfectly and silently stops every envelope from
being written. `ModelGenerationValues.Invalid` in the generator mirrors `ModelGenerations.Invalid`
here for the same reason, and the two must stay in step.
