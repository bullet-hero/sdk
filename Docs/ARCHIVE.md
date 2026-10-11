# Archives, stores, crypto, collections and profiles

Contributor-facing reasoning for `Services/Content`, `Archive`, `Crypto`, `LevelArchive`, `Collections`
and `Profile`. `Services/CLAUDE.md` carries the map; the code keeps one-line pointers here. This SDK
reads files (it was where the SDK started to), so any header claiming it reads none is stale.

## The store

- **`IContentStore` is a named set of blobs, rooted by construction.** "Against files" and "against
  data in memory" are two IMPLEMENTATIONS (`DirectoryContentStore` rooted at the level folder,
  `MemoryContentStore` for tests and for a server reading an upload; the server's own `DbContentStore` is
  one more class), never two parallel APIs - that would mean every pipeline written twice with only
  one half exercised. There is nothing outside the root to address, so "does this escape the folder"
  is a property of the type, which makes the tar-slip defence structural rather than vigilant.
- **`ContentPath` REFUSES rather than repairs.** Converting a backslash renames a file that legitimately
  carries one on Linux; collapsing `a/../b` resolves a traversal instead of reporting it; trimming a
  trailing slash turns a directory into a blob. Names come from an archive, a database or a hostile
  upload: the answer to a bad one is "no". Comparison is ORDINAL, case included (an archive written on
  Linux must read back the same on Windows, and a case-folding store would merge two entries a tar
  holds separately); what the file system under a `DirectoryContentStore` does about case is its own
  answer - a stated gap.
- **`DirectoryContentStore`**: a write creates the directories it needs, root included (export picks a
  folder that may not exist, unpack writes a tree several levels deep); a read never creates anything.
  `Resolve` re-checks the root even though `ContentPath` already refuses `..`, rooted paths and drive
  letters: the guarantee is held by the type, not by an argument that the validator is exhaustive. A
  symlink INSIDE the root pointing out is the one case no path check sees; refusing to unpack a link
  entry covers it, in the archive layer. Everything completes synchronously except the two members that
  return a stream, and those only open a handle.
- **`ValueTask`, not `Task`, on every store member**: they complete synchronously very often, are called
  once per entry in a loop and awaited exactly once. The pipeline entry points (`PackAsync`,
  `WriteAsync`, `ReadAsync`) return `Task` because a caller may store it or `WhenAll` it. `ListAsync`
  has no `IAsyncEnumerable`: a listing is a folder's worth of names and an async stream is extra IL2CPP
  risk for nothing.
- **`MemoryContentStore` has a cap that is not tidiness**: on a server it is what a tar.gz is unpacked
  INTO, and a gzip bomb is a few kilobytes that decompress to as much as the process accepts. The
  archive layer has its own limits; this is the last one, held by the thing allocating, and checked while
  a blob is written (by commit the memory is spent). It subclasses `MemoryStream` and overrides the three
  write entry points it has (span, array segment, single byte), which also covers `WriteAsync`/`CopyTo`;
  subclassing rather than wrapping leaves seeking, `Position` and `ToArray` as callers expect.

## The archive layer

- **Which container is answered by the bytes and nothing else** (`ArchiveFormat`,
  `ArchiveFormatSniffer`): a name is the one part anybody can write. `OpenPgp` is a member because a
  sniff can land on it (a protected export is a PGP message whose plaintext is one of the other two:
  decrypt, sniff again). `SevenZip` is a member although nothing reads or writes 7z, so a refusal can
  say "7z" instead of "not an archive" - the difference between a person trying something else and
  concluding the file is broken.
- **Sniffer order is the whole file**: three formats begin with a magic number; OpenPGP has none (its
  first byte is a packet tag: high bit set and one of six tags), loose enough to accept a byte from an
  unrelated binary, so it is asked LAST. `MagicBytes` is how much a caller must peek (the 7z signature
  is the longest); a shorter buffer is not an error. **Four zip signatures**: `PK\x03\x04` (ordinary),
  `PK\x05\x06` (an empty zip: only an end-of-central-directory record), `PK\x07\x08` and `PK00`
  (spanned forms) - recognising all four lets the level layer say "this zip holds no level" instead of
  "not an archive".
