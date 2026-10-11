# CLAUDE.md - Assets/Plugins/BH.SDK/Models/Values

Read `Assets/Plugins/BH.SDK/Models/CLAUDE.md` first - it carries the folder index,
the `IModel<T>` contract and the cross-folder effect/audio/theme model.

Every authorable value field goes through one of the interfaces in `Models/Interfaces/Values/`
(`IFloat`/`IInt`, `IVector2/3/4`, `IColor3`/`IColor4`, `IString`, `IScreenLimit`, `ILicense`), each
with a discriminator method returning a `*Type` enum. The table of variants, the converters and the
dictionary encodings: `Docs/VALUES.md`.

## Traps

- **A value writes as `[typeEnum, payload]`**, through its own `JsonConverterCustomType<T, TType>`
  subclass - never `$type`/`TypeNameHandling`. A new variant = enum case + class + a case in that
  converter's switch.
- **Converter ORDER in `GetConverters` decides who handles a type** - the `ConverterRouter` caches the
  first match. `VersionedEnvelopeConverter` cannot be routed and sits beside it.
- **`IRequiresDefaultSerializer` lives in `Serialization/Converters/Base/`**, not `Models/Interfaces`.
- **A dictionary whose value carries its key writes as a plain array** (`DictionaryAsListConverter`);
  an id-to-id table uses `DictionaryAsPairListConverter`.
- **`*Lerp` variants are SEGMENTS with no `RulePropertyOrder`** - ordering the pair would make a
  falling line unrepresentable.
- **`CustomLicense` carries no permission flags and its body is uncapped** on purpose.
