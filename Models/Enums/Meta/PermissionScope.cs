namespace BH.SDK.Models.Enums.Meta
{
    // How far a permission reaches: ThisLevel is void once copied, AnyLevel survives reuse. Undefined is 0
    // and means "wrote nothing", never "unlimited" (like AgeRating.Unrated).

    /// <summary> What a permission from a rights holder covers. </summary>
    public enum PermissionScope : byte
    {
        /// <summary> Nothing was declared. Not a claim that anything is permitted. </summary>
        Undefined = 0,

        /// <summary> One named level only - re-using the resource elsewhere needs a new permission. </summary>
        ThisLevel = 1,

        /// <summary> Every level by the same level author. </summary>
        AuthorLevels = 2,

        /// <summary> Any Bullet Hero level by anyone - the rights holder released it to the game. </summary>
        AnyLevel = 3,
    }
}
