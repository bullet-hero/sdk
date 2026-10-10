Design facts of the statistics models in `Models/Statistics/` that the code cannot say by itself.
`Models/Statistics/CLAUDE.md` is the structural map (which document holds what); this file is why each
group is shaped as it is. What a player sees is in `DocsPublic/`.

## Two documents, one per subject

- **`LevelStatistics`** (`stats/<LevelId>.json`) is one level as a player experienced it: a serialization root
  of its own, the only one of the four top-level documents (level, metadata, settings, this) describing the
  relationship between a player and a level. It is NOT a field of the level: a level is portable, and progress
  travelling with it would arrive already won; it also has to survive the level being deleted, hence `stats/`
  beside `backups/`. It is keyed by `LevelId` (a title is edited, translated and duplicated, an id is not);
  copying a level folder yields two levels sharing one id and one file, which is that identifier's contract.
- **`GameStatistics`** (`stats/statistics.json`) is the player across everything, read once at launch and
  written back on a slow cadence. It is its own document rather than a sum computed on demand, because deriving
  it opens every file in `stats/` to draw one screen. Its groups carry no `[ModelGeneration]` of their own (one
  envelope per FILE, like `UserSettings`); a group is a heading on a screen. A group builds its own defaults, and
  a new group is a bump with a snapshot and a migrator. `Pull` keeps every group instance: the app holds a
  reference to each, so replacing one would leave holders pointing at a copy nothing writes.
- **Every timestamp is UTC** (`DateTime.UtcNow`): the file outlives time zones and is read by a person. Unix
  seconds were rejected for the reason the file is JSON at all - an unreadable number is worse than a string.

## LevelStatistics members

- **`RunProfile` is the dictionary key: the conditions a run was played under.** Three lives at half speed
  with a bot is not one life at double speed, so folding them into one "best" would make the number meaningless
  where a player cares. `BestRun` carries none of these fields (a second copy with nothing keeping the two in
  agreement); `LivesLeft` is not one of them (the key says lives GIVEN). Speed is quantized to hundredths: it is
  a key, and a float key compares bit for bit, so 1f and 0.9999999f would open two records for what the screen
  calls "1.00" - hundredths are exactly the precision shown. The invulnerable flag is part of the key, not a
  reason to drop the run (nothing-can-hit is not Zen, which is hit and never killed). It is a struct, so no
  `[RuleContainer]` (class-only; the rule walk never descends into value types); the cap on `Records` bounds it.
- `BestRun.LevelVersion` is the author's version when the record was set, deliberately NOT in the key: a
  record does not void on an edit, but a player comparing two numbers should see they were set on different
  content.
- `CheckpointDeaths` carries its own key (the dictionary serializes as an array of values) and is keyed by the
  checkpoint's FRAME, never an index: an index shifts when a checkpoint is inserted and silently reassigns every
  count, while a frame goes stale honestly (the entry is orphaned, the price the level-global tracks pay too).
- `DifficultyStatistics`: this is the half aimed at the AUTHOR (which passage kills players, a question a game
  can answer and a playtest cannot). Buckets are a fixed COUNT (`StatisticsRules.BucketCount`), a percentage of
  the level, so histograms of different lengths read side by side. `BucketFrameDuration` is stored beside them
  and a length change clears both, since the claim "deaths at this fraction" turns false otherwise; content
  REARRANGED inside the same length is the known limit. The no-checkpoint count is its own field, not a sentinel
  key: `FrameRules.NoFrame` is zero and a dictionary would accept it without a word.
- Play time is REAL seconds on the wall clock, not the level clock (bent by launch speed and the checkpoint
  ramp, which answers "how much level was played").
- `LevelEditorStatistics` is the authoring effort on this level, the only part of the file a run launched from
  the editor may write: an author testing a ten-second passage starts it hundreds of times an evening and folding
  those into `Attempts` would wreck the clear rate.

## GameStatistics groups

- `AvatarStatistics`: both numbers are global only (per level they would be counters nobody reads). Distance is
  in WORLD UNITS (the space level content is authored in; nothing scales it by the camera). Counted from real
  runs only: the menu background arena drives an avatar through the same code and would measure idle time.
- `DeviceTimeStatistics`: which device actually STEERS, measured, since four devices are equals switched on the
  first press. Four fields, not a dictionary (closed enum; a build that drops a device leaves its number
  readable). Only the LEADING device is charged: every active driver is sampled and exactly one steers.
- `EditorTotalsStatistics`: the authoring half of the profile, the device-wide sum plus counters belonging to no
  level. `LevelsDeleted` sits beside `LevelsCreated` (a difference cannot be recovered from one number).
  `TotalResources` counts resources added to levels (images, audio, fonts).
- `ScreenTimeStatistics` / `ScreenTimeKind`: four numbers, since "40 hours" and "30 of them in the editor" are
  different questions. Loading is its own kind: the only one the game can hope to shrink. They do NOT sum to
  `TotalAppSeconds` and are not meant to (a torn-down screen belongs to nobody; menu time over a paused level is
  charged once); the total is measured on its own. A fifth kind means a fifth field, added together.
- `StreakStatistics`: the only group that remembers an ORDER (a clear raises the streak, anything else resets
  it). `MostPlayedLevelId` is kept beside its count so it is maintained by comparison at attempt time; it may
  name a level that no longer exists, deliberately - the per-level file outlives it.
- `TotalsStatistics`: the sum over every level, kept to avoid opening each file. `DistinctLevelsPlayed/Cleared`
  are COUNTERS, not id sets (unbounded growth in a file read on every launch; "which ones" is answered by
  listing `stats/`): incremented when a level file is created / when a level first records a clear.
- `TutorialStatistics`: a completion is every step actually passed, never "dismissed" (that is what
  `UserSettings.TutorialCompleted` also records). A group of its own: the tutorial is an activity, not an
  attribute of the player.
