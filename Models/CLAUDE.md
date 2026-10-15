# CLAUDE.md - Assets/Plugins/BH.SDK/Models

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

**Documentation for this layer is per-subfolder.** Each file below loads only when you touch
a file in that folder.

## Folder index

The level format itself. `Models/Names.cs` is the single source of truth for every `[JsonProperty]`
name, short on purpose (`Docs/SERIALIZATION.md`, "Keys").

| Folder | What it owns | File |
|---|---|---|
| `Clipboard/` | the clipboard aggregate | `Clipboard/CLAUDE.md` |
| `Game/` | `GameLevel` + level-global event tracks | `Game/CLAUDE.md` |
| `Hints/` | `LevelHints` + `LimitHints` - everything advisory | `Hints/CLAUDE.md` |
| `Keyframes/` | keyframe wrapper types | `Keyframes/CLAUDE.md` |
| `Objects/` | the scene-object hierarchy | `Objects/CLAUDE.md` |
| `Resources/` | level resource dictionaries + `TypedResourceId` family | `Resources/CLAUDE.md` |
| `SettingGroups/` | two unrelated aggregates share it: `LevelSettings` (per-level) vs. `UserSettings`' sub-groups (per-device) | `SettingGroups/CLAUDE.md` |
| `Statistics/` | the two statistics roots | `Statistics/CLAUDE.md` |
| `Values/` | polymorphic Value implementations | `Values/CLAUDE.md` |
| `Effects/`, `Audio/`, `AudioEffects/`, `PostProcessing/` | per-domain payload data | below |
| `Events/` | one-shot markers/checkpoints, not animated tracks | - |
| `Data/` | `ThemeData`/`EffectData`/`CompositeShape` - resource-level aggregate payloads | `Docs/RESOURCE_DATA.md` |
| `Meta/` | `Author` (a localizable free-text credit line, not a role enum) and `ResourceMeta` | `Docs/RESOURCE_META.md` |
| `Interfaces/`, `Enums/` | contracts; enums whose numbers a file stores | `Docs/ENUMS.md` |
| `Primitives/` | id structs - **read `Assets/Plugins/BH.SDK/Docs/IDENTIFIERS.md` before adding an id** (`Guid` vs `int`, and whether they will ever be unified) | - |

## The `IModel<T>` contract

Full statement: `Docs/MODEL_CONTRACT.md`.

- **Every live model's `Copy`/`Equals`/`GetHashCode`/`Reset`/`Update`/`Pull` and both codecs are
  GENERATED** for `[GenerateModel]` types; the file holds members and constructors only. A new model is
  `[GenerateModel] public sealed partial class` - `sealed` is the generator's only "can anything
  override this" signal.
- **Hand-written on purpose**: `FrameSpan`, `ModificationKey`, `Modification`. The generator refuses
  rather than skips (`BHS1003`); the escape is `[GenerateModelIgnore]` plus the partial hooks.
- **`Update` vs `Pull` is how deep identity reaches**: `Update` replaces nested models with copies,
  `Pull` writes into the instances already held. Object scopes (`GameLevel`, `Prefab`, `ClipboardData`)
  merge key by key (`[GenerateModelMerge]`, `ModelUtils.PullDictionary`); a polymorphic field goes
  through `ModelUtils.PullFrom` - the interface's own `Update`/`Pull` is a no-op on a sibling.
- **Neither is virtual (`Copy()` is)**: through a base reference `Update` writes only the base half. A
  new `RectObject` subtype extends `ObjectConverter.GetType` by hand; `RectObjectModelPull.PullValue`
  extends itself.
- Frozen snapshots under `Versions/V<n>/` are generated models too (`ModelContractTests`).

## Cross-folder model: effects, audio, post-processing, theme, meta

- **Effects**: `EffectObject.EffectId` points at an `EffectData` resource (`Level.Resources.Effects`):
  `Core`, `Forces` and four independently polymorphic sub-shapes - `Docs/RESOURCE_DATA.md`.
- **Audio**: a `LevelTrackEffects` slot is NULL until touched (= not in the chain); that is neither
  the `MixLevel` floor (audible) nor `LevelTrackEffects.Active` (master switch). `LevelTrack.Volume`
  multiplies the `Effects.Volumes` curve; `LevelTrack.Speed` is not keyframed and `0` is legal (no
  sentinel) - `Docs/AUDIO_TRACKS.md`.
- **PostProcessing**: `Active` defaults `true` (opposite of audio's); `PostProcessingKeyframe` has its
  own `Active`; the grading colours are `IColor3` because URP reads w as an offset; `ColorCurvesKey`'s
  curves are nullable and wrap `ClampForever` - `Docs/POST_PROCESSING.md`.
- **Theme**: `ThemeKeyframe` picks the theme, `ColorType.ThemeRef` stores a raw `int ThemeColorIndex`
  (not a `ThemeId`) into it; `ColorNames` is null normally - `Docs/RESOURCE_DATA.md`.
- **Meta**: the age rating lives on `LevelMeta` alone; difficulty is one unclamped int on `LevelMeta`, read
  through `DifficultyRules.Read`; "contains AI" is derived
  (`LevelMeta.ContainsAiContent`), never stored; `ResourceMeta` is addressed one of three ways -
  `Docs/RESOURCE_META.md`.
- **`Level.ShallowClone`** shares references and is used only by `PrefabVirtualizationUtils.Thin` -
  `Docs/PREFAB_PLACEMENT.md`, "Thin and expand".

## Docs

| Question | File |
|---|---|
| what is generated, `Update` vs `Pull`, which models are hand-written | `Docs/MODEL_CONTRACT.md` |
| track dictionary, effect slots, volume and speed | `Docs/AUDIO_TRACKS.md` |
| theme slot bands, `CompositeShape`, effect shapes and scope | `Docs/RESOURCE_DATA.md` |
| post-processing structure, colour curves, grading colours | `Docs/POST_PROCESSING.md` |
| licence default, hashes, no per-resource age rating, UTC grants | `Docs/RESOURCE_META.md` |
| enum zero values, mirrors, the texture axes | `Docs/ENUMS.md` |
| why a setting default is what it is, which group owns a field | `Docs/SETTING_GROUPS.md` |
| file and folder names | `Docs/FILE_LAYOUT.md` |
| statistics groups | `Docs/STATISTICS.md` |
