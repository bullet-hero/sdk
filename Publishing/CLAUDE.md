# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Publishing

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Publishing/

**the third validation pass, and the only one that asks a question about the
  outside world.** `RuleAnalyzer` asks whether a value is in range; `LevelGraphAnalyzer` asks whether
  the objects agree with each other; `PublishReadinessAnalyzer` asks whether the level may be handed
  to strangers — which nothing in the file can answer alone, only the file plus a service's policy.
  Like the graph findings, **nothing here repairs anything**: naming a license nobody read, or
  crediting an author nobody identified, would be the analyzer inventing the very paperwork it exists
  to demand. The level file itself is OPTIONAL to the pass, by design — `metadata.json` carries the
  attribution, so readiness can be graded without opening the content.
  - `PublishProfile` is **the policy as DATA**, which is the whole design: every service a level can
    reach (Steam Workshop, the official server, a community one, a store build's own catalogue)
    wants a different answer to the same handful of questions, and those answers change over the
    years while the code does not. Adding a service means writing a file; adding a store means
    shipping a stricter one. **Which typical licenses are acceptable lives here and only here** —
    it reads like a property of the license and is a property of the receiving service.
  - `TrustedSourceCatalog`/`TrustedSource`/`SourceTrust` are the site list `UGC-LICENSING-POLICY.md`
    writes out in prose, as data — the document stays the human-readable version and this is what
    code grades against, **so the two are edited together**. A starting roster, not a fixed one:
    every operator is expected to override it, and nothing downstream may assume an entry is
    present. Streaming platforms are listed as `NotAllowed` rather than omitted, since an absent
    site grades differently from a refused one.
  - `PublishIssue`/`PublishRule`/`PublishPayload`/`PublishReadinessReport` are the finding shapes.
  - **`ValidationFacade.ValidateForPublish` is how a host asks all three passes at once**, returning
    a `PublishValidationReport` that carries the content half and this one side by side. It exists
    for one mistake it prevents: `LevelMeta` is its own aggregate root, so `Validate(level)` never
    touches a single rule on it - and the metadata is precisely the half a publish check is about.
    The level is OPTIONAL, for the same reason the analyzer makes it optional. It repairs nothing.
