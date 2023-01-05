using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Rayleigh.CoroutineEx.Tests
{
    public class CoroutineTaskCompletionSourceTests
    {
        [UnityTest]
        public IEnumerator SetFinishedTest()
        {
            var tcs = new CoroutineTaskCompletionSource();
            yield return null;
            tcs.SetFinished();
            yield return tcs.Task;
            Assert.That(tcs.Task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
        }

        [UnityTest]
        public IEnumerator SetResultTest()
        {
            const string value = "value";
            var tcs = new CoroutineTaskCompletionSource<string>();
            yield return null;
            tcs.SetResult(value);
            yield return tcs.Task;
            Assert.That(tcs.Task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(tcs.Task.Result, Is.EqualTo(value));
        }
    }
}