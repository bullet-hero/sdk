using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Meta;

namespace BH.SDK.Publishing
{
    /// <summary> The Creative Commons license elements a typical license carries. </summary>
    [Flags]
    public enum LicenseElements : byte
    {
        None = 0,
        Attribution = 1,
        ShareAlike = 2,
        NonCommercial = 4,
        NoDerivatives = 8,
    }

    // Facts read from the CC legal codes, the same for every service - acceptability is the profile's
    // business, compatibility is not. Docs/PUBLISHING.md, "License compatibility".

    /// <summary> What the Creative Commons legal codes say about combining licenses. </summary>
    public static class LicenseTerms
    {
        private static readonly TypicalLicenseType[] None = Array.Empty<TypicalLicenseType>();
        private static readonly TypicalLicenseType[] BySa4 = { TypicalLicenseType.CC_BY_SA_4_0 };
        private static readonly TypicalLicenseType[] BySa3 =
            { TypicalLicenseType.CC_BY_SA_3_0, TypicalLicenseType.CC_BY_SA_4_0 };
        private static readonly TypicalLicenseType[] ByNcSa4 = { TypicalLicenseType.CC_BY_NC_SA_4_0 };
        private static readonly TypicalLicenseType[] ByNcSa3 =
            { TypicalLicenseType.CC_BY_NC_SA_3_0, TypicalLicenseType.CC_BY_NC_SA_4_0 };

        /// <summary> The CC elements of a typical license; false for anything that is not Creative
        /// Commons (CC0 is Creative Commons with no elements and returns true). </summary>
        public static bool TryGetElements(TypicalLicenseType type, out LicenseElements elements)
        {
            const LicenseElements by = LicenseElements.Attribution;
            const LicenseElements sa = LicenseElements.ShareAlike;
            const LicenseElements nc = LicenseElements.NonCommercial;
            const LicenseElements nd = LicenseElements.NoDerivatives;

            elements = type switch
            {
                TypicalLicenseType.CC0_1_0 => LicenseElements.None,
                TypicalLicenseType.CC_BY_4_0 or TypicalLicenseType.CC_BY_3_0 => by,
                TypicalLicenseType.CC_BY_SA_4_0 or TypicalLicenseType.CC_BY_SA_3_0 => by | sa,
                TypicalLicenseType.CC_BY_NC_4_0 or TypicalLicenseType.CC_BY_NC_3_0 => by | nc,
                TypicalLicenseType.CC_BY_NC_SA_4_0 or TypicalLicenseType.CC_BY_NC_SA_3_0 => by | nc | sa,
                TypicalLicenseType.CC_BY_ND_4_0 or TypicalLicenseType.CC_BY_ND_3_0 => by | nd,
                TypicalLicenseType.CC_BY_NC_ND_4_0 or TypicalLicenseType.CC_BY_NC_ND_3_0 => by | nc | nd,
                _ => LicenseElements.None,
            };
            return type == TypicalLicenseType.CC0_1_0 || elements != LicenseElements.None;
        }

        // 4.0 s.3(b)(1) and 3.0 s.4(b): same elements, this version or later. The 4.0 "BY-SA
        // Compatible License" list (GPLv3) is left out: no shipped profile accepts GPL.

        /// <summary> The licenses an adaptation of a ShareAlike work may carry; empty when the license
        /// is not ShareAlike. </summary>
        public static IReadOnlyList<TypicalLicenseType> ShareAlikeTargets(TypicalLicenseType type) => type switch
        {
            TypicalLicenseType.CC_BY_SA_4_0 => BySa4,
            TypicalLicenseType.CC_BY_SA_3_0 => BySa3,
            TypicalLicenseType.CC_BY_NC_SA_4_0 => ByNcSa4,
            TypicalLicenseType.CC_BY_NC_SA_3_0 => ByNcSa3,
            _ => None,
        };
    }
}
