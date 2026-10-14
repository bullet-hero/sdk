using System;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Utils
{
    // The id inside the file is who a skin is; the name is only where the game looks first, so a file
    // dropped in under any name is still found by a scan.

    /// <summary> How an avatar skin's file under <c>skins/</c> is named: <c>&lt;id&gt;.json</c>. </summary>
    public static class AvatarSkinFiles
    {
        private const string Extension = ".json";

        /// <summary> The file name a skin is written under; null for a built-in or unset id. </summary>
        public static string FileNameOf(AvatarSkinId id) =>
            !id.IsEnabled() || id.IsBuiltin() ? null : id.value.ToString("D") + Extension;

        /// <summary> Whether a file under <c>skins/</c> may hold a skin: any <c>.json</c> but the
        /// selection's. </summary>
        public static bool IsSkinFile(string fileName) =>
            !string.IsNullOrEmpty(fileName) &&
            fileName.EndsWith(Extension, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(fileName, FileNames.SkinSelectionFileName, StringComparison.OrdinalIgnoreCase);

        /// <summary> The id a file's name spells, when it is named the way the game names one. </summary>
        public static bool TryGetId(string fileName, out AvatarSkinId id)
        {
            id = AvatarSkinId.Null;
            if (!IsSkinFile(fileName)) return false;
            if (!Guid.TryParse(fileName.Substring(0, fileName.Length - Extension.Length), out var guid)) return false;

            id = new AvatarSkinId(guid);
            return id.IsEnabled() && !id.IsBuiltin();
        }
    }
}
