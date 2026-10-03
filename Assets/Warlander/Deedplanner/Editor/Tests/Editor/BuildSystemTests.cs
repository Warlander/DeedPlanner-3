using System;
using System.Collections;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Warlander.Deedplanner.Editor.Tests
{
    public class BuildSystemTests
    {
        [UnityTest]
        public IEnumerator BuildWaitsForEditorCallbackAndRunsOnce()
        {
            int calls = 0;
            Task<bool> task = BuildSystem.BuildOnEditorDelayAsync(() =>
            {
                calls++;
                return Task.FromResult(true);
            });

            Assert.That(calls, Is.Zero);
            Assert.That(task.IsCompleted, Is.False);
            while (!task.IsCompleted)
            {
                yield return null;
            }
            Assert.That(task.Result, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            yield return null;
            Assert.That(calls, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BuildExceptionIsReturnedToCaller()
        {
            var exception = new InvalidOperationException("Build failed");
            Task<bool> task = BuildSystem.BuildOnEditorDelayAsync(() => throw exception);

            while (!task.IsCompleted)
            {
                yield return null;
            }
            Assert.That(task.IsFaulted, Is.True);
            Assert.That(task.Exception.InnerException, Is.SameAs(exception));
        }
    }
}
