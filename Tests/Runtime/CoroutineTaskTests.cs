using System;
using System.Collections;
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
    }
}