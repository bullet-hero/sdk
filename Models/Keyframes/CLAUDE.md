# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Keyframes

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## Keyframes

`Keyframe` (`Models/Keyframes/Keyframe.cs`) is the shared base: `Frame` (int) + `Ease` (`EaseType`,
29-value enum, stored **per-keyframe**, not per-track). Concrete keyframe types wrap one payload
field each (`FloatKey`, `Vector2/3/4Key`, `Color3Key`, `Color4Key`, `AngleKey`, `ScaKey`,
`AlignmentKey`, `UVKey`, `ZoomKey`, `ShakeKey`, `ScreenLimitKey`, `VelocityPoint`, ...). **`BoolKey`
is the one outlier** - implements bare `IFrame` (no `Ease`), since a toggle has nothing to
interpolate.

**TWO keyframe families are polymorphic in the KEY CLASS itself**, not merely in a payload value, and
both are resolved the same way - a `JsonConverterCustomType<TInterface, TType>` over an interface
carrying `GetModelType()`, exactly like the value system above:

- **4-corner texture color**, separate from the `IColor4` value system: `IColor4X4Key` → `Color4Key`
  (all 4 corners same) / `ColorHorizontalKey` (Left/Right) / `ColorVerticalKey` (Bottom/Top) /
  `Color4X4Key` (all 4 independent - `BL`/`BR`/`TL`/`TR`), via `Color4X4KeyConverter`.
- **Font size**: `IFontSizeKey` → `FontSizeKey` (one authored size, drawn as-is) / `AutoFontSizeKey`
  (`MinValue`/`MaxValue`, a band the renderer shrinks the text into so it fits its rect), via
  `FontSizeKeyConverter`. This is what makes auto sizing a property of a FRAME rather than of the
  object, and it is the reason `TextObject.FontSizes` is `List<IFontSizeKey>` rather than
  `List<FloatKey>` - **a breaking format change made deliberately and with no migrator**, since the
  track goes from bare objects to `[tag, payload]`. Which mode a frame between two keys is in follows
  the LATER key of the pair (the consumer's `FrameMath.GetGlobalFontSize`, the rule
  `GetGlobalTextEffect` already used for `FillDirection`/`AppearingMode`); both edges are still
  interpolated, because a plain key flattens into a band whose edges are equal.

**No enforced sort order.** `[RuleCollectionUnique(nameof(XKey.Frame))]` validates Frame values are
*unique* within a track's `List<TKeyframe>` - it does not enforce them being sorted. Consumers must
sort/search themselves (Unity's `LevelPlayerMath.FindBoundIndexes` assumes sorted input at that
layer, converted once at load via `LevelStateBuilder`).

**Per-key design facts.** `AutoFontSizeKey` only ever SHRINKS: `MaxValue` is the authored size and the renderer
steps down until the text fits the object's rect (the `Sizes` track), so switching from `FontSizeKey` with
`MaxValue` = the old value cannot change a text that already fitted. Word wrap changes what "fits" means. Neither
bound is checked against the other: min > max is legal authored data, and a graph finding would report it
forever since findings carry no repair. `FillmentKey` and `AppearingKey` hold a plain bounded fraction, not
`IFloat` (a random draw would make the text write itself to a different length every key), and their direction
or mode travels WITH the key like `Ease`: between two keys the LATER one wins, so one track can change mode
without being split. A fresh `ShakeKey` does nothing and snaps (zero intensity, Constant ease): a shake is
authored by raising it from nothing, and easing into a procedural offset would ramp the screen for the whole
segment before a key meant to hit ON a beat. `Velocity` is the directional force (`VelocityPoint` the radial
one); zero is neutral and the default, so a key with no value lets the player move normally. `AngleKey` is
bounded in whole turns (`ValueRules.MaxRotationTurns`) and not wrapped: 0 -> 4pi spins twice. `UVKey` holds
concrete `Vector2Value`, not `IVector2`: a random UV would tear the image every frame. A track is a plain list,
frames unique but not necessarily sorted - sorting is the consumer's job.
