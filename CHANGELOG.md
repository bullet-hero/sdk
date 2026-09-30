# Changelog

## sv 1.1.0 - 2026-09-30

### Added

- Resource collections, a domain of their own: `ResourceCollection` in `collection.json` (`collection_id`,
  `name`, `desc`, `authors`, `license`, `textures`, `fonts`, `audios`, `resources_meta`), one envelope
  per data resource under `prefabs/`, `themes/`, `shapes/`, `effects/`, and `CollectionId`
- `BH.SDK.Services.Collections`: `CollectionReader`, `CollectionWriter`, `CollectionArchive` (tar.gz or
  zip, OpenPGP or AES-256), and `CollectionImportPlanner` / `CollectionExportPlanner` - an import is
  `Plan`, then `Resolve` with the author's answers, and returns a `Remap`
- Resource graph utilities: `ResourceRef` as one address for every family, `ResourceGraph`,
  `ResourceClosure`, `ResourceRemap`, `ResourceNaming`
- `ResourceType` gains `Theme`, `Effect`, `Shape`, `Prefab` (addressed by guid) and `LevelLogo` (the
  cover, addressed by type alone). `ResourceMeta` gains `resource_guid`, so a record can credit a data
  resource, and `ai_generated`
- `LevelMeta.LevelAiGenerated` (`ai_generated`) and `ContainsAiContent()`, over the new
  `AiGeneration { NotSpecified, No, Yes }`
- `RunProfile.NoCollision` (`no_collision`), `GameStatistics.Tutorial` (`tutorial`: `completions`,
  `first_completed_utc`, `last_completed_utc`), `UserSettings.TutorialCompleted` (`tutorial_completed`)
- Settings: `GraphicsSettings.CollidersMode` (`colliders_mode`: `active`, `color`, `use_alpha`,
  `background`), `levels_layout` on `InterfaceSettings` and `EditorInterfaceSettings`,
  `GameEditorSettings.Publishing` (`publishing.lang`), and `cursor_return` on `KeyboardMouseControlsSettings`
  (on) and `TouchscreenControlsSettings` (off)
- `PublishProfile.CreateWorkshop()`, `CollectionReadinessAnalyzer` and five `Collection*` rules in
  `PublishRule`. `PublishIssue.Args` carries structured arguments for a localized message
- `PinScreenAspect` on `EmptyLevelGenerator` and `AudioFileLevelGenerator`, on by default: a new level
  gets a 16:9 screen limit at frame 1
- The NuGet package carries an embedded PDB with Source Link

### Changed

- Settings, `LevelStatistics`, `GameStatistics` and `LevelMeta` are written at generation 2. Files from
  1.0.0 migrate on read, but files from this version do not open in 1.0.0
- The migration keeps what a player had: `cursor_return` on stays on for both pointers, a tilt angle
  becomes the sensitivity that gives full deflection at the same angle, gyro `Relative` becomes
  `Direction`, and every AI flag starts at `NotSpecified`, never `No`
- Publishing readiness: a missing `ResourceUrl` is a warning, not an error, a missing license is a
  warning under a profile that tolerates an unknown one, and each finding names the resource, the
  licenses involved and the host
- `AvatarRules.DamageTime` is 0.15 s, down from 0.2, so knockback travels 7.5 units instead of 10.
  `MinDashFraction` no longer holds a dash to one body length
- The NuGet package is `BulletHero.SDK`, since the `BH.` prefix is reserved there. The DLL and the
  namespaces stay `BH.SDK`

### Removed

- Breaks code compiled against 1.0.0. `ModelGenerations.Release` is `V1_AlphaRelease` now, same value
- The device priority and manual selection: `DeviceSelection`, `ControlsSettings.Priority`,
  `CommonControlsSettings.Selection` / `ManualDevice` / `CursorReturn`, `RuleControlPriorityAttribute`,
  and the keys `controls.priority`, `selection`, `manual_device`
- Gyro calibration: `GyroAxisMapping`, `DeviceGyroControlMode.Relative`, and the keys `axis_mapping`,
  `calibrate_on_start`, `tilt_cntr_x`, `tilt_cntr_y`, `max_tilt_ang`
- `GamepadControlsSettings.DashButtons` (`dash_buttons`) - every button but Start and Select dashes
- Constructors of `CommonControlsSettings`, `ControlsSettings`, `GamepadControlsSettings`,
  `DeviceGyroControlsSettings`, `KeyboardMouseControlsSettings`, `TouchscreenControlsSettings`,
  `GameEditorSettings`, `UserSettings` and `PublishIssue` changed their parameters
