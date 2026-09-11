using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration

    /// <summary> Audio as it stood at Level generation 0: not a versioned domain yet, so it carries no envelope and
    /// is migrated by hand in <c>LevelV0ToV1</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class AudioLevelV0 : IModel<AudioLevelV0>
    {
    }
}
