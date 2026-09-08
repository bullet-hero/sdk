namespace BH.SDK.Models.Enums
{
    /// <summary> Which grain texture the film-grain effect samples, from finest to coarsest. </summary>
    public enum FilmGrainType : byte // FilmGrainLookup
    {
        None = 0,
        Thin1 = 1,
        Thin2 = 2,
        
        Medium1 = 3,
        Medium2 = 4,
        Medium3 = 5,
        Medium4 = 6,
        Medium5 = 7,
        Medium6 = 8,
        
        Large01 = 9,
        Large02 = 10,
    }
}