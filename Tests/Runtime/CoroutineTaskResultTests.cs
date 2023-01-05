using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Rayleigh.CoroutineEx.Tests
{
    public class CoroutineTaskResultTests
    {
        [UnityTest]
        public IEnumerator ResultSet()
        {
            const string value = "value";

            var task = CoroutineTask<string>.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    ctl.SetResult(value);
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Result, Is.EqualTo(value));
        }

        [UnityTest]
        public IEnumerator ForceFail()
        {
            var task = CoroutineTask<bool>.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    ctl.Fail();
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(() => task.Result, Throws.Exception.TypeOf<InvalidOperationException>());
        }
    }
}