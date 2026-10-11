# CLAUDE.md - Assets/Plugins/BH.SDK/Services

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

`SerializationService`-adjacent but SDK-root-level: the level archive (`Content/`, `Archive/`,
`Crypto/`, `LevelArchive/`, read together), resource `Collections/`, the `Profile/` archive, the
built-in shape library (`Shapes/ShapeCatalogService`), the static `Controls/` device catalogue,
`AudioAnalysis/` (tempo, onsets and sections of a track; its own `CLAUDE.md`), and
`CryptographyService`/`TextFormatService`/`FontCharacterService`.

Why each is shaped as it is: `Docs/ARCHIVE.md` ("The folder at a glance" is this folder's map);
the shape catalogue: `Docs/SHAPES.md` - read it before touching the `ShapeId` layout.

## Traps

- **Every refusal is a value, never an exception** (`LevelArchiveReader`, `CollectionReadResult`,
  `CollectionArchiveResult`) - the reader is the future server's entry point.
- **`IContentStore` is rooted by construction**; the same pipeline serves a disk, memory and a server.
- **tar and zip share the four unpack checks but differ**: tar's link entries, a zip's declared size
  (a claim, enforced twice), and duplicate zip names (refused). `NonSeekableWrite` keeps one input one
  output.
- **OpenPGP (SEIPD v1) protects a level, not `CryptographyService`'s AES**; use the `...Utf8`
  overloads for a non-ASCII passphrase.
- **There is no cache**: `Serialization/Blob/` is a format. Don't add a side-car.
- **Device capabilities (`Controls/`) must never be saved** in a file.
- **`CollectionImportPlanner`/`CollectionExportPlanner`/`ProfileMergePlanner`/`StatisticsMerge` are
  pure**; a conflict is resolved by the host's answer, never by the planner. `MediaFingerprints` is
  the import planner's only IO.
- **`CollectionWriter` writes the manifest LAST**, so a half-written folder is not a collection.
- **The profile archive is zip only** (`ArchivePolicy.MaxNameBytes` lifted; tar refuses that policy),
  and `ProfileLayout`'s mapping is also the whitelist.
- `ResourceCollection` and `ProfileManifest` were new domains, not bumps - nothing on disk had their
  shape.
