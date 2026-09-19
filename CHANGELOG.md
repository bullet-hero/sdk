# Changelog

## [Unreleased]

### Changed

- A new `Marker` is red rather than white. It is an editor annotation with no theme behind it, so
  the default is what most of them stay

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
