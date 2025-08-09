![Coroutine Demo](Demo.gif)

# 🚀 CoroutineTimeline.Net

> **Asynchronous power with a natural, sequential control flow — beginner-friendly, powerful for professionals.**

**CoroutineTimeline.Net** is a **lightweight**, **easy-to-use**, and **powerful** coroutine timeline library for **.NET**.
It lets you write asynchronous operations **like a story** — no complex state machines, no messy callbacks — just **`yield`** your delays and go.

## Installation

Install the package via NuGet:

```bash
dotnet add package CoroutineTimeline.Net
```

Or find it on [NuGet Gallery - CoroutineTimeline.Net](https://www.nuget.org/packages/CoroutineTimeline.Net)

## Interface Overview

```csharp
public sealed class Coroutine : IDisposable
{
    public static Coroutine StartCoroutine(
        Func<Coroutine, IEnumerator<object>> coroutine,
        Action CompletionCallback = null,
        Action CancellationCallback = null,
        bool autoDispose = true);

    public void Cancel();
    public void CancelAfter(int millisecondsDelay);
    public void CancelAfter(TimeSpan timeDelay);

    public bool IsCompleted { get; }
    public bool IsCancelled { get; }
    public bool IsDisposed { get; }
    public bool AutoDispose { get; } = true;
    public CancellationToken CancellationToken { get; }

    public event Action Completed;
    public event Action Cancelled;

    // Dispose coroutine (usually auto-disposed after completion/cancellation)
    void IDisposable.Dispose();
}

```
* Uses `Task.Run()` and `Task.Delay()` with a `CancellationTokenSource`.
* Auto-disposes after termination or cancellation.
* Events:

  * `Completed`: Raised when the coroutine finishes normally.
  * `Cancelled`: Raised when `Cancel()` or `CancelAfter()` is called.

## How to Use
```csharp
using CoroutineTimeline;

Coroutine co = Coroutine.StartCoroutine(MyCoroutine, Completed: () => { ... }, Cancelled: () => {...});

// Cancel the coroutine manually if needed
co.Cancel();

// Or cancel after a delay (e.g., 5 seconds)
co.CancelAfter(5000);

static IEnumerator<object> MyCoroutine(Coroutine co) // Your coroutine method
{
    // Logic here
    yield return 2.0f; // wait 2 seconds
    // Logic here
    yield return MyCoroutine2(co, "Deyaa", 22);
}

static IEnumerator<object> MyCoroutine2(Coroutine co, string name, int age) // Your coroutine method
{
    // Logic here

    Task.Delay(1000, co.CancellationToken).Wait(); // Blocks until delay completes or cancelled

    Cosnole.WriteLine($"Hello {name}!");
    yield return 2.0f; // wait 2 seconds
    
    if(age < 18)
        co.Cancel(); // Cancel the coroutine if age is less than 18
    else if(age > 100)
        yield break; // End the coroutine if age is greater than 100)

    Console.WriteLine($"You are {age} years old.");
}

```

## 🌟 **Why It’s Powerful**

* **For Beginners** — Minimal learning curve, easy-to-read code, simple async control.

* **For Professionals** — Fine-grained coroutine control, **auto unlimited deep nesting** of coroutines, `CancellationToken` integration, and multi-threaded orchestration.

* **Cross-Platform** — Developed targeting **.NET Standard 2.0**, so it supports a wide range of platforms including .NET Core, .NET Framework, Xamarin, and more.

## 🔹 **Features**

### 🐣 **Beginner-Friendly**

* **Easy to use** — Learn in minutes.
* **Time-based delays** — Just `yield return` a `TimeSpan`, `int`, or `float` (seconds).
* **Sequential code, async execution** — Write as if synchronous, runs asynchronously.
* **Easy cancellation** — Stop coroutines anytime with `Cancel()` or `CancelAfter(x)`.
* **Easy termination** — End naturally with `yield break`.
* **Auto-dispose** — Clean up automatically when finished.

### 🛠 **Professional-Grade**

* **Full coroutine object access** — Trace state (`IsCompleted`, `IsCancelled`, `IsDisposed`).
* **Deep nesting** — Start a coroutine *inside* another coroutine as deep as you want, stacked internally.
* **Independent nested states** — Each nested coroutine has its own state and can be:

  * **Broken** → pop stack, return to upper level or to parent.
  * **Cancelled** → terminate everything, even the parent coroutine.
  * 
* **Thread-friendly** — Use with `Task` and `CancellationToken`:

  ```csharp
  Task.Delay(1000, coroutine.CancellationToken).Wait(); // Blocks parent coroutine until delay completes, or cancelled.
  Task.Delay(1000, coroutine.CancellationToken).ContinueWith((ant) => { ... }); // Works in parallel, can be cancelled.
  ```
* **Recursive execution** — If a coroutine yields another coroutine, it’s automatically run inside the same control flow.

## 📄 **Examples**

### Beginner — Simple Timed Flow

```csharp
Coroutine.StartCoroutine(MyTimer);

static IEnumerator<object> MyTimer(Coroutine c)
{
    for (int i = 5; i > 0; i--)
    {
        Console.WriteLine($"Countdown: {i}");
        yield return 1; // wait 1 second
    }
    Console.WriteLine("Time's up!");
}
```

💡 **What’s happening?**
You wrote sequential code, but it executes asynchronously with precise delays.

### Pro — Game Loop with Nested Coroutines, State Control

```csharp
Coroutine.StartCoroutine(PlayGame, () => {Console.WriteLinee("Completed")}, () => {Console.WriteLinee("Cancelled")});

static IEnumerator<object> PlayGame(Coroutine co)
{
    // game setup...
    while (true)
    {
        if (GameOver)
        {
            Task.Delay(500, co.CancellationToken).Wait();
            yield return ShowOutro(co, scoreLeft, scoreRight); // Nested coroutine, inject inputs
            ConsoleWriteLine("Player won");
            yield break; // clean exit
        }

        UpdateGame();
        yield return 0.01f; // smooth frame rate
    }
}

static IEnumerator<object> ShowOutro(Coroutine co, int leftScore, int rightScore)
{
    Console.Clear();
    Console.WriteLine("GAME OVER");
    yield return 0.1f; // wait a bit for the screen to clear

    if (rightScore > leftScore)
        yield break; // Exit only this coroutine, "Player won" printed in PlayGame
    if (rightScore < leftScore)
        co.Cancel(); // Cancel the entire coroutine stack, Nothing printed in PlayGame
    else
        yield return PlayerGame(co); // Nested coroutine, stacked internally
}
```

💡 **What’s happening?**

* `PlayGame` is the **main coroutine**.
* `ShowOutro` is a **nested coroutine** (automatically stored on the stack, with its own state).
* Depending on the outcome, we **break only the nested coroutine** or **cancel everything**.

## 🎯 **Strength Summary**

* **Beginners** → Write async without the headaches.
* **Pros** → Gain full coroutine control, multi-threading integration, and deep nesting support.
* **Everyone** → Enjoy clean, readable, maintainable async code.

## Contribute

Contributions are welcome! ❤️
See [CONTRIBUTING.md](CONTRIBUTING.md), or open an issue/PR.

## Conclusion

This coroutine package provides a lightweight and easy-to-use framework for managing asynchronous workflows based on C# iterator patterns. Designed for both beginners and professionals, it simplifies the creation of time-based delays and fine-grained coroutine control across multiple platforms. While still evolving, it aims to streamline async operations with minimal overhead and maximum flexibility. Your feedback and contributions are welcome to help shape its future development.

## Disclaimer

This package is in active development and may undergo significant changes. Your feedback is valuable to help improve its stability and features.
If you encounter any issues or have suggestions, please feel free to open an issue on the [Issues](https://github.com/Deyaa22/coroutine-timeline-net/issues) page.