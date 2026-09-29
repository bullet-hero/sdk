namespace BH.SDK.Models.Enums.Settings
{
    // WHICH WAY A LEVEL BROWSER OPENS, stored twice with two different defaults: the menu's browser
    // opens on the grid (a player picks a level by its cover) and the editor's on the list (an author
    // picks one by its name and description, and scrolls far more of them). The toggle beside the
    // search field still switches it for the session; only where a screen STARTS is remembered.

    /// <summary> How a level browser lays its results out when it opens. </summary>
    public enum LevelBrowserLayout : byte
    {
        /// <summary> Covers first, several per row. </summary>
        Grid = 0,

        /// <summary> One level per row, with its description. </summary>
        List = 1,
    }
}
