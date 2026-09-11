using System;
using System.Collections;
using System.Linq;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Objects
{
    // Limitations for modifications

    // 1. Works only for RectObject and Prefab, not applied to anything else

    // 2. You can't make RectObject a child or any Object in Prefab, no parenting from low levels
    // (but you still can make root of prefab inherit from outside Object, parenting from high levels is allowed)

    // 3. Modification works only for prefab scope where it's located.
    // No deep inheritance of changes

    // ONE OF THE THREE MODELS THE GENERATOR DOES NOT COVER, and the reason is Value. Copying it is
    // not an assignment (a whole-track override stores a List<TKeyframe>, which would alias) and
    // comparing it is not object.Equals (a List has no value equality), so both bodies below are
    // real decisions rather than boilerplate. FrameSpan and ModificationKey are the other two, for
    // the different reason that a struct has no constructor body to read defaults from.
    //
    // It is `partial` anyway, and for a different generator: ValidationGenerator writes a walk for
    // every [RuleContainer], and opting out of ModelGenerator says nothing about that one. There is
    // no third state - a container either gets a generated walk or stays the slowest kind of node on
    // a level's load path - so BHS1101 is an error and this word is how it is answered.

    /// <summary>
    /// One per-placement field override: "in this PrefabObject, that object's that field is this
    /// value instead". Re-applied on top of a fresh template copy after every materialize/resync,
    /// which is what lets a placement diverge from its template without breaking the link.
    /// </summary>
    [RuleContainer]
    public sealed partial class Modification : IModel<Modification>, Serialization.Blob.IBinaryModel, Serialization.Json.IJsonModel
    {
        // WHICH object (inner/template ObjectId) and WHICH field (Path) this override applies to -
        // see ModificationKey's own doc comment. Also PrefabObject.Modifications' dictionary key.

        /// <summary> Target of the override (template object id + field id + element index). </summary>
        [RuleModificationKeyValid]
        [JsonProperty(Names.Key)]
        public ModificationKey Key { get; set; }

        private object _value;

        /// <summary> The overriding value, untyped because a path can point at any field. Normalized
        /// to long/double on assignment so an override still equals itself after a round trip. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ValueShort)]
        public object Value
        {
            get => _value;
            // Widened to the same CLR types Newtonsoft always produces when deserializing a raw
            // JSON number into an `object` property (long for any integral, double for any
            // floating-point) - without this, a Modification built in code with e.g. a plain `int`
            // stops Equals-ing itself after a serialize/deserialize round trip, since the
            // deserialized copy always comes back as long/double regardless of the original width.
            set => _value = NormalizeValue(value);
        }

        private static object NormalizeValue(object value) => value switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong => Convert.ToInt64(value),
            float or double or decimal => Convert.ToDouble(value),
            _ => value,
        };

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Modification()
        {
            Key = new ModificationKey(ObjectId.Null, ModificationFields.None);
            Value = null;
        }
        /// <summary> Built from its id, field and value. </summary>
        public Modification(ObjectId objectId, int field, object value)
        {
            Key = new ModificationKey(objectId, field);
            Value = value;
        }
        /// <summary> Built from its key and value. </summary>
        public Modification(ModificationKey key, object value)
        {
            Key = key;
            Value = value;
        }
        /// <summary> Back to the values the constructor writes. </summary>
        public void Reset()
        {
            var key = Key;
            key.Reset();
            Key = key;
            Value = null;
        }

        /// <summary> The untyped spelling of <c>Copy</c>. </summary>
        public object Clone() => Copy();
        /// <summary> A deep copy, sharing nothing mutable with this one. </summary>
        public Modification Copy() => new(Key.Copy(), CopyValue());

        /// <summary> A copy of the overriding value - an assignment would alias a whole-track override's list. </summary>
        public object CopyValue()
        {
            if (Value == null) return null;
            if (Value.GetType().IsValueType) return Value;
            // A whole-track override (see ModificationUtils.Apply's PropertyCategory.List branch)
            // stores a List<TKeyframe> here - List<T> itself isn't ICloneable, so without this the
            // copy would alias the SAME list instance as the original (a real bug for
            // PrefabObject.CopyImpl/Update). Reconstruct a same-concrete-type list and clone each
            // element through its own ICloneable.Clone() - every keyframe type already implements
            // this transitively via IModel<T> : ICopyable<T> : ICloneable.
            if (Value is IList list)
            {
                var copy = (IList)Activator.CreateInstance(list.GetType());
                foreach (var item in list)
                    copy.Add(item is ICloneable cloneable ? cloneable.Clone() : item);
                return copy;
            }
            if (Value is ICloneable cloneableValue) return cloneableValue.Clone();
            return Value;
        }

        /// <summary> Becomes the source, replacing everything this instance held. </summary>
        public void Update(Modification src)
        {
            Key = src.Key.Copy();
            Value = src.CopyValue();
        }

        /// <summary> Takes the source's contents in place, so nothing pointing inside this instance is invalidated. </summary>
        public void Pull(Modification src)
        {
            Key = src.Key.Copy();
            Value = src.CopyValue();
        }

        /// <summary> The same, boxed. </summary>
        public override bool Equals(object obj) => obj is Modification value && Equals(value);
        /// <summary> Matches the equality above. </summary>
        public override int GetHashCode() => HashCode.Combine(Key, Value);

        /// <summary> Member by member. </summary>
        public bool Equals(Modification other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Key.Equals(other.Key)) return false;
            // List<T> has no value-equality override (default is reference equality) - a whole-track
            // override's Value needs element-wise comparison instead, same reasoning as CopyValue.
            if (Value is IEnumerable and not string && other.Value is IEnumerable and not string)
                return ((IEnumerable)Value).Cast<object>().SequenceEqual(((IEnumerable)other.Value).Cast<object>());
            var result = object.Equals(Value, other.Value);
            return result;
        }

        #region Blob

        // THE ONE MEMBER IN THIS FORMAT WITH NO TYPE, so it is the one the generator refuses and
        // this is the hand-written answer. Value is written as JSON TEXT rather than as a tagged
        // union of the five scalars it usually holds, and that is not laziness - it is what makes
        // the two formats agree. A whole-track override stores a List<TKeyframe> here, whose
        // elements are polymorphic values that only this project's converters know how to write;
        // and reading a .json back always yields a JToken for anything that is not a scalar. Text
        // produced by the same serializer, parsed by the same rules, lands on exactly what a .json
        // round trip lands on - including the long/double widening the setter above applies.
        //
        // It costs a few bytes on a member a level carries a handful of. Anything cleverer would be
        // a second definition of what Value means.

        private static readonly Serialization.SerializationService JsonForValue = new();

        /// <summary> Appends this override to a .blob payload. </summary>
        public void Write(ref Serialization.Blob.BlobWriter writer)
        {
            Serialization.Blob.BlobPrimitives.Write(ref writer, Key);

            using var text = new System.IO.StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            JsonForValue.Serializer.Serialize(text, Value);
            writer.WriteString(text.ToString());
        }

        /// <summary> Reads one back over this instance. </summary>
        public void Read(ref Serialization.Blob.BlobReader reader)
        {
            Key = Serialization.Blob.BlobPrimitives.ReadModificationKey(ref reader);

            var text = reader.ReadString();
            if (text is null)
            {
                Value = null;
                return;
            }

            using var json = new JsonTextReader(new System.IO.StringReader(text));
            Value = JsonForValue.Serializer.Deserialize(json);
        }

        #endregion

        #region Json

        // The same reason the blob half is hand-written: Value has no type, so nothing mechanical
        // can encode it. It goes through the very serializer the rest of the format uses, which is
        // what makes it identical to what the reflective path wrote - including the JToken a
        // non-scalar comes back as.

        /// <summary> Writes this override as JSON. </summary>
        public void WriteJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName(Names.Key);
            Serialization.Json.JsonPrimitives.Write(writer, Key);
            writer.WritePropertyName(Names.ValueShort);
            JsonForValue.Serializer.Serialize(writer, Value);
            writer.WriteEndObject();
        }

        /// <summary> Reads one back over this instance. </summary>
        public void ReadJson(JsonReader reader)
            => Serialization.Json.JsonModels.ReadObject(reader, this);

        /// <summary> Reads one named member, so a subclass can extend the shape without restating the loop. </summary>
        public bool ReadJsonMember(JsonReader reader, string name)
        {
            switch (name)
            {
                case Names.Key:
                    Key = Serialization.Json.JsonPrimitives.ReadModificationKey(reader);
                    return true;
                case Names.ValueShort:
                    Value = JsonForValue.Serializer.Deserialize(reader);
                    return true;
            }

            return false;
        }

        #endregion
    }
}