using BH.SDK.Models.Primitives;

namespace BH.SDK.Models.Interfaces
{
    // Implemented by whichever model owns an ObjectId namespace (LevelSettings, Prefab), so
    // PrefabMaterializer can mint permanent ids wherever the instance is hosted.

    // The two questions below ask first and write nothing on "no": a bulk create (generator, paste,
    // prefab) that failed halfway would leave half a run. GetNextObjectId's assert is only the net.

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
