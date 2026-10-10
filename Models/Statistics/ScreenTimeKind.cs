namespace BH.SDK.Models.Statistics
{
    // The four things a player can be doing, as far as a clock is concerned. A fifth kind means a fifth
    // field on ScreenTimeStatistics. Docs/STATISTICS.md

    /// <summary> Which kind of screen a second of playtime is charged to. </summary>
    public enum ScreenTimeKind : byte
    {
        Menu = 0,
        Game = 1,
        Editor = 2,
        Loading = 3,
    }
}
