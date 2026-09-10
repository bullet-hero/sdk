# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Keyframes

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## Keyframes

`Keyframe` (`Models/Keyframes/Keyframe.cs`) is the shared base: `Frame` (int) + `Ease` (`EaseType`,
29-value enum, stored **per-keyframe**, not per-track). Concrete keyframe types wrap one payload
field each (`FloatKey`, `Vector2/3/4Key`, `Color3Key`, `Color4Key`, `AngleKey`, `ScaKey`,
`AlignmentKey`, `UVKey`, `ZoomKey`, `ShakeKey`, `ScreenLimitKey`, `VelocityPoint`, ...). **`BoolKey`
is the one outlier** — implements bare `IFrame` (no `Ease`), since a toggle has nothing to
interpolate.

**TWO keyframe families are polymorphic in the KEY CLASS itself**, not merely in a payload value, and
both are resolved the same way — a `JsonConverterCustomType<TInterface, TType>` over an interface
carrying `GetModelType()`, exactly like the value system above:

- **4-corner texture color**, separate from the `IColor4` value system: `IColor4X4Key` → `Color4Key`
  (all 4 corners same) / `ColorHorizontalKey` (Left/Right) / `ColorVerticalKey` (Bottom/Top) /
  `Color4X4Key` (all 4 independent — `BL`/`BR`/`TL`/`TR`), via `Color4X4KeyConverter`.
- **Font size**: `IFontSizeKey` → `FontSizeKey` (one authored size, drawn as-is) / `AutoFontSizeKey`
  (`MinValue`/`MaxValue`, a band the renderer shrinks the text into so it fits its rect), via
  `FontSizeKeyConverter`. This is what makes auto sizing a property of a FRAME rather than of the
  object, and it is the reason `TextObject.FontSizes` is `List<IFontSizeKey>` rather than
  `List<FloatKey>` — **a breaking format change made deliberately and with no migrator**, since the
  track goes from bare objects to `[tag, payload]`. Which mode a frame between two keys is in follows
  the LATER key of the pair (the consumer's `FrameMath.GetGlobalFontSize`, the rule
  `GetGlobalTextEffect` already used for `FillDirection`/`AppearingMode`); both edges are still
  interpolated, because a plain key flattens into a band whose edges are equal.

**No enforced sort order.** `[RuleCollectionUnique(nameof(XKey.Frame))]` validates Frame values are
*unique* within a track's `List<TKeyframe>` — it does not enforce them being sorted. Consumers must
sort/search themselves (Unity's `LevelPlayerMath.FindMatchIndexes` assumes sorted input at that
layer, converted once at load via `LevelStateBuilder`).
