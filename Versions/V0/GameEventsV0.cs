using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;

namespace BH.SDK.Versions.V0
{
    // ReSharper disable once InconsistentNaming

    /// <summary> Generation 0 of the game-events domain. A frozen snapshot - never edit it to match
    /// today's shape. </summary>
    [ModelGeneration(ModelDomains.GameEvents, ModelGenerations.Test)]
    [GenerateModel]
    public sealed partial class GameEventsV0 : IModel<GameEventsV0>
    {
    }
}
