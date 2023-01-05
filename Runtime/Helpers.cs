using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rayleigh.CoroutineEx
{
    public static class Helpers
    {
        public static CoroutineTask WhenAll(params IEnumerator[] tasks) => WhenAll((IEnumerable<IEnumerator>)tasks);

        public static CoroutineTask WhenAll(IEnumerable<IEnumerator> tasks) => CoroutineTask.Run(_ =>
        {
            if(tasks is null) throw new ArgumentNullException(nameof(tasks));

            // NOTE: it doesn't matter if the tasks enumerable is changed outside as we using iterator

            return Internal();

            IEnumerator Internal()
            {
                foreach(var task in tasks) yield return task;
            }
        });

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

        public static CoroutineTask LerpValueTo(Action<float> setter, float from = 0f, float to = 1f, float speed = 7f,
            Action onEnd = null, float threshold = 0.0001f)
        {
            return CoroutineTask.Run(_ => Internal());

            IEnumerator Internal()
            {
                var t = 0f;
                var value = from;

                while(!FastApproximately(value, to, threshold))
                {
                    value = Mathf.Lerp(from, to, t);
                    setter?.Invoke(value);
                    t += Time.deltaTime * speed;
                    yield return null;
                }

                setter?.Invoke(to);
                onEnd?.Invoke();
            }
        }

        private static bool FastApproximately(float a, float b, float threshold) =>
            (a - b < 0 ? b - a : a - b) <= threshold;
    }
}