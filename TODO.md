Serialization
- verify SerializationService stability/correctness across all keyframe/value/effect type combinations (round-trip tests + real saved level files), especially after the IRequiresDefaultSerializer refactor
- model versioning core is implemented (DataVersionAttribute + VersionedTypeRegistry + VersionedEnvelopeConverter, replacing CompatibilityService/SaveData<T>/JsonConverterData<T>) - see VERSION-UPDATE.md for what's still open: nested/optional aggregates below the six SaveData kinds (e.g. splitting Level into Settings/Game/Audio/Resources envelopes), and the first real migrator once a version actually needs to bump (Project Arrhythmya import is done and lives in Interop/AfterBeat, deliberately outside the versioning system - a foreign format is not a generation of this one)

Level packages
- external-tool interop is NOT verified yet, and it is the check the formats were chosen for: `tar -tzf`, `gpg -d <file> | tar -tz`, `gpg -d level.json.gpg`, and a file made by `gpg -c --cipher-algo AES256` opened by the game - both directions, and once with a non-ASCII passphrase (that one is what proves the ...Utf8 overloads)
- no Android build has been made since BouncyCastle was added; whether the linker keeps it is the one thing the Editor cannot answer, and `Assets/link.xml` is the answer that has not been tested
- random access to a single entry is gone with ZIP and is not coming back; a listing reads the leading entries instead (documents are packed first)
- server side: `LevelPackageReader` is meant to be its entry point, so a package can be accepted and stored without a file system anywhere - untested outside `MemoryContentStore`
- see the consuming project's `docs/issues/PACKAGE_HISTORY.md` for the whole design record, including what ZIP bought and what was rejected

Licensing
- Add mail for "notice-and-takedown" process, for UGC content and DMCA complience
- Add checkbox like "_I confirm that I have rights to all external resources at this level_"
- Add more formal document for Tos/EULA where I write all content from UGC-LICENSING-POLICY.md
- (maybe) Disable on IOS NOWS, because they can shutdown game

Features after alpha release
- add difficulty metadata (watch OSU for more info)
- create trigger/event system (like PA, but more like GD)
  - first of all - see guide for gd editor
  - https://youtube.com/playlist?list=PLD3NfTCEL4uV7zI5QvMLTDs7qActqY5JB
  - or read PDF files
  - https://www.robtopgames.com/files/GDEditor-RU.pdf
  - https://www.robtopgames.com/files/GDRating-RU.pdf
  - https://www.robtopgames.com/files/GDLeaderboards-RU.pdf
- add sounds in timing, with custom audio support (tutorial level in JSaB)
- add JPG, PNG, SVG importing to textures and anywhere usage
- add tags as metadata for every object in level, only for editor
- add free ingame skins (and maybe add custom)
- Integrate
  - https://kenney.nl/assets/input-prompts

Features after successful full release
- add doxygen for all code
- console window with TUI, maybe fully supported SLI
- 2 game mode: camera follow to player using lerp, direction is always up
