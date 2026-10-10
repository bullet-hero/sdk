# Roslyn generators and their editor tooling: design facts

The layout and build are `Roslyn/README.md`; this file holds why the generators and the Editor-side
builder look the way they do. A rejected idea is recorded as a fact about today.

## Common rules

- **Every refusal is loud.** The defects the generators replace are silent by nature (a member in `Copy`
  and missing from `Equals`; a walk visiting properties in another order, reporting the same findings
  under wrong paths and feeding `RuleFixer` a different repair order, and repairs are not commutative). A
  construct a generator cannot express is an error naming it, never a skip.
- **Specs hold no compiler symbol.** An incremental pipeline caches by value; a cached `ISymbol` pins a
  whole `Compilation` and never compares equal twice. `ModelSpec` flattens to strings and enums once
  (`EquatableArray` for lists, since `ImmutableArray` compares by reference). The generator runs on
  roughly every assembly (Unity scopes an analyzer to the owning asmdef plus every referencing assembly,
  and `BH.SDK` is autoReferenced) over 200+ model types, so a symbol creeping back would re-emit
  everything on every recompile; `ModelGeneratorIncrementalTests` notices.
- **`[GenerateModel]` is what bounds it**: a compilation holding none never matches the predicate.
- **Attributes are matched by simple name** (`ModelGenerationAttribute`): renaming it without renaming the
  literal leaves every type with no domain, the blob emitter stops writing envelopes and the JSON emitter
  stops wrapping nested domains - it compiles clean and the format differs. A wrong arity is reported.
  The test harness carries a stub attribute for the same reason (without it the whole versioned path is
  untested and looks like nothing).
- `ValueSpec` mirrors `ModelGenerations.Invalid` as its own copy: this assembly compiles against no SDK
  reference. Both sides say so; keep them in step.

## Model generator

- **Why**: 208 hand-written `Copy` bodies, 279 typed `Equals`, 206 `Update`, 206 `Pull`; the easiest silent
  bug is `Equals(object obj) => obj is T value && Equals(value)` pasted from a sibling.
- **It reproduces the hand-written shapes exactly, not a tidier design**: `Update`/`Pull` stay overloads,
  `Copy` stays virtual, a polymorphic variant's interface-typed `Update` stays a no-op on a sibling.
  `ModelHierarchyTests` pins all three; a "better" shape is a silent change in 205 types at once.
- A generator sees only a type's DIRECT base, so three chain helpers cover their own members and call the
  base's first (`ResetFromX`, `CopyToX`, `EqualsX`); `Reset()` allocates one defaults instance at any depth.
- **Reset restores a value without replacing an instance**: a nested model is reset in place, a
  collection cleared in place, only a POLYMORPHIC field is replaced (a `Vector2Rect` cannot reset into the
  `Vector2Value` the constructor wanted). `PullFrom` answers both: it keeps the instance while the concrete
  types allow it.
- **A dispatcher is emitted only where something is read through a base** (`RectObject` and the
  value-family interfaces). Most hierarchies are met by concrete type. Every candidate then needs a tag,
  or none can be read back, so an untagged one is an error naming it.
- **The tag is the model's own `GetModelType()`**, read from the enum member the method names (the
  semantic model resolves `=> SomeEnum.Member;` to its number), never a second numbering; a renumbered
  member moves both encodings. 0xFF is null, so no discriminator may reach 255. A body that is not a
  constant is not a tag.
- The dispatcher declares `IBinaryModel`, not the family root: `IModel` is what a model is to the program,
  `IBinaryModel` what it is to a file, and neither drags the other in.
- **Member shape vs leaf**: `MemberShape` (list, dictionary, nested model) is all `Copy`/`Equals` need; an
  encoding needs the LEAF (element, key, enum width). It is a separate axis (`ValueSpec`) because the
  product of the two in one enum is forty cases nobody can read. Which dictionary copy applies is decided
  by "is the key unmanaged", not "is it a model": `ModificationKey` is a struct holding a string.
- `ImplementsSelf` matches by NAME and NAMESPACE, not a formatted display string: `ICopyable<out T>`
  renders its variance and `IModel` does not, and comparing text once made a covariant interface invisible.
