# Validations: the rule engine and the graph pass, design notes

Contributor reference for `Validations/`. The folder map and the generated-walk story are in
`Validations/CLAUDE.md`; per-rule reasoning is in `RULES.md`. This file holds the reasons the code
comments used to carry.

## Three passes, three questions

`RuleAnalyzer` asks whether a value is in range, `LevelGraphAnalyzer` whether the objects agree with
each other, `PublishReadinessAnalyzer` whether the file may be handed to strangers (which needs the
file plus a service's policy). None subsumes another: a level can be flawless content and
unpublishable (nobody credited the music), or publishable and broken (a NaN in a position).
`PublishValidationReport` carries them side by side; an editor shows all three, a client blocks an
upload on `Publish.HasErrors`, a server queues `NeedsManualReview`. `IsReady` is stricter than
`!HasErrors`: the publish check must have had every input (a metadata-only pass cannot know whether
every resource is covered) and the content half must be CLEAN, since a warning means the level does
not play as written.

`ValidationFacade` is one entry point so a consumer need not know there are two halves, that the graph
pass applies only to a `Level`, or that repairs need several passes to settle. It does not decide what
to do with a report (a game refuses on `HasErrors`, an editor shows everything, an importer repairs
first). The publish entry point is one call because the metadata is its own aggregate root: validating
the level alone silently skips every rule on `LevelMeta`. The level is optional there, since
`metadata.json` exists so a catalogue can grade thousands of levels without opening one. Nothing is
repaired on the publish path: a silent repair on content on its way out is the last thing anyone wants.

## Only the content half is repaired

Graph and publish findings are content decisions (which of two colliding ids survives, whose name goes
in the credits), so `GraphIssue` is deliberately NOT a `RuleIssue`: a `RulePath` ends at one value, and
"two objects share an id" or "this chain loops" has no property to point at or write into. It carries
no repair and has no machinery for one. The one relationship with a uniquely determined repair, a
child span reaching outside its parent's, is not a finding: the overhang is legal data resolved on
read, and fitting lifetimes is an author-requested edit (`mod_span_fit`). A future repairable invariant
belongs there too, as a generator. `GraphRuleKeys` is the graph twin of `RuleNameKey` (a stable
translatable key per finding); it THROWS on an unmapped value so an unnamed rule is caught by the first
test that exercises it (same policy as `ObjectConverter.GetType`). It is an extension on the enum
because the mapping is per rule, not per occurrence.

## The graph analyzer

- Written as an explicit walk, not reflection: a handful of invariants, each with its own shape.
- **Beat overlap** is reported per PAIR (a segment is only wrong relative to another; naming both makes
  it actionable) and order is not assumed: the list is authored and may legally hold a segment before
  its predecessor while its span sits after it. It is here because a property attribute cannot see a
  second segment.
- **A child span outside its parent's is not reported**; see above. It used to be, and logged noise on
  every load of a level behaving as designed.
- **Id counter**: a counter at or below a live id hands the next object a colliding id (silent at
  authoring time, corrupt afterwards). The second finding is a counter that is fine but running out:
  ids are never reused, so the number measures edit time, not size. It is reported at three quarters of
  `LevelRules.MaxObjectIds` because at the wall only a full renumber is left. It runs WITHOUT the
  objects-empty early return: an emptied level whose counter sits near the ceiling is the case for it.
- **Copies of other placements**: the set of ids belonging to OTHER placements' copies is deliberately
  one case narrower than `PrefabVirtualizationUtils.CollectMaterializedChildIds`: a placement whose own
  `ObjectIds` names itself is broken data, and hiding it would silence the rule that reports it.
  Elsewhere that set decides what to write, where self-reference means leave the entry alone.
- **Only a genuine placement is asked the placement questions** (`PrefabMaterializer.FindPlacements`
  has the long form): a scope also holds `PrefabObject` entries that are materialized COPIES of another
  placement, whose `ObjectIds` name ids in the INNER template. Reading them as placements reported
  every remap of every copy as broken: 38 736 findings on the level that prompted this, none true.
- **The prefab root is not in `Objects`**, it is a field (`Prefab.Root`), so a dictionary probe read
  every root-addressed override as dangling (19 993 findings over a correct level). What is worth
  reporting is a root override naming a field the root does not own, since `ApplyModifications` would
  drop it (`PrefabRootUtils.TryGetModificationTarget`).

## Rules, severities and the attribute families

Moved here from `Validations/CLAUDE.md`, which keeps the summary.

- **What `Rules/` depends on.** `Rules/` classes are mostly pure `public const` numeric tables that
  depend on no model CLASS - at most on `Models/` enums and primitives (`FrameRules` on
  `Models.Enums`, `LevelRules` on `Models.Primitives`, `TextRules` on `Models.Enums.Text`). Two
  exceptions reach further: `EffectRules` constructs default `CurveValue`/`GradientValue` model
  instances, and `RuleContext` is built over the level's object models.
- **`RuleGroup` (`None/Error/Warning/Advice`) is real** - every rule states its group. While all but
  one rule took `BaseRuleAttribute`'s default, `ValidationReport.HasErrors` was identical to
  `!IsValid` and no consumer could act on it. The CRITERION is written out at the top of
  `RuleGroup.cs`: Error means the file cannot be played as written, Warning means it plays but not as
  authored (content that never appears, a dangling reference taking a fallback, an override that does
  not apply) or that the only repair is destructive, Advice means playback does not change.
  `Tests/Rules/RuleSeverityTests` makes the choice mandatory - a rule either states its own `Group` or
  is named in that file's `IntentionallyError` list, so a new rule cannot inherit Error by silence.
- **`ValueRules.MaxShapeTriangles` is 128** (64 was outgrown by the game's own shapes): an inverted
  32-sided ring is the box's rim, the ring's outer rim and its inner disc, which is 94 triangles, and
  six more built-in shapes sat at exactly 64 with no room at all. Raising a cap can invalidate nothing
  (it only lets a hand-written file carry more than it could before), and `MaxShapeVertices` is
  derived from it rather than restated.
- **`CompositeShape`'s geometry carries no per-property collection rule beyond `[RuleNotNull]`, and
  that is deliberate.** Every generic collection fix is index-destructive on indexed geometry:
  `RuleCollectionNoNullItems` would *remove* a null vertex and shift every index after it onto the
  wrong point, `RuleCollectionMaxCount` would truncate the vertex list out from under the triangles
  still referencing its tail. Both look local and corrupt the shape silently. The class-level
  `RuleShapeGeometry` owns all of it - only a rule seeing both lists can fix one without breaking the
  other. Don't add a collection rule to `Vertices`/`Indices`.
- **`[RuleOptional]` is what makes a nullable member representable at all.**
  `BasePropertyRuleAttribute.IsValid` answers null for every rule at once
  (`if (value == null) return false`), which is the safety net for a forgotten `RuleNotNull` and is
  pinned by a `TestNull` case in each of the ~20 value-rule fixtures. It is also why a member that
  deliberately starts null - `LevelTrackEffects`' eleven DSP slots, `EffectObjectForces`' eleven
  forces, the colours and limits on `ShadowsMidtonesHighlightsKey`/`VignetteKey`/`LensDistortionKey` -
  made every bound on it report an absent value as out of range. The marker is read once per property
  and cached in `RuleWalk`, on the null path only; both walks go through the same `Check`, so the
  reflective and generated paths cannot disagree about it. Null stays `RuleNotNull`'s question -
  `[RuleOptional]` is the opposite answer to the same one, written down instead of inferred from an
  absent attribute.
- **`RuleEnumValid` covers single-choice enums only.** `[Flags]` enums (`ClipboardContent` on
  `ClipboardData`, `KeyBindingMask` on the keyboard-mouse controls settings, `ObjectTypeMask` on
  `EditorInterfaceSettings`, `CurveWeightedMode` on `CurveKeyframeValue`) go through
  `RuleEnumFlagsValid`, which asks "does this carry an undeclared bit" and whose `Fix` masks the
  unknown bits off instead of falling back to a default. Don't loosen `RuleEnumValid` to cover both -
  `Enum.IsDefined` rejects every legitimate flag combination.
- **The attribute families.** `Rules/Attributes/` are declarative `[RuleXxx]` property attributes
  (`[AttributeUsage(Property)]` only - never fields), all `: BaseRuleAttribute`
  (`IsValidType`/`IsValid`/`Fix`). `[RuleContainer]` (a bare class-level marker) opts a type into the
  walk - applied broadly across `Models/`, not just a handful of aggregate roots.
  `Rules/Attributes/Contextual/` need a `RuleContext` (the root `Level` and the resolved object scope):
  `RuleObjectIdValidAttribute`/`RuleParentObjectIdValidAttribute` check `ObjectId` validity/parent
  rules against the scope, `RuleReferenceExistsAttribute` that a level-defined resource id resolves;
  uniqueness within a scope stays the graph pass's, because a property attribute only ever sees one
  property at a time. `Rules/Attributes/Values/` are typed against the polymorphic
  `IFloat`/`IVector2-4`/`IString`/`IPrimitiveInt`/`IPrimitiveGuid` interfaces, switching per concrete
  variant to check/clamp.
- **`RuleLevelFrame` is not a contextual rule any more**: it bounds a frame on the LEFT only
  (`FrameRules.MinFrame`), because a level's end is a number the author drags, so a key parked past it
  is content waiting for the level to grow - a right-hand bound reported it and its `Fix` clamped it
  away.
- **Cross-object invariants are not attributes.** `Validations/Graph/LevelGraphAnalyzer` owns them
  (duplicate `ObjectId`s, missing or cyclic parents, dangling/self-referencing prefab placements, stale
  id counters, broken remap tables), and `ValidationFacade` runs the two passes together. Don't write
  a graph check as a `[RuleXxx]` attribute.
- **`Validations/RuleAnalyzer`** walks any `[RuleContainer]`-typed object graph (generic over the root,
  not hardcoded to `Level`) and returns `List<RuleIssue>`; `RuleIssue`/`RulePath` carry the full trace
  from root to the failing property (including list index / dict key) so a fix knows exactly where to
  write.
- **The walk is on the EDITOR's load path, not the player's** - a decision the consumer made:
  `Core`'s `LevelLoaderService` carries `LevelValidation { Report, Skip }`, `LoadLevel` (playback)
  defaults to `Skip` and `LoadLevelProtected` (opening a level in the editor) defaults to `Report`. So
  the cost is how long an author waits, and nothing a player waits for.
- **The walk is generated.** `BH.SDK.Roslyn`'s `ValidationGenerator` writes a `Validate` for every
  `[RuleContainer]` type, and `RuleWalk.Node` dispatches through `IValidatable`, falling back to
  `RuleAnalyzer.WalkNode` for anything without one (a non-partial type, a test fixture, a future
  hand-written model), so a half-migrated tree is a legal state. Measured on the corpus, three passes:
  volcano's rules pass went **1614 ms to 850 ms** and weathergirl's **264 ms to 112 ms** - 1.9x and
  2.4x, not the order of magnitude the `.blob` codec bought, because there reflection was the whole
  cost of reading and here it is part of it. What survives is the virtual
  `rule.IsValid(object, RuleContext)` and the boxing at that boundary. The two per-node prohibitions
  under "RuleAnalyzer and RuleWalk" were measured at ~1.3 s on a 4.7k-object level;
  `Tests/RuleAnalyzerPerformanceTests` pins the result.
- **`[RuleContainer]` is INHERITED and the generator only sees DECLARED attributes**: a type that is a
  container purely through its base is invisible to `ForAttributeWithMetadataName` and silently keeps
  the reflective walk. `Tests/Rules/RuleContainerCoverageTests` keeps that count at zero and separately
  asserts that every `[RuleContainer]` in this assembly has a generated walk.
- **The acceptance test is that the report did not change**, two-sided: `Core.Tests.ValidationParityTests`
  compares against a stored count-plus-digest AND compares both walks against each other through
  `RuleAnalyzerSettings.useGeneratedWalk` (details in the last bullets below).
- **`Roslyn/Analyzers/RuleContainerAnalyzer.cs`** (shipped as `BH.SDK.Roslyn.dll`, live in the Editor
  since 2026-09-02) enforces at compile time that every `[RuleContainer]` class is non-static,
  non-abstract, and has a public parameterless constructor - because several `Fix*` paths
  (`RuleNotNullAttribute`, the `RuleIPrimitiveXxx` family) call `Activator.CreateInstance` on property
  *types* at runtime, and a violation would otherwise surface only as a rare `MissingMethodException`
  deep inside an editor "auto-fix my level" flow.

## RuleAnalyzer and RuleWalk

- **What the walk must not do per node** (measured, then removed): query `[RuleContainer]` uncached
  (a Mono custom-attribute query allocates a fresh attribute instance per call), and fetch a property
  value that can lead nowhere (`PropertyInfo.GetValue` boxes every value type). `IsWalkable` is the
  once-per-type answer: `RuleContainerAttribute` is class-only (`AttributeTargets.Class`), so a value type, or a collection of
  value types (the walk descends into list items and dictionary values only), is a dead end;
  everything else stays walkable because a reference type's runtime type may carry the attribute, and a
  declared `IList`/`IEnumerable` can be anything.
- **The split**: everything decided once per TYPE lives on `RuleAnalyzer` (walked properties, their
  rules, container-ness, the buffer pool); everything belonging to one CALL lives on `RuleWalk` (trace,
  findings, settings). `RuleWalk.Node` is the only place a child is reached from; `WalkNode` is its
  fallback for a value with no generated walk; the two are mutually recursive on purpose.
- **`RuleWalk` is a class, not a `ref struct`**: `Node` reaches a runtime walk through `IValidatable`,
  and a `ref struct` parameter on an interface method would put `ref RuleWalk` through every generated
  signature to save one object per `Analyze`. A struct passed by value would copy several words across
  about 150k nodes.
- **The context is a parameter, never a field**: it changes per subtree (a template rebases frames and
  parent ids), and holding it means save/restore around every descent, the bug that shape invites.
- **Two phases, not one**: a node checks EVERY property's rules (`Check`) and only then descends into
  every walkable one (`Descend`). Fusing them reorders the report for any object with more than one
  walkable property (every `RectObject`), and issue order is `RuleFixer`'s input; its repairs are not
  commutative.
- **The trace segment is pushed lazily**: nearly every rule passes (volcano's 19 341 objects report
  zero), and pushing unconditionally cost one `List.Add` and one `RemoveAt` per property per node, about
  five million pairs for one level. The throw path pushes first and never pops; the walk dies with the
  call, so an aborted analysis leaves nothing to clear (`RuleAnalyzerPoolTests` pins the outcome).
- **`IsValidType` is asked once per type** (`RuleTable.RulesTypeChecked` / `PropertyEntry.TypeChecked`),
  per property rather than per table so one misapplied rule does not slow the rest. It is NOT hoisted
  out of the loop: an earlier failing rule breaks it, so a later misapplied rule is never reached and
  must not throw. Where a rule failed the type check, `Check` runs the original loop verbatim (same
  throw, same node, same path).
- **A null value takes `IsOptional` first**, asked only when the value IS null, so the ordinary path
  pays one reference comparison. A null COLLECTION is a legal state, answered by every `Descend`: the
  generated walk calls `Descend` INSTEAD of `Check`, so `RuleOptional`'s short-circuit never reaches it
  (`ThemeData.ColorNames` was the first member both a collection and legally absent, and threw).
- A rank-1 array is an `IList` and lands in that branch, not the array branch; both index alike so the
  paths are identical. The array branch serves multi-dimensional and non-generic arrays.
- **Dictionaries: VALUES only.** A `RulePath` is "this property at this key"; there is no way to say
  "the key itself", so an issue on a key would be repaired into the value. Keys are safe without the
  walk: every key is an id struct, never a `[RuleContainer]` (boxed on the way in, so a Fix would write
  into a copy). Whether a key agrees with its value's id is relational and the graph pass's.
- **Ordinals are checked at construction.** A generated walk addresses properties by ordinal and
  `RuleTable`'s static initializer checks them against reflection. Left lazy, a mismatch would surface
  on whichever node first reaches that type, as a `TypeInitializationException`, halfway through a
  level. The analyzer constructor forces the initializers (a stale `BH.SDK.Roslyn.dll` is the likeliest
  cause and the message says so); it costs nothing extra since the warm-up loop already walks every
  `[RuleContainer]` type. The warm-up covers every container in the assembly, not just `Level`, so
  `UserSettings`/`LevelMeta` are cached too.
- **The pool is a reuse optimization, never a capacity limit**: an exhausted pool allocates. Returning
  through `finally` keeps it a pool: the misapplied-rule throw is by design and leaking one buffer per
  throw drained a shared analyzer until every later `Analyze` died on an empty stack.
- **The analyzer logs nothing.** An issue used to be logged the moment it was found, so a level with
  several hundred findings paid twice in stack traces. Policy is the caller's (`ValidationFacade`).
- **`RuleTable` deliberately does not generate** the properties and rules: built once per type by the
  same expression `RuleAnalyzer` uses, so (1) `RulePath.Property` is the same `PropertyInfo` as the
  reflective path, (2) `RuleIssue.Rule` is the same attribute INSTANCE for every node (emitting `new
  RuleInRangeAttribute(0, 10)` per node would change allocation count and reference identity), (3) the
  rule ARRAY keeps reflection's order (under `analyzeAllPropertyRules = false` the first FAILING rule
  wins, so a different order reports a different finding; 145 properties carry two to five rules), (4)
  repeated `AllowMultiple` attributes keep reflection's order (`Color4MinMax` has four
  `[RulePropertyOrder]`), which is not contractually source order. The ordinal contract is checked
  loudly, once: the expected names travel with the ordinals and a mismatch throws at class init,
  because a silent divergence would report the same findings under the wrong paths in the wrong order,
  and `RuleFixer` repairs in reverse.
