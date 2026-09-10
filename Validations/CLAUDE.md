# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Validations

Read `Assets/Plugins/BulletHeroSDK/CLAUDE.md` first — it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.


## Validations/

the rule engine, in two halves. *Declarative*: `RuleAnalyzer`/`RuleFixer`
  (+`RuleIssue`/`RulePath`) walk `[RuleContainer]`-marked object graphs and check/auto-fix every
  `[RuleXxx]`-attributed property, one property at a time. *Relational*: `LevelGraphAnalyzer`
  (+`GraphRule`/`GraphIssue`) checks the cross-object invariants a per-property attribute
  structurally cannot see. `ValidationFacade`/`ValidationReport` run both and are what a consumer
  should call. **Opt-in tooling, not wired into save/load anywhere** — see "Rules & validation" below.

## Rules & validation

`Rules/` classes are mostly pure `public const` numeric tables with zero `Models/` dependency
(`FrameRules`, `ValueRules`, `LevelRules`, `AudioRules`, `PostProcessingRules`, `ResourceRules`,
`TextRules`) — `EffectRules` is the one exception, constructing default `CurveValue`/`GradientValue`
model instances. `RuleGroup` (`None/Error/Warning/Advice`) is the severity enum, and it is **real
now**: 36 rules are Error, 8 are Warning, 1 is Advice. It was not - forty-four of forty-five took
`BaseRuleAttribute`'s default, so `ValidationReport.HasErrors` was identical to `!IsValid` and no
consumer could act on it. What was missing was the CRITERION, which is now written out at the top of
`RuleGroup.cs`: Error means the file cannot be played as written, Warning means it plays but not as
authored (content that never appears, a dangling reference taking a fallback, an override that does
not apply) or that the only repair is destructive, Advice means playback does not change.
`Tests/Rules/RuleSeverityTests` makes the choice mandatory - a rule either states its own `Group` or
is named in that file's `IntentionallyError` list, so a new rule cannot inherit Error by silence.

**`ValueRules.MaxShapeTriangles` is 128, and it was 64 until the game's own shapes outgrew it** —
an inverted 32-sided ring is the box's rim, the ring's outer rim and its inner disc, which is 94
triangles, and six more built-in shapes sat at exactly 64 with no room at all. Raising a cap can
invalidate nothing (it only lets a hand-written file carry more than it could before), and
`MaxShapeVertices` is derived from it rather than restated.

**`CompositeShape`'s geometry carries no per-property collection rule beyond `[RuleNotNull]`, and
that is deliberate.** Every generic collection fix is index-destructive on indexed geometry:
`RuleCollectionNoNullItems` would *remove* a null vertex and shift every index after it onto the
wrong point, `RuleCollectionMaxCount` would truncate the vertex list out from under the triangles
still referencing its tail. Both look local and corrupt the shape silently. The class-level
`RuleShapeGeometry` owns all of it instead — only a rule seeing both lists can fix one without
breaking the other. Don't "helpfully" add a collection rule to `Vertices`/`Indices`.

**`[RuleOptional]` is what makes a nullable member representable at all.** `BasePropertyRuleAttribute
.IsValid` answers null for every rule at once (`if (value == null) return false`), which is the safety
net for a forgotten `RuleNotNull` and is pinned by a `TestNull` case in each of the ~12 value-rule
fixtures. It is also why a member that deliberately starts null - `LevelTrackEffects`' eleven DSP
slots, `EffectObjectForces`' eleven forces, the colours and limits on `ShadowsMidtonesHighlightsKey`/
`VignetteKey`/`LensDistortionKey` - made every bound on it start reporting an absent value as out of
range. The marker is read once per property and cached in `RuleWalk`, on the null path only, so the
ordinary path pays a reference comparison; both walks go through the same `Check`, so the reflective
and generated paths cannot disagree about it. Null stays `RuleNotNull`'s question - `[RuleOptional]`
is the opposite answer to the same one, written down instead of inferred from an absent attribute.

`RuleEnumValid` covers single-choice enums only; `[Flags]` enums (today: `ContentDescriptor` on
`LevelMeta`) go through `RuleEnumFlagsValid`, which asks "does this carry an undeclared bit" and
whose `Fix` masks the unknown bits off instead of falling back to a default. Don't loosen
`RuleEnumValid` to cover both — `Enum.IsDefined` rejects every legitimate flag combination.

`Rules/Attributes/` are declarative `[RuleXxx]` property attributes (`[AttributeUsage(Property)]`
only — never fields), all `: BaseRuleAttribute` (`IsValidType`/`IsValid`/`Fix`). `[RuleContainer]`
(a bare class-level marker) opts a type into the reflective walk — applied broadly across `Models/`
(156+ files), not just a handful of aggregate roots. `Rules/Attributes/Contextual/` need the root
`Level` as context (`RuleLevelFrameAttribute` checks against `Level.Settings.FrameDuration`,
`RuleObjectIdValidAttribute`/`RuleParentObjectIdValidAttribute` check `ObjectId` validity/parent
rules) — both still carry a `// TODO add complex check for parenting and ids uniqueness`, because a
property attribute only ever sees one property at a time. **Cross-object invariants are implemented,
just not here** — `Validations/LevelGraphAnalyzer` owns them (duplicate `ObjectId`s, missing or
cyclic parents, dangling/self-referencing prefab placements, stale id counters, broken remap tables),
and `ValidationFacade` is what runs the two passes together. Don't write a graph check as a
`[RuleXxx]` attribute. `Rules/Attributes/Values/` are
typed against the polymorphic `IFloat`/`IVector2-4`/`IString`/`IPrimitiveInt`/`IPrimitiveGuid`
interfaces, switching per concrete variant to check/clamp.

