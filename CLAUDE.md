# CLAUDE.md - bullet-hero-sdk

This is `bullet-hero-sdk` (root namespace `BH.SDK`), a standalone C# library defining the level/save
file format for the game Bullet Hero. It's vendored into the main Unity project as a git submodule at
`Assets/Plugins/BH.SDK` (own repo: `bullet-hero/sdk`, MIT license) - see the main
project's root `CLAUDE.md` (Rule 4, "Open SDK") for *why* it's split out this way (player-data
longevity, external interop with other rhythm games, third-party modding tools). **This file is
scoped to the SDK repo itself.** Prefer editing here directly over patching a vendored copy; the
Unity project's own `CLAUDE.md`s document the *consumer* side (`Core`'s "SDK model vs runtime state"
split, `GameEditor`'s `TargetScope`/Prefab Mode).

**The core asmdef references no `UnityEngine`** (`noEngineReferences: true`, Newtonsoft only); Unity
code lives in `UnityIntegration/` (dual, `#if BHSDK_UNITY`) and `UnityExtensions/` (engine-only).
Four `.csproj` files build the same sources with no Unity - `netstandard2.1`/`LangVersion 9.0` are
fixed by Unity, and build output goes to `bin~`/`obj~`, never a plain `bin/`. Full statement:
`Docs/STANDALONE_BUILD.md`.

**This layer's documentation is per-subfolder.** The index below names every folder and the file that
describes it; each loads only when you touch a file in that folder.

## Folder index