- **`IValidatable` is an interface, not a virtual on a base**: dispatch must be on the RUNTIME type (a
  member declared `IVector2` holds one of four variants, `RectObject` one of five), which is what
  `RuleWalk.Node`'s type check buys over a per-node `Dictionary<Type, bool>` lookup. Nothing may
  implement it by hand (BHS1107): a hand-written body bypasses the `[RuleContainer]` check and the
  reflective fallback. The one exception is the fixture proving the seam, `RuleWalkSeamTests`.
- **`useGeneratedWalk`** exists for one caller, the parity test that runs a level through both walks
  and compares the reports element by element; same shape as `SerializationSettings.useGeneratedCodecs`
  (anything that changes how data is READ is locked by a test comparing both paths; the project once
  shipped a reader that passed 4 494 tests and could not open a level). It is also why the reflective
  walk is not deleted.
- **The validation acceptance baseline** (`Core.Tests`' `ValidationParityTests`, baselines in
  `Docs/validation-baselines/`, outside `Assets/` so they get no `.meta`) is a COUNT plus a DIGEST, not the
  text (a mutated volcano reports on the order of a hundred thousand findings); the first lines stay
  verbatim so a mismatch can be read. The corpus is MUTATED before analysis, because the three corpus levels
  report zero findings and an unmutated baseline pins nothing, not even that the walk reached anything. The
  mutation breaks a rule on every object and on the FIRST key of each non-empty track (all keys would multiply
  the report by the keyframe count) and only on LEAF rules (a string or an int): a failing COLLECTION rule
  sets `hasInvalidRule` and suppresses the descent into it, shrinking the coverage the test exists for. The
  frame rule is broken BELOW the timeline, since a level's end is draggable and content past it is legal.
  Prefab placements are expanded before mutation (validation never meets an unexpanded level; the bytes alone
  are three quarters empty). The corpus is read and mutated once per fixture; the serializer and analyzer are
  not shared (per-call caches and pools make run order matter). The parity case without baselines (both
  walks, compared as text) is the half that survives the baseline files being regenerated.
- **`RuleFixer`** applies fixes in reverse order because a fix can invalidate data for deeper issues
  (especially with `analyzeAllRecursiveRules`); each issue carries its own context (a fix inside a
  template clamps against that template's timeline) and whether it addresses a property or an object.
  A full repair re-analyzes and goes again, a few passes at most: a repair can create a new violation
  elsewhere (assigning an id to an unset resource disagrees with its dictionary key, padding a
  collection adds entries with rules of their own), which reverse order cannot catch since the second
  issue does not exist yet. Anything still reported after that is unfixable by design (a dangling
  reference) or a repair loop; both deserve a report.