- `[JsonIgnore]` is INHERITED and sits on the abstract declaration (`BaseDeviceControlsSettings` marks
  `GeneralMode`/`Device`, the four device classes do not repeat it); the factory walks the chain like
  Newtonsoft, or it writes two members no level file has.
- An abstract property is written by whichever subtype overrides it, never in the base (Newtonsoft puts the
  override in the DERIVED block: `Resource.Type` lands between `TextureResource`'s members and `src`).
- A get-only property is WRITE-ONLY on the wire and in nothing else (the contract is OptOut in practice:
  the resolver sets `MemberSerialization` after the properties are collected). Dropping it changes the
  format.

### Blob half

Conventions are the level cache's hand-written codec: strict write/read pairs in member order, a length
prefix of -1 for null (an empty keyframe list and a missing one are different states), a one-byte tag for a
polymorphic value with 0xFF for null. New is what a format needs and a cache did not: a per-domain
envelope carrying its own version and a length the reader checks content against.

### JSON half

Its acceptance test is BYTE IDENTITY with what Newtonsoft's contract and the 35 converters already write;
"cleaner" is never an argument.

- Members are written DERIVED FIRST, THEN BASE (a `ShapeObject` reads `shid, cid, ... , id, pid, name`).
- Reading is order-independent and skips unknown names, so a hand-edited or foreign file works and an
  additive member is free. `ReadJson` is emitted on the ROOT of a hierarchy only (a writer must know which
  members to write; a reader loops over the document and hands each name to the overridden
  `ReadJsonMember`), public because the read loop lives in `JsonModels`, virtual only where something can
  override it (a virtual member on a sealed type does not compile).
- **A null sub-model is omitted whole, name included**, and only a sub-model is: null says "never touched"
  (`ColorCurvesKey`'s eight curves), and `"crvmstr":null` x8 per keyframe cost more than the sentinel it
  replaced. Absent reads back as the constructor's value, so a nullable member must be constructed null.
  Collections are NOT skipped: an empty list and a missing one are different states the blob keeps apart.
- A polymorphic value is a two-element array, `[tag, payload]` (`"clr":[0,{...}]`), the same tag as the
  blob. A CONCRETE member of a value family is still tagged (`ConverterRouter` resolves by runtime type and
  a family converter matches every implementor; fifteen members are like this, the corpus found them). The
  reader accepts the tag as a NAME too. The fallback for a bad tag is the family's LOWEST tag, its plain
  literal form, which the reflective converters also take, so both stacks agree on a degraded read.
- A nested domain is wrapped by whoever HOLDS it, and both the domain and the generation go across: the
  generation says a file disagrees, only the domain says what to resolve it against. A known generation
  migrates through the snapshot's own codec; an unknown one is read by name into today's class and reported.
- Reset/Equals tests: `ModelGeneratorTests` assert compilation of original plus generated sources first
  (the strongest check), then shape. `List<int>` goes through `CopyValueList`, not the copy constructor: a
  value list is the one shape a member may be null in and `new List<T>(null)` throws.

## Modification table generator

- Replaces a reflective table (~200 types walked at static init, dotted-path parsing per apply, an
  unresolved lookup that returned silently with the warning commented out; three of 28 live paths never
  resolved). The apply is a flat, reflection-free switch: a type test at most, one conversion, a direct
  assignment, no boxing. It keys on the MEMBER, so inheritance is free (the attribute on `RectObject.Layer`
  is reached by shape, text and effect objects through one number) and the generated `Apply` takes the root
  type, casting down only where the declaring type is not the root. `RectObject` is the only target type by
  design of the override system (a placement overrides objects inside a template, all `RectObject`s).
- **The number is written by hand, so every wrong way of writing it is an ERROR**: declaration order would
  renumber on a reorder, a hash of the JSON key re-couples the id to the spelling, a hash of the C# name
  makes a rename the breaking change. Collisions, unserialized members and stray bands are refused; a
  duplicate id would give one override two fields.
- `PropertyOf` is the one reflective exception: the rule-checked write needs the `PropertyInfo` to read
  `[RuleXxx]` attributes, with no compile-time form. Built lazily from literal `typeof`/`nameof` pairs so
  IL2CPP stripping cannot remove the members; `Apply` never touches it.
- An index addresses ONE element and replaces it; no index replaces the track. An index past the end is
  refused rather than growing the list: an override diverges from a template that HAS that element.
- All diagnostics are reported from the collected output: a collision is only visible across members, and
  splitting reporting would name one problem this build and the other the next.
- **A `[GenerateModelIgnore]` member is addressable**: what an override must save is the MODIFICATION, so a
  derived property (a view onto a saved member; `PrefabObject.PlacementDuration` is the first, a view onto
  Span's duration) qualifies, and the ignore attribute marks it as derived. The setter check does not
  relax. The check is not a second encodability check (BHS1003 is the model generator's).

## Validation generator

- **Why**: `RuleAnalyzer` walked by reflection: per node three dictionary lookups and one
  `PropertyInfo.GetValue` per property, boxing every value type. Measured on the corpus, 1.6 s of the 3.8 s
  to open volcano in the Editor (three passes, 2026-09-03), none of it rule evaluation. A compiled-accessor
  cache is not an option: `Reflection.Emit` is unavailable on IL2CPP and `Expression.Compile` degrades to
  interpretation under AOT, fast in the Editor and worthless where slow validation is felt. Expect 3-6x,
  not the 49x of the blob codec (there reflection was the whole cost); the virtual `rule.IsValid` and
  boxing at that boundary remain.
- It does NOT generate the rules: `RuleTable` builds the `PropertyInfo` and attribute arrays by the same
  reflection, once per type, so `RulePath.Property` and `RuleIssue.Rule` stay the same objects and rules
  keep reflection's order (which decides WHICH finding is reported when a property has several).
- **The emitted walk is the reflective one unrolled, statement for statement** (same findings, same order,
  same `RulePath` traces on a corpus level producing ~a hundred thousand). It is FLAT, not chained:
  `GetProperties` is derived-first so a base-first chain inverts the report, and the two phases each finish
  before the next begins. **Every property's own rules run first; only then does anything descend.** Fusing
  them reorders the report for any object with more than one walkable property (every `RectObject`).
- **The ordinal contract**: the names are what the generator believed the property list to be; `RuleTable`
  re-derives it by reflection and throws on disagreement. A stale `BH.SDK.Roslyn.dll` is the likeliest cause
  and the message says so. The factory must produce exactly
  `GetProperties(Public | Instance).Where(CanRead && CanWrite)`, derived-first, declaration order per
  level, an override once at the derived position. A private SETTER does not change membership; a
  non-public GETTER does, so it is refused.
- **Retired refusals**: "a `[RuleContainer]` must be partial" (BHS1101) and "`IValidatable` is the
  generator's" (BHS1107) were withdrawn: both describe LEGAL states. A non-partial container keeps the
  reflective walk (`RuleWalk.Node`'s fallback), and a hand-implemented `IValidatable` already answers the
  question. They errored on dozens of private nested fixtures in `Tests/Rules`, the fallback's only
  coverage. 1101 and 1107 are never reissued. The claim they protected moved to
  `RuleContainerCoverageTests`: every `[RuleContainer]` IN THE SDK ASSEMBLY has a generated walk. None of
  the live refusals is violated today (zero new-hidden properties, non-public accessors, indexers, or
  unreachable rules across all 200 containers); they keep it so.
- `[RuleContainer]` is inherited but `ForAttributeWithMetadataName` matches declared attributes only: a type
  that is a container purely by inheritance is invisible and keeps the reflective walk. Measured, two SDK
  types were in that state; the coverage test keeps it at zero now both declare it.
- The generator names exactly one model type, `Prefab` (the only object scope with a timeline of its own,
  since a template's length is its root's span). `RuleContext.WithScope` takes that concrete type, so the
  emitted call would not compile for anything else, and a member-less marker interface no runtime code
  reads is worse than one name in one generator.
- A declared type that cannot be a collection at runtime pins the shape (sealed, not a collection);
  anything else (object, interface, open base) keeps the runtime dispatch, or it gets one `RulePath` where
  the reflective walk pushes N indexed ones.
- `RuleContainerAnalyzer` allows abstract containers: `[RuleContainer]` is inherited, so a base class is how
  a family opts in (`Resource`, `BaseGraphicsSettings`). The `Activator` calls construct the property's own
  concrete type, never the container.

## Tests (`Roslyn/Tests~/`)

A generator is a pure function from a compilation to sources, so the harness builds a compilation from
strings. It stubs the model API instead of referencing `BH.SDK`: a mass refactor leaves the library
uncompilable for long stretches, exactly when a broken generator must be visible. The cost is drift,
paid on the other side by `BH.SDK.Tests` exercising the generator against the real types. The smoke test
points at `SandboxProbeGenerator` on purpose, so a failure is the rig and never the component.

Validation walks are proven elsewhere (`BH.SDK.Tests` and `Core.Tests` run a real 19 341-object level
through both walks and compare ~a hundred thousand findings); this suite is mostly refusals, because
every one describes a construct nobody has written yet. The validation API stub carries only what the
generator NAMES; what the walk does is asserted on the real corpus. The dispatcher refusal is the one that
is not "emit nothing": both types are still emitted, only the dispatcher between them cannot be built.
`SandboxProbe*` exist to prove Unity loaded the assembly as an analyzer (`BHS0001`) and that a generator
saw the real compilation.

## Editor tooling (`UnityExtensions/Editor/`)

- **`RoslynBuilder`** shells out to MSBuild so the artifact is identical to `dotnet build` on a machine
  with no Unity. By hand: `cd <sdk>/Roslyn; dotnet build BH.SDK.Roslyn.csproj -c Release; cp
  bin~/Release/BH.SDK.Roslyn.dll ../BH.SDK.Roslyn.dll`. The copy is not optional (`bin~` is invisible to
  Unity). It uses Unity's own `dotnet` (`Editor/Data/DotNetSdk`), so nothing is installed and the SDK
  version follows the Editor. Unity recompiles the whole project afterwards, which is inherent: the
  analyzer is an input to every assembly it is scoped to.
- **`RoslynCompilerReferences`** installs the Roslyn assemblies from the Editor's own installation instead
  of committing them: `BH.SDK.Roslyn.Src.asmdef` needs them as plugin assets under `Assets/`, and
  committing 13 MB of binaries would pin a Roslyn version to whichever Editor is open. It runs on load
  (a fresh clone has no `Refs/`); the check is five `File.Exists` calls.
- **`RoslynImportSettings`** is the mechanism, not decoration. `Refs/` is gitignored (its metas are
  recreated from defaults on every machine) and the analyzer is overwritten by every build; a default meta
  means "Any Platform, auto-referenced", which pushes Roslyn into every assembly and player build and makes
  Unity link the analyzer as an ordinary plugin instead of handing it to the compiler. `Refs/*.dll` are
  Editor-only and explicitly referenced (visible only to the asmdef naming them in
  `precompiledReferences`); `BH.SDK.Roslyn.dll` has no platform plus the `RoslynAnalyzer` label. The tool
  that just wrote a file calls it EXPLICITLY and the postprocessor only mirrors: `OnPreprocessAsset` is
  never called for a `.dll` (`PluginImporter` has no matching hook) and an `OnPostprocessAllAssets` pass
  fires on an import Unity may skip when only content changed. Every setting is compared before it is
  written, which is what makes `SaveAndReimport` safe (an agreeing file triggers no write, so it stops).
- **`RoslynLayout`** asks for every path instead of writing it down: the consuming project mounts the SDK
  wherever it likes, so the one fixed point is this assembly's own asmdef path (the SDK root is two
  folders above it). Deliberately not generic: one project, one artifact, one reference folder. The five
  compiler references are Roslyn plus `ImmutableArray` (three) and two more because the Editor's
  `System.Collections.Immutable` spells its members with `Span`; without them the build fails with CS0012
  naming a type nobody wrote.
