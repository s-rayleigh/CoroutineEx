using System;
using System.Collections;
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
            /// <para>Forcibly ends the sequence execution and sets the <see cref="CoroutineTaskState.RanToCompletion"/> state.</para>
            /// <para>Does nothing if the current state is not <see cref="CoroutineTaskState.Running"/> or <see cref="CoroutineTaskState.Created"/>.</para>
            /// </summary>
            public void Finish()
            {
                if(this.Task.State is not (CoroutineTaskState.Running or CoroutineTaskState.Created)) return;
                var task = (CoroutineTask)this.Task;
                task.State = CoroutineTaskState.RanToCompletion;
                if(task.coroutine is not null) task.scheduler.StopCoroutine(task.coroutine);
            }

            /// <summary>
            /// <para>Forcibly ends the sequence execution and sets the <see cref="CoroutineTaskState.Faulted"/> state.</para>
            /// <para>Does nothing if the current state is not <see cref="CoroutineTaskState.Running"/> or <see cref="CoroutineTaskState.Created"/>.</para>
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

        public static CoroutineTask FromCancelled() => new() { State = CoroutineTaskState.Canceled };

        public static CoroutineTask<T> FromCancelled<T>() => new() { State = CoroutineTaskState.Canceled };

        public static CoroutineTask FromException(Exception exception) =>
            new() { State = CoroutineTaskState.Faulted, Exception = exception };
        
        public static CoroutineTask<T> FromException<T>(Exception exception) =>
            new() { State = CoroutineTaskState.Faulted, Exception = exception };

        public static CoroutineTask<T> FromResult<T>(T result) =>
            new() { State = CoroutineTaskState.RanToCompletion, result = result };
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