# CLAUDE.md - Assets/Plugins/BH.SDK/Serialization

Read `Assets/Plugins/BH.SDK/CLAUDE.md` first - it carries the mental model, the folder index and the
layer-wide conventions. This file is folder-local.

`SerializationService` (the JSON/`.blob` entry point, at the folder root), `Serializers/`
(`IDataSerializer`s, `SerializationType`), `Json/`, `Blob/`, `Converters/Base/` +
`Converters/CustomTypes/` + `Converters/Dict/` (the polymorphism/id/dictionary JsonConverters).

## Docs

| Question | File |
|---|---|
| why the codecs, converters, envelopes and retired enum numbers are shaped as they are; every measurement | `Docs/SERIALIZATION.md` |
| generations, snapshots, migrations, degrade vs refuse | `Docs/VERSIONING.md` (design) and `Versions/README.md` (folder convention) |

## Traps

- **Anything that changes how a level is READ is locked by a parity test** comparing the same bytes
  through both JSON paths (`JsonParityTests`, `SerializationService.CreateWithoutGeneratedCodecs()`,
  `Core.Tests`' `CorpusLoadCostTests`) - the format may not change by one byte. Reports are compared
  as well as models.
- **Nothing on the read path materializes a `JToken` tree** - a rule, not an implementation detail.
- **`GeneratedModelConverter` goes LAST** in the converter list; the router takes the first match.
- **`ObjectCreationHandling = Replace` is load-bearing**; `settingsDefault` is the bare escape hatch
  every `IRequiresDefaultSerializer` converter needs.
- **`SerializationType` numbers 1 (`Bson`) and 2 (`JsonPretty`) are retired, never reissued**; never
  write a two-branch ternary over the enum (`Core/Utils/SerializationModeUtils` exists for that).
- **The envelope key is `Names.Generation` (`"g"`), never `Names.Version` (`"vrs"`)**, which is the
  author's version of a level.
- **`ModelGenerations.Invalid` is `-1`; zero is a real generation** (`Versions/V0`) - readers ask
  "did we read one" with a flag.
- **A newer generation is refused (`NewerGenerationException`), an older one migrates or degrades -
  and every degrade is reported** through `SerializationReport`, never silent defaults.
- **A blob count the file chose is refused before it is believed** (`BlobReader.ReadCount`);
  `BlobPrimitives` holds the five structs the generator cannot write.
- **Editing `Roslyn/` changes nothing until the `.dll` is rebuilt**
  (`Tools/BH.SDK.Roslyn/Build Analyzer`, the `roslyn-rebuild` skill;
  `Assets/Plugins/BH.SDK/Roslyn/README.md` is the record).
- `CompatibilityService`/`SaveData<T>`/`JsonConverterData<T>` are gone - never reintroduce them.
