using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Statistics;
using BH.SDK.Serialization.Converters;
using BH.SDK.Serialization.Converters.Base;
using BH.SDK.Serialization.Converters.CustomTypes;
using BH.SDK.Serialization.Converters.Dict;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BH.SDK.Serialization
{
    /// <summary> The one place the whole converter stack is assembled: which converters exist, in which order
    /// they are routed, and which serializer each format is driven by. Everything that reads or writes a
    /// document in this project goes through an instance of this. </summary>
    public class SerializationService
    {
        /// <summary> The serializer every format shares - only the reader/writer over the bytes differs. </summary>
        public readonly JsonSerializer Serializer;

        private readonly Dictionary<SerializationType, IDataSerializer> _dataSerializers = new();

        /// <summary> The serializer for one format, built on first use and kept for the rest of the session. </summary>
        public IDataSerializer GetDataSerializer(SerializationType type)
        {
            if (_dataSerializers.TryGetValue(type, out var dataSerializer)) return dataSerializer;

            dataSerializer = type switch
            {
                SerializationType.Json => new JsonDataSerializer(Serializer),
                // The one implementation that shares nothing below the interface: its payload is
                // written by generated code on the models themselves, so it needs no JsonSerializer,
                // no converters and no contract resolver.
                SerializationType.Blob => new BlobDataSerializer(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
            _dataSerializers[type] = dataSerializer;
            return dataSerializer;
        }

        /// <summary> The settings the game itself runs with. </summary>
        public SerializationService() : this(new SerializationSettings())
        {
        }

        /// <summary> The reflective path, with no generated codecs - what every level on disk was
        /// written by. Its ONE caller is the parity test that compares the same bytes through both
        /// paths; it is a method rather than a settings flag at the call site because SerializationSettings'
        /// own fields are Newtonsoft types, and Core.Tests does not reference Newtonsoft. </summary>
        public static SerializationService CreateWithoutGeneratedCodecs()
            => new(new SerializationSettings { useGeneratedCodecs = false });

        /// <summary> Built against settings, which is how the one test that needs the reflective path gets it. </summary>
        public SerializationService(SerializationSettings serializationSettings)
        {
            var contractResolver = new ContractResolver(serializationSettings);

            var settingsDefault = new JsonSerializerSettings
            {
                TypeNameHandling = serializationSettings.typeNameHandling,
                ContractResolver = contractResolver,
                // Without Replace, Newtonsoft appends into a non-null nested list left by a
                // parameterless constructor (e.g. a two-key default curve), breaking round-trip equality.
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            };

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = serializationSettings.typeNameHandling,
                ContractResolver = contractResolver,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Converters = GetConverters(settingsDefault, serializationSettings.useGeneratedCodecs),
            };
            Serializer = JsonSerializer.Create(settings);
        }

        // The list is handed to a ConverterRouter (Newtonsoft asks every converter per value, uncached);
        // order still decides the winner. VersionedEnvelopeConverter is NOT routed: its CanConvert
        // depends on the domain being written, which a per-type cache cannot express.

        /// <summary> Builds the converter set a serializer needs, already routed. </summary>
        public static List<JsonConverter> GetConverters(JsonSerializerSettings settingsDefault,
            bool useGeneratedCodecs = true)
        {
            var versionedEnvelope = new VersionedEnvelopeConverter();
            var converters = new List<JsonConverter>
            {
                new VersionConverter(),

                new DictionaryObjectsConverter(),
                new DictionaryAudiosConverter(),

                new DictionaryTextureResourcesConverter(),
                new DictionaryFontResourcesConverter(),
                new DictionaryAudioResourcesConverter(),
                new DictionaryCompositeShapeResourcesConverter(),
                new DictionaryThemesConverter(),
                new DictionaryEffectsConverter(),
                new DictionaryPrefabsConverter(),
                new DictionaryModificationsConverter(),
                new DictionaryCachedFontTextsConverter(),
                new DictionaryCheckpointDeathsConverter(),
                new DictionaryAsPairListConverter<ObjectId, ObjectId>(),

                // The pair form: BestRun carries none of the numbers RunProfile is made of, and
                // RunProfile is a value type with no TypeConverter, so the default dictionary path throws.
                new DictionaryAsPairListConverter<RunProfile, BestRun>(),

                new PrimitiveIntConverter(),
                new PrimitiveGuidConverter(),
                new PrimitiveFloatConverter(),
                new FrameSpanConverter(),

                new IntConverter(),
                new FloatConverter(),
                new StringConverter(),
                new LicenseConverter(),
                new ColorConverter(),
                new Color3Converter(),
                new Vector2Converter(),
                new Vector3Converter(),
                new Vector4Converter(),

                new EffectShapeConverter(),
                new EffectAngleConverter(),
                new EffectScaleConverter(),
                new EffectColorConverter(),
                new EffectShapeSpreadConverter(),

                new ScreenLimitConverter(),
                new Color4X4KeyConverter(),
                new FontSizeKeyConverter(),
                new ObjectConverter(),
            };

            // LAST, and that is the whole of its wiring (GeneratedModelConverter): first match wins, so
            // tagged values still reach the converter that tags them.
            if (useGeneratedCodecs) converters.Add(new GeneratedModelConverter());

            // Each IRequiresDefaultSerializer gets its own router: each excludes a different converter
            // (itself), and a router caches answers for the exact set it was built with.
            foreach (var converter in converters.OfType<IRequiresDefaultSerializer>())
            {
                var excluded = new HashSet<JsonConverter>(converter.GetExcludedConverters(converters));
                var included = new List<JsonConverter>(converters.Count);
                foreach (var other in converters)
                {
                    if (!excluded.Contains(other))
                        included.Add(other);
                }

                var defaultSerializer = JsonSerializer.CreateDefault(settingsDefault);
                defaultSerializer.Converters.Add(versionedEnvelope);
                defaultSerializer.Converters.Add(new ConverterRouter(included));
                converter.SetDefaultSerializer(defaultSerializer);
            }

            return new List<JsonConverter> { versionedEnvelope, new ConverterRouter(converters) };
        }

        /// <summary> Applies the project's own member rules on top of Newtonsoft's: opt-in serialization and
        /// the property order the format was written in. </summary>
        public class ContractResolver : DefaultContractResolver
        {
            private readonly SerializationSettings _serializationSettings;

            /// <summary> Takes the settings whose member rules it applies. </summary>
            public ContractResolver(SerializationSettings serializationSettings)
            {
                _serializationSettings = serializationSettings;
            }

            /// <summary> Applies the opt-in member rule, so a member reaches a file only by carrying a JSON name. </summary>
            protected override JsonObjectContract CreateObjectContract(Type objectType)
            {
                var contract = base.CreateObjectContract(objectType);

                contract.MemberSerialization = _serializationSettings.memberSerialization;

                return contract;
            }

            // Agree byte for byte with the generated path (JsonParityTests): omit a null SUB-MODEL whole
            // (the member's type is an IModel<T>) and nothing else, so a null collection stays distinct
            // from an empty one, as in the blob codec.

            /// <summary> Applies the property order the format was written in, which is what keeps a re-saved file byte-identical. </summary>
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (property.PropertyType != null && IsModel(property.PropertyType))
                    property.NullValueHandling = NullValueHandling.Ignore;

                return property;
            }

            private static bool IsModel(Type type)
            {
                foreach (var contract in type.GetInterfaces())
                    if (contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IModel<>))
                        return true;
                return false;
            }
        }

        // The mode reaches the writer, not the shared serializer. Plain-text entry point: use
        // GetDataSerializer for the binary one.

        /// <summary> The TEXT api: always compact JSON, whatever a caller's chosen file format is.
        /// Which FORMAT a level is written in is SerializeEnvelope's question, since only bytes can
        /// answer it - a blob has no text form, and there is no longer a second JSON shape. </summary>
        public string SerializeData<TValue>(TValue value)
        {
            if (value == null) return string.Empty;

            if (!VersionedTypeRegistry.CanConvert(value.GetType()))
            {
                throw new ArgumentException(CantConvertMessage<TValue>(nameof(SerializeData)), typeof(TValue).Name);
            }

            using var stringWriter = new StringWriter();
            using (var textWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.None })
                Serializer.Serialize(textWriter, value);

            var json = stringWriter.ToString();
            return json;
        }

        /// <summary> Reads one envelope out of a JSON string, migrations included. </summary>
        public TValue DeserializeData<TValue>(string json)
        {
            if (!VersionedTypeRegistry.CanConvert(typeof(TValue)))
            {
                throw new ArgumentException(CantConvertMessage<TValue>(nameof(DeserializeData)), typeof(TValue).Name);
            }

            using var stringReader = new StringReader(json);
            using var jsonTextReader = new JsonTextReader(stringReader);

            return (TValue)Serializer.Deserialize(jsonTextReader, typeof(TValue));
        }

        // The byte-level counterparts of SerializeData/DeserializeData, for anything that moves a whole
        // aggregate around (archive entries, a server column). Domain and generation come off the
        // type's own [ModelGeneration], never the caller: an envelope must not be able to lie.

        /// <summary> Serializes a versioned aggregate into an envelope's bytes. </summary>
        public byte[] SerializeEnvelope<TValue>(TValue value, SerializationType type)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var attribute = value.GetType().GetCustomAttribute<ModelGenerationAttribute>();
            if (attribute == null)
                throw new ArgumentException(CantConvertMessage<TValue>(nameof(SerializeEnvelope)), typeof(TValue).Name);

            return GetDataSerializer(type)
                .SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Generation, value));
        }

        /// <summary> Reads a versioned aggregate back out of an envelope's bytes, migrating it to
        /// the domain's current shape on the way. </summary>
        public TValue DeserializeEnvelope<TValue>(byte[] bytes, SerializationType type)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));

            if (!VersionedTypeRegistry.CanConvert(typeof(TValue)))
                throw new ArgumentException(CantConvertMessage<TValue>(nameof(DeserializeEnvelope)),
                    typeof(TValue).Name);

            return GetDataSerializer(type).DeserializeEnvelope(bytes, typeof(TValue)).GetPayload<TValue>();
        }

        private static string CantConvertMessage<TValue>(string methodName)
            => $"Type '{typeof(TValue)}' has no [ModelGeneration] attribute and cannot be used with {methodName}";
    }
}