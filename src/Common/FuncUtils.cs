using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Vostok.Logging.Abstractions;

namespace Ulearn.Common
{
	public static class FuncUtils
	{
		private static ILog Log => LogProvider.Get().ForContext(typeof(FuncUtils));

		public static async Task<T> TrySeveralTimesAsync<T>(Func<Task<T>> func, int triesCount, Func<Task> runAfterFail, Type exceptionType)
		{
			for (var tryIndex = 0; tryIndex < triesCount; tryIndex++)
				try
				{
					return await func().ConfigureAwait(false);
				}
				catch (Exception e) when (exceptionType.IsInstanceOfType(e))
				{
					Log.Warn(e, $"Исключение (попытка {tryIndex + 1} из {triesCount}):");
					if (tryIndex >= triesCount - 1)
						throw;
					Log.Warn($"Попробую ещё раз (попытка {tryIndex + 2} из {triesCount})");
					await runAfterFail().ConfigureAwait(false);
				}
				catch (Exception e)
				{
					Log.Info($"На попытке {tryIndex + 1} произошло исключение {e.Message}");
					throw;
				}

			throw new Exception($"Can\'t run function {func} for {triesCount} times");
		}

		public static Task<T> TrySeveralTimesAsync<T>(Func<Task<T>> func, int triesCount, Func<Task> runAfterFail)
		{
			return TrySeveralTimesAsync(func, triesCount, runAfterFail, typeof(Exception));
		}

		public static Task<T> TrySeveralTimesAsync<T>(Func<Task<T>> func, int triesCount, Type exceptionType)
		{
			return TrySeveralTimesAsync(func, triesCount, () => Task.Delay(100), exceptionType);
		}

		public static Task<T> TrySeveralTimesAsync<T>(Func<Task<T>> func, int triesCount)
		{
			return TrySeveralTimesAsync(func, triesCount, () => Task.Delay(100));
		}

		public static Task TrySeveralTimesAsync(Func<Task> func, int triesCount)
		{
			return TrySeveralTimesAsync(async () =>
			{
				await func().ConfigureAwait(false);
				return 0;
			}, triesCount, () => Task.Delay(100));
		}

		public static T TrySeveralTimes<T>(Func<T> func, int triesCount, Action runAfterFail, Type exceptionType)
		{
			return TrySeveralTimesAsync(() => Task.Run(func), triesCount, () => Task.Run(runAfterFail), exceptionType).GetAwaiter().GetResult();
		}

		public static T TrySeveralTimes<T>(Func<T> func, int triesCount, Action runAfterFail)
		{
			return TrySeveralTimes(func, triesCount, runAfterFail, typeof(Exception));
		}

		public static T TrySeveralTimes<T>(Func<T> func, int triesCount, Type exceptionType)
		{
			return TrySeveralTimes(func, triesCount, () => { }, exceptionType);
		}

		public static T TrySeveralTimes<T>(Func<T> func, int triesCount)
		{
			return TrySeveralTimes(func, triesCount, () => { });
		}

		public static void TrySeveralTimes(Action func, int triesCount)
		{
			TrySeveralTimes(() =>
			{
				func();
				return 0;
			}, triesCount, () => { });
		}

		public static void Using<TDisposable>(TDisposable disposable, Action<TDisposable> body, Action<TDisposable> additionalDisposeAction = null)
			where TDisposable : IDisposable
		{
			try
			{
				body(disposable);
			}
			finally
			{
				additionalDisposeAction?.Invoke(disposable);
				disposable.Dispose();
			}
		}

		public static TOut Using<TDisposable, TOut>(TDisposable disposable, Func<TDisposable, TOut> body, Action<TDisposable> additionalDisposeAction = null)
			where TDisposable : IDisposable
		{
			try
			{
				return body(disposable);
			}
			finally
			{
				additionalDisposeAction?.Invoke(disposable);
				disposable.Dispose();
			}
		}

		public static IEnumerable<TOut> Using<TDisposable, TOut>(TDisposable disposable, Func<TDisposable, IEnumerable<TOut>> body, Action<TDisposable> additionalDisposeAction = null)
			where TDisposable : IDisposable
		{
			try
			{
				foreach (var result in body(disposable))
					yield return result;
			}
			finally
			{
				additionalDisposeAction?.Invoke(disposable);
				disposable.Dispose();
			}
		}
	}

	[TestFixture]
	public class FuncUtils_should
	{
		[Test]
		public void TrySeveralTimes_test()
		{
			var index = 0;
			FuncUtils.TrySeveralTimes<object>(() =>
				{
					index++;
					Console.WriteLine($"Run #{index}");
					if (index != 3)
						throw new Exception("exception");
					return null;
				},
				3,
				() => { Thread.Sleep(200); });
			Assert.AreEqual(index, 3);
		}

		[Test]
		public async Task TrySeveralTimesAsync_test()
		{
			var index = 0;
			await FuncUtils.TrySeveralTimesAsync(async () =>
			{
				index++;
				Console.WriteLine($"Run #{index}");
				if (index != 3)
					throw new Exception("exception");
				await Task.Delay(100);
				return (object)null;
			}, 3, async () => await Task.Delay(200));
			Assert.AreEqual(index, 3);
		}
	}
}