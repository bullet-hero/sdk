namespace BH.SDK.Models.Enums.Settings
{
    // Stored twice with different defaults: the menu opens on the grid, the editor on the list. Only where
    // a screen STARTS is remembered.
    public enum LevelBrowserLayout : byte
    {
        /// <summary> Covers first, several per row. </summary>
        Grid = 0,

        /// <summary> One level per row, with its description. </summary>
        List = 1,
    }
}
