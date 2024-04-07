using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayleigh.CoroutineEx.Tests
{
    public class CoroutineTaskHelpersTests
    {
        [UnityTest]
        public IEnumerator WhenAllEnumerable()
        {
            const int num = 5;
            const float longestTaskWait = .1f + .5f * (num - 1);

            var list = new List<CoroutineTask>(num);
            var timeStart = Time.time;

            for(var i = 0; i < num; i++)
            {
                var i1 = i;

                list.Add(CoroutineTask.Run(_ =>
                {
                    return Internal();

                    IEnumerator Internal()
                    {
                        yield return new WaitForSeconds(.1f + i1 * .5f);
                    }
                }));
            }

            var whenAllTask = CoroutineTask.WhenAll(list);
            yield return whenAllTask;

            Assert.That(whenAllTask.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(Time.time - (timeStart + longestTaskWait), Is.LessThan(.05f));
            for(var i = 0; i < num; i++) Assert.That(list[i].State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
        }

        [UnityTest]
        public IEnumerator WhenAnyArray()
        {
            var quickTask = new CoroutineTask(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return new WaitForSeconds(.1f);
                }
            });

            var avgTask = new CoroutineTask(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return new WaitForSeconds(.5f);
                }
            });

            var longTask = new CoroutineTask(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return new WaitForSeconds(1f);
                }
            });

            var whenAnyTask = CoroutineTask.WhenAny(quickTask, avgTask, longTask);

            quickTask.Start();
            avgTask.Start();
            longTask.Start();

            yield return whenAnyTask;

            Assert.That(whenAnyTask.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(whenAnyTask.Result, Is.EqualTo(quickTask));
        }
    }
}