Design facts of the credit and licence records in `Models/Meta/` that the code cannot say by itself. A
contributor changing `Author`, `ResourceMeta` or `PermissionGrant` reads this first. The public side
(what an author writes) is `DocsPublic/en/docs/2_editor/3_rights/`.

## Author

A credit line, not a role enum: the vocabulary of a credits list is open (mixing, mastering, playtesting,
"level design, second half") and a fixed list is a lie the moment a level needs a word not in it. It is
localizable for the same reason `Name` is: a handle and what it did are read by the same person in the same
language. A record has no identity of its own - two with the same name are the same person only by
convention. It holds a name, a credit and a url, and an empty credit is an ordinary record.

## ResourceMeta

- **Consumed by `LevelMeta` (itself NOT in `Models/Meta/`), addressed in one of THREE ways**: a file
  resource's negative int, a data resource's guid, or neither for the cover (`ResourceType.LevelLogo`),
  since a level has exactly one - `RuleResourceMetaId` enforces which. It carries licensing/attribution
  only. From `Models/CLAUDE.md` ("Meta"), which keeps the summary.
- **The licence defaults to `NoSpecifiedLicense`.** An unfilled record is one nobody read the terms of;
  defaulting it to a real licence (it was CC BY-NC) made the file state something the author never
  checked, about a work usually someone else's. A publish profile can then refuse the level, which is the
  correct outcome - a level wrongly labelled CC BY-NC is indistinguishable from one that is. It matches
  what `RuleNotNull` repairs a null into.
- **Hashes identify the BYTES, not the record.** A takedown names a work, and answering it means finding
  every level carrying it: by name that is a guess, by hash a lookup. Plural because one resource can be
  several files (a track re-encoded per platform), each with its own digest. The form is
  `<algorithm>:<hex>`, so the algorithm can change without breaking what is stored.
- **A flag says which record a card names** (which track is "the music"): a fact of `level.json` that a
  level's presentation may never open, so the record says it about itself. Generic rather than
  audio-only, since cover art and a display font are the same question for another family.
- **AI generation is a property of the bytes** (like licence and authors), three states (`AiGeneration`)
  so an unfilled record says "unknown" rather than "no".
- **No age rating on a resource, on purpose.** It lives on `LevelMeta` alone: a rating describes what a
  player is about to experience, and the same track is menu music in one level and a jump scare in another.
  A per-resource rating would be guessed by whoever imported the asset and would make the level's own
  meaningless.
- Members are appended last (the blob is declaration-ordered and append-only).

## PermissionGrant

The storable form of "option B" of the UGC licensing policy: a resource whose licence forbids
redistribution can still be published if its rights holder said yes specifically, and a private "sure"
is worthless to a moderator who cannot see it. So the claim carries who said it, how far it reaches
(`PermissionScope`), until when, and where the proof is. Without this type the only expression was a
free-text line in `ResourceSources`, which no code can act on.

- Both timestamps are UTC, and nothing enforces it because nothing can: a `DateTime` carries its Kind
  through JSON and the blob, but a caller handing over a Local one writes a moment that reads differently
  on another machine. Whoever fills them in converts first - a permission lapsing "on the 3rd" must not
  lapse a day early for a moderator in another timezone.
- `default(DateTime)` is the unset value and reads as "no constraint" in both places: an open-ended grant
  on `ExpiresAt` (the common case), a record whose author skipped the date on `GrantedAt`.

## LevelMeta

- **The age rating is on the level only** (see ResourceMeta above) and it is ONE number: a `ContentDescriptor`
  flag set (nine checkboxes about their own level, shown to a player who had read the number) was removed.

### Difficulty

- **One value, `LevelDifficulty` (key `difficulty`), not an author's claim beside a moderator's verdict.** The
  author writes it, a moderator (a service re-publishing the level) overwrites the same field, a player reads
  whatever is there. Who may overwrite it is the publishing service's business, not the format's.
- **Every `int` is legal**, so the member carries no `Rule*`. Zero or less is not rated
  (`DifficultyRules.Undefined`); a larger number is always at least as hard, so sorting by the raw value is
  sorting by difficulty.
- **Read through `DifficultyRules.Read`, never stored as a tier.** `DifficultyTier` is derived: values
  `Auto` (1) up to `DifficultyRules.ScaleMax`, each equal to the number it is read from. A value above
  `ScaleMax` is kept as written and read as the top tier, so a later scale past it is a reading change, not a
  format change - and an editor must not rewrite such a value unless the author picks another tier.
- **The tier names are the client's.** The format holds only the number.
- A file from before generation 3 migrates unrated (`LevelMetaV2ToV3`): a migrator cannot judge a level.
- **Tags are plain strings, not `IString`.** Everything else a player READS is localized; a tag is a filter
  token a player MATCHES, and a translated one stops matching when two players run different languages. Free
  text, not a closed set, for the reason `ILevelSource.Id` is a string: a shipped vocabulary is wrong for every
  genre nobody thought of.
- **The duration is denormalised from `Level.Settings`, and has to be:** the browser reads metadata for every
  level and never opens `level.json`, which would be hundreds of files parsed to draw one filter chip.
  `EditorService` rewrites it on every save. ZERO MEANS UNKNOWN, not "instant" (a level written before the field,
  or by another tool): a duration filter must treat 0 as "do not know" or it hides every level predating it.
- **`MinGeneration` is the gate that costs no read:** `metadata.json` is read per level without touching the
  level, so the card can say "needs a newer build" and Play and the editor refuse before a multi-megabyte
  `level.json` is read (which would throw `NewerGenerationException` anyway). It is computed at save from the
  registry (`LevelGenerations.Required()`), never typed; `ModelGenerations.Invalid` is what a file making no
  claim holds, since zero is a real generation. The key has to ship in the first released client: it is
  additive, but a shipped client that knows to look at it cannot be added later.
- **The author list covers the level's own content only** (design, objects, texts). Every resource declares
  itself on its own record, the cover included (`ResourceType.LevelLogo`). `ContainsAiContent` is therefore
  derived from what is in the file and is not a member: a stored aggregate would drift on the first edit.
