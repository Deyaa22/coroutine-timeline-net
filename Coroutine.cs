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
		public event Action Cancelled;

		public event Action Terminated;

		public static Coroutine StartCoroutine(Func<Coroutine, IEnumerator<object>> coroutine, Action TerminationCallback = null, Action CancellationCallback = null)
		{
			Coroutine newCoroutine = new Coroutine(coroutine);
			newCoroutine.Terminated += TerminationCallback;
			newCoroutine.Cancelled += CancellationCallback;
			newCoroutine.Run();
			return newCoroutine;
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
				if (IsDisposed || IsTerminated || IsCancelled)
					return;

				IsCancelled = true;
				OnCancel();
				((IDisposable)this).Dispose();
			}
		}

		void IDisposable.Dispose()
		{
			if (IsDisposed)
				return;

			if (!IsCancelled && !IsTerminated)
			{
				CancelSilently();
			}

			IsDisposed = true;
			_cancellationTokenSource.Dispose();
			_cancellationTokenSource = null;
			_coroutineMethod = null;
			Cancelled = null;
			Terminated = null;
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
			while (!IsCancelled && !IsDisposed && enumerator.MoveNext())
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
				}
				catch
				{
					break;
				}
			}
			enumerator.Dispose();

			if (!IsCancelled && !IsDisposed)
				Terminate();
		}

		private void Terminate()
		{
			lock (_lock)
			{
				if (IsDisposed || IsTerminated || IsCancelled)
					return;

				IsTerminated = true;
				OnTerminated();
				((IDisposable)this).Dispose();
			}
		}

		private void CancelSilently()
        {
			if (!IsDisposed && !_cancellationTokenSource.IsCancellationRequested)
				_cancellationTokenSource.Cancel();
        }

		private void OnCancel()
		{
			Cancelled?.Invoke();
		}

		private void OnTerminated()
		{
			Terminated?.Invoke();
		}
	}

	// Attributes & Properties
	public sealed partial class Coroutine : IDisposable
	{
		private object _lock = new object();
		private Func<Coroutine, IEnumerator<object>> _coroutineMethod;
		private CancellationTokenSource _cancellationTokenSource;

		public CancellationToken CancellationToken { get { return _cancellationTokenSource.Token; } }

		public bool IsDisposed { get; private set; }
		public bool IsTerminated { get; private set; }
		public bool IsCancelled { get; private set; }
	}
}