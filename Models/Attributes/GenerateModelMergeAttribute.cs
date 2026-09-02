using System;

namespace BH.SDK.Models.Attributes
{
    // THE ONE THING ABOUT PULL A TYPE CANNOT SAY. Pull's promise is that a reference anyone holds
    // inside this subtree stays live, and for almost every collection that promise is worth
    // nothing: an element is addressed by its index or its key, so replacing the collection loses
    // nobody anything. In an OBJECT SCOPE the reference IS the address - the editor's selection,
    // its operation buffer and every materialized prefab child point at RectObjects - and a
    // clipboard's sections are the same case one level up, each held as its own timeline's buffer.
    //
    // Nothing in the type of a Dictionary<ObjectId, RectObject> distinguishes those five members
    // from the seven that are replaced wholesale, so the type cannot be read for it and a hook
    // cannot un-emit the assignment. It is marked.

    /// <summary>
    /// Makes the generated <c>Pull</c> merge this dictionary key by key - dropping keys the source
    /// no longer has and pulling into the values already there - instead of replacing it wholesale.
    /// The dictionary instance itself is then never swapped. Has no effect on Copy or Update.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelMergeAttribute : Attribute
    {
    }
}
