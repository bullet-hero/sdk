using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    // For converters that resolve a concrete type of a polymorphic family and need a private serializer
    // WITHOUT whichever converter would re-wrap the concrete payload as another [type, value] array.
    // SerializationService.GetConverters wires every implementer automatically.

    /// <summary> A converter that needs a second serializer of its own to read the concrete type it resolved. </summary>
    public interface IRequiresDefaultSerializer
    {
        /// <summary> Which converters that private serializer must NOT carry - normally just this one. </summary>
        IEnumerable<JsonConverter> GetExcludedConverters(IReadOnlyList<JsonConverter> allConverters);

        /// <summary> Handed the serializer built from that exclusion, once, at wiring time. </summary>
        void SetDefaultSerializer(JsonSerializer serializer);
    }
}
