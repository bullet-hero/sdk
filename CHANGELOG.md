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

## sv 1.0.0 - 2026-09-27

### Changed

- Released together with the game. Files from this version open in every later one; a file from a
  newer version throws `NewerGenerationException` instead of reading as defaults. 1.0.0 does not yet
  promise API stability - the SDK still moves with the game

## sv 0.16.2 - 2026-09-24

### Added

- `ABLayerImport.Packed`, the new default: depth still orders what draws in front, but time decides
  the row, children take own layers 1..N and a placement is one row. weathergirl's busiest row went
  from 128 simultaneous clips to 2, and its 687 negative child layers to 0
- `ABPrefabExtractor` recovers prefab structure on import: expanded instances (`pre_iid`) become
  placements again, nested `pobjs` are inlined; repeated subtrees and duplicate templates are opt-in
  (`ExtractRepeatedSubtrees`, `MergeDuplicateTemplates`)
- `ABOptions.CollidersAbovePlayer`, off - puts everything that can hurt the player at layer 1 and up
  instead of Afterbeat's own bands

### Changed

- The Afterbeat export writes a static, untrimmed, unmodified placement as a real `prefab_objects`
  entry and leaves its copies out; any other placement is flattened as before and reported as
  `placement_flattened:<reason>`
- Exported `d` and `ed.l`/`ed.b` come from one map per scope: a level wider than `[-121, 61]` is
  ranked into depths 0..60, and the editor row is the layer's rank from the top

## sv 0.13.1 - 2026-09-21

### Fixed

- `LevelGraphAnalyzer` reads a remap table only off a genuine placement. A materialized copy is
  `PrefabObject`-typed too and its `ids` names ids in the INNER template's scope, so every remap of
  every copy was reported as broken - 38736 findings on a 700-object level, none of them true
- An override addressed at `ObjectId.PrefabRoot` is no longer reported as dangling. The root is a
  FIELD of the template and never an entry in its objects, so the probe could not find it - 19993
  findings on that same level. What is reported instead is a root override naming a field the root
  does not own, which `ApplyModifications` drops without a word

## sv 0.13.0 - 2026-09-20

### Added

- `ResourceMeta.ResourceFeatured` (`featured`), false by default - whether this resource is worth
  naming on the level's card and not only inside the full credits. Which track is "the music" is a
  fact of `level.json`, and a level's presentation may never open that file, so the record says it
  about itself. Generic rather than audio-only: cover art and a display font are the same question
  asked of another family. Declared last, so the blob's member order stays append-only

## sv 0.12.0 - 2026-09-20

### Added

- `ThemeData.ColorNames` (`clrn`), a nullable list of one label per theme slot, `ValueRules.ThemeCount`
  long when present. Null is the normal state and is what an unnamed palette writes - the slot layout
  comment in `ThemeData` says what a slot is FOR, which is a different question from what an author
  calls it
- `VectorType.RandomLerp` / `RandomLerpStep` and `ColorType.RandomLerp`, with `Vector2/3/4Lerp`,
  `Vector2/3/4LerpStep` and `Color3/4Lerp` behind them - two points, `From` and `To`, and ONE roll
  every component is read at, so the value travels the segment between them instead of filling the
  box its `Rect`/`MinMax` sibling rolls. They carry NO property-order rule: a segment has no smaller
  end, and ordering the pair would make a line that falls as it advances unrepresentable. Pinning one
  axis is the same number in both ends. `RandomCircle` has no segment form, since one roll ties its
  radius to its angle
- `ModelUtils.CopyValueList`, the null-preserving copy a value list needs now that one may be absent

- `CurveKeyframeValue.BrokenTangents` (`brkt`), false by default - whether the key's two tangents are
  meant to differ. They were always two separate numbers, so a corner was already representable; what
  was missing is whether the author meant one, without which an editor dragging a handle has to guess
- `IObjectIdCounter.CanMintObjectIds(count)` and `GetRemainingObjectIds()`, so a bulk create can ask
  for its whole run before writing any of it. `LevelRules` carries the arithmetic
  (`RemainingObjectIds`, `CanMintObjectIds`, `IsObjectIdCounterNearExhaustion`, `AssertObjectIdAvailable`)
- `GraphRule.IdCounterNearExhaustion`, a warning raised per scope once a counter passes
  `LevelRules.ObjectIdCounterWarning` - three quarters of the id range

### Changed

- The generated JSON codec writes an id-remap pair as `{"k":…,"v":…}` and reads either case. It wrote
  and read `"K"`/`"V"` while `DictionaryAsPairListConverter` used `Names.KeyShort`/`ValueShort`, so
  the two codecs disagreed about every remap table in the format - invisible while each one only read
  back what it had written
- `LevelSettings.ObjectIdCounter` and `Prefab.ObjectIdCounter` are bounded by `MaxObjectIds`
  (`int.MaxValue`), not by `MaxObjects` (262 144). The old bound was an Error whose `Fix` clamped the
  counter back down and re-issued ids that were already live
- `GetNextObjectId()` throws at the end of the id space instead of wrapping into the reserved
  negatives, which are game-space objects and the three reserved parents
- `TextureResource.TextureResourceUV` is bounded by `ValueRules.MinUv`/`MaxUv`, like `UVKey`, instead
  of inheriting `Vector4Value`'s generic 1e6
- `CurveWeightedMode` is `[Flags]`, which it always was in shape, so
  `CurveKeyframeValue.WeightedMode` validates with `RuleEnumFlagsValid` rather than `RuleEnumValid`
- A new `Marker` is red rather than white. It is an editor annotation with no theme behind it, so
  the default is what most of them stay

### Removed

- `LayerKey`. An animated `Layer` was never planned and would silently reroll every random value
  under the object, `RandomKey` being hashed on the effective layer

## sv 0.11.2 - 2026-09-18

### Added

- `ObjectTypeMask` (`Models/Enums`) - which object kinds a surface shows unfolded, one bit per
  `ObjectType`. A bit PRESENT means expanded, the inverse of how a filter mask reads, and each bit's
  index is its `ObjectType`'s own value
- `EditorInterfaceSettings.ExpansionMask` (`expansion_mask`), defaulting to every kind except
  `PrefabObject`

### Changed

- `EditorInterfaceSettings`' constructor takes the new mask, so it is a breaking signature change for
  anything building that settings group by hand
- `gen_level_audio` gives the audio track the length of the song rather than the level's whole
  `FrameDuration`. The level is the song plus its tail, and nothing sounds through the tail now
