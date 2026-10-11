Design facts of the settings groups in `Models/SettingGroups/` that the code cannot say by itself: why a
default is what it is, why a field lives in one group rather than another. A contributor adding or changing a
setting reads this first. `Models/SettingGroups/CLAUDE.md` is the structural map (which aggregate is which);
every number a player sees is in `DocsPublic/`.

Two rules run through the whole tree. **Settings belong to the PERSON, not the level**: two authors may
disagree about what a new shape should start at, and nothing here is read at playback once a keyframe is
written. **A current view is session state in a consumer, what a session STARTS with is remembered here**
(`EditorGridSettings`, `EditorPlayerSettings.ActiveDefault`, the expansion rungs, `EditorEffectsSettings`).

## Audio (`AudioSettings`)

- The editor has its own fader for level audio because a level sounds the same in both places and is
  listened to differently: a player hears it once through, an author hears eight bars a hundred times and
  wants it down without turning the game down. It covers the level audio the editor PLAYS (music and its
  tracks); auditioning a resource is a preview, the editor's own clicks are `EditorUI`. Full by default (the
  value it replaces while the editor is open).
- Interface sound defaults to HALF where the others are full: interface sounds are feedback on a press
  already made, the music is what the player came for.
- The editor is its own category because its sounds are earned differently: an author is in it for hours,
  so it sounds OUTCOMES only (a save, a refusal, an undo, a deletion), never the ordinary press.

## General, Keybindings

