namespace BH.SDK.Models.Enums.Resources
{
    /// <summary> Where a resource's bytes are fetched from. </summary>
    public enum ResourceUriType : byte
    {
        /// <summary> Nothing said - the resource cannot be fetched at all. </summary>
        Undefined = 0,

        /// <summary> Inside the level's own folder, which is what makes a level portable. </summary>
        LevelPath = 1,

        /// <summary> Somewhere else on this device - what creating a level around a song produces, and what
        /// exporting a package collects in. </summary>
        AbsolutePath = 2,

        /// <summary> Fetched over the network. </summary>
        DirectUrl = 3,

        /// <summary> Shipped with the game rather than with the level. </summary>
        StreamingAssets = 4,
        
        // Addressables is not valid for levels, it has many architecture complications,
        // mostly because Addressable != file and Addressable works with imported file (AudioClip != .mp3 file).
        // For now to clarify - audio file = data
    }
}