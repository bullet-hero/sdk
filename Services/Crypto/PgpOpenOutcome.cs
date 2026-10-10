namespace BH.SDK.Services.Crypto
{
    /// <summary> Why opening an OpenPGP message ended the way it did. </summary>
    public enum PgpOpenResult
    {
        /// <summary> Opened, and its integrity check passed. </summary>
        Ok = 0,

        /// <summary> The passphrase does not open it. </summary>
        WrongPassphrase = 1,

        /// <summary> It opened and then failed its integrity check - the bytes were altered or the
        /// file is truncated. </summary>
        Tampered = 2,

        /// <summary> Not an OpenPGP message at all. </summary>
        NotOpenPgp = 3,

        /// <summary> A real OpenPGP message this reader does not implement - a public-key recipient,
        /// an AEAD packet from RFC 9580, or a message carrying no integrity protection. </summary>
        Unsupported = 4,
    }

    // Two outcomes exist because the integrity check separates a wrong passphrase from a damaged file.
    // One-directional: about 1 wrong passphrase in 65536 passes the 16-bit quick check and reports as
    // Tampered; a damaged file is never reported as a wrong passphrase.

    /// <summary> The result of opening an OpenPGP message, and what it said about itself. </summary>
    public readonly struct PgpOpenOutcome
    {
        /// <summary> Why it ended the way it did. </summary>
        public readonly PgpOpenResult Result;

        /// <summary> The file name recorded inside the message - what gpg would restore the file
        /// as. Empty when the message carries none. This is how an archive knows whether it wraps
        /// a level.json or a level.bson without a header of our own. </summary>
        public readonly string InnerFileName;

        private PgpOpenOutcome(PgpOpenResult result, string innerFileName)
        {
            Result = result;
            InnerFileName = innerFileName ?? string.Empty;
        }

        /// <summary> Whether the message opened and verified. </summary>
        public bool IsOk => Result == PgpOpenResult.Ok;

        /// <summary> The file opened, and this is what it was called inside. </summary>
        public static PgpOpenOutcome Opened(string innerFileName) =>
            new PgpOpenOutcome(PgpOpenResult.Ok, innerFileName);

        /// <summary> It did not open, and this is why - a wrong passphrase and a damaged file are two answers. </summary>
        public static PgpOpenOutcome Failed(PgpOpenResult result) =>
            new PgpOpenOutcome(result, string.Empty);

        /// <summary> One line, for a log. </summary>
        public override string ToString() => IsOk
            ? $"{Result} ('{InnerFileName}')"
            : Result.ToString();
    }
}