- **`ArchiveLimits`** stand between an upload and the disk: a gzip bomb or a tar of a million empty
  entries is built of perfectly legal names, so validating names catches neither. The caps are checked
  WHILE unpacking, entry by entry, aborting at the first one passed (afterwards the damage is done); the
  store has its own cap, this one reports in the archive's terms. Defaults are sized for a LEVEL: a
  handful of documents and a song.
- **`ArchiveEntrySource` is "how to open it", not the bytes.** An archive holds documents the export just
  REWROTE (memory only), media in the level's own store, and files collected from elsewhere on the
  machine; only the first can be a `byte[]` without regret. An opener is one delegate and lets a 40 MB
  track stream straight in. It also keeps ABSOLUTE PATHS OUT of this layer: a rooted store is a safety
  property the unpacker's defence rests on, so the one place allowed outside one is the export that was
  asked to collect a file, and it passes an opener in.
- **`ArchivePassphraseExceptions`: two exceptions, because "not given yet" (ask) and "given and wrong"
  (say so) differ.** Collapsing them tells a player they got a password wrong before typing one, the
  same split `PgpSymmetricService` makes with `PgpOpenResult`. They are exceptions here because this is
  the FORMAT layer, where a refusal is already an `InvalidDataException`; the level layer turns all three
  into values.
- **`ArchivePolicy` exists to make two packs of the same content byte-identical**, so a backend can
  recognise a re-upload by digest: a tar entry carries a pinned time, no owner, no group, one fixed mode.
  The claim is exactly "same input, packed twice by the same build, same bytes"; deflate's output across
  runtimes is not promised and nothing depends on it (the digest is computed on what was written).
  - **Names are capped at 100 bytes** on write, shorter than ustar's 100 + 155 prefix form, because
    SharpZipLib's `TarHeader` cannot fill the prefix and would silently TRUNCATE a longer name into a
    different file. A caller renames what does not fit and says so (`LevelArchiveBuilder`).
  - **The cap is shared by every format**: one level exported three ways must carry the SAME names, or
    `level.json` names a file the tar.gz export renamed (plays in one archive, not the other). On READ
    nothing applies it - `ContentPath` and the file system are what an arriving archive is judged by, or
    an ordinary `zip -r` from outside would stop importing.
  - **A profile is the one writer allowed past it** (`MaxNameBytes` on its own policy): written to zip
    only, it mirrors the player's folders under their own names and cannot rename anything (`level.json`
    refers to media by name). tar refuses a policy that raises the cap rather than truncate.
- **`ArchiveProtection`: two schemes, the second is not a duplicate.** `OpenPgp` wraps the whole
  archive in a message `gpg -d` opens and is the default: ONE scheme covers a document, a folder and an
  archive. `ZipAes256` encrypts each entry inside the zip, because the person on the other end very
  often has 7-Zip and nothing else. Its costs: SharpZipLib takes the password as a `string`, which
  cannot be zeroed like the `char[]` every other path carries (the string is built as late as possible
  and dropped when the archive is finished - a mitigation, not a fix); Windows Explorer refuses an AES
  zip; AE-2 is WinZip's extension, not a formal standard; the salt is random, so such an archive is
  never byte-reproducible. Only zip can carry `ZipAes256`; asking tar.gz for it is a caller mistake, not
  a state to degrade.
- **`NonSeekableWrite`** wraps every zip destination: `ZipOutputStream` writes a different file when it
  can seek (local header, then patched sizes) than when it cannot (data descriptor). A desktop export is
  a `FileStream` and an Android SAF export is not, so without it the container would depend on WHERE it
  was written, against `ArchivePolicy`'s claim. Patching only ever saved a few bytes per entry. Write-only
  by construction.
