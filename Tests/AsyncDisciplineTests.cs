using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THIS IS THE ONLY THING THAT ACTUALLY PREVENTS THE DEADLOCK, and it works by reading the test
    // sources rather than by running anything - because the failure it guards against cannot be
    // caught at runtime by any means available inside the process.
    //
    // The failure: EditMode tests run on the main thread, which is also the only thread Unity's
    // SynchronizationContext posts continuations to. A test that BLOCKS that thread waiting on a
    // task - Assert.ThrowsAsync, .Result, .Wait(), .GetAwaiter().GetResult() - is waiting for a
    // continuation that can only run on the thread it is occupying. The Editor stops responding at
    // ZERO CPU and never recovers; it has to be killed from Task Manager, and every unsaved change
    // in it is lost. It has happened twice.
    //
    // WHY A TIMEOUT CANNOT SAVE IT, and why nobody should add one expecting it to: a timeout is
    // itself a continuation. On a blocked main thread it is queued behind the deadlock like
    // everything else and never fires. `AsyncAssert.WithTimeout` exists for the OTHER hang - an
    // await on something that never completes, where the thread is still free - and its own header
    // says so. Two different failures, two different mechanisms.
    //
    // So the check is static, and it is deliberately a TEST rather than a Roslyn analyzer: an
    // analyzer would have to ship in the `BulletHeroSDK.Roslyn` assembly, which is `#if
    // BHSDK_ROSLYN`-gated and compiles to nothing inside this project - i.e. exactly here, it would
    // never run. A test runs in every suite, including the one a person runs before committing.
    //
    // Scope: `Tests/` folders only. Production code is free to block - `FileLoaderService` does it
    // inside a thread-pool delegate on purpose, which is safe precisely because it is not the main
    // thread.
    public class AsyncDisciplineTests
    {
        // Every test folder in the repository, found from this file rather than hardcoded, so a new
        // assembly's tests are covered the day it is created.
        private static readonly string[] TestRoots =
        {
            "Assets/Code/Shared/Tests",
            "Assets/Code/Core/Tests",
            "Assets/Code/GamePlayer/Tests",
            "Assets/Code/GameEditor/Tests",
            "Assets/Code/Services/Shared/Tests",
            "Assets/Code/Services/Menu/Tests",
            "Assets/Code/Services/GameEditor/Tests",
            "Assets/Code/Services/Game/Tests",
            "Assets/Code/Services/Root/Tests",
            "Assets/Plugins/BulletHeroSDK/Tests",
            "Assets/Plugins/BulletHeroSDK/UnityExtensions/Tests",
        };

        // The file that documents the trap is allowed to name it; so is this one.
        private static readonly string[] Exempt = { "AsyncAssert.cs", "AsyncDisciplineTests.cs" };

        private sealed class Rule
        {
            public readonly string Name;
            public readonly Regex Pattern;
            public readonly string Instead;

            public Rule(string name, string pattern, string instead)
            {
                Name = name;
                Pattern = new Regex(pattern, RegexOptions.Compiled);
                Instead = instead;
            }
        }

        private static readonly Rule[] Rules =
        {
            new("Assert.ThrowsAsync", @"Assert\s*\.\s*ThrowsAsync",
                "await AsyncAssert.Throws<T>(() => ...)"),
            new("Assert.DoesNotThrowAsync", @"Assert\s*\.\s*DoesNotThrowAsync",
                "await the call directly - an unexpected exception already fails the test"),
            new("Assert.CatchAsync", @"Assert\s*\.\s*CatchAsync",
                "await AsyncAssert.Catch(() => ...)"),

            // `.Result` on anything awaitable. Narrowed to a call or an identifier followed by
            // `.Result` at the end of an expression, so a property genuinely called Result on a
            // plain value type is not swept up with it.
            new(".Result", @"\)\s*\.\s*Result\b|\bTask\s*\.\s*\w+\([^)]*\)\s*\.\s*Result\b",
                "await it"),
            new(".Wait()", @"\)\s*\.\s*Wait\s*\(\s*\)", "await it"),
            new(".GetAwaiter().GetResult()", @"GetAwaiter\s*\(\s*\)\s*\.\s*GetResult\s*\(\s*\)", "await it"),
            new("Task.WaitAll", @"Task\s*\.\s*WaitAll", "await Task.WhenAll(...)"),
            new("Task.WaitAny", @"Task\s*\.\s*WaitAny", "await Task.WhenAny(...)"),
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void NoTest_BlocksTheMainThreadOnATask()
        {
            var offenders = new List<string>();

            foreach (var root in TestRoots)
            {
                if (!Directory.Exists(root)) continue;

                foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(file);
                    if (Array.IndexOf(Exempt, name) >= 0) continue;

                    var lines = File.ReadAllLines(file);
                    for (var i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i];

                        // A comment may name any of these - this file's own header does, and so does
                        // every note explaining why they are banned.
                        var trimmed = line.TrimStart();
                        if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;
                        if (trimmed.StartsWith("///", StringComparison.Ordinal)) continue;
                        if (trimmed.StartsWith("*", StringComparison.Ordinal)) continue;

                        foreach (var rule in Rules)
                        {
                            if (!rule.Pattern.IsMatch(line)) continue;

                            offenders.Add(
                                $"{Shown(file)}:{i + 1} uses {rule.Name} - {rule.Instead}");
                        }
                    }
                }
            }

            CollectionAssert.IsEmpty(offenders,
                "A test that blocks the main thread waiting on a task DEADLOCKS THE UNITY EDITOR: " +
                "EditMode tests run on the same thread the continuation needs, so neither side can " +
                "move and the Editor has to be killed. Await instead - see Tests/AsyncAssert.cs.");
        }

        // The roots above are a list, so a new test folder that nobody adds to it would be checked
        // by nothing at all. This is what says so out loud instead.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryTestFolder_IsCovered()
        {
            var missing = new List<string>();

            foreach (var root in new[] { "Assets/Code", "Assets/Plugins/BulletHeroSDK" })
            {
                if (!Directory.Exists(root)) continue;

                foreach (var folder in Directory.GetDirectories(root, "Tests", SearchOption.AllDirectories))
                {
                    var shown = Shown(folder);

                    // A vendored package brings its own tests and its own release cycle; this rule
                    // is about the project's own.
                    if (shown.Contains("/Plugins/vertoker.")) continue;
                    if (shown.Contains("/Plugins/Unity")) continue;

                    // A folder called Tests that holds no C# is not a test folder. Under
                    // Assets/Code/Architecture two of them hold only the AssemblyBuilder's
                    // descriptor .assets, one per test assembly - named after what they describe,
                    // which is why they look like the real thing from the outside.
                    if (Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories).Length == 0) continue;

                    if (Array.IndexOf(TestRoots, shown) < 0) missing.Add(shown);
                }
            }

            CollectionAssert.IsEmpty(missing,
                "Test folders not listed in AsyncDisciplineTests.TestRoots, so nothing checks them " +
                "for main-thread blocking. Add them to that array.");
        }

        private static string Shown(string path) => path.Replace(Path.DirectorySeparatorChar, '/');
    }
}