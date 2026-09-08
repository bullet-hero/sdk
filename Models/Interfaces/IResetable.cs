namespace BH.SDK.Models.Interfaces
{
    /// <summary> Can be put back to the state a fresh instance would have. </summary>
    public interface IResetable
    {
        /// <summary> Restore every member to its constructor value. </summary>
        public void Reset();
    }
}