- **`TarGzService`**: POSIX tar in gzip, because `tar -xzf`, Windows Explorer, 7-Zip, Keka and Ark open
  it, so a level stays openable by tools people already have even if this game is gone. The cost is
  random access: tar.gz is a stream, so the writer puts the documents first and `ReadLeadingAsync` lets a
  reader that wants only the level card decompress a few kilobytes and stop; replacing one entry in
  place is gone. **The fourth unpack check is new relative to ZIP**: names validated, sizes capped, the
  store unable to address outside its root - and tar also carries SYMBOLIC AND HARD LINKS, an outward
  link being a traversal no name check sees, so any entry that is not a plain file or a directory is
  refused (`LF_OLDNORM`, the pre-ustar plain file, is still accepted; symlinks, hard links, devices,
  FIFOs and the GNU long-name and sparse extensions are an attack or a shape this format does not carry).
  A leading `./` (what GNU tar writes when packing a directory) is stripped: the one normalisation the
  reader does, a fact about tar rather than a repair. The codec is synchronous: deflate is CPU work and
  this library never decides which thread it runs on.
- **`ZipService`** is the container a player's computer already opens (Explorer on double click). Neither
  container is the "real" one; the sniffer decides what a file IS. It brings back the central directory
  (`ReadEntriesAsync` takes an entry from anywhere for the cost of the directory). The same four unpack
  checks, two harder than for tar:
  - **a zip's declared size is a CLAIM**: the directory can say 1 KB and the stream inflate to ten
    gigabytes, so the limit is enforced TWICE - from the directory, cheaply, before unpacking, and as a
    hard byte counter while copying (the one that stops a bomb);
  - **two entries may share a name** (a scanner reads the first, an extractor writes the second): refused
    by name; tar degrades into a silent overwrite;
  - symlinks live in the high 16 bits of `ExternalFileAttributes` (the unix mode), refused for tar's
    reason; a zero there means no unix modes were recorded, which is every Windows Explorer archive, and
    refusing those would refuse the most ordinary zip there is;
  - **an entry left out by a filter is still judged**: name, type and duplicate checks run as for a kept
    one, so a filter is never the way a hostile entry gets past them; the size limits count the KEPT
    entries only (those reach the store);
  - **two readers, on purpose**: `ZipFile` (seekable, central directory, cross-checks local headers,
    decrypts AES) is the main one; `ZipInputStream` is for a forward-only pass and CANNOT read an AES
    entry (`CanDecompressEntry` drops WinZipAES);
  - legacy ZipCrypto is READ (`ZipFile` does it free) and never WRITTEN: offering it would offer a
    protection that is not there;
  - **the passphrase is UTF-8** (`PassphraseCodec`): SharpZipLib defaults to the machine's ANSI code page
    (what ZipCrypto specified) while 7-Zip, WinZip and every AE-2 implementation derive the key from UTF-8
    bytes. ASCII agrees; a Cyrillic password does not, and the archive could not be opened by the tool the
    option exists for. Measured: 7-Zip opened an ASCII-passworded archive and reported "Wrong password" for
    a Cyrillic one until this codec was passed in. Only the CRYPTO encoding is overridden; entry names keep
    their own path (UTF-8 flagged per entry by `IsUnicodeText`).

## OpenPGP (`Services/Crypto`)

- **`PgpSymmetricService` is exactly what `gpg --symmetric --cipher-algo AES256` produces**: SKESK v4, S2K
  type 3 (iterated and salted) over SHA-256, SEIPD v1 (CFB with an MDC) under AES-256, a literal-data
  packet inside. That sentence is the whole specification; nothing about the layout is ours to choose.
- **SEIPD v1, not RFC 9580's v2/AEAD**: GnuPG does not implement the new formats (its developers went to
  LibrePGP), and gpg is the tool the format was picked for. v1 is read by gpg, Sequoia, OpenPGP.js and
  GopenPGP; when that changes the reader already answers `Unsupported` with a clear message.
- **The `...Utf8` overloads are mandatory**: `AddMethod`/`GetDataStream` encode a passphrase one byte per
  char while gpg treats it as UTF-8. ASCII agrees; a Cyrillic passphrase would not open in gpg, nor a gpg
  file here - silently, since both sides report a wrong passphrase. The audience is Russian-speaking, so
  that is the default case.
