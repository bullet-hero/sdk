using BH.SDK.Models.Primitives;

namespace BH.SDK.Models.Interfaces
{
    // Implemented by whichever model owns an ObjectId namespace of its own (LevelSettings for a
    // level's own objects, Prefab for a template's objects) - lets BH.Core.Services.PrefabMaterializer
    // mint fresh, permanent object ids for a materialized prefab instance without caring whether the
    // instance is hosted directly by a level or nested inside another prefab's template.

    // The two questions below the mint are what a BULK create needs. A generator, a paste and a
    // prefab materialize all produce many objects from one gesture, and a mint that refused halfway
    // through would leave the scope holding half a run - the one failure worse than refusing at all.
    // So they ask first and write nothing when the answer is no; GetNextObjectId's own assert is the
    // net under that, not the mechanism. See LevelRules.MaxObjectIds.

    /// <summary> Mints object ids for one scope's own id namespace. </summary>
    public interface IObjectIdCounter
    {
        /// <summary> The next unused id in this scope, consuming it. </summary>
        public ObjectId GetNextObjectId();

        /// <summary> How many more ids this scope can mint before its counter reaches the end of
        /// the id space. </summary>
        public long GetRemainingObjectIds();

        /// <summary> Whether a bulk create of <paramref name="count"/> objects can be committed
        /// whole - asked BEFORE anything is written. </summary>
        public bool CanMintObjectIds(int count);
    }
}
