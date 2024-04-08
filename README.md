# CoroutineEx
[![openupm](https://img.shields.io/npm/v/com.rayleigh.coroutineex?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.rayleigh.coroutineex/)

This library aims to provide a more convenient and feature rich abstraction for coroutine-based asynchronous 
operations in Unity 3D and also attempts to replicate the TAP design pattern and `Task` class from .NET.

# Features
- Allows for precise control over the execution sequence.
- Offers multiple task states.
- Provides a way to return a value from the task.
- Works in Editor scripts.
- Replicates some features from the TAP.

# Installation
Follow the guide by [this link](https://openupm.com/packages/com.rayleigh.coroutineex/#modal-manualinstallation) to
install the package using OpenUPM (recommended).

Or you can add the package to your project via [UPM](https://docs.unity3d.com/Manual/upm-ui-giturl.html) using
this Git URL: https://github.com/s-rayleigh/CoroutineEx.git

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
| `Task.WhenAll`           | `CoroutineTask.WhenAll`           |
| `Task.WhenAny`           | `CoroutineTask.WhenAny`           |
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