- **`Verify()` is always called**: it separates "wrong passphrase" from "damaged file" (two different
  sentences for a player; the practical reason encrypt-then-authenticate earns its place). A symmetric
  message with no MDC is refused, not opened unauthenticated: gpg has written one by default since 2003.
  The distinction is one-directional (`PgpOpenOutcome`): the session key's quick check is 16 bits, so
  about one wrong passphrase in 65536 passes it and fails the MDC, reported as Tampered; a damaged file
  is never reported as a wrong passphrase, so nobody hunts for a password that would not have helped.
- **ASCII armor is not written**: a third more bytes for a shape nothing here transports as text.
- **BouncyCastle's `itCount` is the CODED OCTET, not a count** (named `itCount`, takes an int, throws
  "must be in range 0-255" for any honest number). The layers above speak in BYTES hashed, the unit S2K
  work is measured in, and rounding happens in this class, UPWARDS to the first rung that meets the
  request, so a caller never gets less stretching than asked.
- **`PgpEncryptOptions`**: S2K work is bytes hashed; gpg's default asks ~65 MB of SHA-256 (a fifth of a
  second on a desktop, possibly two on a cheap phone, every open). The default here is a quarter of that,
  defensible because this protects a file on the player's own disk behind a shared password - not DRM. A
  file gpg wrote with a higher count still opens (reading takes what the file declares). The literal-data
  modification time is PINNED: the local clock is not something an export should leak, and two exports of
  one document then differ only where OpenPGP randomizes (salt, IV).
- **`CryptographyService` is NOT what protects a level**: that is OpenPGP, chosen for reach (gpg and every
  archiver that reads what gpg wrote; an AES blob of our own opens in nothing). It stays a raw AES-256-CBC
  primitive with no integrity check, deliberately: a MAC would be a second half-format that opens in
  nothing. Callers that must tell wrong password from damage use `PgpSymmetricService`. Input shorter than
  salt + IV is refused before the length arithmetic (it would otherwise throw `OverflowException` from
  inside the crypto path, reading as "the cipher broke"). Salt and IV come from the platform's own random
  source and nothing else: a constant-seeded `System.Random` would give every installation the same salt
  and IV, and the SDK is open source.

## The level archive

- **What goes in a package is computed from the model, never from a directory listing** (a folder
  accumulates swapped textures, unzipped exports, `.bak` files). The listing is still read, to report how
  many files were left behind - not counting the documents the writer regenerates, nor the files a
  DIFFERENT format left behind (converting to Blob must not make the old `level.json` look stray).
- **The `ai/` folder is the one thing packed from a listing**, because no model names it: only with an
  `ai/manifest.json`, and only the files `AiFolderRules.SelectShareable` passes, through the same `Reserve`
  path as a resource (sorted, pinned times). `share: false`, an unreadable manifest or ANY protected mode
  (`LevelExportModeExtensions.IsProtected`; a protected folder would ship the notes in plaintext beside an
  encrypted level) withholds it: `archive.ai_context_withheld`, and it is not counted as unreferenced. With no
  manifest it is an ordinary leftover. A level without `ai/` packs to the same bytes as before
  (`LevelArchiveAiContextTests` pins the zip, and the tar inside the tar.gz: BCL `GZipStream` output differs
  between Mono and .NET 8, so a pinned `.tar.gz` hash holds on one runtime only).
- **Collecting is not optional**: the editor sets `UriType = AbsolutePath` for the song a level is created
  around, so the commonest way to start a level makes one whose music lives outside its folder. An export
  that packed the folder as-is would open and load with no sound. An absolute path is COPIED in and its
  key rewritten to `LevelPath` in the exported copy alone. **A URL is left exactly as it is** (it cannot
  travel in an archive and is not broken); reporting it is all that can be done. This is the ONE place in
  the SDK that opens an absolute path.
- **Routing state is shared across every key**: which SOURCES are taken (a file referenced twice is
  packed once) and which NAMES (two different `song.ogg` must not become one). A name must be a legal
  store path AND fit the tar header; enforced here rather than in the writer so a folder export and an
  archive export carry identical names. Truncation keeps the EXTENSION (a shortened stem is cosmetic, a
  lost extension is a texture nothing opens); a path whose folders alone overflow loses folders.
