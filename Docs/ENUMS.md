Design facts of the enums under `Models/Enums/` that the code cannot say by itself. A member's number is
what a file stores, so every enum here is append-only, and a zero value is chosen for what an absent key
must mean (no migration).

## Zero value: default, or not

- **Zero is the default, additive by construction:** `BotKind.None`, `ShaderType.Auto`,
  `AiGeneration.NotSpecified`, `AgeRating.Unrated` (the numeric value IS the minimum age, and an unfilled
  record never claims to be adult content), `PermissionScope.Undefined`, `CheckpointSpace.World` (a file
  from before a checkpoint carried a position reads as "respawn where the level says"; this is why
  `GameEvents` never bumped), `VSyncMode.Off` (= Unity's `vSyncCount`, so the applier is a cast),
  `TimelineTimeFormat` (a frame number is what the ruler always printed; members are ordered by detail).
- **Zero is NOT the default, and it costs nothing:** an absent JSON key is never written, so the
  constructor value survives deserialization. `LevelOrientation` (default Horizontal: every older level
  reads back as the game played it, never opted into a portrait screen), `MenuBackgroundKind`,
  `ScreenOrientationLock` (zero is Unlock: the panel follows the measured aspect, so a phone turned over
  follows; a level still outranks it via `OrientationMath.FromLevel`), `WindowMode` (default Windowed:
  the game may never put itself full screen).
- `AiGeneration` is three states, not a bool: `false` would state "no AI" for every record nobody filled
  in. Both No and Yes are the author's declaration, unverified.
- `AgeRating` is one scale, not ESRB/PEGI/RARS fields: user levels are submitted to no board.
- `PermissionScope` is stored because the cases fail differently: a `ThisLevel` grant is void once the
  resource is copied into another level, an `AnyLevel` grant survives every reuse.
- `NoLicenseSourceType` is an enum, not a `TrustedSource` key: it is the author's statement of where a
  file came from (a short, slow list of platforms issuing no license); the roster grades sites and is
  operator-owned. It left `TypicalLicenseType` because a platform is not a license, the origin duplicated
  `ResourceUrl`, and every new site would have been a new number in a format that keeps its numbers.
- `CurveTangentMode` is authored intent only; every producer writes Free. A "Broken" member does not
  belong: in Unity brokenness is an independent bit beside a per-side mode. `CurveWeightedMode` is
  `[Flags]` because something now tests one side at a time.
- `TextAppearingMode.Random` is the zero value: text resolving out of noise rather than a wipe.

## Mirrors and lockstep

- Values 1 and 2 of `LevelOrientation` and `ScreenOrientationLock` line up on purpose (as do `ControlMode`
  and the per-device mode enums at 0/1/2); `OrientationMath.FromLevel` is the one conversion. Unlock is a
  player's choice and a real answer; `NotSpecified` is an absence of opinion and never one.
- `WindowMode` mirrors `UnityEngine.FullScreenMode` value for value (this assembly may not reference
  UnityEngine) with no Maximized member and a gap at 2 that is never reused; Core's `DisplayModeUtils` is
  the one place that converts. `ScreenOrientationLock` is a lock on an AXIS, never a side.
- `ObjectTypeMask`: a bit PRESENT means EXPANDED (the inverse of a filter). Each bit's index is its
  `ObjectType` value (`ToMask` shifts by `(int)type`); `ObjectTypeMaskTests` pins the pairing.
- `ControlDevice`: four sources, each supporting all three `ControlMode`s. Listed whether or not the
  platform reaches it (`InputPlatformRules`, each driver's `IsPresent`). A gamepad is one device: the Input
  System exposes neither a pad's touchpad nor gyro on any layout it ships. One mode enum per device so an
  unsupported mode is unrepresentable. `ShortcutModifiers` has three (no Command/Super: macOS is not a
  target; the wire form is a name, so adding one costs nothing).
- `KeyBindingMask` is a small curated set: a full key enum would be a second source of truth.
- `ResourceType`: five FILE kinds (negative level-local id), four DATA kinds (a guid, only creditable via
  `ResourceMeta`), and the level cover (no id, a level has exactly one).
- `ShaderType`: Opaque and Transparent are the two render paths; Auto defers to the consumer, which must
  never change how a level looks (Auto-Opaque only when every alpha the object resolves to is 1).
- `ExpansionMode`: three rungs on one button; the middle one resolves to
  `GameEditorSettings.Interface.ExpansionMask`, the ends to every / no bit. The cycle keeps all three steps
  even when the mask makes two coincide, so the button does not depend on a setting two screens away. The
  enum lives here because settings serialize the starting rung; the cycle order is the consumer's.
- `LevelBrowserLayout` is stored twice with different defaults: the menu opens on the grid (pick by
  cover), the editor on the list (pick by name).
- `AngleDisplayUnit` is display only: angles are stored and processed in radians everywhere.
- `BotKind` carries no version: a version is the display name's ("Reflex Bot v1"), a member is what the
  player chose, and shipping v2 must not invalidate a settings file saying `Reflex`.
- `TimelineTimeFormat` names the MAXIMUM detail; a label prints only the components that carry information
  at the current grid step. The bar/beat format is the only one with unevenly spaced labels and falls back
  to `Timecode` where the tempo map has no bars.
- `VSyncMode` is desktop-only (Unity ignores `vSyncCount` on mobile) and outranks the framerate policy:
  `targetFrameRate` is ignored whenever it is non-zero, so the settings screen disables those rows.
- The whole Display group is disabled on mobile, not hidden: a phone has one window and it is the screen.

## Texture axes

The author's axes are `TextureKind` (what the picture IS, never what the device should do),
`TextureSampling` (sharp or soft is a look, not a budget), `TextureCompressionKind` (the veto half),
`TextureAlpha` and `TextureWrapKind`. The player's are `TextureSizeLimit`, `TextureFilterMode` and
`TextureCompressionQuality`; they meet in Core's `TextureLoadPlanner`.

- A kind SEEDS sampling and compression (their `Auto` reads it: PixelArt seeds Sharp and a compression
  refusal), so nothing authored before the split changed. Only an explicit value overrides.
- Kinds are stated as intent: "resampling destroys it" is true of pixel art on every device, "compress to
  BC1" is one device on one day. PixelArt is never mip-mapped (a lower mip is a blurred copy) and is the
  one kind refusing to be AVERAGED while scaled down; Gradient refuses compression (BC stores two endpoint
  colours per 4x4 block, which bands a ramp); Photo survives both best, so a device turns it down first.
- `TextureSampling.Sharp` says how the finished texture is DRAWN; it does not touch resampling (a sharp
  graphic reduced from 4096 to 1024 must still average). Mip-maps are not on the compression axis: they
  trade aliasing for memory, the opposite trade. No author axis touches the size cap, which is what bounds
  a careless level's worst case.
- `TextureAlpha` is a second axis because "uses the alpha channel its file carries" is undecidable
  elsewhere: the header proves a file CANNOT be transparent (33 bytes), but proving a present alpha is 255
  everywhere needs every pixel read. `Opaque` is an unchecked claim; a wrong one flattens a fade, hence the
  editor's "alpha dropped" report. An enum, not a bool, since `Cutout` (alpha only 0 or 1) is next.
- `TextureWrapKind` is the author's like the sub-rect: tiling reached the shader as `_BaseMap_ST` but the
  consumer hard-coded Clamp, so tiling only stretched one row of pixels.
- Player side: `TextureCompressionQuality` is a load-time cost (the device's budget), ignored by the
  ETC/EAC encoders on mobile; `Auto` derives from the kind (everything but a photo takes the careful
  encoder). `TextureFilterMode` is only how the GPU samples; the kind still wins where it claims content
  (PixelArt forces Point) and reduction is `TextureLoadPlan.BoxDownscale`. `TextureSizeLimit` is a cap,
  halved until it fits; memory falls with the SQUARE of the side. Its numbers are appended, never in
  ladder order, so the settings screen passes `LocalizedEnumDropdown` an explicit array, never
  `Enum.GetValues`.

## Other settings enums

- `AntiAliasingType`: MSAA is the default because every shape edge is real geometry; its cost grows with
  transparent overdraw. FXAA is one full-screen pass whose cost is independent of overdraw.
- `FramerateTarget` fixed refresh rate: not lower than 1, pointless above the screen's, used to save power.
