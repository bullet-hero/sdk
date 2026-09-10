# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/SettingGroups

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## `Models/SettingGroups/` — one folder, two unrelated aggregates

`LevelSettings` (`Level.Settings`, per-level: `Framerate`, `FrameDuration`, `ObjectIdCounter`,
`AudioIdCounter` — the `IObjectIdCounter` implementation — and `Seed`) has nothing to do with the
rest of this folder (`GeneralSettings`/`ControlsSettings`/`AudioSettings`/`GraphicsSettings`/
`GameEditorSettings`/`InterfaceSettings`), which are all sub-groups of `UserSettings` (per-device,
`settings.json`).

`KeybindingsSettings` is the newest of them and the one whose shape is unlike the rest: a sparse
`Dictionary<string, string>` of shortcut id to binding string, holding only what a player actually
rebound. It is also **the SDK's first serialized string-keyed dictionary**, which needs no converter
at all - every existing dict converter in `SerializationService.GetConverters()` exists solely because
Newtonsoft's default dictionary serialization throws for value-type keys, and a `string` key takes its
happy path. What a binding string may SAY lives in `Utils/ShortcutSyntax` (and is validated by
`RuleShortcutBindings`); which KEYS exist deliberately does not - there is no `KeyToken` enum here,
for the reason `KeyBindingMask`'s own header gives, and the consumer resolves the name against
`UnityEngine.InputSystem.Key`. An empty value is a real state ("the player unbound this") and is not
the same as an absent key ("the player never touched it"). It shipped additively like everything else
here, so `UserSettings` stays at generation 1.

`InterfaceSettings` is the newest of them (the game's own overlays — the diagnostics readout's
`StatsActive` + `StatsFrameObjects`/`StatsLevelObjects`/`StatsMemory` + `StatsAlignmentX`/`Y`, plus
`OpenMenuOnLose`, which is a BEHAVIOUR rather than an
overlay: off — the default — a lost run rewinds itself to the last checkpoint it reached instead of
opening the result window, see root `CLAUDE.md`, "Checkpoints") and shipped **without bumping the
`UserSettings` domain**: an
additive property whose constructor supplies a default needs no snapshot and no migrator, exactly like
`LevelSettings.Seed` and `GameEvents.Beats`. Its alignment pair is two free `[0,1]` floats rather than
a nine-value enum, because it is the same convention level content is authored in (`0,0` lower-left) —
the settings screen offers the nine presets, a hand-edited value between them is legal data.
The three block switches are all **false** by default, which here is the zero value — and for the
first two it is also the behaviour they shipped with. `StatsMemory` is the exception and the only
one of the group that TOOK something away: that block used to be drawn whatever the file said, so a
`settings.json` written before the key reads back without it. Rule 11 is what makes that a one-line
change rather than a migrator. The level switch gates a WALK rather than a label: the consumer's
overlay collects `LevelStatsUtils` once a second, which is O(objects) over a level the editor may be
holding tens of thousands of, so off it collects nothing at all.
`GameEditorSettings.Grid` (`ActiveDefault`/`Size`/`Opacity` — the editor's viewport grid: on at
startup, one world unit per cell and a quarter opacity by default, floored at
`ValueRules.MinGridSize` and ranged `[0,1]`) shipped the same way and is worth reading as the worked
example of what belongs here at all: how the grid LOOKS and where a session STARTS is how the author
works and is remembered, while whether it is currently drawn is the current view and stays in the
editor's session (`Services.GameEditor`'s `GridModeService`, which reads `ActiveDefault` once and
never polls it) — the same split the active gizmo has, and the same pair of names the preview
player's `ActiveDefault` already uses. `ActiveDefault` defaults to **true** despite being a `bool`,
for the reason `LevelOrientation.Horizontal` defaults away from its enum's zero: a file written
before the field reads back as the field's default, so the default is also what decides whether the
addition needs a migrator. Opacity is the only part of its colour anyone authors; the hue is derived from the
camera background live, which is why there is no grid colour here.

`AntiAliasingGraphicsSettings` (`Type`/`Msaa`/`Hdr`) is the one graphics sub-group that does **not**
derive from `BaseGraphicsSettings`, and the omission is deliberate: an inherited `Render` would mean
"is anti-aliasing on", which is exactly what `Type = None` already says, and two switches for one
decision can disagree. It shipped additively like everything else here — the domain stays at
generation 1, and a settings file written before it deserializes to the constructor's defaults (MSAA,
x2, no HDR) rather than to a zeroed pair that would read as "off". `MsaaType`'s value **is** its
sample count, except `None = 0`, which every graphics API states as 1 — convert with
`MsaaTypeExtensions.ToSampleCount`, never a cast.

`TexturesGraphicsSettings` (`Compression`/`SizeLimit`/`Mipmaps`/`Filtering`/`CompressionQuality`) is
the newest of them and the one whose design is half outside this repo: it is the DEVICE's half of how
a level's images are loaded, while the author's half is six fields on the image itself
(`TextureResource`'s `Kind`/`Alpha`/`Sampling`/`Compression`/`WrapU`/`WrapV`). Neither can express
the other's half - a level has to play the
same everywhere, so an author may not author a device's memory budget, and a player must never be
asked what a picture depicts. Every field defaults to `Auto` and resolves per platform in the
consumer (`Core`'s `TextureLoadPlanner`), which is what makes an older `settings.json` with no
`"textures"` key correct rather than merely tolerated. Additive like everything else here, so
`UserSettings` stays at generation 1.

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

The `UserSettings` sub-groups are the reason `IMoveable<T>`'s `Pull(source)` exists at all — an
in-place merge that keeps every sub-group instance, since the device hands them out one at a time.
It is part of `IModel<T>` now, so every model has one; see "`IModel<T>` pattern" for how it differs
from `Update`.

`LevelSettings.Seed` is the level's own random seed, and **`LevelRules.NullSeed` (0) is its
default and means "not authored"**, not seed number zero — test it with `LevelRules.IsValidSeed`,
never with a literal (same shape as `AudioRules.IsActiveMixLevel`). `LevelRules` carries **two
ranges, and conflating them is the mistake to avoid**: `[MinValidSeed, MaxValidSeed]` = `[1,
int.MaxValue]` is what a REAL seed is and what every generator must draw from (`IsValidSeed`/
`AssertSeed`), while `[MinSeed, MaxValidSeed]` is what a seed *field* may hold, `NullSeed` included
(`IsSeedInput`/`AssertSeedInput`/`ClampSeed`, and what `[RuleMin]` validates). A generator that
could return 0 would occasionally produce a run nobody can reproduce, since 0 reads as "unseeded"
one level load later. A level ships without one and the
consumer generates a fresh seed on every load, which is the ordinary case; an author sets it only to
pin a run down, and a host may still override it per-launch. The consumer side of that three-tier
ladder lives in the Unity project (`Core`'s `SettingsGroup`, see its CLAUDE.md "Determinism") — the
format only stores the middle tier. Adding the field needed no migration: the domain stays at
generation 1 and an older file simply deserializes to 0.
