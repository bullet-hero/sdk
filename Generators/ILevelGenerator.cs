namespace BH.SDK.Generators
{
    /// <summary> A generator that builds a whole level from nothing. It runs before any level is open:
    /// no GeneratorContext, no change log (undo is not creating the level). </summary>
    public interface ILevelGenerator : IGenerator
    {
        GeneratedLevel Create(object parameters);
    }
}
