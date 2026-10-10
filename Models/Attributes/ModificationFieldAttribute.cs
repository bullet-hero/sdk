using System;

namespace BH.SDK.Models.Attributes
{
    // The number is written by hand: order, key hash and member-name hash each re-couple it to
    // something a refactor changes. Rejected: Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md
    // Sits ABOVE [JsonProperty]; BH.SDK.Roslyn refuses a collision, an unencodable member or a bad band.

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
