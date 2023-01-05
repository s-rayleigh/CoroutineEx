namespace Rayleigh.CoroutineEx
{
    public interface ICoroutineTaskScheduler
    {
        public void EnqueueTask(CoroutineWrapper.SchedulerControl ctl);

        public void StopCoroutine(object coroutine);
    }
}