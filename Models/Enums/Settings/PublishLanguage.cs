namespace BH.SDK.Models.Enums.Settings
{
    /// <summary> Which language a Workshop item's title and description are taken in first. </summary>
    public enum PublishLanguage : byte
    {
        /// <summary> The title and description go up in English first. </summary>
        English = 0,

        /// <summary> The title and description go up in the device's language first. </summary>
        System = 1,
    }
}
