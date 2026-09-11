# CLAUDE.md — bullet-hero-sdk

This is `bullet-hero-sdk` (root namespace `BH.SDK`), a standalone C# library defining the level/save
file format for the game Bullet Hero. It's vendored into the main Unity project as a git submodule at
`Assets/Plugins/BulletHeroSDK` (own repo: `vertoker/bullet-hero-sdk`, MIT license) — see the main
project's root `CLAUDE.md` (Rule 4, "Open SDK") for *why* it's split out this way (player-data
longevity, external interop with other rhythm games, third-party modding tools). **This file is
scoped to the SDK repo itself.** Prefer editing here directly over patching a vendored copy; the
Unity project's own `CLAUDE.md`s document the *consumer* side (`Core`'s "SDK model vs runtime state"
split, `GameEditor`'s `TargetScope`/Prefab Mode).

The main `BH.SDK` asmdef is deliberately Unity-independent: `noEngineReferences: true`,
depends only on Newtonsoft.Json/Newtonsoft.Json.Bson (NuGet). Everything that touches `UnityEngine`
lives in separate, opt-in asmdefs — `UnityIntegration/` (**dual**: every file compiles with and
without an engine, `#if BHSDK_UNITY` choosing, so it ships in both builds) and `UnityExtensions/`
(**engine-only**, no such branch) — so the core model/serialization/rules code can be reused
standalone (e.g. by external tooling, a future
companion website) without dragging Unity along.

**That independence is now CHECKED rather than merely stated.** Four `.csproj` files build this
repo with no Unity anywhere, and every one of them is the same sources Unity compiles — never a copy
(the fourth is the exception that proves it: Unity must NOT compile the Roslyn tests, hence the
tilde):

| Project | Target | What it builds |
|---|---|---|
| `BH.SDK.csproj` | netstandard2.1 | The library: everything except `Roslyn/`, `Tests/` and `UnityExtensions/`. **`UnityIntegration/` is included WHOLE** — every file there is dual by contract (`#if BHSDK_UNITY`), and this build is what enforces it; see its `README.md` |
| `Tests/BH.SDK.Tests.csproj` | net8.0 | Every fixture the Unity Test Runner runs, under `dotnet test` — **2030 passing** outside Unity. Its `Compile` include is RECURSIVE; while it was the folder root alone, `Tests/Rules` and `Tests/Services` were silently absent and the run reported a green 454 |
| `Roslyn/BH.SDK.Roslyn.csproj` | netstandard2.0 | The analyzers and generators — see `Roslyn/README.md` |
| `Roslyn/Tests~/BH.SDK.Roslyn.Tests.csproj` | net8.0 | Tests for the components themselves. **Invisible to Unity by the tilde**, and has to be — the asmdef above it would otherwise swallow the fixtures |

`netstandard2.1` and `LangVersion 9.0` are not choices: they are what the Unity project compiles
with (`apiCompatibilityLevel: 6`). Raising either would let code in that Unity then refuses.
`Directory.Build.props` redirects every project's output to `bin~`/`obj~`, because **Unity imports
any folder under `Assets/` that is not suffixed with a tilde** — a plain `bin/` hands the Editor a
second copy of every assembly here.

**This layer's documentation is per-subfolder.** The index below names every folder and the file that
describes it; each loads only when you touch a file in that folder.

## Folder index