- The "show levels the source does not claim" setting is about discovery, not one store: a source can say
  what belongs to this player or what is lying there, and the two disagree (an unsubscribed workshop item
  whose files remain, a hand-copied level, an unfinished download). Off by default, because the narrow answer
  is the honest one; a source with one answer (the player's own folder) ignores it. Today only the Steam
  workshop differs.
- An empty language means "follow the device", so it carries no `RuleStringPattern`: the pattern demands two
  letters and its fix would rewrite empty to `en`.
- `KeybindingsSettings` is a SPARSE map: only entries a player moved. Shortcut ids, names and default keys are
  code and change with it, so an absent id takes the catalog default and "reset" is a removal. The value is a
  string (`ctrl+shift+d`, owned by `ShortcutSyntax`) because `settings.json` is a file people open. An unbound
  entry is the empty string, so "overridden" is not `ContainsKey`'s question. An unknown id or key name is
  harmless: neither resolves.

## InterfaceSettings

- The alignment pair is two free floats in level-content convention (0,0 lower left, 1,1 upper right), the
  screen offers nine presets, a hand-edited 0.25,0.9 is legal; outside [0,1] would park the overlay off screen.
- Defaults that name existing behaviour are not the zero value: HUD flags true, the HUD hitbox ring above
  zero (the drawn body is 1x1 against a collision radius of `AvatarRules.CollisionScale`, so the silhouette
  overstates the hitbox by more than half its area and levels are balanced for someone who can see it; the
  slider is legibility, 0 turns it off), the error window true.
- The lost-run flag is false: a death used to stop into the result window, four clicks between a player and
  the retry they chose; off, a lost run rewinds to the last checkpoint (`CheckpointService`).
- The diagnostics overlay blocks default off (the overlay is opened to read a frame time). The memory block was
  drawn unconditionally, so its default is a removal. The level block gates an O(objects) walk once a second
  and the UI census counts every element (eighteen thousand on the editor screen), so off collects nothing; the
  census does nothing in a release build, where every profiler marker compiles out.
- The shell's orientation is not the game's: a running level's `LevelSettings.Orientation` outranks it
  (`OrientationMath`); honoured on phones only, and the stored value is left untouched on desktop so a value
  chosen on a phone survives the same account on a PC.
- The HUD hide flags hide what is DRAWN, never what is there: the pause button still takes a press, so the
  implementation uses opacity, not display or visibility (both stop an element being picked).
- The error flag gates the WINDOW, never the report: `ErrorReportService` builds and publishes every report
  anyway, so turning it off cannot hide an error from a bug report.
- The four true-by-default flags are not constructor parameters (a source break for every caller); they are
  defaulted in both constructors so a built value and a freshly defaulted one compare equal.

## LevelSettings

`LevelRules.NullSeed` (0) means "no seed authored", not seed zero: the middle tier of the three-step seed
resolution, so a host may still override it per launch. Test it with `LevelRules.IsValidSeed`.

## Controls (`Models/SettingGroups/Controls`)

- The base group holds what every device shares and NOT the mode: each device stores its own mode enum so an
  unsupported mode is unrepresentable. `GeneralMode` maps a device's enum onto the shared `ControlMode`. These
  settings describe how the AVATAR is steered, nothing else; the menu and editor drive themselves.
- Gyro has nothing local: Rest is the phone lying flat read against gravity - no calibration, no stored neutral
  point, no axis mapping (each made the neutral pose depend on how the phone was held earlier, and players read
  that as a broken sensor). The shared Sensitivity decides reach (1 is full deflection at
  `ControlsRules.DefaultTiltAngle`). A gyro has no buttons, hence `DashSource`: a tap anywhere is the default.
- Keyboard and mouse: Space and Shift both dash (the editor CLAIMS Space for play/pause through
  `ControlService.ClaimKeys`, so the driver never reads it there). The cursor hides only WHILE steering and is
  never captured; the two modes default apart (Absolute puts the avatar where the arrow is, Relative moves the
  in-world cursor BY it). A mouse dash button makes the mouse a complete one-handed controller, and is not
  excluded when it equals the hold button (a press is an edge, a hold is a level). Letting go of the hold
  button is a FEEL question answered per device: a mouse player releases to stop (the cursor returns to the
  avatar), a finger lifts constantly to reposition (the touchscreen default leaves it).
- Touchscreen: the Direction-only layout block is stored here because it only exists with this device in
  Direction mode. Sizes are fractions of screen height and anchors the nine reach positions, so a layout lands
  alike on every phone; Handedness mirrors the whole set at once.

## Editor groups

- Camera: the whole pan and zoom gesture is a preference here; what stays in the project asset is not (the
  start position and size, the pinch epsilon). Inverted drag defaults on ("drag the content").
- Creation: an empty `Sizes` track reads zero, so a type that draws something gets an explicit first size
  keyframe (`SizeSeed`), which an author can delete and a per-type fallback never could. Text is wider than
  tall because a `TextObject`'s size is the block its glyphs lay out in, not the glyphs. Four floats, not two
  vectors: a settings group is a flat list of scalars.
- Effects: the model default is true (the behaviour the editor already had), while a `settings.json` BORN on a
  phone is seeded false by `RootEntryPoint`: a default answers "what did this field mean before", a seed
  "what does this device want".
- Gizmos is its own group: a gizmo is what an author DRAGS, a selection is what they PICK.
- Grid: alpha is the only authored colour decision (the hue inverts the camera), and the camera's own alpha
  never takes part, or a fading background would take the grid with it. The grid is off at session start: on a
  phone the lines read as content.
- Interface: named for the EDITOR's interface, unrelated to `UserSettings.Interface` (both use the `iface` key
  and never co-occur). The commit debounce is read when a view is CONSTRUCTED, so a change reaches the editor on
  the next open; zero commits on every keystroke. Inframes are off (engine-owned rows, a diagnostic view). The
  shape-collider link mode modifies one action (picking a shape writes its id into `ColliderId`) and picking
  None never reaches the collider, since a Null `ShapeId` beside a real `ColliderId` is how an invisible hitbox
  is authored. Tree and timeline keep separate sync answers: a fold CLOSES a row in a tree and REMOVES one from
  a timeline. The save-findings line is one `UserLogger` entry (console row, notification, sound), silent by
  default because findings are advisory. `ExpansionMask`: a bit present means UNFOLDED; the default is
  everything but prefabs, and only the STARTING rung of each surface is stored.
