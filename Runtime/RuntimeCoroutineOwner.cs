using System.Collections;
using UnityEngine;

namespace Rayleigh.CoroutineEx
{
    public class RuntimeCoroutineOwner : MonoBehaviour, ICoroutineOwner
    {
        public new object StartCoroutine(IEnumerator sequence) => base.StartCoroutine(sequence);

        public void StopCoroutine(object coroutine) => base.StopCoroutine((Coroutine)coroutine);

        public static RuntimeCoroutineOwner Create()
        {
            var obj = new GameObject("Coroutine Tasks Owner");
            DontDestroyOnLoad(obj);
            return obj.AddComponent<RuntimeCoroutineOwner>();
        }
    }
}