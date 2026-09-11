using System;
using BH.SDK.Serialization;
using Newtonsoft.Json.Linq;

namespace BH.SDK.Utils
{
    // WHY A CONVERSION IS NEEDED AT ALL, since an override is written and read by this project's
    // own code: Modification.Value normalizes every integral to long and every floating-point to
    // double on assignment, deliberately, so an override built in code still Equals itself after a
    // round trip. That widening is what the old apply path then tripped over - it guarded the write
    // with typeof(int).IsAssignableFrom(typeof(long)), which is false, so an int override never
    // applied at all and an enum or an id stopped applying the moment it had been through the
    // serializer. Seven kinds landed and four did not; ModificationApplyTests holds the record.
    //
    // THE CONVERSION GOES BACK THROUGH THE SERIALIZER THAT SHAPED THE VALUE, rather than through a
    // hand-written table of the shapes an override can hold. A long that was an int, a number that
    // was an enum or an id, a JToken that was a model or a list - every one of them has that shape
    // BECAUSE this serializer gave it one, so converting back through the same one is a definition
    // that already applies. A second definition written by hand would be one more thing to keep in
    // lockstep with the converters, and it would drift exactly where the format is most polymorphic.
    //
    // It allocates. The apply path runs once per override per materialize, not per frame, and the
    // fast path below already covers the common case - a value that is already the right type,
    // which is every override applied in the same session it was recorded in.

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
