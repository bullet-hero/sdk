# CLAUDE.md — Assets/Plugins/BulletHeroSDK/Models/Clipboard

Read `Assets/Plugins/BulletHeroSDK/Models/CLAUDE.md` first — it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.


## Clipboard (`Models/Clipboard/`)

`ClipboardData` (`[ModelGeneration(ModelDomains.ClipboardData, ModelGenerations.Release)]`) is one copied editor selection,
split into **one section per editor timeline** — the consumer keeps a single instance as the backing
store of all its per-timeline buffers, and the same instance is what leaves the process as JSON when
the author exports it. It is a **partial level**: every section is a collection type the format
already owns (`Dictionary<ObjectId, RectObject>` ×3, `Dictionary<AudioId, LevelTrack>` ×2, the four
`GameLevel` event aggregates), so it rides on the existing converters and introduces no new
polymorphism of its own.

Two things about its shape are decisions rather than accidents:
- **`Objects` and `KeyObjects` hold the same value type and are separate sections.** The first means
  "create these objects", the second "add these keyframes to an object that already exists" — a
  copied keyframe travels inside a *stripped copy of its owner* because which track a keyframe
  belongs to has no representation in the format other than which property of which object its list
  hangs off. One dictionary cannot express both intents.
- **No anchor frame is stored.** Where a paste lands is derived from the section's own contents at
  paste time; a stored anchor is one more thing that can disagree with what it describes after a
  partial edit or a clear.

`ClipboardContent` (`Models/Enums/`, `[Flags] byte`) records which sections carry something and is
validated by `RuleEnumFlagsValid`, not `RuleEnumValid` (see Rules below). Covered by
`Tests/ClipboardDataTests` — a round trip is the feature here, not a nicety.
