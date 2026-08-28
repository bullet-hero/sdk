using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace BH.SDK.Serialization.Serializers
{
    // BSON implementation of IDataSerializer - see NewtonsoftDataSerializer for the shared envelope
    // logic. Reuses the exact same JsonSerializer/converter chain as JsonDataSerializer; only the
    // raw reader/writer over the byte stream differs. See VERSION-UPDATE.md, "Format-agnosticism".
    public class BsonDataSerializer : BaseNewtonsoftDataSerializer
    {
        public BsonDataSerializer(JsonSerializer serializer) : base(serializer)
        {
        }

        public override SerializationType Type => SerializationType.Bson;

        protected override JsonWriter CreateWriter(Stream stream) =>
            new BsonDataWriter(stream) { CloseOutput = false };

        // DateTimeKindHandling IS NOT A DEFAULT WORTH KEEPING. BSON stores an instant as
        // milliseconds since the epoch, so the instant survives either way - but BsonDataReader
        // hands it back as DateTimeKind.Local by default, which rewrites the TICKS into the
        // reading machine timezone. DateTime.Equals compares ticks and ignores Kind, so a value
        // written on one machine and read on another compares unequal to itself, and a round trip
        // fails anywhere but UTC+0. Every DateTime this format carries is written as UtcNow
        // (statistics timestamps, PermissionGrant), so reading them back as UTC is what makes a
        // BSON round trip an identity instead of a timezone-dependent one.
        protected override JsonReader CreateReader(Stream stream) =>
            new BsonDataReader(stream) { CloseInput = false, DateTimeKindHandling = DateTimeKind.Utc };
    }
}
