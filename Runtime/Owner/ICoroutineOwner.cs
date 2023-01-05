using System.Collections;

namespace Rayleigh.CoroutineEx
{
    public interface ICoroutineOwner
    {
        public object StartCoroutine(IEnumerator sequence);

        public void StopCoroutine(object coroutine);
    }
}