using System;
using BH.SDK.Serialization;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Utils
{
    // Modification.Value widens integrals to long and floats to double, so apply converts BACK through the
    // serializer that shaped the value - never a hand-written table of shapes (Docs/UTILS_DESIGN.md).

    /// <summary> Turns an override's untyped value into the type the member it addresses actually holds. </summary>
    public static class ModificationValues
    {
        // The same service Modification's own blob and JSON halves use, for the same reason: what
        // reads a value back has to be what wrote it.
        private static readonly SerializationService Json = new();

        /// <summary>
        /// The value as a <typeparamref name="T"/>, or <c>false</c> if it cannot be one. Never throws -
        /// a failed conversion is an override that does not land, which is the caller's whole answer.
        /// </summary>
        public static bool TryConvert<T>(object value, out T result)
        {
            if (value is T typed)
            {
                result = typed;
                return true;
            }

            result = default;

            // Null is not a convertible value in either direction: Modification.Value carries
            // [RuleNotNull], so a null here is a corrupt override rather than a legal state.
            if (value is null) return false;

            try
            {
                var token = value as JToken ?? JToken.FromObject(value, Json.Serializer);
                result = token.ToObject<T>(Json.Serializer);
                return true;
            }
            catch (Exception)
            {
                // Every way this fails is the same answer to the caller - a value of the wrong
                // shape. Newtonsoft distinguishes a dozen of them and none changes what happens.
                result = default;
                return false;
            }
        }
    }
}
