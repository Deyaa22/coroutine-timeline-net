using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace CoroutineNet
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

		public static Coroutine StartCoroutine(Func<Coroutine, IEnumerator<object>>   coroutine, Action TerminationCallback = null, Action CancellationCallback = null)
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
                await Task.Delay(timeDelay, _cancellationTokenSource.Token).ContinueWith((ant) => { Cancel(); });
        }

		public void Cancel()
		{
			lock (_lock)
			{
				if (IsTerminated)
					return;

				IsCancelled = true;
				if (!_isDisposed)
					_cancellationTokenSource.Cancel();
				OnCancel();
				Terminate();
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			if (!IsCancelled)
			{
				Cancel();
				return;
			}
			_isDisposed = true;
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
		private Coroutine(Func<Coroutine, IEnumerator<object>>   coroutine)
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
			while (!IsCancelled && enumerator.MoveNext())
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

			if (!IsCancelled)
				Terminate();
		}

		private void Terminate()
		{
			lock (_lock)
			{
				if (IsTerminated)
					return;

				IsTerminated = true;
				OnTerminated();
				Dispose();
			}
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
		private Func<Coroutine, IEnumerator<object>>  _coroutineMethod;
		private CancellationTokenSource _cancellationTokenSource;

		private bool _isDisposed = false;
		private bool _isTerminated = false;
		private bool _isCancelled = false;

		public CancellationToken CancellationToken { get { return _cancellationTokenSource.Token; } }
		public bool IsTerminated { get { return _isTerminated || IsCancelled; } private set { _isTerminated = value; } }
		public bool IsCancelled { get { return _isCancelled || (!_isDisposed && _cancellationTokenSource.IsCancellationRequested); } private set { _isCancelled = value; } }
	}
}