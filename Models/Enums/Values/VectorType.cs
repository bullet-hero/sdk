namespace BH.SDK.Models.Enums.Values
{
    /// <summary> Which form an authored vector is in. </summary>
    public enum VectorType : byte
    {
        /// <summary>
        /// Just vector, X as X, Y as Y
        /// </summary>
        Value = 0,
        
        /// <summary>
        /// Random point in rect, X from (X, X2), Y from (Y, Y2)
        /// </summary>
        RandomRect = 1,
        
        /// <summary>
        /// Random point in rect with grid, X from (X, X2, Step), Y from (Y, Y2, Step)
        /// </summary>
        RandomRectStep = 2,
        
        /// <summary>
        /// Random point in circle, use sqrt method
        /// </summary>
        RandomCircle = 3,
        
        /// <summary>
        /// Random point in rect, ONE roll shared by every axis - the point slides along the rect's
        /// diagonal instead of landing anywhere inside it
        /// </summary>
        RandomLerp = 4,
        
        /// <summary>
        /// The same single roll, then each axis quantized to its own grid by Step
        /// </summary>
        RandomLerpStep = 5,
    }
}