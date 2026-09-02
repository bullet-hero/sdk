using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // WHAT THE AVATAR DID, summed over every real run. Both numbers are deliberately global only:
    // per level they would be a fifth and sixth counter nobody reads, and the interesting form of
    // "how far have I travelled in this game" is exactly the one that crosses every level.
    //
    // Distance is in WORLD UNITS, the same space level content is authored in, so the number keeps
    // meaning something when it is shown next to a level that is 20 units wide. Nothing scales it
    // by the camera - a zoomed-out shot does not make the avatar walk further.
    //
    // Counted from real runs only. The menu background arena drives an avatar through the same code
    // and would otherwise add distance for as long as the game sits on the main menu, which would
    // make the number a measure of idle time rather than of play.

    /// <summary> What the avatar itself has done, across every level. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AvatarStatistics : IModel<AvatarStatistics>
    {
        /// <summary> Dashes spent. A long for the same reason the frame counter is one. </summary>
        [JsonProperty(Names.TotalDashes)]
        public long TotalDashes { get; set; }

        /// <summary> Distance travelled, in world units. </summary>
        [JsonProperty(Names.TotalDistanceMoved)]
        public double TotalDistanceMoved { get; set; }

        public AvatarStatistics()
        {
            TotalDashes = 0L;
            TotalDistanceMoved = 0.0;
        }
    }
}