| Folder | What is in it | Its own file |
|---|---|---|
| `Models/` | the level format itself (see "Object model" / "Value system" below for the architecturally… | `Models/CLAUDE.md` |
| `Serialization/` | `SerializationService` (the JSON/.blob entry point), `Serializers/`, `Converters/Base/` +… | `Serialization/CLAUDE.md` |
| `Versions/` | the model-versioning/migration system (`[ModelGeneration]`, `VersionedTypeRegistry`,… | `Versions/CLAUDE.md` |
| `Rules/` | `public const` numeric/enum clamp tables (`FrameRules`, `ValueRules`, `LevelRules`,… | `Rules/CLAUDE.md` |
| `Validations/` | the rule engine, in two halves. *Declarative*: `RuleAnalyzer`/`RuleFixer`… | `Validations/CLAUDE.md` |
| `Utils/` | `BHSDKMath` (Unity-independent math, since the core assembly can't reference… | `Utils/CLAUDE.md` |
| `Services/` | `SerializationService`-adjacent but SDK-root-level. **Four of its subfolders are the level-archive feature** - `Content/`, `Archive/` (tar.gz AND zip), `Crypto/`, `LevelArchive/` - and only… | `Services/CLAUDE.md` |
| `Publishing/` | **the third validation pass, and the only one that asks a question about the outside… | `Publishing/CLAUDE.md` |
| `Generators/` | authoring automation: a generator produces level content from a few parameters. Non-generic… | `Generators/CLAUDE.md` |
| `Roslyn/` | the compile-time half: analyzers and incremental source generators, **running** since… | `Roslyn/CLAUDE.md` |
| `UnityExtensions/` | everything that genuinely needs Unity, in four groups. Own asmdef, unconditionally requires… | `UnityExtensions/CLAUDE.md` |
| `UnityIntegration/` | `Cat.cs`, a tiny `Debug.Log`-style logging façade (`Meow`/`MeowWarn`/ `MeowError`/...)… | `UnityIntegration/CLAUDE.md` |
| `Agent/` | the pure half of the level-editing bridge: wire constants and DTOs, the readable value codec, `TimeAddress` and `MusicGrid`, the endpoint file. No level or save model lives here, so nothing in it bumps a version | `Agent/CLAUDE.md` |
| `Tests/` | `BH.SDK.Tests.asmdef`, NUnit. **There is a SECOND test assembly**, `UnityExtensions/Tests/`… | `Tests/CLAUDE.md` |

## Mental model

A level on disk is **three independent files**, each its own serialization root:
- `level.json`/`.blob` - a `Level` (`Models/Level.cs`): `Settings`/`Game`/`Audio`/`Resources`, plus
  `Hints` - the one aggregate holding nothing authored (see `Models/Hints/`).
- `metadata.json`/`.blob` - a `LevelMeta` (`Models/LevelMeta.cs`): name/description/authors/license/
  per-resource UGC metadata. **Not a field of `Level`** - a wholly separate aggregate, easy to
  assume otherwise since both describe "the same level."
- `settings.json` - a `UserSettings` (device-wide player options, not per-level).

Every serialization root is a class carrying `[ModelGeneration(domain, generation)]`; everything else
is a plain nested model versioned only as part of its containing envelope. `Models/FileNames.cs`
names the on-disk files; `PathUtils.FindDataFile` resolves the actual extension (json vs bson chosen
per-level at creation).

Almost every "value-shaped" field is **polymorphic**: a keyframeable float, vector, or color can be a
literal `Value`, `RandomMinMax[Step]`, `RandomCircle`/`RandomRect[Step]`, or (for colors) a
`ThemeRef`. This is the SDK-side half of what the Unity project's `Assets/Code/Core/CLAUDE.md` calls
"two parallel data shapes": the SDK model keeps values tagged with their *kind*; Unity's runtime
`*KeyState`/`*State` structs keep that same tag alive so `GamePlayer`'s jobs can re-roll randomness
every frame instead of freezing it once at load.

## Conventions

- **`Names.cs` constants, never string literals, for `[JsonProperty]`** - deliberately short (`"f"`
  for Frame, `"v"` for Value, `"t"` for both TimeShort and TangentShort, which never co-occur on one
  model). `Versions/V<n>/` snapshots use their own frozen literals/`NamesV<n>`, so renaming a
  *current* key can never corrupt what an old snapshot deserializes.
- **New polymorphic value/effect variant** = new enum case + concrete class + a case in the matching
  `JsonConverterCustomType<T,TType>` subclass's `GetType`/`GetCustomType` switch. No auto-discovery
  (unlike `[RuleContainer]`'s or `VersionedTypeRegistry`'s reflection scans) - every converter is a
  manual mapping. Same for `RectObject` subtypes via `ObjectConverter`.
- **New `[ModelGeneration]` aggregate** = the attribute at `ModelGenerations.Current` if genuinely
  new, or `ModelGenerations.Current + 1` + a `V<old>/` snapshot + `IMigration` pair if changing an
  existing domain's shape - read `Versions/README.md` in full first (nested property stays typed as
  the *current* class, snapshot classes skip `IModel<T>`, a domain with no independent envelope at
  some generation gets a snapshot with no `[ModelGeneration]` at all).
- **`ValueRules`' layer constants are reserved draw-order bands, not just a clamp** - authored
  `[MinLayer, MaxLayer]`, the editor overlays above it; `LayerZOffsetStep` × `LayerZOffsetCount` is a
  different family and must stay below 1.0. Don't widen or merge either: `Docs/RULES.md`, "Layers and
  depth bands".
- **Collection caps live in `Rules/LevelRules.cs`/`AudioRules.cs`** - check there before hardcoding
  one. `MaxObjects` (caps `GameLevel.Objects`: how many may EXIST) and `MaxObjectIds` (how many were ever MINTED, never reused)
  are separate caps on purpose (`Docs/RULES.md`).
- **A new model is `[GenerateModel] public sealed partial class`, and nothing else.** Write the
  members, the constructors and the `[JsonProperty]` names; the `IModel<T>` contract and the blob
  codec are generated. `sealed` is the generator's ONLY signal for "can anything override this" -
  leave it off only if something derives from it.

## Testing

Every test method carries `[Author(Metadata.Author.Vertoker)]`, `[Category(Metadata.Category.Self)]`
and exactly one difficulty category; `MockData.cs` is the shared fixture builder. **Treat
serialization-adjacent changes as higher-risk than they look** (`TODO.md`). Why the fixtures are
shaped as they are: `Docs/TESTING.md`, "Shared fixtures and the attribute convention".
