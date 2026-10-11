using Newtonsoft.Json;

namespace BH.SDK.Agent
{
    /// <summary>
    /// <c>ai/manifest.json</c> in a level folder: whether the AI context travels with the level. A plain
    /// file, not a level model, so it takes no generation.
    /// </summary>
    public sealed class AiFolderManifest
    {
        /// <summary> The <see cref="Format"/> this build writes. </summary>
        public const int CurrentFormat = 1;

        /// <summary> The manifest's own format number. </summary>
        [JsonProperty("format")]
        public int Format { get; set; } = CurrentFormat;

        /// <summary> False keeps <c>ai/</c> out of every archive and Workshop upload. </summary>
        [JsonProperty("share")]
        public bool Share { get; set; } = true;

        /// <summary> Reads a manifest; false, with the reason, when the text is not one. </summary>
        public static bool TryParse(string json, out AiFolderManifest manifest, out string error)
        {
            manifest = null;

            try
            {
                manifest = JsonConvert.DeserializeObject<AiFolderManifest>(json ?? string.Empty);
            }
            catch (JsonException exception)
            {
                error = exception.Message;
                return false;
            }

            error = manifest == null ? "the manifest is empty" : null;
            return manifest != null;
        }

        /// <summary> The manifest as the text written to disk. </summary>
        public string ToJson() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
