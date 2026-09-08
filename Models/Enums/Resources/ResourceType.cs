namespace BH.SDK.Models.Enums.Resources
{
    /// <summary> What a level resource becomes once loaded. </summary>
    public enum ResourceType : byte
    {
        Bytes = 0, // byte[]
        Text = 1, // string
        Texture = 2, // Texture
        Audio = 3, // AudioClip
        Font = 4, // UniTextFont
    }
}