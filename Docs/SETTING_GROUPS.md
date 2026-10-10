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
