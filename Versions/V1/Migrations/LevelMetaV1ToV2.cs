using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Meta;

namespace BH.SDK.Versions.V1.Migrations
{
    // ReSharper disable once InconsistentNaming

    // NOTSPECIFIED, NEVER NO. A 1.0.0 file declares nothing about AI generation, and that is exactly
    // what NotSpecified says; writing No would put a claim in the author's mouth. Nor is a record
    // synthesized for a cover the old file has: a migrator cannot hash a file, and an empty record
    // would stand for paperwork nobody wrote.

    /// <summary> Level metadata, generation 1 to generation 2 - everything carried as it is, every
    /// resource record given an empty guid (before generation 2 a record could only describe a file
    /// resource, which is addressed by its int id alone), and every AI declaration left unspecified. </summary>
    public class LevelMetaV1ToV2 : ModelMigration<LevelMetaV1, LevelMeta>
    {
        /// <summary> Builds the newer shape out of the older one. </summary>
        public override LevelMeta Migrate(LevelMetaV1 from)
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

            return new LevelMeta(from.LevelId, from.LevelName, from.LevelDescription, from.LevelLogo,
                from.LevelVersion, from.LevelLicense, from.LevelAuthors, records, from.LevelAgeRating)
            {
                LevelTags = from.LevelTags,
                LevelDuration = from.LevelDuration,
                MinGeneration = from.MinGeneration,
                LevelAiGenerated = AiGeneration.NotSpecified,
            };
        }
    }
}
