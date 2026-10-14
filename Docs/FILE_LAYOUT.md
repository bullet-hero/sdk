Why the on-disk names and folders in `Models/FileNames.cs` are what they are. A contributor adding a file or a
folder reads this first; a level is a folder of files a player can zip and send, which every rule here protects.

- **Backups live OUTSIDE `levels/`**, one folder per level id. A backup inside the folder it protects is copied,
  zipped, shared and deleted along with it, and a level folder is a portable document, not a place to hide a
  history. A level deleted by mistake keeps its copies.
- **The cover has two names and nothing probes for either**, unlike the level and metadata documents, whose
  extension is resolved by what is on disk. It is reached through `LevelMeta.LevelLogo`, an ordinary resource
  uri, so the name a writer chooses is the name it records in the metadata in the same step. Which of the two
  it chooses is decided by the file's own bytes (the game's `LevelLogoFile`), never by what the source was called.
- **A collection is a folder, the way a level is:** `resources/collections/<CollectionId>/` holding a
  `collection.*` manifest, the four data folders with the device library's own envelope files byte for byte, and
  `media/` for the file resources. Reusing the library layout lets one reader serve both and lets a library entry
  be copied into a collection as a file.
- **`stats/` is a sibling of `levels/`, like `backups/`, for the same reason:** it describes the PLAYER, so it
  must survive the level folder being zipped, shared, re-imported or deleted. A per-level file is named by the
  `LevelId`, the one identifier a rename, a translation or a folder move cannot change.
  `stats/achievements.json` (`FileNames.AchievementsFileName`) sits beside the statistics for the same reason;
  its name is not a Guid, so nothing that lists per-level files mistakes it for one.
- **A protected level is `level.json.gpg`;** the inner extension stays in the name because that is what `gpg`
  itself does (`gpg -c level.json`) and it answers "which `SerializationType`" without a header byte or a guess.
  It is appended, never replacing (`level.gpg` would lose that answer).
- **Three archive containers, one feature.** A level archive is named by whichever wrote it: tar.gz is what a
  shell opens, zip is what Explorer opens on a double click, and 7z is only recognised well enough to be refused
  by name. What a file IS is still sniffed from its bytes; these constants name what an export WRITES.
- **A profile archive mirrors the folders above under their own names,** so it needs only its own manifest.
  A cloud save (Steam Cloud) mirrors the same relative paths and adds `cloud.json`
  (`FileNames.CloudManifestFileName`, a `CloudManifest`), its index of levels and their tombstones;
  it is never written to disk.
  `profile-backups/` is a sibling owning everything the profile transfer writes on the device (the one kept
  backup, and in `staging/` every temporary of an import); it is never itself packed, or a backup carrying the
  previous backup would grow without end.
- **`mcp/` holds the level-editing bridge's own files** (`AgentEndpoint`: the port and token of the running game). It is
  machine-local and describes a running process, so it is no `ProfileCategory`: a profile never packs it, and no
  level carries it.
- **`ai/` inside a level folder is the opposite: notes about THAT level**, so it lives in the folder and travels
  with it. Not a model and never read by the loader; `ai/manifest.json` says whether an export carries it, and
  `Agent/AiFolderRules` keeps every name a plain lowercase text file but `CONTEXT.md`.
