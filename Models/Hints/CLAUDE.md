# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Hints

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.

`LevelHints` (`Level.Hints`) is the fifth aggregate on `Level` and the only one carrying **nothing an
author wrote**: `Limits` (`LimitHints`, peak-simultaneous counts) and `FontCharacters` (a glyph
warm-up set per font). Everything about it: `Docs/LEVEL_HINTS.md`.

## Traps

- **The membership test**: a field belongs here only when it is recomputable from the rest of the
  level AND nothing looks different when it is missing, wrong or stale. Authored editor-only data
  (`Marker`, `BeatSegment`, `Name`, span anchors) is never a hint.
- **A hint is never authoritative** - a consumer treats `Limits` as a lower bound at most and clamps
  it; all zeroes means "no hint", not "no objects".
- **`FontCharacters` is keyed by ANY `FontResourceId`**, game-defined included, and its value is an
  `IString` so a localized level warms per language. Build it with `Services/FontCharacterService` or
  `gen_font_cache`.
- **`GeneratorContext.Hints` is null in Prefab Mode** - a template is not a file a player loads.
- **`LimitHints` counts shapes in two buckets**; `ShaderType.Auto` counts as transparent when no
  resolver is passed.
