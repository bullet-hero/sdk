# Changelog

## sv 1.3.0 - 2026-10-17

### Added

- `LevelMeta.LevelDifficulty` (`difficulty`, 0 not rated, 1 auto, up to `DifficultyRules.ScaleMax`) with the
  `DifficultyTier` enum (`Auto` … `Singularity`) and `DifficultyRules.Read`, and `LevelMeta.LevelDemand`
  (`demand`), the level's measured load. Files from this version do not open in older ones
- Avatar skins, a domain of their own: `AvatarSkin` (`id`, `name`, `size`, `columns`, `rows`, `mask`, `cells`,
  `parts`, `colors`, `meshes`, `elements`, `eyes`, `hands`, `shadow`, `move_effect`, `dash_effect`,
  `damage_effect`, `dash_body`), `AvatarSkinPart` (`shape`, `color`, `rotation`, `mirror`, `scale`),
  `AvatarSkinShape` (`id`, `vts`, `idxs`) and the `[Flags]` `AvatarSkinElements`, one `<id>.json` per skin
- `AvatarSkinSelection` (`skin`) in `skins/selection.json`, `AvatarSkinPack` (`skins`), the `AvatarSkinId`
  primitive, `AvatarSkinRules`, `AvatarSkinDefaults`, and `AvatarSkinValidation.Validate`: 1-8 colours, at
  least 5 cells in one connected body, `cells` matching `parts`, clean triangles in every own mesh
- `BH.SDK.Services.Skins`: `AvatarSkinGenerator`, `AvatarSkinPresets`, `AvatarSkinShapes` and
  `AvatarSkinSilhouettes`
- Performance rating: `DeviceProfile` in `device_profile.json` (`benchmark_version`, `device_model`,
  `gpu_name`, `screen_width`, `screen_height`, `gpu_timing`, `cpu_base`, `gpu_base`, `cpu_coefficients`,
  `gpu_coefficients`, `cpu_fit`, `gpu_fit`, `area_factors`, `drift_error`, `trusted`, `measured_at`),
  `LevelDemand` (`analyzer_version`, `exact`, `windows`) of `DemandWindow`s (`start`, `values`) over
  12 `DemandDimension`s, and `PerformanceRules`
- `BH.SDK.Utils.Performance`: `LevelDemandCounter`, `PerformanceScore.Evaluate` into a `PerformanceBand`
  (`Holds` … `DoesNotHold`), the `ReferenceDevice` profiles, `BenchmarkMath` and `PerformanceValidation`,
  plus `StressLevelGenerator` for the stress levels a device is measured on
- `AchievementsRecord` in `achievements.json` (`unlocked`, each an `AchievementUnlock` of `id` and
  `unlocked_utc`), merged by `StatisticsMerge.Merge` as a union keeping the earliest time
- `CloudManifest` in `cloud.json` (`levels`, each a `CloudLevelEntry` of `level_id`, `folder`,
  `modified_utc`, `deleted`) - the state of the last cloud sync
- `ProfileCategory.Skins`, part of `All` and mapped to the `skins` folder. Profile archives with it do not
  open in older versions
- Four `PublishRule`s comparing a level's license with its resources': `ResourceForbidsAdaptation`,
  `LevelLicenseShareAlikeMismatch`, `ResourceShareAlikeConflict` and `LevelLicenseOmitsNonCommercial`, built on
  the new `LicenseTerms` and `[Flags]` `LicenseElements`
- `BH.SDK.Agent`, the protocol of the MCP server (`AgentProtocol`, `AgentEndpoint`, the messages,
  `TimeAddress`, `MusicGrid`), and the level's `ai/` folder: `AiFolderManifest` (`format`, `share`) and
  `AiContextDocument`. `LevelArchiveBuilder.BuildAsync` takes a `LevelExportMode` and packs `ai/` only when
  `share` is true and the export is not protected
- `BH.SDK.Services.AudioAnalysis` (`AudioAnalyzer.Analyze`, `AudioMap`, `Fft`), `GeneratorSchema.Build` (a
  JSON schema of a generator's parameters), `GeneratorHints.Builder.Describe` with a description on every
  built-in generator's field, `ShapePolygonUtils`, `FrameRules.FrameAtProgress`

### Changed

- `AvatarSkin` is a `sealed partial` model instead of an empty class, and `LevelMetaV1ToV2` now returns the
  frozen `LevelMetaV2` - code deriving from the first or calling the second directly no longer compiles
- `QuantizeKeyframesGenerator` snaps to the level's beat grid by default (`UseBeatGrid`), and its BPM step is
  no longer rounded to whole frames
- `ResourceRules.MaxCompositeShapes` is 1024, up from 256
- `CollectionReadinessAnalyzer` no longer folds license-compatibility issues into its collection-wide
  resource issues

### Fixed

- The generated JSON reader reads a `ulong` above `long.MaxValue` (`JsonPrimitives.ReadUInt64`), which
  `AvatarSkin.mask` needs
