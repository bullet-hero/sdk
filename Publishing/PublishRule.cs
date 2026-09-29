namespace BH.SDK.Publishing
{
    /// <summary>
    /// What a level can fail on when it is offered to a service. Distinct from RuleXxx (one property
    /// being out of range) and from GraphRule (one broken relationship): everything here is about
    /// whether a level may be REDISTRIBUTED, which is a question about the level's paperwork and the
    /// receiving service's policy, not about whether the level is well-formed or even playable.
    /// </summary>
    public enum PublishRule : byte
    {
        None = 0,

        /// <summary> The level's own license is not one the service accepts. Args: none when it
        /// states no license, else its license and the accepted TypicalLicenseType[]. </summary>
        LevelLicenseNotAllowed = 1,

        /// <summary> The level declares no age rating and the service requires one. </summary>
        LevelAgeRatingMissing = 2,

        /// <summary> Nobody is credited for the level itself. </summary>
        LevelAuthorsMissing = 3,

        /// <summary> A user-defined resource of the level has no ResourceMeta record at all - the
        /// one finding that needs both files to see. Args: type, name. </summary>
        ResourceMetaMissing = 4,

        /// <summary> A ResourceMeta record describes a resource the level does not have. Args: type,
        /// name. </summary>
        ResourceMetaOrphaned = 5,

        /// <summary> A resource's license is not one the service accepts. Args: type, name, its
        /// license, the accepted TypicalLicenseType[]. </summary>
        ResourceLicenseNotAllowed = 6,

        /// <summary> A resource states no license, and no permission stands in for one. Args: type,
        /// name, and the platform it was taken from (NoLicenseSourceType) when one is named. </summary>
        ResourceLicenseUnspecified = 7,

        /// <summary> A resource that must be credited names nobody. Args: type, name. </summary>
        ResourceAttributionMissing = 8,

        /// <summary> A resource records no page the work can be traced back to. Args: type, name. </summary>
        ResourceUrlMissing = 9,

        /// <summary> A resource carries no content hash, so a takedown could not find it again.
        /// Args: type, name. </summary>
        ResourceHashMissing = 10,

        /// <summary> A permission names no scope, or points at no evidence anyone could check. Args:
        /// type, name. </summary>
        PermissionIncomplete = 11,

        /// <summary> Every permission covering a resource has lapsed. Args: type, name. </summary>
        PermissionExpired = 12,

        /// <summary> A resource is fetched in a way the service does not accept (typically an
        /// arbitrary URL) - needs the level file to see. Args: type, name, its ResourceUriType, the
        /// accepted ResourceUriType[]. </summary>
        ResourceUriTypeNotAllowed = 13,

        /// <summary> The resource comes from a site nothing may be published from. Args: type, name,
        /// the site, the roster's note. </summary>
        SourceNotAllowed = 14,

        /// <summary> The site hosts more than one kind of terms - a human has to confirm this one.
        /// Args: type, name, the site, the roster's note. </summary>
        SourceNeedsReview = 15,

        /// <summary> The site is in no roster entry, so nothing is known about its terms. Args: type,
        /// name, the site's host. </summary>
        SourceUnknown = 16,

        /// <summary> One resource file is bigger than the service accepts. Args: type, name, its
        /// size, the limit. </summary>
        ResourceTooLarge = 17,

        /// <summary> level.json or metadata.json alone is bigger than the service accepts. Args: the
        /// file, its size, the limit. </summary>
        DataFileTooLarge = 18,

        /// <summary> The whole level folder is bigger than the service accepts. Args: its size, the
        /// limit. </summary>
        PayloadTooLarge = 19,

        // A COLLECTION'S OWN FINDINGS - CollectionReadinessAnalyzer. A collection has no level to
        // inspect, so what it is graded on is its own manifest and whether it stands on its own.

        /// <summary> The collection has no name to list it under. </summary>
        CollectionNameMissing = 20,

        /// <summary> The collection states no license of its own. </summary>
        CollectionLicenseUnspecified = 21,

        /// <summary> The collection holds nothing. </summary>
        CollectionEmpty = 22,

        /// <summary> A file the manifest names is not in the collection. Args: type, name, the
        /// missing path. </summary>
        CollectionMediaMissing = 23,

        /// <summary> A resource points at a user resource the collection does not carry. Args:
        /// type, the missing reference, and the type and name of the resource pointing at it. </summary>
        CollectionReferenceMissing = 24,
    }
}
