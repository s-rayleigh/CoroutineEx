using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rayleigh.CoroutineEx
{
    public enum CoroutineTaskState
    {
        Created,
        Running,
        RanToCompletion,
        Faulted,
        Canceled
    }

    public abstract class CoroutineWrapper : CustomYieldInstruction
    {
        protected object coroutine;

        public CoroutineTaskState State { get; internal set; }

        public Exception Exception { get; internal set; }

        public override bool keepWaiting => this.State is CoroutineTaskState.Created or CoroutineTaskState.Running;

        protected ICoroutineTaskScheduler scheduler;

        public sealed class SchedulerControl
        {
            internal readonly CoroutineWrapper task;

            internal SchedulerControl(CoroutineWrapper task) => this.task = task;

            public IEnumerator Execute() => this.task.ExecuteSequence();

            public void NotifyStarted(object coroutine)
            {
                this.task.coroutine = coroutine;
                if(this.task.State is CoroutineTaskState.Created) this.task.State = CoroutineTaskState.Running;
            }

            public void NotifyCompletion()
            {
                if(this.task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                this.task.State = CoroutineTaskState.RanToCompletion;
            }

            public void NotifyCanceled()
            {
                this.task.State = CoroutineTaskState.Canceled;
            }

            public void NotifyException(Exception exception)
            {
                this.task.State = CoroutineTaskState.Faulted;
                this.task.Exception = exception;
            }
        }

        protected abstract IEnumerator ExecuteSequence();

        public void Cancel()
        {
            if(this.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
            if(this.coroutine is not null) this.scheduler.StopCoroutine(this.coroutine);
            this.State = CoroutineTaskState.Canceled;
        }
    }

    public abstract class CoroutineTaskBase<TExecCtl> : CoroutineWrapper
        where TExecCtl : CoroutineTaskBase<TExecCtl>.ExecutionControlBase, new()
    {
        public abstract class ExecutionControlBase
        {
            protected CoroutineTaskBase<TExecCtl> Task { get; private set; }

            internal static TExecCtl Create(CoroutineTaskBase<TExecCtl> task) => new() { Task = task };

            public void Cancel() => this.Task.Cancel();
        }

        public delegate IEnumerator Sequence(TExecCtl ctl);

        private readonly Sequence sequence;

        protected CoroutineTaskBase() { }

        public CoroutineTaskBase(Sequence sequence)
        {
            this.sequence = sequence;
            this.State = CoroutineTaskState.Created;
        }

        protected override IEnumerator ExecuteSequence() => this.sequence(ExecutionControlBase.Create(this));

        public void Start() => this.Start(CoroutineTaskScheduler.Default);

        public void Start(ICoroutineTaskScheduler scheduler)
        {
            if(this.State is not CoroutineTaskState.Created)
                throw new InvalidOperationException("Task cannot be started for a second time.");

            this.scheduler = scheduler;
            scheduler.EnqueueTask(new(this));
        }
    }

    public sealed class CoroutineTask : CoroutineTaskBase<CoroutineTask.ExecutionControl>
    {
        public static CoroutineTask CompletedTask => new() { State = CoroutineTaskState.RanToCompletion };
        
        public sealed class ExecutionControl : ExecutionControlBase
        {
            /// <summary>
            /// <para>
            /// Forcibly ends the sequence execution and sets the <see cref="CoroutineTaskState.RanToCompletion"/> state.
            /// </para>
            /// <para>
            /// Does nothing if the current state is not <see cref="CoroutineTaskState.Running"/> or
            /// <see cref="CoroutineTaskState.Created"/>.
            /// </para>
            /// </summary>
            public void Finish()
            {
                if(this.Task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                var task = (CoroutineTask)this.Task;
                task.State = CoroutineTaskState.RanToCompletion;
                if(task.coroutine is not null) task.scheduler.StopCoroutine(task.coroutine);
            }

            /// <summary>
            /// <para>
            /// Forcibly ends the sequence execution and sets the <see cref="CoroutineTaskState.Faulted"/> state.
            /// </para>
            /// <para>
            /// Does nothing if the current state is not <see cref="CoroutineTaskState.Running"/> or
            /// <see cref="CoroutineTaskState.Created"/>.
            /// </para>
            /// </summary>
            public void Fail()
            {
                if(this.Task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                var task = (CoroutineTask)this.Task;
                task.State = CoroutineTaskState.Faulted;
                if(task.coroutine is not null) task.scheduler.StopCoroutine(task.coroutine);
            }
        }

        private CoroutineTask() { }
        
        public CoroutineTask(Sequence sequence) : base(sequence) { }

        public static CoroutineTask Run(Sequence sequence)
        {
            var task = new CoroutineTask(sequence);
            task.Start();
            return task;
        }

        public static CoroutineTask FromCancelled() => new() { State = CoroutineTaskState.Canceled };

        public static CoroutineTask<T> FromCancelled<T>() => new() { State = CoroutineTaskState.Canceled };

        public static CoroutineTask FromException(Exception exception) =>
            new() { State = CoroutineTaskState.Faulted, Exception = exception };
        
        public static CoroutineTask<T> FromException<T>(Exception exception) =>
            new() { State = CoroutineTaskState.Faulted, Exception = exception };

        public static CoroutineTask<T> FromResult<T>(T result) =>
            new() { State = CoroutineTaskState.RanToCompletion, result = result };
        
        /// <summary>
        /// Creates a coroutine task that will complete after a time delay.
        /// </summary>
        /// <param name="delay">The <see cref="TimeSpan"/> to wait before completing the returned task.</param>
        /// <returns>A coroutine task that represents the time delay.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <see cref="delay"/> represents a negative time interval.
        /// </exception>
        public static CoroutineTask Delay(TimeSpan delay)
        {
            if(delay <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(delay));

            return Run(_ => Internal());

            IEnumerator Internal()
            {
                var beginning = DateTime.UtcNow;
                while(beginning + delay > DateTime.UtcNow) yield return null;
            }
        }
        
        /// <summary>
        /// Creates a coroutine task that will complete when all of the supplied yield instructions have completed.
        /// </summary>
        /// <param name="tasks">The instructions to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the yield instructions.</returns>
        public static CoroutineTask WhenAll(params IEnumerator[] tasks) => WhenAll((IEnumerable<IEnumerator>)tasks);

        public static CoroutineTask WhenAll(IEnumerable<IEnumerator> tasks) => Run(_ =>
        {
            if(tasks is null) throw new ArgumentNullException(nameof(tasks));
            return Internal();

            IEnumerator Internal()
            {
                foreach(var task in tasks) yield return task;
            }
        });

        /// <summary>
        /// Creates a coroutine task that will complete when all of the supplied coroutine tasks have completed.
        /// </summary>
        /// <param name="tasks">The coroutine tasks to wait on for completion.</param>
        /// <returns>A task that represents the completion of all of the coroutine tasks.</returns>
        public static CoroutineTask<CoroutineWrapper> WhenAny(IEnumerable<CoroutineWrapper> tasks)
        {
            if(tasks is CoroutineWrapper[] tasksArray) return WhenAny(tasksArray);
            if(tasks is null) throw new ArgumentNullException(nameof(tasks));

            var copy = new List<CoroutineWrapper>(tasks);

            for(var i = 0; i < copy.Count; i++)
            {
                if(copy[i] is null) throw new ArgumentException("Task cannot be null.", nameof(tasks));
            }

            return CoroutineTask<CoroutineWrapper>.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    while(true)
                    {
                        for(var i = 0; i < copy.Count; i++)
                        {
                            if(copy[i].keepWaiting) continue;
                            ctl.SetResult(copy[i]);
                            yield break;
                        }

                        yield return null;
                    }
                }
            });
        }

        /// <summary>
        /// Creates a coroutine task that will complete when any of the supplied coroutine tasks have completed.
        /// </summary>
        /// <param name="tasks">The coroutine tasks to wait on for completion.</param>
        /// <returns>A coroutine task that represents the completion of one of the supplied coroutine tasks.
        /// The return coroutine task's <see cref="CoroutineTask{TResult}.Result"/> is the coroutine task
        /// that completed.</returns>
        /// <exception cref="ArgumentNullException">The <see cref="tasks"/> argument was null.</exception>
        /// <exception cref="ArgumentException">
        /// The <see cref="tasks"/> array contained a null task, or was empty.
        /// </exception>
        public static CoroutineTask<CoroutineWrapper> WhenAny(params CoroutineWrapper[] tasks)
        {
            if(tasks is null) throw new ArgumentNullException(nameof(tasks));

            var copy = new CoroutineWrapper[tasks.Length];

            for(var i = 0; i < tasks.Length; i++)
            {
                if(tasks[i] is null) throw new ArgumentException("Task cannot be null", nameof(tasks));
                copy[i] = tasks[i];
            }

            return CoroutineTask<CoroutineWrapper>.Run(ctl =>
            {
                return Internal();

                IEnumerator Internal()
                {
                    while(true)
                    {
                        for(var i = 0; i < copy.Length; i++)
                        {
                            if(copy[i].keepWaiting) continue;
                            ctl.SetResult(copy[i]);
                            yield break;
                        }

                        yield return null;
                    }
                }
            });
        }

        /// <summary>
        /// Transitions float value in the range defined by the <see cref="from"/> and <see cref="to"/> arguments with
        /// the specified <see cref="speed"/>.
        /// </summary>
        /// <param name="onSet">Setter for the value.</param>
        /// <param name="from">Value from which a transition starts.</param>
        /// <param name="to">Value at which a transition ends.</param>
        /// <param name="speed">Speed of the transition defined as a value change per second.</param>
        /// <param name="onEnd">Callback to be called at the end of the transition.</param>
        /// <param name="threshold">Precision used to determine the value reached the end of the transition.</param>
        /// <returns>Coroutine task to wait for the end of the transition.</returns>
        public static CoroutineTask TransitionBySpeed(Action<float> onSet, float from = 0f, float to = 1f,
            float speed = 7f, Action onEnd = default, float threshold = 0.0001f)
        {
            return onSet is null ? CompletedTask : Run(_ => Internal());

            IEnumerator Internal()
            {
                var t = 0f;
                var value = from;

                while(!FastApproximately(value, to, threshold))
                {
                    value = Mathf.Lerp(from, to, t);
                    onSet(value);
                    t += speed * Time.deltaTime;
                    yield return null;
                }

                onSet(to);
                onEnd?.Invoke();
            }
        }
        
        /// <summary>
        /// Transitions float value in the range defined by the <see cref="from"/> and <see cref="to"/> arguments during
        /// the specified <see cref="time"/>.
        /// </summary>
        /// <param name="onSet">Setter for the value.</param>
        /// <param name="from">Value from which a transition starts.</param>
        /// <param name="to">Value at which a transition ends.</param>
        /// <param name="time">Duration of the transition in seconds.</param>
        /// <param name="onEnd">Callback to be called at the end of the transition.</param>
        /// <returns>Coroutine task to wait for the end of the transition.</returns>
        public static CoroutineTask TransitionByTime(Action<float> onSet, float from = 0f, float to = 1f,
            float time = 1f, Action onEnd = default)
        {
            return onSet is null ? CompletedTask : Run(_ => Internal());
            
            IEnumerator Internal()
            {
                var perSecond = (to - from) / time;
                
                for (var val = from; val <= to; val += perSecond * Time.deltaTime)
                {
                    yield return null;
                    onSet(val);
                }
        
                onSet(to);
                onEnd?.Invoke();
            }
        }
        
        private static bool FastApproximately(float a, float b, float threshold) =>
            (a - b < 0 ? b - a : a - b) <= threshold;
    }

    public class CoroutineTask<TResult> : CoroutineTaskBase<CoroutineTask<TResult>.ExecutionControl>
    {
        public sealed class ExecutionControl : ExecutionControlBase
        {
            public void SetResult(TResult result)
            {
                if(this.Task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                var task = (CoroutineTask<TResult>)this.Task;
                task.result = result;
            }

            /// <summary>
            /// <para>Forcibly ends the sequence execution and sets the <see cref="CoroutineTaskState.Faulted"/> state.</para>
            /// <para>Does nothing if the current state is not <see cref="CoroutineTaskState.Running"/> or <see cref="CoroutineTaskState.Created"/>.</para>
            /// </summary>
            public void Fail()
            {
                if(this.Task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                var task = (CoroutineTask<TResult>)this.Task;
                task.State = CoroutineTaskState.Faulted;
                if(task.coroutine is not null) task.scheduler.StopCoroutine(task.coroutine);
            }
        }

        internal TResult result;

        public TResult Result
        {
            get
            {
                if(this.State is not CoroutineTaskState.RanToCompletion)
                    throw new InvalidOperationException("Operation was not successful or not finished yet.");

                return this.result;
            }
        }

        internal CoroutineTask() { }
        
        public CoroutineTask(Sequence sequence) : base(sequence) { }

        public static CoroutineTask<TResult> Run(Sequence sequence)
        {
            var task = new CoroutineTask<TResult>(sequence);
            task.Start();
            return task;
        }
    }
}