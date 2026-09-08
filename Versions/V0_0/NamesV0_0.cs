namespace BH.SDK.Versions.V0_0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> The JSON keys as v0.0 spelled them. A snapshot must spell keys the way its own generation did,
    /// which is why it cannot read them from <c>Names</c>. </summary>
    public static class NamesV0_0
    {
        /// <summary> The key v0.0 wrote the settings half under. </summary>
        public const string Settings = "test_settings";

        /// <summary> The key v0.0 wrote the game half under. </summary>
        public const string Game = "test_game";

        /// <summary> The key v0.0 wrote the resources half under. </summary>
        public const string Resources = "test_resources";
        
        /// <summary> The key v0.0 wrote the events under. </summary>
        public const string GameEvents = "test_game_events";
    }
}