namespace BH.SDK.Models.Enums.Values
{
    /// <summary> What a curve does past its last key. </summary>
    public enum CurveWrapMode : byte
    {
        Default = 0,
        Once = 1,
        Loop = 2,
        PingPong = 4,
        ClampForever = 8,
    }
}