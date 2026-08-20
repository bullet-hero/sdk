namespace BH.SDK.Models.Enums.Settings
{
    /// <summary> The one conversion <see cref="MsaaType"/> needs. </summary>
    public static class MsaaTypeExtensions
    {
        /// <summary> Samples per pixel, in the form every graphics API states it: 1 means no
        /// multisampling, never 0. </summary>
        public static int ToSampleCount(this MsaaType type)
            => type == MsaaType.None ? 1 : (int)type;
    }
}
