using System;

namespace BH.SDK.Models.Attributes
{
    // THE NUMBER IS WRITTEN BY HAND, and every cheaper-looking alternative was rejected for the
    // same reason: it would tie the id to something a refactoring tool may change without asking.
    // Declaration order renumbers everything on a member reorder, which ReSharper does on request.
    // A hash of the JSON key re-couples the id to the spelling this whole mechanism exists to
    // decouple from. A hash of the C# member name makes a rename the breaking change instead.
    // So the author writes it, once, and BH.SDK.Roslyn refuses every way of writing it wrongly:
    // a collision (BHS1201), a member it cannot encode or that declares no [JsonProperty]
    // (BHS1202), a zero or a band belonging to another declaring type (BHS1203).
    //
    // The attribute sits ABOVE the [JsonProperty] by convention, so the two read as one block
    // saying how the member is addressed - by number from a placement, by key from a file.

    /// <summary>
    /// Marks a member a prefab override may address, by its stable
    /// <see cref="ModificationFields"/> number.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ModificationFieldAttribute : Attribute
    {
        /// <summary> The member's <see cref="ModificationFields"/> constant. </summary>
        public int Field { get; }

        /// <summary> Takes the constant, never a literal. </summary>
        public ModificationFieldAttribute(int field)
        {
            Field = field;
        }
    }
}
