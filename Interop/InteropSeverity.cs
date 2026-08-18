namespace BH.SDK.Interop
{
    /// <summary>
    /// How much a conversion had to give up on one thing. Ordered so a caller can ask "was anything
    /// worse than X" with a comparison.
    /// </summary>
    public enum InteropSeverity : byte
    {
        /// <summary> Worth telling the author, nothing was lost. </summary>
        Info = 0,

        /// <summary> The nearest thing the target format has was used instead. </summary>
        Approximated = 1,

        /// <summary> The target format has no equivalent yet, but is expected to grow one. </summary>
        Deferred = 2,

        /// <summary> The target format has no equivalent and is not going to; the data is gone. </summary>
        Dropped = 3,

        /// <summary> The source could not be read at all. Everything downstream of it is missing. </summary>
        Failed = 4,
    }
}
