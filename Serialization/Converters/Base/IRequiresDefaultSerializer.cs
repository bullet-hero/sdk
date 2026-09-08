using System.Collections.Generic;
using Newtonsoft.Json;

namespace BH.SDK.Serialization.Converters.Base
{
    // Implemented by converters that resolve a concrete implementation of a polymorphic type and need
    // a private "default" JsonSerializer to populate that concrete type's own members - one that carries
    // every other registered converter (so nested custom types, e.g. an IVector2 field inside a Color4Key,
    // still work) but leaves out whichever converter(s) would otherwise try to re-wrap the concrete type's
    // plain member JSON as another [type, value] array.
    // SerializationService.GetConverters wires this up automatically for every converter in its list that
    // implements this interface, so adding a new one is the only step required - no manual bookkeeping.

    /// <summary> A converter that needs a second serializer of its own to read the concrete type it resolved. </summary>
    public interface IRequiresDefaultSerializer
    {
        /// <summary> Which converters that private serializer must NOT carry - normally just this one. </summary>
        IEnumerable<JsonConverter> GetExcludedConverters(IReadOnlyList<JsonConverter> allConverters);

        /// <summary> Handed the serializer built from that exclusion, once, at wiring time. </summary>
        void SetDefaultSerializer(JsonSerializer serializer);
    }
}
