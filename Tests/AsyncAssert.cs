using System;
using System.Threading.Tasks;

namespace BH.SDK.Tests
{
    // NUNIT'S OWN Assert.ThrowsAsync FREEZES THE UNITY EDITOR, and it does it silently: the whole
    // application stops responding at zero CPU and never comes back, so the run has to be killed
    // from Task Manager. It is worth stating exactly why, because the call looks completely
    // ordinary and the failure looks like an infinite loop.
    //
    // Assert.ThrowsAsync BLOCKS the calling thread on the task it was handed. EditMode tests run on
    // the main thread, which is also the only thread Unity's SynchronizationContext posts
    // continuations to. So the moment the awaited work suspends for real - a GZipStream.WriteAsync,
    // a tar entry read, anything that does not finish synchronously - its continuation is queued
    // for a thread that is sitting inside the assert waiting for that very continuation. Neither
    // side can move.
    //
    // It survived a first round of tests here only by accident: those exceptions were thrown
    // synchronously, before the first await, so nothing ever suspended. That is precisely the kind
    // of luck that makes the trap worth a file of its own rather than a comment somewhere.
    //
    // What is safe is AWAITING the failure instead of blocking on it. Every caller is an
    // `async Task` test, so the await returns to the pump like any other.
    //
    // THE TIMEOUT BELOW IS NOT A CURE FOR THAT DEADLOCK AND MUST NOT BE READ AS ONE. A timeout
    // needs its own continuation to run, so a BLOCKED main thread swallows it exactly as it
    // swallows everything else. What it does catch is the other hang: a test awaiting something
    // that simply never completes - a completion source nobody sets, an event that never fires -
    // where the thread is free and the run would otherwise sit at that test forever. Preventing the
    // blocking kind is `Tests/AsyncDisciplineTests`' job, which reads the test sources themselves.

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

        // Task.WhenAny rather than a CancellationToken, deliberately: a token only helps when the
        // awaited work honours one, and the work worth guarding here is precisely the work that has
        // stopped honouring anything. Losing the race is reported and the original task is
        // abandoned - it cannot be killed, and pretending otherwise would be worse than saying so.

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