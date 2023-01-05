using System.Collections;

namespace Rayleigh.CoroutineEx
{
    public sealed class CoroutineTaskCompletionSource
    {
        public CoroutineTask Task { get; }

        private CoroutineTask.ExecutionControl executionControl;

        public CoroutineTaskCompletionSource()
        {
            this.Task = new(ctl =>
            {
                this.executionControl = ctl;
                return Endless();
            });

            this.Task.Start();
        }

        private static IEnumerator Endless()
        {
            while(true) yield return null;
        }

        public void SetCanceled() => this.executionControl.Cancel();

        public void SetFinished() => this.executionControl.Finish();

        public void SetFaulted() => this.executionControl.Fail();
    }

    public sealed class CoroutineTaskCompletionSource<T>
    {
        public CoroutineTask<T> Task { get; }

        private CoroutineTask<T>.ExecutionControl executionControl;

        private bool run;

        public CoroutineTaskCompletionSource()
        {
            this.run = true;

            this.Task = new(ctl =>
            {
                this.executionControl = ctl;
                return this.AlmostEndless();
            });

            this.Task.Start();
        }

        private IEnumerator AlmostEndless()
        {
            while(this.run) yield return null;
        }

        public void SetCanceled() => this.executionControl.Cancel();

        public void SetResult(T result)
        {
            this.executionControl.SetResult(result);
            this.run = false;
        }

        public void SetFaulted() => this.executionControl.Fail();
    }
}