- Player: the preview toggle starts OFF everywhere (touch gives the avatar the whole viewport the moment it
  exists). The bot is a bool, not a choice: the only fitting bot is the reflex bot (no preparation, survives a
  scrub); a run's choice is `LevelPlayerInfo.Bot`.
- Savings: the autosave numbers and the history depth answer one question - how much work an author will
  lose - and trade disk against memory. The second ceiling counts objects, because a node costs what its
  operation cached (deleting fifty thousand objects is ONE node): zero is no limit, positive is that many,
  negative lets the device decide (`HistoryBudgetPlanner`). Auto resolved per platform, never a value SEEDED
  into a fresh file: a seeded default is indistinguishable from a choice and can never be revised.
- Selection: multi-selection is a modifier on desktop and a mode on touch (a long press ignores the setting
  either way). The long-press numbers are an accessibility gesture, so they live here, not in an asset. The
  collider fill is off by default (it covers what was just selected). Whole-rect picking is off (a shape is
  clicked by what it draws) and does not rescue a size-less group node, which has no rect.
- Serialization: the write format is split by what is written (level vs library resource) and never affects
  reading (`PathUtils.FindDataFile`). The clipboard has no format: it is text through the OS buffer; the key
  `copy` is retired and never reissued.
- Timeline: only five of the timeline's settings are here - those whose wrong value is uncomfortable, not
  WRONG (lane heights, pixels-per-frame and overscans stay in the asset). The loop answer is per timeline (a
  local loop repeats one object's span). One time format serves every ruler AND the playhead readout. The
  reset-tool flag defaults on: a blade left armed on a tab is the one way a tool change costs an unasked edit.

## Graphics

- Anti-aliasing is not a `BaseGraphicsSettings` (an inherited Render would be a second switch for what `Type =
  None` says). HDR lives with it: the same decision as the sample count, paid in the same tile memory; the game
  ships it off because nothing rendered needs values above 1 and HDR doubles the bandwidth MSAA multiplies.
- Avatar: the body is a grid of 64 cells health takes apart; off collapses it to one square with health as
  transparency. True everywhere including phones (64 inframe squares is little next to a level, and it is the
  main readable feedback of a run). Render here is not "draw the avatar".
- Colliders mode: not a `BaseGraphicsSettings` (Render would read opposite to switching a mode on); Active is the
  one switch, off by default. The hue is a setting now (shared with the editor's collider overlay); the guard
  against an invisible fill is the consumer's `CollidersOnlyMath.MinAlpha` plus Reset. Alpha is read only while
  `UseAlpha` is on. The background is not black, which is the letterbox colour.
- Display (desktop half): inert on mobile, disabled not hidden (a control that vanishes per platform makes a
  screenshot of the screen unreadable). Resolution is two ints with a zero sentinel meaning "the display's own"
  (like `LevelRules.NullSeed`); they decide the aspect and therefore the orientation the UI lays out for on
  desktop, so this group and `ScreenOrientationLock` are two halves of one feature. The refresh rate is NOT
  stored: `Screen.SetResolution` without one keeps the current rate, a stored one is wrong when the window moves
  monitor. Nothing resolves per platform: desktop honours all of it, a phone none.
- Effects budgets are simulation STEPS (GPU dispatches), separate numbers: the first is per effect and decides
  how a replay LOOKS (a replay rebuilds a graph from empty, each step one spawn cohort, too few and a stream
  returns as visible packets), the second is per frame and decides what the worst frame COSTS. Catching a
  running graph up after a frame drop is a constant in GamePlayer, since a wrong value there feeds a longer
  frame into an even longer one.
- Post-processing: one switch per effect, mirroring `PostProcessingEvents` field for field.
- Textures: the device's half of image loading; the author's half is `TextureResource`'s six fields, which say
  only what is true of the PICTURE. They meet in Core's `TextureLoadPlanner`. Every field defaults to Auto and
  resolves per platform; none is a `BaseGraphicsSettings` Render ("do not render textures" is not a state). The
  encoder EFFORT is the player's outright; filtering is shared - the author's `TextureSampling` wins over Point,
  while Bilinear against Trilinear stays a device cost. Read when a level's resources LOAD, so nothing pushes
  them through `SettingsApplier`.

## Per-group notes (moved from `Models/SettingGroups/CLAUDE.md`)

Moved here whole from the folder file, which keeps the traps. Some of it restates the sections above
in more detail. **Every "no bump" / "no migrator" below describes an addition made before 1.0.0**
(all of them landed between 2026-08 and 2026-09-19), when an additive member was free. Since the release
an added member always bumps its domain (`Docs/VERSION-BUMP.md` in the Unity project, "What does NOT
need a bump") - `UserSettings` is at `V2_SimplifyEntrance` for exactly that reason.

`LevelSettings` (`Level.Settings`, per-level: `Fps`, `FrameDuration`, `ObjectIdCounter`,
`AudioIdCounter` - the `IObjectIdCounter` implementation - and `Seed`) has nothing to do with the
rest of this folder (`GeneralSettings`/`ControlsSettings`/`AudioSettings`/`GraphicsSettings`/
`GameEditorSettings`/`InterfaceSettings`), which are all sub-groups of `UserSettings` (per-device,
`settings.json`).

`KeybindingsSettings` is the one whose shape is unlike the rest: a sparse
`Dictionary<string, string>` of shortcut id to binding string, holding only what a player actually
rebound. It is also **the SDK's first serialized string-keyed dictionary**, which needs no converter
at all - every existing dict converter in `SerializationService.GetConverters()` exists solely because
Newtonsoft's default dictionary serialization throws for value-type keys, and a `string` key takes its
happy path. What a binding string may SAY lives in `Utils/ShortcutSyntax` (and is validated by
`RuleShortcutBindings`); which KEYS exist deliberately does not - there is no `KeyToken` enum here,
for the reason `KeyBindingMask`'s own header gives, and the consumer resolves the name against
`UnityEngine.InputSystem.Key`. An empty value is a real state ("the player unbound this") and is not
the same as an absent key ("the player never touched it"). It shipped additively like everything else
here, so `UserSettings` took no bump for it.

`InterfaceSettings` holds the game's own overlays (the diagnostics readout's
`StatsActive` + `StatsFrameObjects`/`StatsLevelObjects`/`StatsMemory`/`StatsProfiling` + `StatsAlignmentX`/`Y`, plus
`OpenMenuOnLose`, which is a BEHAVIOUR rather than an
overlay: off - the default - a lost run rewinds itself to the last checkpoint it reached instead of
opening the result window, see the Unity project's `Docs/Architecture/CHECKPOINTS.md`, "What a death does") and shipped **without bumping the
`UserSettings` domain**: before 1.0.0 an
additive property whose constructor supplied a default needed no snapshot and no migrator, exactly like
`LevelSettings.Seed` and `GameEvents.Beats`. Its alignment pair is two free `[0,1]` floats rather than
a nine-value enum, because it is the same convention level content is authored in (`0,0` lower-left) -
the settings screen offers the nine presets, a hand-edited value between them is legal data.
The four block switches are all **false** by default, which here is the zero value - and for the
first two it is also the behaviour they shipped with. `StatsMemory` is the exception and the only
one of the group that TOOK something away: that block used to be drawn whatever the file said, so a
`settings.json` written before the key reads back without it. It landed before 1.0.0, when Rule 11
still made that a one-line change rather than a migrator. The level switch gates a WALK rather than a label: the consumer's
overlay collects `LevelStatsUtils` once a second, which is O(objects) over a level the editor may be
holding tens of thousands of, so off it collects nothing at all.
`GameEditorSettings.Grid` (`ActiveDefault`/`Size`/`Opacity` - the editor's viewport grid: on at
startup, one world unit per cell and a quarter opacity by default, floored at
`ValueRules.MinGridSize` and ranged `[0,1]`) shipped the same way and is worth reading as the worked
example of what belongs here at all: how the grid LOOKS and where a session STARTS is how the author
works and is remembered, while whether it is currently drawn is the current view and stays in the
editor's session (`Services.GameEditor`'s `GridModeService`, which reads `ActiveDefault` once and
never polls it) - the same split the active gizmo has, and the same pair of names the preview
player's `ActiveDefault` already uses. `ActiveDefault` defaults to **true** despite being a `bool`,
for the reason `LevelOrientation.Horizontal` defaults away from its enum's zero: a file written
before the field reads back as the field's default, so the default is also what decides whether the
addition needs a migrator. Opacity is the only part of its colour anyone authors; the hue is derived from the
camera background live, which is why there is no grid colour here.

**Three settings shipped with the editor's Medium fix pass and each is the same additive shape.**
`GameEditorSettings.Interface.HierarchyExpansion` / `.TimelineExpansion` are the RUNG each
folding surface opens a level at (`ExpansionMode`, which moved into `Models/Enums/Settings/` from
the consumer for this - a serialized member may not be typed by a consumer's enum). They default
to `Collapsed` and `Expanded`, which is what the two surfaces did before they were configurable,
and they disagree for the reason stated above `SyncTimelineExpansion`: a fold closes a row in a
tree and REMOVES one from a timeline. Only the STARTING rung is stored - which rung a surface is
currently on stays the consumer's session state, the same split `EditorGridSettings.ActiveDefault`
makes. `GameEditorSettings.Timeline.ToolResetOnTabChange` (default **true**) is what let the
editor's timeline tool become per-tab at all: with it on, the tab arrived at comes back on the
Selection tool rather than on a blade left armed there. `AudioSettings.EditorGame` (default
**1**, like `Game`) is the level's own volume while the EDITOR is playing it - the same level
through the same path, listened to for hours instead of once, and the consumer picks between the
two faders with a claim taken for the editor scope's lifetime.

`GameEditorSettings.Interface.ExpansionMask` is the same shape, one layer up from a bool:
`ObjectTypeMask`, a bit per `ObjectType`, and a bit PRESENT means that kind of object is shown
UNFOLDED - the inverse of how a filter mask reads, which is the one thing about it that gets read
backwards. It is what the MIDDLE rung of the fold button means in the frame hierarchy and the object
timelines (`Services.GameEditor`'s `ExpansionMode`), one mask for both surfaces and both scopes,
because the rung is one rung. Which rung a surface is currently ON is view state and stays in the
session, exactly as the grid's ActiveDefault/current split does. Defaults to everything-but-prefabs,
since a placement's materialized children are the one subtree an author almost never wants listed
row by row.

`AvatarGraphicsSettings` is the smallest group here and carries no member of its own: the inherited
`Render` IS the setting, and what it switches is whether the player avatar's body comes apart cell by
cell as health falls (the consumer's `ShatterGridMath` - 25 squares for the body and 25 for its
shadow) or collapses to one square each that health fades instead. Two readings of one number, one of
them a twenty-fifth of the objects, which is what makes it a graphics option rather than a
preference. **True on every platform**, phones included: 50 inframe squares are not much next to
what a level draws, and it is the game's main piece
of readable feedback about how a run is going. Additive like everything else here, so `UserSettings`
took no bump for it.

`AntiAliasingGraphicsSettings` (`Type`/`MSAA`/`HDR`) is the one graphics sub-group that does **not**
derive from `BaseGraphicsSettings`, and the omission is deliberate: an inherited `Render` would mean
"is anti-aliasing on", which is exactly what `Type = None` already says, and two switches for one
decision can disagree. It shipped additively like everything else here - the domain took no bump
for it, and a settings file written before it deserializes to the constructor's defaults (MSAA,
x2, no HDR) rather than to a zeroed pair that would read as "off". `MsaaType`'s value **is** its
sample count, `None = 1` included (no multisampling is one sample per pixel), so a cast is the
conversion.

`TexturesGraphicsSettings` (`Compression`/`SizeLimit`/`Mipmaps`/`Filtering`/`CompressionQuality`) is
the one whose design is half outside this repo: it is the DEVICE's half of how
a level's images are loaded, while the author's half is six fields on the image itself
(`TextureResource`'s `Kind`/`Alpha`/`Sampling`/`Compression`/`WrapU`/`WrapV`). Neither can express
the other's half - a level has to play the
same everywhere, so an author may not author a device's memory budget, and a player must never be
asked what a picture depicts. Every field defaults to `Auto` and resolves per platform in the
consumer (`Core`'s `TextureLoadPlanner`), which is what makes an older `settings.json` with no
`"textures"` key correct rather than merely tolerated. Additive like everything else here, so
`UserSettings` took no bump for it.

**`Filtering` and `CompressionQuality` were both DERIVED from the author's kind before they existed,
and only `CompressionQuality` belonged here**: the encoder's effort yields the same size in the same
format either way, so all it trades is the player's own loading time. `Filtering` turned out to be
SHARED, because its four members conflate two questions - sharp against soft is a LOOK, costs the
same on every GPU and is therefore the author's (`TextureSampling` overrules `Point`), while bilinear
against trilinear is a real device cost and stays here. Their `Auto` reproduces the old derivation,
which is why neither needed a migration either.

**`TextureSizeLimit`'s two newest rungs are `Side512 = 5` and `Side8192 = 6`, out of ladder order on
purpose**: a member's number is what a settings file stores, so a rung is APPENDED and never
renumbered - the rule `RandomTracks` states for its track ids. The consumer therefore may not present
these through `Enum.GetValues`; `Services.Shared`'s `SettingsEnumOrders` is the explicit order, and
`SettingsEnumOrdersTests` is what fails the day a rung is added and forgotten there.

The `UserSettings` sub-groups are the reason `IMoveable<T>`'s `Pull(source)` exists at all - an
in-place merge that keeps every sub-group instance, since the device hands them out one at a time.
It is part of `IModel<T>` now, so every model has one; see `Docs/MODEL_CONTRACT.md`, "`Update` and `Pull`", for how it differs
from `Update`.

`LevelSettings.Seed` is the level's own random seed, and **`LevelRules.NullSeed` (0) is its
default and means "not authored"**, not seed number zero - test it with `LevelRules.IsValidSeed`,
never with a literal (same shape as `AudioRules.IsActiveMixLevel`). `LevelRules` carries **two
ranges, and conflating them is the mistake to avoid**: `[MinValidSeed, MaxValidSeed]` = `[1,
int.MaxValue]` is what a REAL seed is and what every generator must draw from (`IsValidSeed`/
`AssertSeed`), while `[MinSeed, MaxValidSeed]` is what a seed *field* may hold, `NullSeed` included
(`IsSeedInput`/`AssertSeedInput`/`ClampSeed`, and what `[RuleMin]` validates). A generator that
could return 0 would occasionally produce a run nobody can reproduce, since 0 reads as "unseeded"
one level load later. A level ships without one and the
consumer generates a fresh seed on every load, which is the ordinary case; an author sets it only to
pin a run down, and a host may still override it per-launch. The consumer side of that three-tier
ladder lives in the Unity project (`Core`'s `SettingsGroup`, see the Unity project's `Docs/Architecture/RANDOMNESS.md`, "The seed ladder in code") - the
format only stores the middle tier. Adding the field needed no migration: the domain stays at
generation 1 and an older file simply deserializes to 0.