- **`LevelArchivePlan` is decided before a byte is written** (testable without a disk, reportable first);
  its level and meta are COPIES, always - collecting rewrites keys, and the author asked for an export,
  not an edit of the open level.
- **`LevelArchiveReader` is also the backend's entry point**: a server reads whatever an upload contained,
  so nothing trusts a name, a length or a header; limits go to the archive layer, the store cannot
  address outside itself, and every failure is a value. The format is SNIFFED, and a PROTECTED archive is
  asked TWICE (outer OpenPGP, plaintext says tar.gz or zip; assuming gzip once made a protected zip read
  as "not an archive"). The plaintext is decrypted into memory because the tar reader needs it from the
  start and an OpenPGP stream cannot be rewound; the decompression limit bounds it. The passphrase has
  THREE answers (not given / wrong / ok). A peek answers `PassphraseRequired` without decrypting (the
  metadata of a `.gpg` is inside the encryption), reads a zip from its central directory and a tar.gz
  from the front (documents first).
- **`LevelArchiveContent` is bytes and a store, not a `Level`**: deserializing is the caller's step, so an
  editor can migrate while a server writes bytes to a column having built no model. Formats travel beside
  the bytes because the format of a document is decided by the NAME it was stored under (`level.json` /
  `level.blob`, and `level.json.gpg` keeps the inner extension so one probe answers both). The container
  travels with a refusal too ("this build does not read 7z yet" sends a person to re-zip; "unsupported"
  sends them looking for a fault); a folder answers `Unknown`.
- **`LevelArchiveOptions` holds the container and passphrase scheme** because it is already where a host
  turns a `LevelExportMode` into what the writer needs; a second pair of parameters would give one fact two
  homes. `LevelArchivePeek` returns a RESULT using the same enum a full read does, so a host maps one set
  of outcomes.
- **`LevelArchiveWriter`**: four shapes are two independent choices (file or folder, protected or not), so
  two methods; the mode enum only names the pair for a dropdown.
  - **Documents go FIRST, always** (the only mitigation tar.gz allows for having no central directory).
  - **"Protected" differs on purpose**: a protected ARCHIVE is one `.gpg` file with nothing readable (in
    transit it has no reason to advertise); a protected FOLDER encrypts the level document ALONE, so the
    level browser still renders a card with a name and picture and asks for the passphrase on open. What is
    protected is the content, not the existence.
  - **A passphrase on its own means protected**: the options carry WHICH scheme, a passphrase says THAT;
    the alternative reading (write in the clear, drop the passphrase) yields a file the author believes
    is protected - silent, survives being sent, the worst outcome available. Which scheme depends on the
    container: a zip carries its own AES (what a password on an archive means by default, since the
    recipient has an archiver); a `.tar.gz` has no encryption, so OpenPGP or nothing.
  - The archive is packed straight INTO the encrypted message so a big level never exists twice in memory
    (the length is unknown when the literal packet opens - the shape gpg itself writes). The level
    document is the opposite: small, already a `byte[]`, buffered so the message can use definite-length
    packets, the shape every reader handles.
  - The level document is THINNED like any other level write: an archive's document is a `level.json` by
    another name, so a placement's materialized prefab copies stay out and are rebuilt on import
    (`LevelArchiveGenerator`); the host's own writes thin in `FileLoaderService`, which this path never
    reaches.
  - The collected-file opener is built HERE, not in the archive layer, which must stay unable to address
    anything outside a rooted store.
