using System;

namespace BH.SDK.Services.Archive
{
    // Two exceptions because "not given yet" (ask) and "given and wrong" (say so) differ; PgpOpenResult
    // splits the same way. Exceptions because this is the FORMAT layer; the level layer makes values.

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
