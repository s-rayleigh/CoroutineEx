using System.Collections;
using Unity.EditorCoroutines.Editor;

namespace Rayleigh.CoroutineEx
{
    public class EditorCoroutineOwner : ICoroutineOwner
    {
        public object StartCoroutine(IEnumerator sequence) => EditorCoroutineUtility.StartCoroutineOwnerless(sequence);

        public void StopCoroutine(object coroutine) => EditorCoroutineUtility.StopCoroutine((EditorCoroutine)coroutine);
    }
}