- **`LevelExportMode` flattens three independent choices into one enum** (an author picks ONE thing from a
  dropdown). **The number is identity, `DisplayOrder` is the dropdown**: a value read back by position
  means a reorder silently turns one export into another, so a mode's number never moves (2 and 3 stay
  the tar.gz pair). `ZipEncrypted` (the zip's own AES-256, any archiver) is NOT a second spelling of
  `ZipProtected` (an OpenPGP message around the archive, `gpg -d`, one scheme for a document, folder and
  archive; it stays for people who live in that world, and for the folder shape where nothing else can
  do the job). The dropdown order ranks the passworded shapes by use: zip AES first, then `.tar.gz.gpg`,
  then the rare `.zip.gpg`; every mode appears exactly once (`LevelExportModeTests`).

## Collections

- **`CollectionContent` is shaped like a level's resources**: data resources and the manifest's file
  resources are gathered into a `LevelResources`, so `ResourceClosure`, `ResourceRemap` and every planner
  already speak it. The three file dictionaries ARE the manifest's (shared instances, not copies).
- **`CollectionArchive` uses the same two containers and nothing else**; the level archive's rules hold
  unchanged (sniffed format, `ArchiveLimits`, a store that cannot address outside itself, every refusal a
  value). Encryption is the level archive's two schemes for any content; a missing or wrong passphrase is
  a value the host answers by asking. The manifest is packed FIRST for the reason the level archive packs
  its metadata first, though nothing peeks a collection archive yet.
- **`CollectionReader`**: a collection arrives from anywhere (a subscribed Workshop folder, a received
  archive, a hand-made folder), so every failure is a value and nothing is trusted. The MANIFEST decides
  whether there is a collection at all; one bad entry costs only that entry (`Skipped`).
- **`CollectionWriter` writes a whole snapshot**: every data resource as its own envelope, the manifest and
  the manifest's media; leftover documents and media the content no longer holds are removed; anything
  else in the store (a cover, a readme) is not its to touch. **The manifest goes LAST**, so an
  interrupted write leaves the previous collection with some new files beside it, or no manifest on a
  fresh one - never a manifest pointing at unwritten files.
- **Export is the mirror of import with one decision reversed** (`CollectionExportPlanner`): a data
  resource already held under the same id is REPLACED, not asked about (the target is the author's own,
  read-only ones are never a target, so a second export of an edited prefab is an update). A file resource
  gets a collection-local id and a file under `media/` whatever its source (a `LevelPath` or
  `AbsolutePath` file is bytes on this device and is copied in); a URL or StreamingAssets source is
  carried as is (no bytes to copy).
- **Import is planned first and resolved second**, because a person sits between (`CollectionImportPlanner`):
  the plan answers everything with one answer (the closure, what the level holds identically, which file
  resources need a fresh id and a free name) and lists the rest (same id, different content); the editor
  shows it, the author answers each, `Resolve` builds the copies; nothing touches a disk. **"Identical" is
  the generated `Equals`**, not a byte comparison of serializations (a second definition would disagree
  the first time a default is written differently).
- **A file resource is identified by its bytes** (`MediaFingerprints`): its id is local to its folder, so
  the collection's `-1` says nothing about the level's. The FIRST `LevelPath` source is the file (later
  ones are loader fallbacks); a URL or absolute path has no bytes in the store and is never matched. With
  both sides' fingerprints a same-kind, same-digest file is pointed at instead of copied under a `_1`
  name; without them every file is new - wasteful, never wrong. This is the one IO an import plan needs,
  which keeps the planner pure.

## Profiles

- **A profile is not a level**: neither archive default fits. Names are the player's own paths and cannot
  be renamed (zip only, lifted cap, see `ArchivePolicy`); size is the player's whole history, so unpack
  limits are what the archive's own directory DECLARED - the player was shown the size and the disk
  checked before anything is written, so a file that inflates past its claim is lying and the copy's hard
  counter stops it. A bomb can make the directory claim a lot (which the player sees) but cannot make the
  disk receive more than that. Every refusal is a value (`LevelArchiveReader`'s contract).
- **`ProfileLayout` mirrors the profile root**: a category is one folder (or, for settings, one file)
  directly under the root, so an entry's category is its first path segment and nothing is renamed on the
  way. It is also the WHITELIST, read the same on pack and unpack: a path mapping to no category is
  ignored whoever put it in the zip, which keeps `profile-backups/` out of every archive without a
  special case.
- **`ProfileArchiveWriter` takes one store per category**, never one at the profile root (a store lists
  everything under its root and would walk `profile-backups/` on every export). Settings come as bytes
  (the settings the game is running with, not a file possibly a debounce behind). Entries sort ordinally,
  so the same profile packs to the same bytes.
- **`ProfileMergePlanner`**: a level is matched by `LevelId`, never by folder (a folder is a name a player
  can change and two can share). The winner is WHOLE: documents and media refer to each other, so mixing
  a newer `level.json` with an older media folder yields a level neither side had. A tie keeps local.
  Folder names compare case-insensitively (the file systems a profile lands on mostly do).
- **`StatisticsMerge` is MAX, not SUM** (decided with the author, `Docs/Plans/PROFILE_TRANSFER_PLAN*.md`
  in the game repository): the common case is one history travelling, and a profile carries no identity a
  merge could subtract a common base with. Summing would double every counter on the second trip; max is
  idempotent (`merge(a, a) == a`) and never inflates, at the cost of undercounting two genuinely separate
  histories to the larger. Everything follows from "never invent a number": a first-time is the earlier
  of the two that are set (a default `DateTime` is "never"), a last-time the later, a value describing the
  latest state (a level's name, the streak) comes from whichever side played last, a record is the better
  of two real runs, never a blend. Merges write INTO the local instance and keep its nested instances
  (`StatisticsStore` views hold references). The histograms are tied to a length: a bucket is a fraction of
  the level as it was when deaths were counted, so equal lengths merge bucket by bucket and different
  lengths take the copy from whichever side played last. `BestRunOrder` is the game's `StatisticsMath.IsBetter`
  order (further, fewer hits, more lives left, fewer dashes) plus one tie-break (the earlier run, achieved
  first); change one, change both.

## Controls catalog

`ControlDeviceInfo` holds STATIC per-device facts and lives here, not in the settings tree, because they
are not the player's to change and must not survive a file (a saved "this device supports Relative" would
outlive the build's support). Two gates decide whether a device drives the avatar: Permitted (static, per
platform - the game's `InputPlatformRules`) and Present (runtime, each driver's `IsPresent`). There is no
per-scene gate: these settings describe how the AVATAR is steered, and the menu and editor drive themselves.

## Font character sets

`FontCharacterService` builds `LevelHints.FontCharacters`, the per-font character sets a player warms a
glyph atlas from. It lives in the SDK so a third-party tool writes byte-identical sets (a hint is trusted
blindly, so every writer must agree on its content). Building is split from writing: `Build`/`BuildAll`
are pure and return values so a caller that journals its writes (`gen_font_cache`, whose undo depends on
it) routes them through its own mechanism, while a caller owning the model uses `Apply`.

- Localized text makes the set localized (a Cyrillic level's glyphs do not belong in an English player's
  atlas); text that is NOT localized is visible in every language, so its characters land in every set.
  The appearing-effect mask goes into the SHARED set: it is drawn in place of whatever the text says, and a
  miss shows as boxes at the one moment the author is looking at them.
- `SortedSet` everywhere, not `HashSet`: the output is diffed and version-controlled, and hash order is
  not contractually stable across runtimes. The sets hold CODE POINTS, not chars: per code unit, an astral
  character would be stored as two entries sorted among the BMP ones, writing surrogates that pair with
  nothing (a reader would warm U+FFFD - harmless but wrong).
- Truncation at the cap is silent and deliberate: the set is advisory, and a CJK level overrunning it
  warms a prefix and renders the rest on demand. Merging happens BEFORE the cap so a shared character
  cannot be dropped in favour of an earlier-sorting language-specific one; the cap counts code units
  (it bounds the file) and a character is taken whole or not at all.

## The folder at a glance (moved from `Services/CLAUDE.md`)

The folder file keeps the traps; this is its map, with what the sections above do not already say.

- **Four subfolders are the level-archive feature and only make sense read together** (design record:
  `docs/issues/ARCHIVE_HISTORY.md` in the consuming project). `Content/` (`IContentStore` - a named
  set of blobs, ROOTED BY CONSTRUCTION, so "does this escape the folder" is a property of the type
  rather than a check at every call site; `DirectoryContentStore`/`MemoryContentStore` are two
  implementations rather than two parallel APIs, which lets the same pipeline serve a disk, a test and
  a server). `Archive/` (**two containers behind one set of rules** - `TarGzService` and `ZipService`,
  `ArchiveFormatSniffer` deciding from the first bytes and naming 7z so a refusal can say so rather
  than call a good file corrupt; the four unpack checks are the same for both; tar carries LINK
  ENTRIES, which no name check catches, while a zip's declared size is a CLAIM - so its limits are
  enforced twice, cheaply from the central directory and then as a hard counter during the copy - and
  two zip entries may SHARE A NAME, which is refused outright; `NonSeekableWrite` keeps one input
  producing one output, since ZipOutputStream writes a different file when it can seek, and a desktop
  export can while an Android SAF export cannot). `Crypto/` (`PgpSymmetricService`, OpenPGP symmetric,
  SEIPD v1 because GnuPG does not implement RFC 9580; the `...Utf8` overloads are what make a Cyrillic
  passphrase interoperate with gpg at all). `LevelArchive/` (what a level archive contains, written and
  read; the reader is also the future server's entry point, which is why every refusal is a value
  rather than an exception). **This is where the SDK started reading files** - a header claiming it
  reads none is stale.
- **`Cache/` is gone**, and what replaced it is a FORMAT rather than another layer: a side-car cache has
  to decide when it is stale, carry an invalidation hook on every write path and keep a second
  serializer in step with the real one, while a format cannot disagree with the file because it IS the
  file. `Serialization/Blob/` is that format; `LevelObjectCodec`, the object tree written out by hand,
  was its worked example and is gone.
- `CryptographyService` (AES-256-CBC, NOT what protects a level - OpenPGP does), `TextFormatService`
  (`{variable}` string templating), `FontCharacterService` (builds `LevelHints.FontCharacters`).
- **`Shapes/ShapeCatalogService`** + `ShapeParameters` - the game's own built-in shape library, here
  rather than in the consumer because it is what a `ShapeId` means: 497 shapes as the cross product of
  a form, a sector, a thickness rung and an invert flag, an id that IS those parameters packed one axis
  per nibble (`Encode`/`TryDecode`), and `Build` producing the geometry in pure C#. The consumer only
  bakes what this enumerates. `SHAPES.md`'s six rules are what make a future axis free and an inserted
  side count harmless.
- **`Controls/`** (`ControlDeviceCatalog`/`ControlDeviceInfo`) - the STATIC per-device facts, outside
  the settings tree because they are not the player's to change and **must not survive a file**: a
  saved "this device supports Relative" would still claim so after the build stopped supporting it.
  The matrix is uniform but for one hole (three devices do all three modes, `DeviceGyro` has no
  Relative, so its `SupportedModes` is `Absolute | Direction`); the mask is the only place a future
  device that genuinely cannot do one - a pedal, a wheel, a MIDI pad - could say so without every
  consumer growing a special case.
- **`Collections/`** - the unit the game keeps in `resources/collections/<guid>/`; its manifest is
  `Models/Collections/ResourceCollection`, a new domain at `V2_SimplifyEntrance`, not a bump. Every failure is a
  value (`CollectionReadResult`, `CollectionArchiveResult`), never a throw. The four data kinds sit in
  `prefabs/`, `themes/`, `shapes/`, `effects/` as one envelope per file; a missing or wrong passphrase
  comes back as `PassphraseRequired`/`WrongPassphrase`, which the host answers by asking. The planners
  close over `Utils/ResourceClosure`.
- **`Profile/`** - the player's whole data folder as one zip (a category is one folder, or
  `settings.json`), written to zip only, under a policy that
  lifts the shared 100-byte name cap (`ArchivePolicy.MaxNameBytes`; tar refuses such a policy). The
  manifest `Models/Profile/ProfileManifest` stores each level's export-time write time, since every
  zip entry is pinned to 1980. The unpack limits are what the archive's own directory declared
  (`ProfileArchiveReader`). A file resource with the same digest as the level's own is `Reused`.
