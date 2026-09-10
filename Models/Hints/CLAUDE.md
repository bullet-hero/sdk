# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Hints

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## `Models/Hints/` — the level's advisory aggregate

`LevelHints` (`Level.Hints`, `[ModelGeneration(LevelHints, ModelGenerations.Release)]`) is the fifth aggregate on `Level`
and the only one carrying **nothing an author wrote**. Everything in it is DERIVED from the other
four, written by whoever saves the level, and safe to drop: a consumer that ignores the whole object
plays the identical level, only paying at load (or mid-playback) for work the hint front-loads.
**That is the membership test** — a field belongs here when it is recomputable from the rest of the
level *and* nothing looks different when it is missing, wrong or stale. Anything an author decides
belongs in the aggregate it describes, however small and however editor-only: a `Marker`, a
`BeatSegment`, an object's `Name` and a `FrameSpan`'s anchors are all *authored* editor-only data,
unrecoverable once dropped, and none of them is a hint.

It exists as its own aggregate rather than as fields on what it measures (`Limits` used to sit on
`LevelSettings`, `FontCharacters` on `LevelResources`) because those neighbourhoods are
authoritative: a reader there cannot tell "the author decided this" from "a tool measured this last
save". Behind one property the distinction is structural, and "recompute every hint" / "drop every
hint" become one call each instead of a list someone has to keep current.

Two members today:

- **`Limits`** (`LimitHints`) — six peak-simultaneous-object counts (instances/shapes-opaque/
  shapes-transparent/effects/texts/tracks) an editor writes on save from
  `LevelCapacityUtils.GetPeakUsage`, so a player can preallocate its per-frame buffers instead of
  growing them mid-level. Never authoritative — the file may be hand-edited, foreign or stale, so a
  consumer treats it as a lower bound at most, measures/grows on its own anyway, and clamps it
  against what the device allows. All zeroes (the default, and what an older level deserializes to)
  means "no hint", not "no objects". `LevelRules.Min/MaxCapacityHint` bound it purely so a hostile
  file can't request a gigabyte-scale preallocation.
- **`FontCharacters`** (`Dictionary<FontResourceId, CachedFontText>`) — each font's distinct-character
  set, a glyph-atlas warm-up hint. Keyed by **any** `FontResourceId`, game-defined ids included
  (unlike every dictionary in `LevelResources`, which is user-defined-only), because a level's
  most-used font is usually the game's own and needs warming just as much as a shipped one. Text
  renders identically without it; an empty or absent entry means "warm nothing for this font", not
  "this font has no text", and no consumer recomputes it at load. The value is a `CachedFontText`
  (`Models/Data/`) carrying its own `FontResourceId` plus the characters as an **`IString`**, not a
  bare `string`, so a localized level warms per language through the very same resolution its text
  goes through. Carrying the key is what lets it serialize like every other keyed collection — a
  plain array with the key dropped and recovered on read (`DictionaryCachedFontTextsConverter :
  DictionaryAsListConverter`) — instead of the `{k, v}` pair form a bare `id -> IString` map needs.
  Build it with `Services/FontCharacterService` (`Build`/`BuildAll` are pure and return values;
  `Apply` writes into the model) or run the `gen_font_cache` generator, which does the same through
  `GeneratorContext` so the run stays undoable (`Generators/Utility/FontCacheGenerator.cs`).

Both reach a generator through `GeneratorContext.Hints`, which is **null in Prefab Mode** exactly
like `Game`/`Audio`: a hint describes the file a player loads, and a template is not one.

**No migration, and the domains stay at generation 1.** A level written before this existed deserializes
to an all-empty `Hints`, which is precisely "no hint"; one whose hints were written under the old
layout simply loses them at the next save. That is what advisory means, and it is why the move needed
nothing else.
