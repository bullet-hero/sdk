# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Statistics

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## `Models/Statistics/` - what a player has done

Two serialization roots rather than one, and the split is the whole design: `GameStatistics`
(`stats/statistics.json`, device-wide) is read once at launch and answers "this person", while
`LevelStatistics` (`stats/<LevelId>.json`) answers "this level". A single document holding every
level would be read and rewritten whole for every run of every level, putting the entire history at
risk on each write - and the menu only ever needs a handful of levels to draw a list.

- **`RunProfile` is a KEY, and that is why its speed is an int.** A record only means something
  against runs it can be compared with, so `Records` is `Dictionary<RunProfile, BestRun>` - lives,
  speed, checkpoints and bot. The speed control is a continuous slider whose readout shows two
  decimals; a `float` key compares bit for bit, so `1f` and `0.9999999f` would file two records for
  what the player and the screen both call "1.00", and both would sit in the file forever.
  `SpeedCenti` is exactly the precision the player is shown.
- **`BestRun` carries none of the key's four numbers.** `LivesLeft` is not one of them - the key says
  how many lives the run was GIVEN, the record says how many it ended with.
- **Hence `DictionaryAsPairListConverter` for `Records`**, not the key-from-value form every other
  dictionary here uses: that one requires the value to embed its own key, which is the exact thing
  this value refuses to do. `DeathsByCheckpoint` makes the opposite choice for the opposite reason -
  `CheckpointDeaths` carries its own `Frame`, so it writes as a flat array.
- **`DifficultyStatistics.BucketFrameDuration` is what keeps the histograms honest.** A bucket means
  "deaths at this fraction of the level", and that claim silently becomes false the moment the level
  changes length. The length the buckets were built against is stored beside them and a change clears
  them. `DeathsBeforeCheckpoint` is its own field rather than a key of `-1`, since `-1` is this
  project's one reserved frame number (`FrameSpan.LastFrame`).
- **Every timestamp is UTC**, written as `DateTime.UtcNow`. A statistics file travels between
  machines and is read by a person, so it stores an absolute instant in a readable form; unix seconds
  were rejected for the reason the file is JSON at all.
- The consuming half - the accumulator, the merge rules and the services - lives in the Unity
  project; `docs/issues/STATISTICS_HISTORY.md` there is the design record.
