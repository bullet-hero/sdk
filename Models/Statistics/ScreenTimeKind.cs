namespace BH.SDK.Models.Statistics
{
    // The four things a player can be doing, as far as a clock is concerned. It lives beside the
    // model rather than in the consumer because it names that model fields - a fifth kind means a
    // fifth field, and the two have to be added together.
    //
    // Loading is a kind of its own rather than being charged to the screen underneath it: it is the
    // only one of the four the game can hope to shrink, so it is the one worth measuring across a
    // release.

    /// <summary> Which kind of screen a second of playtime is charged to. </summary>
    public enum ScreenTimeKind : byte
    {
        Menu = 0,
        Game = 1,
        Editor = 2,
        Loading = 3,
    }
}
