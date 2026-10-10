using System;
using System.Threading.Tasks;

namespace BH.SDK.Tests
{
    // NUnit's Assert.ThrowsAsync BLOCKS the main thread and freezes the Editor at zero CPU: await the
    // failure instead. The timeout below does NOT cure that deadlock (it needs a free thread); it catches
    // an await that never completes. Full account: BH.SDK/Docs/TESTING.md, "Async tests".

    /// <summary> Awaiting counterpart of NUnit's Assert.ThrowsAsync, which must not be used in
    /// EditMode tests - see this file's header. </summary>
    internal static class AsyncAssert
    {
        /// <summary> How long an awaited test step may take before it is called a hang. Generous:
        /// this is a backstop against never, not a performance budget. </summary>
        public const int DefaultTimeoutSeconds = 60;

        /// <summary> Runs the action and returns whatever it threw, or null. </summary>
        public static async Task<Exception> Catch(Func<Task> action)
        {
            try
            {
                await action();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }

        /// <summary> Runs the action and returns the exception of the expected type, failing the
        /// test when it threw something else or nothing at all. </summary>
        public static async Task<TException> Throws<TException>(Func<Task> action)
            where TException : Exception
        {
            var thrown = await Catch(action);

            switch (thrown)
            {
                case null:
                    throw new NUnit.Framework.AssertionException(
                        $"Expected {typeof(TException).Name}, but nothing was thrown.");
                case TException expected:
                    return expected;
                default:
                    throw new NUnit.Framework.AssertionException(
                        $"Expected {typeof(TException).Name}, but got {thrown.GetType().Name}: {thrown.Message}");
            }
        }

        // Task.WhenAny, not a CancellationToken: the work worth guarding stopped honouring tokens.

        /// <summary> Awaits an operation, failing the test rather than hanging the run when it does
        /// not finish. See this file's header for the one hang this cannot catch. </summary>
        public static async Task WithTimeout(Func<Task> action, int seconds = DefaultTimeoutSeconds,
            string what = null)
        {
            var work = action();
            var finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(seconds)));

            if (!ReferenceEquals(finished, work))
                throw new NUnit.Framework.AssertionException(
                    $"{what ?? "The awaited operation"} did not finish within {seconds}s and was abandoned. " +
                    "Something it waits on never completes.");

            // Awaited rather than left alone, so a failure inside it is reported as itself instead
            // of as an unobserved task exception later.
            await work;
        }

        /// <summary> The value-returning twin. </summary>
        public static async Task<TValue> WithTimeout<TValue>(Func<Task<TValue>> action,
            int seconds = DefaultTimeoutSeconds, string what = null)
        {
            var work = action();
            var finished = await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(seconds)));

            if (!ReferenceEquals(finished, work))
                throw new NUnit.Framework.AssertionException(
                    $"{what ?? "The awaited operation"} did not finish within {seconds}s and was abandoned. " +
                    "Something it waits on never completes.");

            return await work;
        }
    }
}