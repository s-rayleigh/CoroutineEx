# CoroutineEx
This library aims to provide a more convenient and feature rich abstraction for coroutine-based asynchronous 
operations in Unity 3D and also attempts to replicate the TAP design pattern and `Task` class from .NET.

# Installation
Add the package to your project via [UPM](https://docs.unity3d.com/Manual/upm-ui-giturl.html) using this link:
https://github.com/s-rayleigh/CoroutineEx.git

# System.Threading.Task vs CoroutineTask

| **Task**                 | **CoroutineTask**                 |
|--------------------------|-----------------------------------|
| `Task.Run`               | `CoroutineTask.Run`               |
| `Task.Delay`             | `CoroutineTask.Delay`             |
| `Task.CompletedTask`     | `CoroutineTask.CompletedTask`     |
| `Task.FromCancelled`     | `CoroutineTask.FromCancelled`     |
| `Task.FromCancelled<>`   | `CoroutineTask.FromCancelled<>`   |
| `Task.FromException`     | `CoroutineTask.FromException`     |
| `Task.FromException<>`   | `CoroutineTask.FromException<>`   |
| `Task.FromResult`        | `CoroutineTask.FromResult`        |
| `Task.Exception`         | `CoroutineTask.Exception`         |
| `Task.Status`            | `CoroutineTask.State`             |
| `Task.WhenAll`           | `Helpers.WhenAll`                 |
| `Task.WhenAny`           | `Helpers.WhenAny`                 |
| `await`                  | `yield return`                    |
| `lock` or `AsyncLock`    | `CoroutineLock`                   |
| `TaskCompletionSource`   | `CoroutineTaskCompletionSource`   |
| `TaskCompletionSource<>` | `CoroutineTaskCompletionSource<>` |

# Examples

Wait 5 seconds and print "Hello world!":
```csharp
CoroutineTask.Run(ctl => 
{
    return Internal();

    IEnumerator Internal()
    {
        yield return CoroutineTask.Delay(TimeSpan.FromSeconds(5));
        Debug.Log("Hello world!");
        ctl.Finish();
        yield return null;
        Debug.Log("This message will never be printed.");
    }
});
```

Return result from the `CoroutineTask`:
```csharp
var task = CoroutineTask<int>.Run(ctl =>
{
    return Internal();

    IEnumerator Internal()
    {
        yield return null;
        ctl.SetResult(42);
    }
});

yield return task;
Debug.Log(task.Result);
```

# TODO
- [ ] Move `Helpers.WhenAll` and `Helpers.WhenAny` to `CoroutineTask` class
- [ ] `CancellationToken` support for `CoroutineTask.Run` and `CoroutineTask.Delay`
- [ ] Exception bubbling
- [ ] OpenUPM package
- [ ] Docs (for now, use the tests as an example)