`Validations/RuleAnalyzer` walks any `[RuleContainer]`-typed object graph (generic over the root, not
hardcoded to `Level`) and returns `List<RuleIssue>`; `RuleFixer` applies fixes **in reverse trace
order** deliberately (fixing may invalidate/shift deeper issues). `RuleIssue`/`RulePath` carry the
full trace from root to the failing property (including list index / dict key) so a fix knows exactly
where to write. **The analyzer logs nothing** — an issue used to go to the console the moment it was
found, on top of whatever the caller did with the returned list, so a level breaking one rule on
every object paid for each finding twice in Editor stack traces; what to do about a report is
`ValidationFacade`'s caller's policy.

**The walk is on the EDITOR's load path, not the player's**, and that is a decision the consumer
made rather than a property of this code: `Core`'s `LevelLoaderService` carries an enum
`LevelValidation { Report, Skip }`, `LoadLevel` (playback) defaults to `Skip` and
`LoadLevelProtected` (opening a level in the editor) defaults to `Report`. So what this costs is how
long an author waits, and nothing a player waits for. Anything here claiming it runs on every level
read is stale.

**IT IS GENERATED NOW.** `BH.SDK.Roslyn`'s `ValidationGenerator` writes a `Validate` for every
`[RuleContainer]` type - 200 of them - and `RuleWalk.Node` dispatches a value to its own walk through
`IValidatable`, falling back to `RuleAnalyzer.WalkNode` for anything without one (a non-partial type,
a test fixture, a future hand-written model). The two are mutually recursive through that one point,
so a half-migrated tree is a legal state. Measured on the corpus, three passes, volcano's rules pass
went **1614 ms to 850 ms** and weathergirl's **264 ms to 112 ms** - 1.9x and 2.4x, not the
order-of-magnitude the `.blob` codec bought, because there reflection was the whole cost of reading
and here it is part of it. What survives is the virtual `rule.IsValid(object, RuleContext)` and the
boxing at that boundary.

Four things carry that:

- **`RuleWalk`** owns one `Analyze` call's state - the trace, the findings, the settings - and is the
  only place a child node is reached from. A class rather than a `ref struct`: it travels through an
  interface, so `ref` would spread to 200 generated signatures to save one object per call.
- **`RuleTable`** is what the generator deliberately does NOT produce. Reflection builds the
  `PropertyInfo` array and the rule arrays once per type, so a trace holds the same `PropertyInfo`
  object either way, `RuleIssue.Rule` is the same attribute instance it has always been, and the
  rule array keeps reflection's order - which decides WHICH finding is reported when a property
  carries several. The generator supplies values by ordinal plus the property names it expected, and
  a disagreement throws at analyzer construction naming a stale `BH.SDK.Roslyn.dll`.
- **`IsValidType` is asked once per type, not per node**, on both paths (`RuleTable.RulesTypeChecked`
  and `PropertyEntry.TypeChecked`). It is a reflective type test that used to run per rule per node,
  and hoisting it is where most of the speedup came from. It is NOT hoisted out of the rule loop
  entirely, and must not be: an earlier failing rule breaks that loop, so a later misapplied one is
  never reached and must not throw.
- **The trace segment is pushed lazily** - only once a finding actually needs it.

**Two things the walk must not do per node** predate all of this and still hold, both removed after
measuring ~1.3 s on a 4.7k-object level: query `[RuleContainer]` uncached (a Mono custom-attribute
lookup allocates a fresh attribute instance every call), and read a property whose value can lead
nowhere. `RuleContainerAttribute` is `AttributeTargets.Class`, so a value type — or a collection of
value types, since the walk only descends into items/values — is a proven dead end and is never
fetched at all. `Tests/RuleAnalyzerPerformanceTests` pins the result.

**`[RuleContainer]` is INHERITED and the generator only sees DECLARED attributes**, which is the one
trap in the arrangement: a type that is a container purely through its base is invisible to
`ForAttributeWithMetadataName` and silently keeps the reflective walk. Two types were in that state
and now declare it; `Tests/Rules/RuleContainerCoverageTests` keeps the count at zero and separately
asserts that every `[RuleContainer]` in this assembly has a generated walk.

**The acceptance test is that the report did not change**, and it is two-sided:
`Core.Tests.ValidationParityTests` mutates each corpus level so every object and track reports (the
unmutated corpus reports nothing at all, so a baseline of it would pin nothing), then compares
against a stored count-plus-digest AND compares both walks against each other through
`RuleAnalyzerSettings.useGeneratedWalk`. That switch exists for that one caller, exactly as
`SerializationSettings.useGeneratedCodecs` does - which is also why the reflective path is not
deleted now that every model has a generated one: deleting it would take the proof with it.

`Roslyn/Analyzers/RuleContainerAnalyzer.cs` (shipped as `BH.SDK.Roslyn.dll` in the SDK root, and
live in the Editor since 2026-09-02) enforces at compile time that every `[RuleContainer]` class is
non-static, non-abstract, and has a public parameterless constructor — because several `Fix*` paths
(`RuleNotNullAttribute`, the `RuleIPrimitiveXxx` family) call `Activator.CreateInstance` on property
*types* at runtime, and a violation here would otherwise only surface as a rare, hard-to-place
`MissingMethodException` deep inside an editor "auto-fix my level" flow.
