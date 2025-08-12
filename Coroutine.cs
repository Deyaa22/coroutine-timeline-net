using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoroutineTimeline
{
	/// <summary>
	/// Provides an easy-to-use and lightweight coroutine timeline based on the C# iterator pattern with support for time-based delays.
	/// Supports yielding <see cref="TimeSpan"/>, <see cref="int"/> (seconds), and <see cref="float"/> (seconds) for time delays.
	/// </summary>
	public sealed partial class Coroutine : IDisposable
	{

	}

	// Public Interface
	public sealed partial class Coroutine : IDisposable
	{
		public event Action<Coroutine> Ended;

		public static Coroutine StartCoroutine(Func<Coroutine, IEnumerator<object>> coroutine, Action<Coroutine> terminationCallBack = null, bool autoDispose = true)
		{
			Coroutine newCoroutine = new Coroutine(coroutine);
			newCoroutine.Ended += terminationCallBack;
			newCoroutine.AutoDispose = autoDispose;
			newCoroutine.Run();
			return newCoroutine;
		}

		public static Coroutine StartCoroutine(IEnumerator<object> instance, Action<Coroutine> endedCallback = null, bool autoDispose = true)
		{
			return StartCoroutine((coroutine) => instance, endedCallback, autoDispose);
		}

		public void CancelAfter(int millisecondsDelay)
		{
			CancelAfter(TimeSpan.FromMilliseconds(millisecondsDelay));
		}

		public async void CancelAfter(TimeSpan timeDelay)
		{
			if (IsDisposed)
				return;

			await Task.Delay(timeDelay, _cancellationTokenSource.Token).ContinueWith((ant) => { if (IsDisposed) return; Cancel(); });
		}

		public void Cancel()
		{
			lock (_lock)
			{
				if (!IsRunning)
					return;

				State |= CoroutineState.Cancelled;
				_cancellationTokenSource.Cancel();
				OnEnded();
				if (AutoDispose)
					((IDisposable)this).Dispose();
			}
		}

		/// <summary>
		/// Blocks caller thread until the coroutine stops running.
		/// </summary>
		/// <param name="token">The CancellationToken to observe, stops waiting and blocking caller thread.</param>
		public void Wait(CancellationToken token)
		{
			try
			{
				_finishedEvent.Wait(token);
			}
			catch { }
		}

		void IDisposable.Dispose()
		{
			lock (_lock)
			{
				if (IsDisposed)
					return;

				if (IsRunning)
					_cancellationTokenSource.Cancel();

				State |= CoroutineState.Disposed;
				_cancellationTokenSource.Dispose();
				_cancellationTokenSource = null;
				_coroutineMethod = null;
				Ended = null;
			}
		}
	}

	// Functionality
	public sealed partial class Coroutine : IDisposable
	{
		private Coroutine(Func<Coroutine, IEnumerator<object>> coroutine)
		{
			_coroutineMethod = coroutine;
			_cancellationTokenSource = new CancellationTokenSource();
		}

		private async Task Run()
		{
			await Task.Run(StartASyncExecution, _cancellationTokenSource.Token);
		}

		private async Task StartASyncExecution()
		{
			var enumerator = _coroutineMethod(this);
			await Task.Run(() => StartASyncExecution(enumerator));

			//if (!IsCancelled && !IsDisposed)
			if (IsRunning)
				Complete();
		}

		private async Task StartASyncExecution(IEnumerator<object> enumerator)
		{
			//while (!IsCancelled && !IsDisposed && enumerator.MoveNext())
			while (IsRunning && enumerator.MoveNext())
			{
				try
				{
					var i = enumerator.Current;
					if (i is TimeSpan timespan)
					{
						await Task.Delay(timespan, _cancellationTokenSource.Token);
					}
					else if (i is int seconds)
					{
						await Task.Delay(seconds * 1000, _cancellationTokenSource.Token);
					}
					else if (i is float secs)
					{
						await Task.Delay((int)(secs * 1000), _cancellationTokenSource.Token);
					}
					else if (i is IEnumerator<object> iterator)
					{
						await Task.Run(() => StartASyncExecution(iterator), CancellationToken);
					}
				}
				catch
				{
					break;
				}
			}
			enumerator.Dispose();
		}

		private void Complete()
		{
			//if (IsDisposed || IsCompleted || IsCancelled)
			if (!IsRunning)
				return;

			//IsCompleting = true;
			State |= CoroutineState.Completed;
			OnEnded();
			if (AutoDispose)
				((IDisposable)this).Dispose();
		}

		private void OnEnded()
		{
			_finishedEvent.Set();
		}
	}

	// Attributes & Properties
	public sealed partial class Coroutine : IDisposable
	{
		private object _lock = new object();
		private Func<Coroutine, IEnumerator<object>> _coroutineMethod;
		private CancellationTokenSource _cancellationTokenSource;
		private readonly ManualResetEventSlim _finishedEvent = new ManualResetEventSlim(false);

		public CancellationToken CancellationToken { get { return _cancellationTokenSource.Token; } }

		public CoroutineState State { get; private set; } = CoroutineState.Running;

		public bool AutoDispose { get; private set; } = true;
		public bool IsDisposed { get { return (State & CoroutineState.Disposed) != 0; } }
		public bool IsRunning { get { return State == CoroutineState.Running; } }
	}

	public enum CoroutineState : sbyte
	{
		Running = 0 << 0,
		Completed = 1 << 0,
		Cancelled = 1 << 1,
		Disposed = 1 << 2
	}
}