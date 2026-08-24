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

    // TWO OF THESE ARE THE WHOLE REASON THE FORMAT HAS AN INTEGRITY CHECK. Without one, a wrong
    // passphrase and a damaged file are indistinguishable - both surface as the padding failing to
    // unwrap - and the player is told "something went wrong" about two situations with completely
    // different answers ("try again" versus "this download is broken, get another copy").
    //
    // The distinction is not perfect and the imperfection is one-directional: OpenPGP's own quick
    // check on the session key is 16 bits, so about one wrong passphrase in 65536 gets past it and
    // then fails the MDC, and that one is reported as Tampered. A damaged file is never reported as
    // a wrong passphrase, which is the direction that matters - nobody is sent hunting for a
    // password that would not have helped.

    /// <summary> The result of opening an OpenPGP message, and what it said about itself. </summary>
    public readonly struct PgpOpenOutcome
    {
        /// <summary> Why it ended the way it did. </summary>
        public readonly PgpOpenResult Result;

        /// <summary> The file name recorded inside the message - what gpg would restore the file
        /// as. Empty when the message carries none. This is how a package knows whether it wraps
        /// a level.json or a level.bson without a header of our own. </summary>
        public readonly string InnerFileName;

        private PgpOpenOutcome(PgpOpenResult result, string innerFileName)
        {
            Result = result;
            InnerFileName = innerFileName ?? string.Empty;
        }

        /// <summary> Whether the message opened and verified. </summary>
        public bool IsOk => Result == PgpOpenResult.Ok;

        public static PgpOpenOutcome Opened(string innerFileName) =>
            new PgpOpenOutcome(PgpOpenResult.Ok, innerFileName);

        public static PgpOpenOutcome Failed(PgpOpenResult result) =>
            new PgpOpenOutcome(result, string.Empty);

        public override string ToString() => IsOk
            ? $"{Result} ('{InnerFileName}')"
            : Result.ToString();
    }
}
