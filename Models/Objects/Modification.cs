using System;
using System.Collections;
using System.Linq;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Objects
{
    // Limitations for modifications

    // 1. Works only for RectObject and Prefab, not applied to anything else

    // 2. You can't make RectObject a child or any Object in Prefab, no parenting from low levels
    // (but you still can make root of prefab inherit from outside Object, parenting from high levels is allowed)

    // 3. Modification works only for prefab scope where it's located.
    // No deep inheritance of changes

    // One of the three models the generator does not cover (Value is untyped), yet `partial` for
    // ValidationGenerator: BHS1101 is an error otherwise. Models/Objects/CLAUDE.md

    /// <summary>
    /// One per-placement field override: "in this PrefabObject, that object's that field is this value
    /// instead". Re-applied on a fresh template copy after every materialize/resync.
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
            // Widened to the CLR types Newtonsoft produces for a raw JSON number (long, double), or a plain
            // `int` stops Equals-ing itself after a round trip.
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
            // A whole-track override stores a List<TKeyframe>: List<T> is not ICloneable, so Copy rebuilds
            // a same-type list and clones each element, or the copy would alias the original's list.
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

            // A non-scalar Value comes back as a JToken and JContainer compares by REFERENCE, hence
            // DeepEquals. It does NOT equal an in-memory List<TKeyframe>: the promise is about scalars.
            if (Value is JToken token && other.Value is JToken otherToken)
                return JToken.DeepEquals(token, otherToken);

            // List<T> has no value-equality override (default is reference equality) - a whole-track
            // override's Value needs element-wise comparison instead, same reasoning as CopyValue.
            if (Value is IEnumerable and not string && other.Value is IEnumerable and not string)
                return ((IEnumerable)Value).Cast<object>().SequenceEqual(((IEnumerable)other.Value).Cast<object>());
            var result = object.Equals(Value, other.Value);
            return result;
        }

        #region Blob

        // The one member with no type, so the one the generator refuses. Value is written as JSON TEXT:
        // that is what makes .json and .blob agree. Models/Objects/CLAUDE.md

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

        // Hand-written for the same reason as the blob half: Value has no type. It goes through the
        // format's own serializer, so it matches what the reflective path wrote (a JToken for non-scalars).

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