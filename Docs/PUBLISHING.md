# Publishing: why the readiness check is shaped the way it is

Contributor-facing reasoning for `Publishing/`; the folder's `CLAUDE.md` carries the map. Code
comments there keep one line and point here.

## What the pass is

The third validation pass, and the only one that asks about the outside world. `RuleAnalyzer` asks
whether a value is in range, `LevelGraphAnalyzer` whether the objects agree with each other,
`PublishReadinessAnalyzer` whether the level may be handed to strangers - which nothing in the file
answers alone, only the file plus a service's policy.

- **Nothing repairs anything**, as with graph findings: every fix is a fact only the author has.
  Naming a license nobody read, or crediting an author nobody identified, would be the analyzer
  inventing the paperwork it exists to demand. `PublishIssue` is shaped like `GraphIssue` for the same
  reason: there is no single property to point at or repair ("this resource has no metadata record"
  spans two files).
- **The level file is optional.** `metadata.json` is its own serialization root so a catalogue can
  grade thousands of levels without opening one, and the meta-only pass covers most of the policy.
  Exactly two findings need `level.json` - a resource with no record at all (`ResourceMetaMissing`)
  and where a resource is fetched from (`ResourceUriTypeNotAllowed`). `PublishReadinessReport.LevelInspected`
  says which pass it was: a clean meta-only report means "nothing wrong in what was read", never
  "ready to publish".
- **`PublishIssue` reuses `RuleGroup`**: `Error` is refusal, `Warning` is "publish, but a human has
  to look" (the split that lets one analyzer serve the client's automatic check and a server's
  moderation queue), `Advice` never gates anything.
- **Facts travel as data (`Args`), not only in the English `Message`**: a client in another language
  needs the resource, the license and the accepted list as values; an enum stays an enum so the
  caller names it through its own string table.
- **Sizes are not part of the format.** `PublishPayload` is an input to the check (a plain class, no
  `[JsonProperty]`): the model knows a texture is referenced, not that it is 90 MB, so the sizes come
  from whoever holds the bytes (the editor before an upload, a server after one). It is optional, and
  a profile with no size limits never needs it.
- **`now` (UTC) is the analyzer's only clock.** Unset means "no clock", under which no permission
  ever expires.

## The profile is data

`PublishProfile` is one service's answers to the same handful of questions, serialized: adding a
service is writing a file, a stricter store ships a stricter file, never an analyzer edit. What the
caller does with the answer is deliberately not decided here (a client blocks on `HasErrors`, a
server queues `NeedsManualReview`, a catalogue re-runs on a policy change).

- **Which typical licenses are acceptable lives in the profile only.** It reads like a property of
  the license and is a property of the receiving service (BY-SA is fine for a server that does not
  mind ShareAlike propagating). The analyzer does not decode typical licenses into an acceptability
  table: that would put the same policy in two places. Whether two licenses can be COMBINED is a
  different question with one answer for every service - "License compatibility" below.
- **Three size ceilings, because they fail differently**: one huge file is an unconverted asset
  (re-encode); an oversized `level.json` is structural (hundreds of thousands of keyframes, no
  re-encode helps); a total fitting neither pattern is a level nobody can download on a phone. Sizes
  are bytes (`ByteSizeUtils`), zero means no limit.
- **The three shipped profiles** answer: may this level exist at all (local, always), may it go to
  strangers, may it go to strangers on a platform whose own store rules apply on top. An operator
  writes their own by editing the json.
- **GPL/AGPL/LGPL are absent from the accepted list**, not because they are unsuitable for a level:
  a GPL work redistributed through the App Store collides with Apple's terms (the VLC case), so
  allowing it would build a catalogue that cannot later be served to iOS. MIT/Apache cover the same
  ground without that collision. **CC BY-SA and CC BY-ND are absent** per the public UGC licensing
  policy: ShareAlike would force the level's own license to change, NoDerivatives forbids the
  editing the game is built around.
- **The Workshop profile asks less than a public server**: Steam's agreement puts a takedown on
  the uploader, so an upload is refused only where plainly unsafe (a license forbidding redistribution:
  proprietary, or a custom text nobody graded; or copyleft that collides with a store). Every CC BY
  variant passes. An ND or SA license on its own is accepted; what a track's license demands of the
  level carrying it is reported as a warning by the compatibility check below.
- **A public server asks rather than refuses**: no license stated, no credits record and no source
  page are warnings, never blocks.

## How the analyzer grades

- **A service that lists licenses and still tolerates an unknown one wants to hear about it**
  (`AsksAboutUnknowns`): a missing license or credits record is a warning, not silence. A profile
  that lists none (the local one) stays silent; one that does not tolerate an unknown refuses.
