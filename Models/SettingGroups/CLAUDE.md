# CLAUDE.md - Assets/Plugins/BH.SDK/Models/SettingGroups

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.

## One folder, two unrelated aggregates

`LevelSettings` (`Level.Settings`, per-level: `Fps`, `FrameDuration`, `ObjectIdCounter`,
`AudioIdCounter` - the `IObjectIdCounter` implementation - and `Seed`) has nothing to do with the
rest of this folder (`GeneralSettings`/`ControlsSettings`/`AudioSettings`/`GraphicsSettings`/
`GameEditorSettings`/`InterfaceSettings`), which are all sub-groups of `UserSettings` (per-device,
`settings.json`).

Why each default is what it is, and every group's design: `Docs/SETTING_GROUPS.md` (the per-group
notes at its end are this folder's former text, whole).

## Traps

- **A new setting member bumps `UserSettings`** (root Rule 11): "additive with a constructor default,
  no migrator" was the pre-1.0.0 rule, and every such note in `SETTING_GROUPS.md` dates from then.
  The constructor default still decides what the field starts at (`ActiveDefault` defaults to `true`).
- **Only what a session STARTS with is stored here**; what a surface is currently showing is the
  consumer's session state (`EditorGridSettings.ActiveDefault`, the expansion rungs).
- **A serialized member may not be typed by a consumer's enum** (`ExpansionMode` moved into
  `Models/Enums/Settings/` for this).
- **`ExpansionMask`: a bit PRESENT means UNFOLDED** - the inverse of a filter mask.
- **`KeybindingsSettings` is a sparse string-keyed dictionary**: an empty value ("unbound") is not an
  absent key ("never touched"). Binding syntax lives in `Utils/ShortcutSyntax`; no `KeyToken` enum.
- **`AntiAliasingGraphicsSettings` deliberately does not derive from `BaseGraphicsSettings`**;
  `MsaaType`'s value is its sample count (`None = 1`).
- **An enum rung is APPENDED, never renumbered** (`TextureSizeLimit`'s `Side512 = 5`,
  `Side8192 = 6`); the consumer orders it through `SettingsEnumOrders`, never `Enum.GetValues`.
- **`LevelRules.NullSeed` (0) means "not authored"** - test with `LevelRules.IsValidSeed`, never a
  literal. A REAL seed is `[MinValidSeed, MaxValidSeed]`; a seed FIELD is `[MinSeed, MaxValidSeed]`.
  A generator must never return 0.
