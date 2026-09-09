A level model addresses things four different ways — a `Guid`, an `int`, a frame number, and a
string field path — and a reader who meets them in that order concludes the format is inconsistent.
It is not: each answers a different question, and which one a new member takes is decided by **one
criterion**, stated below. This file is that criterion, the two id spaces it produces, **what
actually threatens this format's stability** — which is not the criterion, see "Stability" — and the
measured cost of the choice.

This is not a list of every id in the SDK — each id type's own header carries its ranges and its
reserved values. It is the answer to "which kind do I give the thing I am adding", to "will these
ever have to be migrated wholesale", and to "what can still silently change what an already-written
id points at".

### The criterion

> **Does the name have to be unique OUTSIDE the file that holds it?**
> Yes — `Guid`. No — `int`.

Nothing else decides it. Not how many there are, not how big they get, not whether they look like a
key in another format.

| Kind | Types | Where the name is used outside the file |
|---|---|---|
| `Guid` | `LevelId`, `ThemeId`, `EffectId`, `PrefabId`, `ShapeId` | each one **is a file name** in a device-wide store — `<persistentDataPath>/resources/{themes,effects,prefabs,shapes}/<Id>.json\|blob`, and `stats/<LevelId>.json` |
| `int` | `ObjectId`, `AudioId`, `TypedResourceId` and its five per-type spellings (`Audio`/`Texture`/`Text`/`Bytes`/`Font`) | never — handed out by a counter inside one scope, meaningless one file over |

The correspondence is exact and is meant to stay exact: there are four device-wide libraries
(`ThemeLibraryService`, `EffectLibraryService`, `PrefabLibraryService`, `ShapeLibraryService`) and
four resource `Guid`s, plus `LevelId`, which names the statistics file a level's progress lives in.
**An `int` id may never become a file name**, and a `Guid` appearing with no store behind it is a
mistake to correct rather than a style to follow.

Both spellings are wrapped in a one-field struct implementing `IPrimitiveInt` / `IPrimitiveGuid` —
never a bare `int` or `Guid` on a model — so the compiler refuses one kind of id where another is
meant. `PrimitiveIntConverter` writes a bare scalar; `PrimitiveGuidConverter` writes a string under
JSON and a native 16-byte UUID under `.blob`.

### Why not one kind for everything

Both directions were considered and both lose something the format needs.

**Everything `Guid`** — the tempting one, since it is the direction that at least *could* be done —
costs four things and buys only uniformity:

1. **The runtime.** `ObjectId` is the key of some fifteen `NativeHashMap`/`NativeListedHashMap`
   collections and of the `NativeIntervalIndex` behind `IdsGroup`, all rebuilt every frame inside
   Burst jobs over up to ~100k objects. A 16-byte key against a 4-byte one is four times the table
   memory and four times the cache traffic on the hot path that is this engine's whole technical
   claim — and `Guid`'s hash is not the single load an `int`'s is. That cost is paid every frame,
   forever, to make a file look tidier.
2. **The sign**, below — three range schemes would become well-known `Guid` constants plus a
   generated registry of game-defined resources, i.e. a second source of truth where an
   `if (value > 0)` stands today.
3. **Size — in the wrong direction.** Measured on the corpus, rewriting every `int` id as a `Guid`
   grows `level.json` by **10.8%** (volcano), **17.8%** (weathergirl) and **20.0%** (maze), and the
   gzipped package by 5.7–8.8%; `.blob` grows by 12 bytes per id, 545 KB on volcano alone. A change
   argued from "a `Guid` takes a lot of space" would end up spending more of it than it saves.
4. **Readability.** `"id":1234,"pid":1200` becomes two 36-character strings, in the file the
   project's longevity promise is about.

And nothing is bought: a globally unique `ObjectId` is useless, because `PrefabMaterializer`
renumbers every id it materializes and a paste creates new objects either way; `AudioId` and the
resource ids never leave the level. **Uniformity already exists where it is worth having** — both
spellings are one-field structs behind `IPrimitiveInt`/`IPrimitiveGuid` with the same `Null`,
`IsValid`, operators and `ToString`, so consuming code reads the same whichever it holds.

The sign is the second reason, and it is load-bearing in all three int spaces:

| Type | `0` | positive | negative |
|---|---|---|---|
| `ObjectId` | unset / no parent | user-authored objects | reserved game objects — `Camera` (-1), `LocalPlayer` (-2), `PrefabRoot` (-3), and inframe-only ids at -4 and below |
| `TypedResourceId` and the five per-type ids | unset | shipped with the game | authored by this level |
| `AudioId` | unset | this level's audio tracks | banned, for consistency — tracks only ever exist per level |

A `Guid` has no ordering to carve, so each of those splits would become a second stored field or a
lookup, and `ObjectId.Camera` would stop being a constant a job can compare against.

**Only the resource ids `Guid`** is the narrow version of the same proposal — `ObjectId` is
untouchable, so the question becomes whether `TypedResourceId` and its five spellings should move.
They should not, and the reason is not the runtime this time:

