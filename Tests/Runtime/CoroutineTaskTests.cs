using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rayleigh.CoroutineEx.Tests
{
    public class CoroutineTaskTests
    {
        [UnityTest]
        public IEnumerator ManualStart()
        {
            var test = false;
            var scheduler = CoroutineTaskScheduler.Default;

            var task = new CoroutineTask(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    test = true;
                }
            });

            task.Start(scheduler);

            // Must fail to start for a second time.
            Assert.That(() => task.Start(scheduler), Throws.Exception.TypeOf<InvalidOperationException>());

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.True);
        }

        [UnityTest]
        public IEnumerator RanToCompletion()
        {
            var test = false;

            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.True);
        }

        [UnityTest]
        public IEnumerator EmptyRanToCompletion()
        {
            var test = false;

            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
#pragma warning disable CS0162
                    if(false) yield return null;
#pragma warning restore CS0162
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.True);
        }

        [UnityTest]
        public IEnumerator ForceFinish()
        {
            var test = false;

            var task = CoroutineTask.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    ctl.Finish();
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator ForceFail()
        {
            var test = false;

            var task = CoroutineTask.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    ctl.Fail();
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator ForceFailImmediate()
        {
            var test = false;

            var task = CoroutineTask.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    ctl.Fail();
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator ExceptionFail()
        {
            var exception = new Exception();

            var task = CoroutineTask.Run(_ =>
            {
                IEnumerator Internal()
                {
                    yield return null;
                    throw exception;
                }

                return Internal();
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.EqualTo(exception));
        }

        [UnityTest]
        public IEnumerator ExceptionFailImmediate()
        {
            var exception = new Exception();

            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    throw exception;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.EqualTo(exception));
        }

        [UnityTest]
        public IEnumerator ControlCancel()
        {
            var test = false;

            var task = CoroutineTask.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    ctl.Cancel();
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator ControlCancelImmediate()
        {
            var test = false;

            var task = CoroutineTask.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    ctl.Cancel();
                    yield return null;
                    test = true;
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator OuterCancel()
        {
            var test = false;

            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return new WaitForSeconds(1f);
                    test = true;
                }
            });

            yield return new WaitForSeconds(.3f);
            task.Cancel();
            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
            Assert.That(test, Is.False);
        }

        [UnityTest]
        public IEnumerator ExceptionCancel()
        {
            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    yield return null;
                    throw new TaskCanceledException();
                }
            });

            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
        }

        [UnityTest]
        public IEnumerator CancellationTokenCancel()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            var task = CoroutineTask.Run(_ =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    while(true)
                    {
                        token.ThrowIfCancellationRequested();
                        yield return null;
                    }
                    // ReSharper disable once IteratorNeverReturns
                }
            });

            yield return new WaitForSecondsRealtime(.1f);
            cancellationTokenSource.Cancel();
            yield return task;

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
        }

        [UnityTest]
        public IEnumerator CompletedTask()
        {
            var task = CoroutineTask.CompletedTask;
            
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            
            yield return task;
        }

        [UnityTest]
        public IEnumerator FromCancelled()
        {
            var task = CoroutineTask.FromCancelled();
            
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
            
            yield return task;
        }

        [UnityTest]
        public IEnumerator FromCancelledGeneric()
        {
            var task = CoroutineTask.FromCancelled<int>();
            
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
            
            yield return task;
        }

        [UnityTest]
        public IEnumerator FromException()
        {
            var exception = new Exception();
            var task = CoroutineTask.FromException(exception);

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.EqualTo(exception));
            
            yield return task;
        }
        
        [UnityTest]
        public IEnumerator FromExceptionGeneric()
        {
            var exception = new Exception();
            var task = CoroutineTask.FromException<int>(exception);

            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.EqualTo(exception));
            
            yield return task;
        }

        [UnityTest]
        public IEnumerator FromResult()
        {
            const int result = 13;
            var task = CoroutineTask.FromResult(result);
            
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(task.Exception, Is.Null);
            Assert.That(task.Result, Is.EqualTo(result));

            yield return task;
        }

        [UnityTest]
        public IEnumerator Delay()
        {
            var ts = TimeSpan.FromSeconds(.3d);
            var stopwatch = Stopwatch.StartNew();
            yield return CoroutineTask.Delay(ts);
            stopwatch.Stop();
            Assert.That(stopwatch.Elapsed, Is.EqualTo(ts).Within(TimeSpan.FromSeconds(0.01d)));
        }

        [UnityTest]
        public IEnumerator ExceptionBubbling()
        {
            var exception = new Exception();
            CoroutineTask innerTask = null;
            var task = CoroutineTask.Run(_ =>
            {
                return Internal();
                IEnumerator Internal()
                {
                    innerTask = CoroutineTask.Run(_ =>
                    {
                        return Internal2();
                        IEnumerator Internal2() => throw exception;
                    });
                    yield return innerTask;
                }
            });

            yield return task;

            Assert.That(innerTask.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(innerTask.Exception, Is.EqualTo(exception));
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Faulted));
            Assert.That(task.Exception, Is.EqualTo(exception));
        }

        [UnityTest]
        public IEnumerator CancellationBubbling()
        {
            CoroutineTask innerTask = null;
            var task = CoroutineTask.Run(_ =>
            {
                return Internal();
                IEnumerator Internal()
                {
                    innerTask = CoroutineTask.Run(_ =>
                    {
                        return Internal2();
                        IEnumerator Internal2() => throw new TaskCanceledException();
                    });
                    yield return innerTask;
                }
            });

            yield return task;

            Assert.That(innerTask.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(innerTask.Exception, Is.Null);
            Assert.That(task.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(task.Exception, Is.Null);
        }

        [UnityTest]
        public IEnumerator DelayAndTransitionsCancel()
        {
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(.25d));
            var cancellationToken = cancellationTokenSource.Token;
            var delayTask = CoroutineTask.Delay(TimeSpan.FromSeconds(5d), cancellationToken);
            var transitionBySpeedTask = CoroutineTask.TransitionBySpeed(_ => { }, speed: .01f,
                cancellationToken: cancellationToken);
            var transitionByTimeTask = CoroutineTask.TransitionByTime(_ => { }, time: 5f,
                cancellationToken: cancellationToken);
            yield return CoroutineTask.WhenAll(delayTask, transitionBySpeedTask, transitionByTimeTask);

            Assert.That(delayTask.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(transitionBySpeedTask.State, Is.EqualTo(CoroutineTaskState.Canceled));
            Assert.That(transitionByTimeTask.State, Is.EqualTo(CoroutineTaskState.Canceled));
        }

        [UnityTest]
        public IEnumerator SuppressThrowing()
        {
            var cancelSuppressTask = CoroutineTask.Run(_ =>
            {
                return Internal();
                IEnumerator Internal()
                {
                    yield return CoroutineTask.FromCancelled().ConfigureYield(true);
                }
            });

            var exceptionSuppressTask = CoroutineTask.Run(_ =>
            {
                return Internal();
                IEnumerator Internal()
                {
                    yield return CoroutineTask.FromException(new Exception()).ConfigureYield(true);
                }
            });

            yield return cancelSuppressTask;
            yield return exceptionSuppressTask;
            
            Assert.That(cancelSuppressTask.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(exceptionSuppressTask.State, Is.EqualTo(CoroutineTaskState.RanToCompletion));
            Assert.That(exceptionSuppressTask.Exception, Is.Null);
        }
    }
}