# The model contract: `IModel<T>`, generated

Split out of `Models/CLAUDE.md`, which keeps the summary and links here. How the generator writes it
is `ROSLYN_GENERATORS.md`, "Model generator"; the helpers it calls are `UTILS_DESIGN.md`, "Model copying
and merging (`ModelUtils`)".

## What a model is

`IModel<T> : ICopyable<T>, IEquatable<T>, IResetable, IUpdatable<T>, IMoveable<T>` - every live
domain model implements: `Copy()` (new instance), `Equals(T)`/`Equals(object)`/`GetHashCode()`,
`Reset()` (back to defaults, in place), and the two become-another-instance operations,
`Update(src)` and `Pull(src)`.

**ALL OF IT IS GENERATED, AND SO ARE BOTH CODECS.** `BH.SDK.Roslyn`'s `ModelGenerator` writes every
one of those bodies for every type carrying `[GenerateModel]`, plus that type's `.blob` codec
(`IBinaryModel`) and its JSON codec (`IJsonModel`); the model file itself holds only its members, its
constructors and whatever is genuinely its own. Hand-written per class, it was 208 `Copy` bodies, 279
typed `Equals`, 206 `Update`, 206 `Pull`, and the failure mode was never a compile error but a member
quietly missing from one of them. `Equals(object obj) => obj is T value && Equals(value)` was the
single easiest place in this codebase to introduce a silent bug, because pasting it from a sibling
class compiles and only makes the boxed comparison always false. That whole class of defect is gone
by construction.

**A new model is `[GenerateModel] public sealed partial class`, and nothing else.** Write the members,
the constructors and the `[JsonProperty]` names; the whole `IModel<T>` contract and the blob codec are
written for you. Leave it non-sealed only if something derives from it - `sealed` is the ONLY signal
the generator has for "can anything override this", so a base that forgets it gets a pointless
virtual on every member, and a leaf that forgets it gets the same.

**Three models are still hand-written, each for its own reason.** `FrameSpan` and `ModificationKey`
are structs: C# 9 gives a struct no parameterless constructor body, so "back to what the constructor
built" means nothing for them and `Update` is `this = src`. `Modification` holds an untyped `object`
whose copy is not an assignment (a whole-track override stores a `List<TKeyframe>`, which would
alias) and whose equality is not `object.Equals` (a `List` has no value equality). All three are
simply not marked `[GenerateModel]`, which is what the attribute being opt-in is for.

**What the generator will NOT do is fail quietly.** A member it has no encoding for is an ERROR naming
the member (`BHS1003`), never a member skipped; so is a type that forgot `partial`, one with no
parameterless constructor, and one whose base is a model the generator does not also own. The escape
valve is explicit: `[GenerateModelIgnore]` on the property, and the partial hooks (`OnReset`,
`OnCopied`, `OnUpdated`, `OnPulled`, `OnWriteBlob`, `OnReadBlob`) to handle it by hand.

## `Update` and `Pull`

**`[GenerateModelMerge]` is the one thing about `Pull` a type cannot say for itself.** Nine
dictionaries are merged key by key instead of being replaced - the three object scopes (`GameLevel`,
`Prefab`, `ClipboardData`) and the two statistics maps - and nothing in the TYPE of a
`Dictionary<ObjectId, RectObject>` distinguishes them from the seven that are replaced wholesale. The
dispatcher that merges one polymorphic value is generated too (`RectObjectModelPull.PullValue`), which
is what removed `LevelUtils.PullObject` - a hand-kept switch whose own header called out that a new
`RectObject` subtype had to remember to extend it.

**`Update` and `Pull` are what a model offers when its reference is already held somewhere else** -
`GameData` hands `UserSettings` out live, the editor's operation buffer holds an object across an
undo, and for both of them replacing the reference is the one thing that must not happen. They differ
in exactly one way, and it is not which fields they write: **how deep the identity promise reaches**.
`Update` replaces every nested model with a fresh copy, so only the receiver itself survives; `Pull`
writes into the nested instances it already has, so a reference anyone holds anywhere in that subtree
stays live - which is why the `UserSettings` groups are pulled, being handed out one sub-group at a
time. On a model carrying no nested model of its own the two bodies coincide; that is the contract
agreeing rather than duplication to collapse, and a nested model added later changes `Pull` alone.

**Collections are replaced wholesale by both, except the keyed ones something holds into**: an
element is normally addressed by index or key rather than by reference, so keeping the instance buys
nothing. An **object scope** is where the reference *is* the address - the editor's selection, its
operation buffer and every materialized prefab child hold `RectObject`s - and a **clipboard's**
sections are held one per timeline, so `GameLevel`, `Prefab` and `ClipboardData` merge theirs key by
key with `ModelUtils.PullDictionary`: keys the source no longer has are dropped, a key whose concrete
type changed is replaced by a copy, everything else is pulled into the instance already there, and
the dictionary object itself is never swapped. What merges one **value** is the caller's choice - the
generated `RectObjectModelPull.PullValue` for a `RectObject`, the constrained `PullDictionary`
overload (i.e. `PullFrom`) for a concretely typed value like `LevelTrack`. `PullValue` dispatches to
the concrete type because pulling through a `RectObject` reference would write the base half and drop
the subclass's own; its cases are whatever `[GenerateModel]` types derive from the base, so a new
`RectObject` subtype extends `ObjectConverter.GetType`'s switch by hand and this one by itself.
`AudioLevel.Tracks` and the seven `LevelResources` dictionaries are still replaced; nothing is known
to hold into them across a pull, and they take the same two lines when something does.

A **polymorphic** field (`IVector2`, `IFloat`, `IEffectShape` …) cannot always keep its instance - a
`Vector2Value` cannot become a `RandomVector2` - so it goes through `ModelUtils.PullFrom`, which pulls
in place while the concrete types match and copies when they do not. Each concrete type also
implements the *interface's* own `Update`/`Pull` explicitly, and those do **nothing** when handed a
sibling implementation; `PullFrom` is the correct path, the no-op is what stops a wrong call writing
one variant's fields out of another's.

Neither is **virtual**, and `Copy()` being virtual is the contrast worth noticing: a subclass adds an
*overload* (`Update(ShapeObject)` beside the inherited `Update(RectObject)`), so `Update` called
through the base type writes the base half and leaves the rest - exactly what `Equals(RectObject)`
already does. Address a model by its own type and it is total. `Tests/ModelHierarchyTests` pins both
halves.

## Snapshots are models too

**Frozen historical snapshot classes under `Versions/V<n>/` are generated models too** - a migrating
reader reads one with a codec rather than with reflection, so a one-shot deserialization target that
skipped the contract would be unreadable. They carry `[GenerateModel]`, declare `IModel<T>`, construct
their members, and `ModelContractTests` sweeps them with everything else.
