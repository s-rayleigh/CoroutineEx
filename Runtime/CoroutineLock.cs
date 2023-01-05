using System;
using UnityEngine;

namespace Rayleigh.CoroutineEx
{
    public sealed class CoroutineLock
    {
        public sealed class Waiter : CustomYieldInstruction, IDisposable
        {
            private readonly CoroutineLock cl;

            internal Waiter(CoroutineLock cl) => this.cl = cl;

            public void Dispose() => this.cl.Exit();

            public override bool keepWaiting
            {
                get
                {
                    var cur = this.cl.locked;

                    // Enter the lock
                    if(!cur) this.cl.locked = true;

                    return cur;
                }
            }
        }

        private bool locked;

        public Waiter WaitAndEnter() => new(this);

        private void Exit() => this.locked = false;
    }
}