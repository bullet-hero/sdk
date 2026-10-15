using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Meta;
using BH.SDK.Versions.V2;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    // NotSpecified, never No: a 1.0.0 file declares nothing about AI generation, and No would be a
    // claim in the author's mouth. No cover record is synthesized either: a migrator cannot hash.

    /// <summary> Level metadata, generation 1 to generation 2 - everything carried as it is, every
    /// resource record given an empty guid (before generation 2 a record could only describe a file
    /// resource, which is addressed by its int id alone), and every AI declaration left unspecified. </summary>
    public class LevelMetaV1ToV2 : ModelMigration<LevelMetaV1, LevelMetaV2>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelMetaV2 Migrate(LevelMetaV1 from)
        {
            var records = new List<ResourceMeta>(from.ResourcesMeta.Count);
            foreach (var record in from.ResourcesMeta)
            {
                if (record == null) continue;

                records.Add(new ResourceMeta(record.ResourceType, record.ResourceId, record.ResourceTitle,
                    record.ResourceDescription, record.ResourceUrl, record.ResourceLicense,
                    record.ResourceSources, record.ResourceAuthors, record.ResourcePermissions,
                    record.ResourceHashes, record.ResourceFeatured, Guid.Empty, AiGeneration.NotSpecified));
            }

            return new LevelMetaV2
            {
                LevelId = from.LevelId,
                LevelName = from.LevelName,
                LevelDescription = from.LevelDescription,
                LevelLogo = from.LevelLogo,
                LevelVersion = from.LevelVersion,
                LevelLicense = from.LevelLicense,
                LevelAuthors = from.LevelAuthors,
                ResourcesMeta = records,
                LevelAgeRating = from.LevelAgeRating,
                LevelTags = from.LevelTags,
                LevelDuration = from.LevelDuration,
                MinGeneration = from.MinGeneration,
                LevelAiGenerated = AiGeneration.NotSpecified,
            };
        }
    }
}
