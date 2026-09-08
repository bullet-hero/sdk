namespace BH.SDK.Models.Interfaces
{
    /// <summary> Can become another instance in place, keeping every reference pointing inside it valid. </summary>
    public interface IMoveable<in T>
    {
        /// <summary> Take the source's contents without replacing this instance - the non-invalidating half of <see cref="IUpdatable{T}"/>. </summary>
        public void Pull(T source);
    }
}