- **A resource already has three names, and a `Guid` would replace none of them.** `ResourceKey`
  (`UriType` + `Uri`, several of them as fallbacks) is its address in the world — a file, a URL, an
  addressable key; `TypedResourceId` is its address inside one level file; `Core`'s own
  `ResourceId` is a runtime-only lookup key, reassigned every session. Global identity is what the
  *file* provides, which is exactly why the criterion answers "no" here. A `Guid` would be a fourth
  name for the same thing.
- **The sign is in live use in both directions.** Measured on the corpus: `fnid` is always positive
  (game-shipped fonts), `auid` always negative (the level's own song). The texture preset tier is
  gone, but the other four still have one, so moving to `Guid` means a generated registry of
  well-known ids for the shipped fonts and sounds — `ShapeId.g.cs` machinery, bought for nothing.
- **The runtime is quieter than `ObjectId`'s but not free**: `TextureResourceId` keys five
  `NativeHashMap`s in `ShapeInstancesGroup`, one of them `NativeHashMap<ObjectId,
  TextureResourceId>` rebuilt per frame; `FontResourceId` sits inside `TextInstance`, copied through
  jobs; `AudioResourceId` fills a `NativeArray`.
- **Size, again upward**: +3.2% (volcano) to +5.7% (maze, weathergirl) of `level.json`, +1.2–1.7%
  gzipped.
- What it would buy is one scenario — a device-wide pack of shared resources. That is already solved
  the way the level's own song is: the file travels and its `ResourceKey` is rewritten, the id
  staying local to whichever level holds it — what `LevelPackageBuilder` already does for an
  `AbsolutePath` resource on export.

**Everything `int`** gives up the libraries. A theme imported from the device library into two
different levels has to keep the same name in both, or "this level uses that theme" is not a
statement anyone can make; a counter cannot promise that across files written on different devices.

### `ShapeId` is the one type that pulls both ways, and it stays a `Guid`

It is a `Guid`, but the 497 built-in shapes are **packed parameters** — one axis per nibble, owned
by `ShapeCatalogService` — so a built-in `ShapeId` is a *value* describing what the shape is, not an
identity handed out to something. By the criterion above it would be an `int`.

It is a `Guid` because **one type serves both tiers**: the same field also holds a level-authored
`CompositeShape` and an id imported from the shape library, which do need a file name. A second id
type for the built-ins could only disagree with this one — `ShapeObject.ShapeId` and `ColliderId`
would each have to say which tier they are on before they could be compared. Which tier an id is on
is answered by **which collection it is found in** (`GameResources.CompositeShapes` vs
`Level.Resources.CompositeShapes`), never by the value.

This is also where nearly all of the format's `Guid` bytes are spent — see the measurements below.
It is a known, accepted cost, not an oversight.

### Two things are addressed with no id at all

Both are deliberate, and neither is a candidate for one.

- **By position in time.** `Marker`, `Checkpoint` and `BeatSegment` are addressed by their own frame
  (a `BeatSegment` by its `Span.StartFrame`, which works because segments cannot overlap), and a
  keyframe by `KeyId` = track + frame. The cost is stated where it is paid: an edit that moves one
  changes its identity, so whoever held it re-selects at the new frame. An id would buy stability
  across a move that nothing needs.
- **By field path.** `ModificationKey` is a template-inner `ObjectId` plus a dotted string
  (`"pos[0].v"`), resolved through each property's `[JsonProperty]` name. **This is the most fragile
  address in the format** — more so than any id — because renaming a serialized key under `NAMING.md`
  silently invalidates every prefab override written against the old spelling, in every level already
  on disk. Weigh that before renaming a key on anything a `PrefabObject` can override.

A slot inside a palette is a third case and is a plain `int` index
(`Color4ThemeRef.ThemeColorIndex`): it is a position inside one addressed object, not an object.

### Stability: where an id's meaning lives

The `Guid`/`int` question is settled above and is not where this format's stability is decided.
What decides it is **where the thing an id points at is written down**, and that gives three classes.
Only one of them can drift, and it contains both spellings.

**A — the meaning is in the same file (or in the file the id names).** `ObjectId`, `AudioId`,
user-defined resources (negative), and every randomly generated `Guid`: `LevelId`, `ThemeId`,
`EffectId`, `PrefabId` and a level's own `CompositeShape` ids. All of them resolve through a
dictionary sitting beside them, or through a file named by the id itself. **No future build can
change what one of these points at** — there is nothing in a build for it to point at. This class is
stable by construction and needs nothing done to it.

**B — the meaning is in the game's own build.** The game-defined tiers: a positive `FontResourceId`,
a positive `AudioResourceId`, and the built-in half of `ShapeId`. An id here is a promise the *client*
keeps, so it drifts whenever the client renumbers what it ships — and this is the class that has
already broken once, when the shape library numbered its 78 shapes by array position and inserting
one rewrote every level after it.

- **Built-in `ShapeId` is safe by construction**, and that is what its six layout rules buy:
  the id is the parameters, reserved nibbles are zero in every id ever written (so a future axis
  cannot change an old id's meaning), the form is the side count itself rather than a table index,
  and form code 0 is reserved so no retired 1..78 id can decode into a valid shape. An id carrying a
  bit in the reserved range was written by a build that knows an axis this one does not, and
  degrades to the shape without it.
- **`FontResourceId` presets are safe today by hand, not by rule**: the id is a `[SerializeField]`
  on each `FontResourceScriptable`, so reordering the list changes nothing. Nothing checks that two
  presets do not claim the same number, and nothing stops a retired number being handed out again.
- **`AudioResourceId` presets are NOT safe.** `AudioResourceGeneratorScriptable.Generate()` clears
  the folder and renumbers from `MinGameDefinedValue` in list order, so deleting or inserting one
  clip shifts every id after it — the 78-shape defect exactly, on 121 assets. The editor's audio
  search offers these presets to authors (`SearchOrigin.Game`), so a level can and does reference
  them, and a level referencing a shifted id plays a different sound with nothing to report.

**C — the meaning is in the spelling of JSON keys.** `ModificationKey.Path` alone. It is resolved
through `[JsonProperty]` names at read time, and `ModificationService` answers an unresolvable path
with `null`/`false` — silently, which is correct at that layer and is what makes the failure
invisible. Renaming a serialized key on any type a `PrefabObject` can override therefore voids every
override already written against the old name, in every level already on disk. `NAMING.md` allows
key renames; this is the one place where that permission has a cost that outlives the release.

**So the work stability actually asks for is not a change of id type.** It is: freeze the
game-defined audio numbering, assert uniqueness of both preset tiers, and treat a key rename on an
overridable member as a breaking change.

### What the choice costs, measured

Measured on the bot corpus, `level.json`, raw and gzip -6:

| Level | `level.json` | `Guid` occurrences | distinct | `Guid` bytes | share of file |
|---|---|---|---|---|---|
| volcano | 14.55 MB | 25 105 | 282 | 954 KB | 6.6% |
| quaternions | 6.26 MB | 14 354 | 18 | 546 KB | 8.7% |
| weathergirl | 3.42 MB | 10 381 | 446 | 395 KB | 11.5% |
| maze | 3.11 MB | 10 047 | 84 | 382 KB | 12.3% |
| party | 2.65 MB | 8 547 | 56 | 325 KB | 12.3% |

**96% of those bytes are `shid`/`cid`** — `ShapeObject`'s two shape fields, one pair per object,
resolving to a few dozen distinct values. They are references to presets, not identities, which is
why the distinct counts are two to three orders of magnitude below the occurrence counts.

Rewriting every `Guid` in a level as a short `int` — the theoretical maximum this could ever save —
takes volcano from 14.55 MB to 13.66 MB (-6.2%), and **after gzip, from 0.56 MB to 0.53 MB**
(-5.6%; -16.2% on weathergirl, the corpus' best case). A level ships as a `tar.gz` beside its audio,
so the whole `Guid` overhead of the project's largest level is about 30 KB of a package measured in
megabytes. In `.blob` an id is 16 bytes against an int's 4, the same order.

**Where the bytes actually go is not the id type at all.** `"txid":0` — one `ShapeObject` saying it
has no texture — is written **12 029 times in volcano, 108 KB**, more than `thid`, `eid` and `pfid`
cost together, and every one of those levels uses no textures whatsoever. An id struct is a value
type and cannot be null, so the null-means-absent skip that empties an untouched `LevelTrackEffects`
chain does not reach it. Not-writing a default id is worth more than any change of id type, and
costs no migration.

**Size is therefore not a reason to move any id.** Where the raw byte count does cost something is
JSON *read* time, and `.blob` is the answer to that (203 ms against 9 946 ms on volcano) rather than
a narrower id.

### Adding a new id

1. Apply the criterion. If the thing will ever be stored in a device-wide library, shared between
   levels, or named by a file, it is a `Guid`. Otherwise an `int`.
2. Wrap it in its own struct implementing `IPrimitiveInt` / `IPrimitiveGuid`. Copy the nearest
   existing neighbour — the members, the reserved values and the operators are the same every time.
3. Reserve `0` / `Guid.Empty` as the unset value, and never hand it out.
4. For an `int`, say in the header what the negative half means, or that it is banned (`AudioId`).
5. Give the key its length by `NAMING.md`' rule — one per file is a word, thousands are abbreviated.

### What would force a wholesale migration

Nothing currently on the roadmap, and the two directions are not symmetric.

- **`Guid` → `int`** cannot happen: it would break the device-wide libraries and every file already
  named by an id. Not a scenario, just an impossibility to record.
- **`int` → `Guid`** has exactly one plausible trigger — a device-wide library of *resources*
  (a texture or sound pack shared between levels). Even then it need not fire: a resource is a
  **file**, and a file already carries a global name of its own, so an import copies the file and
  renumbers a local id, exactly as it does today. `ObjectId` can never move: `PrefabMaterializer`
  renumbers every id in a template on materialization, so a global one would be discarded on arrival.
- The int spaces are signed 32-bit and nowhere near exhausted, so no id moves for range.

The realistic long-term risk to this format is not the id types. It is `ModificationKey.Path`, above.
