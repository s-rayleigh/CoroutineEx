using System;
using System.Collections;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Rayleigh.CoroutineEx
{
    public sealed class CoroutineTaskScheduler : ICoroutineTaskScheduler
    {
        private static ICoroutineTaskScheduler editorDefault;

        private static ICoroutineTaskScheduler runtimeDefault;

        public static ICoroutineTaskScheduler Default => Application.isPlaying ? runtimeDefault : editorDefault;

        private readonly ICoroutineOwner coroutineOwner;

        [PublicAPI]
        public CoroutineTaskScheduler(ICoroutineOwner coroutineOwner) => this.coroutineOwner = coroutineOwner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInit() =>
            runtimeDefault = new CoroutineTaskScheduler(RuntimeCoroutineOwner.Create());

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        private static void EditorInit() =>
            editorDefault = new CoroutineTaskScheduler(new EditorCoroutineOwner());
#endif
        public void EnqueueTask(CoroutineWrapper.SchedulerControl ctl)
        {
            ctl.NotifyStarted(this.coroutineOwner.StartCoroutine(ControlSequence()));

            IEnumerator ControlSequence()
            {
                IEnumerator enumerator;

                // We need this try-catch because of unity (or mono) implementation. Logically it wouldn't be needed.
                // If we have an empty enumerator only with exception throw, it will execute the enumerator body on
                // the method calling.
                try
                {
                    enumerator = ctl.Execute();
                }
                catch(OperationCanceledException)
                {
                    ctl.NotifyCanceled();
                    yield break;
                }
                catch(Exception e)
                {
                    ctl.NotifyException(e);
                    yield break;
                }

                while(ctl.task.State is CoroutineTaskState.Created or CoroutineTaskState.Running)
                {
                    bool next;

                    try
                    {
                        next = enumerator.MoveNext();
                    }
                    catch(OperationCanceledException)
                    {
                        ctl.NotifyCanceled();
                        yield break;
                    }
                    catch(Exception e)
                    {
                        ctl.NotifyException(e);
                        yield break;
                    }

                    if(!next) break;

                    // TODO: detect the CoroutineTask and handle it separately (instead of passing it to Unity)
                    yield return enumerator.Current;
                }

                ctl.NotifyCompletion();
            }
        }

        public void StopCoroutine(object coroutine) => this.coroutineOwner.StopCoroutine(coroutine);
    }
}