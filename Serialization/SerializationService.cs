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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BH.SDK.Serialization
{
    public class SerializationService
    {
        public readonly JsonSerializer Serializer;

        private readonly Dictionary<SerializationType, IDataSerializer> _dataSerializers = new();

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

        public SerializationService() : this(new SerializationSettings())
        {
        }

        /// <summary> The reflective path, with no generated codecs - what every level on disk was
        /// written by. Its ONE caller is the parity test that compares the same bytes through both
        /// paths; it is a method rather than a settings flag at the call site because SerializationSettings'
        /// own fields are Newtonsoft types, and Core.Tests does not reference Newtonsoft. </summary>
        public static SerializationService CreateWithoutGeneratedCodecs()
            => new(new SerializationSettings { useGeneratedCodecs = false });

        public SerializationService(SerializationSettings serializationSettings)
        {
            var contractResolver = new ContractResolver(serializationSettings);

            var settingsDefault = new JsonSerializerSettings
            {
                TypeNameHandling = serializationSettings.typeNameHandling,
                ContractResolver = contractResolver,
                // Without this, Newtonsoft populates (appends into) a non-null nested object/list
                // property left behind by the parameterless constructor instead of replacing it -
                // e.g. EffectAngleCurvesBySpeed's ctor seeds Curve with 2 default keyframes, and
                // deserializing a JSON curve with its own keyframes would append onto those instead
                // of starting fresh, breaking round-trip equality for any model with a non-empty
                // constructor default.
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

        // The list below is not what a serializer ends up holding: it is handed to a ConverterRouter,
        // and the serializer holds that one plus VersionedEnvelopeConverter. Newtonsoft asks every
        // converter in a serializer's list whether it can convert, once per value and cached nowhere,
        // so a list this long is paid for on every number in the file - see ConverterRouter's header
        // for what that measured. Order still decides which converter wins a type, and the router
        // preserves it exactly, so adding a converter here works the same as it always did.
        //
        // VersionedEnvelopeConverter is the one that cannot be routed: its CanConvert answers
        // differently depending on which domain is currently being written, which is what stops it
        // re-wrapping its own payload, and a per-type cache cannot express that.

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

                // The pair form rather than the key-from-value one above, because BestRun carries
                // none of the four numbers RunProfile is made of - a record is filed under the
                // conditions it was set under, and repeating them inside the record would be a
                // second copy with nothing keeping the two in agreement. RunProfile is also a
                // value type with no TypeConverter, so Newtonsoft's own dictionary path throws.
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

            // LAST, AND THAT IS THE WHOLE OF ITS WIRING - see its own header. First match wins, so
            // every tagged value still reaches the converter that tags it, and this one answers for
            // the payload inside. It is what makes reading a level stop binding members by
            // reflection; the thirty-five converters above stay for the shapes it hands back to.
            if (useGeneratedCodecs) converters.Add(new GeneratedModelConverter());

            // Some converters above resolve a concrete implementation of a polymorphic type and need a
            // private "default" JsonSerializer to populate that concrete type's own members (see
            // IRequiresDefaultSerializer for why). Wired automatically here, so adding a new converter of
            // that kind to the list above is the only step a future change needs - nothing here has to change.
            // Each gets its own router, since each excludes a different converter (itself) and a router
            // caches the answers for the exact set it was built with.
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

        public class ContractResolver : DefaultContractResolver
        {
            private readonly SerializationSettings _serializationSettings;

            public ContractResolver(SerializationSettings serializationSettings)
            {
                _serializationSettings = serializationSettings;
            }

            protected override JsonObjectContract CreateObjectContract(Type objectType)
            {
                var contract = base.CreateObjectContract(objectType);

                contract.MemberSerialization = _serializationSettings.memberSerialization;

                return contract;
            }
        }

        // The mode reaches the writer, not the shared Serializer: Formatting used to live on
        // SerializationSettings and therefore applied to every save this service ever made, which is
        // the opposite of what a per-save choice needs. Bson is not a valid argument here - this is
        // the plain-text entry point; use GetDataSerializer for the binary one.
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

        // THE BYTE-LEVEL COUNTERPARTS OF SerializeData/DeserializeData, and the pair anything that
        // moves a whole aggregate around needs: a level package writes a document into an archive
        // entry, a reader takes one back out, and a server stores one in a column. Every one of
        // them holds BYTES, and none of them can go through the string API without deciding that
        // Bson does not exist.
        //
        // The domain and the version come off the type's own [DataVersion] rather than from the
        // caller, for the same reason the string API refuses a type without one: an envelope whose
        // version was supplied by whoever wrote it is an envelope that can lie about what it holds.

        /// <summary> Serializes a versioned aggregate into an envelope's bytes. </summary>
        public byte[] SerializeEnvelope<TValue>(TValue value, SerializationType type)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            var attribute = value.GetType().GetCustomAttribute<DataVersionAttribute>();
            if (attribute == null)
                throw new ArgumentException(CantConvertMessage<TValue>(nameof(SerializeEnvelope)), typeof(TValue).Name);

            return GetDataSerializer(type)
                .SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Version, value));
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
            => $"Type '{typeof(TValue)}' has no [DataVersion] attribute and cannot be used with {methodName}";
    }
}