# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Services

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Services/

`SerializationService`-adjacent but SDK-root-level. **Four of its subfolders are
  the level-package feature and only make sense read together** (design record:
  `docs/issues/PACKAGE_HISTORY.md` in the consuming project): `Content/` (`IContentStore` — a named
  set of blobs, ROOTED BY CONSTRUCTION, so "does this escape the folder" is a property of the type
  rather than a check at every call site; `DirectoryContentStore`/`MemoryContentStore` are two
  implementations rather than two parallel APIs, which is what lets the same pipeline serve a disk,
  a test and a server), `Archive/` (tar.gz — and its unpack refuses a link entry, which is the one
  attack surface tar has that ZIP did not), `Crypto/` (`PgpSymmetricService`, OpenPGP symmetric,
  SEIPD v1 because GnuPG does not implement RFC 9580 — read its header before touching the
  `...Utf8` overloads, which are what make a Cyrillic passphrase interoperate with gpg at all), and
  `Package/` (what a level package contains, written and read; the reader is also the future
  server's entry point, which is why every refusal is a value rather than an exception).
  **This is where the SDK started reading files**, so any header claiming it reads none is stale.
  **`Cache/` is GONE**, and what replaced it is a FORMAT rather than another layer: a side-car
  cache has to decide when it is stale, carry an invalidation hook on every write path and keep a
  second serializer in step with the real one, while a format cannot disagree with the file because
  it IS the file. `Serialization/Blob/` is that format; `LevelObjectCodec`, the object tree written
  out by hand, was its worked example and is no longer needed.
  Also `CryptographyService`
  (AES-256-CBC, and NOT what protects a level any more — see its own header), `TextFormatService` (`{variable}` string templating), `FontCharacterService` (builds
  `LevelHints.FontCharacters`, see below), and **`Shapes/ShapeCatalogService`** + `ShapeParameters`
  — the game's own built-in shape library, which lives HERE rather than in the consumer because it
  is what a `ShapeId` means: 497 shapes as the cross product of a form, a sector, a thickness rung
  and an invert flag, an id that IS those parameters packed one axis per nibble
  (`Encode`/`TryDecode`), and `Build` producing the geometry in pure C#. The consuming project only
  bakes what this enumerates. Read its header before touching the id layout — the six rules there
  are what make a future axis free and an inserted side count harmless.
  Finally `Controls/` (`ControlDeviceCatalog`/`ControlDeviceInfo`) — the STATIC per-device facts,
  here rather than in the settings tree because they are not the player's to change and **must not
  survive a file**: a saved "this device supports Relative" would still claim so after the build
  stopped supporting it. The matrix is deliberately uniform today (all four devices do all three
  modes, so `SupportedModes` reads `All` everywhere); the mask exists anyway, as the only place a
  future device that genuinely cannot do one — a pedal, a wheel, a MIDI pad — could say so
  without every consumer growing a special case.
