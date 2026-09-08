using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An RGB colour, authored either literally or as a reference into the level's theme. </summary>
    public interface IColor3 : IModel<IColor3>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ColorType GetModelType();
    }
}
