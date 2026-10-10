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

## RuleAnalyzer and RuleWalk

- **What the walk must not do per node** (measured, then removed): query `[RuleContainer]` uncached
  (a Mono custom-attribute query allocates a fresh attribute instance per call), and fetch a property
  value that can lead nowhere (`PropertyInfo.GetValue` boxes every value type). `IsWalkable` is the
  once-per-type answer: `RuleContainerAttribute` is class-only, so a value type, or a collection of
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
