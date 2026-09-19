# Changelog

## [Unreleased]

### Added

- `ThemeData.ColorNames` (`clrn`), a nullable list of one label per theme slot, `ValueRules.ThemeCount`
  long when present. Null is the normal state and is what an unnamed palette writes - the slot layout
  comment in `ThemeData` says what a slot is FOR, which is a different question from what an author
  calls it
- `VectorType.RandomRectUniform` / `RandomRectStepUniform` and `ColorType.RandomMinMaxUniform`, with
  `Vector2/3/4RectUniform`, `Vector2/3/4RectStepUniform` and `Color3/4MinMaxUniform` behind them - the
  same bounds as their per-component siblings, resolved from ONE roll shared by every component.
  `RandomCircle` has no uniform form, since one roll ties its radius to its angle
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