- **A custom license is never acceptable automatically.** The profile knows typical licenses and no
  rule can grade prose it has never seen; it lands as "not accepted", and a rights holder's grant
  downgrades that to a review (a person reads the terms). The profile is the only reason to demand
  a credit: the author's own grading of their own wording is the claim the analyzer cannot verify.
- **A permission never makes a refused license acceptable**, it makes it a question for a person:
  the grant carries proof so that somebody opens it, so that path always yields a review, not a pass.
- **A rostered site and an unrostered one can carry the same grade** (most profiles map "never heard
  of it" onto `RequiresLicenseCheck`), so the grade alone cannot decide the finding: the rule comes
  from whether an entry was found, the severity from the grade. Reporting differs: the first names
  the entry, the second says there is none.
- **A finding names the resource the author knows** (`ResourceNames`): the record's title, then the
  resource's own name, the address only last.

## License compatibility

`LicenseTerms` is what the Creative Commons legal codes say about combining licenses: the elements
each typical license carries (`LicenseElements`) and the licenses an adaptation of a ShareAlike work
may carry. These are facts, not policy, so they are code and no profile can change them.
`PublishReadinessAnalyzer` uses them to grade a level's license against its **audio**:

- **A synced track is always an adaptation.** CC 4.0 s.1(a) and CC 3.0 s.1(a) both say so of music
  synched in timed relation with a moving image. An image is left out: placed unmodified it is a
  collection, and nothing records whether it was modified.
- **ND forbids sharing an adaptation** (`ResourceForbidsAdaptation`). Reported only when the profile
  accepts the license; a refused one already has `ResourceLicenseNotAllowed`.
- **SA fixes the level's license** (4.0 s.3(b)(1), 3.0 s.4(b): same elements, this version or later).
  `LevelLicenseShareAlikeMismatch` when the level carries something else, `ResourceShareAlikeConflict`
  when two tracks admit no common license. The 4.0 "BY-SA Compatible License" list (GPLv3) is not
  modelled: no shipped profile accepts GPL.
- **NC without SA binds nothing**, but a level whose own license allows commercial use misleads:
  `LevelLicenseOmitsNonCommercial`, advice only.
- **A usable permission silences all of it**: the grant is the rights holder's own terms, and
  `AnalyzeResourceLicense` already turns it into a review.
- **Severity follows the profile's tolerance**: none on a profile that lists no licenses (local use),
  a warning where `AllowUnknownLicense` is set (the Workshop), an error elsewhere.
- **A collection is not graded on it**: it is a set of resources, not an adaptation, and each track is
  judged in the level that syncs it.

## The site roster

`TrustedSourceCatalog` is the public UGC licensing policy's site list as data
(https://bullethero.space/en/notes/ugc-licensing-policy); the two are edited together. It ships as a
starting roster so a fresh server and a client with no server can grade a URL; every operator
overrides it in their profile, and nothing downstream may assume an entry is present.

- **Grades are not a severity ladder to compare with `>=`.** Each maps to one outcome (publish,
  publish and queue for review, refuse), and `PublishReadinessAnalyzer` is the only place that
  mapping lives. A CC0 claim on a Kenney URL needs nobody to look; the same claim on a random blog
  needs a human; a claim on a site that licenses nothing is already a no.
- **Streaming platforms are listed as `NotAllowed` rather than omitted**: an absent site grades as
  the profile's unknown ("somebody should look"), far too mild for the commonest way a level becomes
  unpublishable.
- **Matching is by host, not by URL string**: path, query and scheme vary per resource and a prefix
  match fails on a `www.` or country-prefixed link. Subdomains match their parent domain, so one
  entry covers a site.

## Collections

`CollectionReadinessAnalyzer` grades whether a collection stands on its own, the one thing an
importer relies on and nobody can fix after upload: every file the manifest names is present, every
user resource its prefabs and effects point at is carried (a left-behind closure is an unresolved
reference in every importing level), and it says who made it and under what terms. The per-resource
license checks are the level analyzer's own, run on the credits the collection carries; the profile's
size cap applies to the whole folder. It reads no files: present store paths and a byte count are
handed in, so the same check runs on a folder, an unpacked upload on a server, or memory.

Only a reference that LOOKS user-made can be missing: a file resource with a negative id, or a guid
the collection does not hold. A game-preset guid cannot be told from a dangling one, so guid
references are checked against the closure of the data resources only; a game-defined shape is
recognised by being addressed through the built-in catalogue.
