using System;

namespace BH.SDK.Services.Archive
{
    // TWO EXCEPTIONS RATHER THAN ONE, because "not given yet" and "given and wrong" are different
    // situations for whoever is looking at the screen: the first means ask, the second means say
    // the password was wrong. Collapsing them tells a player they got a password wrong before they
    // have typed one, which is the defect this split exists to prevent - the same split
    // PgpSymmetricService already makes with PgpOpenResult.
    //
    // They are exceptions rather than values because this is the FORMAT layer, where a refusal is
    // already an InvalidDataException; the level layer above turns all three into the values its
    // own callers read.

    /// <summary> An archive is encrypted and nobody has supplied a passphrase. </summary>
    public sealed class ArchivePassphraseRequiredException : Exception
    {
        /// <summary> Built from what could not be opened. </summary>
        public ArchivePassphraseRequiredException(string message) : base(message) { }

        /// <summary> Built from what could not be opened, over the library's own failure. </summary>
        public ArchivePassphraseRequiredException(string message, Exception inner) : base(message, inner) { }
    }

    /// <summary> A passphrase was supplied and does not open the archive. </summary>
    public sealed class ArchiveWrongPassphraseException : Exception
    {
        /// <summary> Built from what could not be opened. </summary>
        public ArchiveWrongPassphraseException(string message) : base(message) { }

        /// <summary> Built from what could not be opened, over the library's own failure. </summary>
        public ArchiveWrongPassphraseException(string message, Exception inner) : base(message, inner) { }
    }
}
