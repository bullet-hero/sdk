using System;

namespace BH.SDK.Models.Attributes
{
    // THE ONE THING A Dictionary<K,V> CANNOT SAY ABOUT ITSELF. Almost every keyed collection in this
    // format writes as a bare ARRAY OF VALUES with the key dropped, because the value already
    // carries it - a RectObject knows its own ObjectId, a LevelTrack its AudioId, a Resource its id.
    // Recovering the key on read means knowing WHICH property it is, and nothing in
    // `Dictionary<ObjectId, RectObject>` distinguishes ObjectId from ParentObjectId.
    //
    // That knowledge used to live in twelve hand-written DictionaryAsListConverter subclasses, one
    // per collection, each existing only to answer `GetKey`. This says the same thing where the
    // collection is declared, in one line, and the twelve classes are what it replaces.
    //
    // A dictionary WITHOUT it writes as an array of {K,V} pairs - the shape for a key that genuinely
    // cannot be derived (PrefabObject.ObjectIds is id-to-id; LevelStatistics.Records is filed under
    // a RunProfile that BestRun deliberately does not repeat). A string-keyed one writes as a plain
    // JSON object, which is Newtonsoft's own happy path and needs nothing.

    /// <summary>
    /// Names the property on the VALUE that holds the dictionary's key, so the collection can be
    /// written as a bare array and rebuilt from it on read.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelKeyedAttribute : Attribute
    {
        public GenerateModelKeyedAttribute(string keyProperty) => KeyProperty = keyProperty;

        /// <summary> Property name on the value type. </summary>
        public string KeyProperty { get; }
    }
}
