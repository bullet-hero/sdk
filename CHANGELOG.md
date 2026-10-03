# Changelog

## sv 1.2.0 - 2026-10-03

### Added

- Profile archives, a domain of their own: `ProfileManifest` in `profile.json` (`created_utc`,
  `game_version`, `model_generation`, `platform`, `categories`, `levels`), each level a
  `ProfileLevelEntry` (`level_id`, `folder`, `modified_utc`), and the `[Flags]` `ProfileCategory`
  (`Levels`, `Statistics`, `Settings`, `Library`, `Backups`, `Reports`, `Recordings`)
- `BH.SDK.Services.Profile`: `ProfileArchiveWriter.PackAsync`, `ProfileArchiveReader.PeekAsync` (the
  manifest alone) and `UnpackAsync`, `ProfileLayout`, and `ProfileMergePlanner.PlanLevels` - a level both
  sides hold is kept in its newer copy
- `StatisticsMerge.Merge` for `LevelStatistics` and `GameStatistics` keeps the higher of each number,
  `BestRunOrder.Compare` orders two `BestRun`s
- `ZipService.ListEntries` returns `ArchiveEntryInfo` (name, declared size, encrypted) without
  unpacking, and `ZipService.UnpackAsync` takes an `include` filter - a skipped entry is still checked,
  only the size limits count the kept ones alone
- `ArchivePolicy.MaxNameBytes` and `ArchivePolicy.Fits`, so a policy can cap entry names below the format's
  own limit. `FileNames` gains the `recordings` and profile folder and file names

### Changed

- `ZipService` packing checks entry names against the policy instead of the static cap, and
  `TarGzService.PackAsync` throws `ArgumentException` for a policy allowing longer names than tar.gz can hold

### Removed

- `ShapeSynthUtils.RoundedShape`, `FitRadius`, `Arrow`, `ArrowHead` and `MaxFilletPoints`