| Folder | What is in it | Its own file |
|---|---|---|
| `Models/` | the level format itself (see "Object model" / "Value system" below for the architecturally… | `Models/CLAUDE.md` |
| `Serialization/` | `Serializers/` (`SerializationService`, the JSON/BSON entry point), `Converters/Base/` +… | `Serialization/CLAUDE.md` |
| `Versions/` | the model-versioning/migration system (`[ModelGeneration]`, `VersionedTypeRegistry`,… | `Versions/CLAUDE.md` |
| `Rules/` | `public const` numeric/enum clamp tables (`FrameRules`, `ValueRules`, `LevelRules`,… | `Rules/CLAUDE.md` |
| `Validations/` | the rule engine, in two halves. *Declarative*: `RuleAnalyzer`/`RuleFixer`… | `Validations/CLAUDE.md` |
| `Utils/` | `BHSDKMath` (Unity-independent math, since the core assembly can't reference… | `Utils/CLAUDE.md` |
| `Services/` | `SerializationService`-adjacent but SDK-root-level. **Four of its subfolders are the… | `Services/CLAUDE.md` |
| `Publishing/` | **the third validation pass, and the only one that asks a question about the outside… | `Publishing/CLAUDE.md` |
| `Generators/` | authoring automation: a generator produces level content from a few parameters. Non-generic… | `Generators/CLAUDE.md` |
| `Roslyn/` | the compile-time half: analyzers and incremental source generators, **running** since… | `Roslyn/CLAUDE.md` |
| `UnityExtensions/` | everything that genuinely needs Unity, in four groups. Own asmdef, unconditionally requires… | `UnityExtensions/CLAUDE.md` |
| `UnityIntegration/` | `Cat.cs`, a tiny `Debug.Log`-style logging façade (`Meow`/`MeowWarn`/ `MeowError`/...)… | `UnityIntegration/CLAUDE.md` |
| `Tests/` | `BH.SDK.Tests.asmdef`, NUnit. **There is a SECOND test assembly**, `UnityExtensions/Tests/`… | `Tests/CLAUDE.md` |

## Mental model

A level on disk is **three independent files**, each its own serialization root:
- `level.json`/`.blob` — a `Level` (`Models/Level.cs`): `Settings`/`Game`/`Audio`/`Resources`, plus
  `Hints` — the one aggregate holding nothing authored (see `Models/Hints/` below).
- `metadata.json`/`.blob` — a `LevelMeta` (`Models/LevelMeta.cs`): name/description/authors/license/
  per-resource UGC metadata. **Not a field of `Level`** — a wholly separate aggregate, easy to
  assume otherwise since both describe "the same level."
- `settings.json` — a `UserSettings` (device-wide player options, not per-level).

Every serialization root is a class carrying `[ModelGeneration(domain, generation)]` — see
"Serialization & versioning" below; everything else is a plain nested model versioned only as part
of its containing envelope. `Models/FileNames.cs` names the on-disk files; `PathUtils.FindDataFile`
resolves the actual extension (json vs bson chosen per-level at creation).

Almost every "value-shaped" field in the model tree is **polymorphic**: a keyframeable float,
vector, or color can be a literal `Value`, `RandomMinMax[Step]`, `RandomCircle`/`RandomRect[Step]`,
or (for colors) a `ThemeRef` — see "The polymorphic Value system" below. This is the SDK-side half
of what the Unity project's `Assets/Code/Core/CLAUDE.md` calls "two parallel data shapes": the SDK model keeps
values tagged with their *kind*; Unity's runtime `*KeyState`/`*State` structs keep that same tag
alive so `GamePlayer`'s jobs can re-roll randomness every frame instead of freezing it once at load.

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
- **New `[ModelGeneration]` aggregate** = add the attribute at `ModelGenerations.Release` if
  genuinely new, or take the next free generation + write a `VX/` snapshot + `IMigration` pair if
  changing an existing domain's shape — see `Versions/
  README.md`'s folder-convention rules in full before doing this, several easy-to-miss subtleties
  (nested property must stay typed as the *current* class, snapshot classes skip `IModel<T>`, a
  domain with no independent envelope yet at some generation gets a snapshot with no `[ModelGeneration]`
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
- **A new model is `[GenerateModel] public sealed partial class`, and nothing else.** Write the
  members, the constructors and the `[JsonProperty]` names; the whole `IModel<T>` contract and the
  blob codec are written for you. Leave it non-sealed only if something derives from it - `sealed`
  is the ONLY signal the generator has for "can anything override this", so a base that forgets it
  gets a pointless virtual on every member, and a leaf that forgets it gets the same.

## Testing

`Tests/` (`BH.SDK.Tests.asmdef`, NUnit): `MockData.cs` is the shared fixture builder (moved out
of `SerializationTests` specifically so every test file can reuse it) — `CreateTestXxx`/
`CreateValidXxx` factories deliberately touch as much field surface as possible while staying
rule-valid; `CreateInvalidXxx` factories are deliberately minimal, each encoding exactly one rule
violation `RuleFixer` must detect and fix. A `#region Generation 0` half builds equivalents against
`V0` snapshot types for exercising the migration path, including a hand-spliced JSON envelope
builder (`CreateTestLevelV0Json`) — needed because `VersionedEnvelopeConverter` always tags a
*whole* current-shape object with the *current* version when serializing, so each historical fragment
has to be serialized standalone from its own real `VX_Y` type and spliced in by hand.

**Every test method carries three attributes**, no exceptions — `[Author(Metadata.Author.Vertoker)]`,
`[Category(Metadata.Category.Self)]` (`"BH.SDK"`, this namespace minus its `.Tests` suffix), and
exactly one difficulty out of `Metadata.Category.VeryEasy`/`Easy`/`Normal`/`Hard`/`Extreme`. They sit
between the `[Test]`/`[TestCase]` attribute(s) and the signature. `Metadata.cs` holds the constants;
their string values are ordinal-prefixed (`"1_very_easy"` … `"5_extreme"`) so a category dropdown
sorts cheapest-first. The consuming Unity project states this as a hard rule and applies the same
convention to its own test assemblies — see its root `CLAUDE.md`.

**The modification fixtures are the worked example of what this suite is for**, and of what it
missed for as long as they did not exist. `ModificationApplyTests` drives one field of each kind
through `Apply` - in memory and after a serializer round trip - and it is what found that an int
override never applied at all while an enum and an id stopped applying once they had been through
the serializer. `ModificationValuesTests` pins the conversion that fixed them,
`ModificationFieldsTests` the id table from both sides (the constants, and the members claiming
them), and `Rules/ModificationCheckedWriteTests` the one write in the format that reaches a model
with nothing to judge it. `Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md` is the record.

Per the SDK's own `TODO.md`: `SerializationService`'s stability across all keyframe/value/effect type
combinations (round-trip + real saved level files) is explicitly flagged as not yet fully verified,
especially after the `IRequiresDefaultSerializer` refactor — treat serialization-adjacent changes
here as higher-risk than they might look.
