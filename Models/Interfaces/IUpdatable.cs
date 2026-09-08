namespace BH.SDK.Models.Interfaces
{
    /// <summary> Can take another instance's contents wholesale, replacing what it held. </summary>
    public interface IUpdatable<in T>
    {
        /// <summary> Overwrite this instance with the source's contents. </summary>
        public void Update(T src);
    }
}