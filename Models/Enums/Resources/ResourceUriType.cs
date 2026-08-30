namespace BH.SDK.Models.Enums.Resources
{
    public enum ResourceUriType : byte
    {
        Undefined = 0,
        LevelPath = 1,
        AbsolutePath = 2,
        DirectUrl = 3,
        StreamingAssets = 4,
        
        // Addressables is not valid for levels, it has many architecture complications,
        // mostly because Addressable != file and Addressable works with imported file (AudioClip != .mp3 file).
        // For now to clarify - audio file = data
    }
}