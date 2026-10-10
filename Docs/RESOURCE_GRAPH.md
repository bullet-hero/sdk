# Resource graph: references, closure, remap, naming

`Utils/ResourceGraph` lists every place the format points at a resource, once. Three questions walk the
same edges, so a reference one learns about is one all three see:

- **What still uses this** - `ResourceReferenceUtils` (the delete confirmation).
- **What does this need** - `ResourceClosure` (a collection export or import).
- **Point these somewhere else** - `ResourceRemap` (an import into a level whose ids are taken).

Three hand-written walks would drift exactly where it hurts: a field one forgot is a dependency left
behind, or a reference that survives an import still pointing at the old id.

## Walk contract

- One method per owner; each edge is a read AND a write: the callback returns the reference to store, the
  walk writes it back only when it changed, a reader just returns what it was given.
- A null id is never visited (it is how an object says it draws no texture, collides with no shape,
  points at no template) and never counted - counting it would report every plain object as a reference
  to whatever is about to be deleted. A game-defined id IS visited like any other: whether it names
  something the level owns is the caller's question, because an id's value cannot say which tier it is in.
- **Modifications are optional.** A placement's id overrides are real references (a closure skipping them
  ships a prefab whose placement swaps in a shape nobody packed), but in a live level they are already
  applied to the materialized copies, so a COUNT walking both would report every overridden reference
  twice. Count and usage walks leave them out; the closure and remap include them.
- A `ThemeRef` colour is not a reference to one theme: it stores a slot INDEX into whichever theme is
  active at that frame, so only the theme track names a `ThemeId`.

## Usage count (`ResourceReferenceUtils`)

- It answers the OPPOSITE direction to the validator (`LevelGraphAnalyzer.UnresolvedReference` finds a
  reference with no resource, the state a delete CREATES, discovered on the next load). Removing a texture
  eight objects draw with used to be silent.
- A COUNT rather than a list: the question is "is this safe to delete, how much is at stake"; a list of
  ids is unreadable in a confirmation and would need names this layer must not know.
- Every scope is walked, not only `Level.Game`: a texture used only by a prefab is used. `Prefab.Root` is
  walked too and is the one object no dictionary holds (a field) - the same trap `PrefabSearchEntries`
  and `LayerMath`'s lane sweep had to be taught.

## Closure (`ResourceClosure`)

A prefab picked out of a level or collection is useless without the custom shapes, textures, fonts,
effects and nested templates it uses. The closure is transitive over the graph's edges; only prefabs and
effects have outgoing edges, so it is shallow except through nested templates. Only what the SOURCE OWNS is
collected: a reference with no entry is either game-defined (every build ships it) or already dangling
(nothing to carry); validation reports the dangling kind as always.

## Remap, naming, `ResourceRef`

- File resources (texture, font, audio) are addressed by level-local ints, so two sources both have a `-1`
  and an import renumbers the incoming one. A guid resource travels as-is, except when the author answers
  a conflict with "import as a copy", which mints a fresh guid for the same reason. `ResourceGraph` says
  where the references are; the remap only decides what they become.
- `ResourceNaming` holds the two things every move of a file resource invents: a fresh level-local id and a
  file name nothing in the destination uses. They were private to the editor's `LevelResourcesService`;
  an import planner in the SDK (and one day on a server) needs the same answers.
- `ResourceRef` is one address for seven id types (level-local ints for file resources, guids for data
  resources), reusing `ResourceType` rather than mirroring it in a second enum. The cover
  (`ResourceType.LevelLogo`) fills neither slot and is never null: its type is its address, and nothing
  that walks resources ever produces it.
