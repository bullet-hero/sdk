using System;

namespace BH.SDK.Models.Attributes
{
    // Marked, not inferred: nothing in Dictionary<ObjectId, RectObject> says its references are the
    // address (selection, operation buffer, prefab children), and a hook cannot un-emit the assignment.

    /// <summary>
    /// Makes the generated <c>Pull</c> merge this dictionary key by key instead of replacing it wholesale;
    /// the instance is never swapped. No effect on Copy or Update.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class GenerateModelMergeAttribute : Attribute
    {
    }
}
