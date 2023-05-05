using System;
using System.IO;
using System.Threading.Tasks;

namespace Ulearn.Common;

public class AsyncReader
{
	private const int maxNotLohLength = 85000 / 2; // При больше 85000 байт, объект попадает в LOH. Делим на 2, т.к. char всегда 2 байта.
	private readonly Task<int> readerTask;
	private char[] buffer;

	public AsyncReader(StreamReader reader, int limit)
	{
		readerTask = ReaderTask(reader, limit);
	}

	private bool IsCompleted => readerTask.IsCompleted;

	public int ReadLength => IsCompleted ? readerTask.Result : -1;

	private async Task<int> ReaderTask(StreamReader reader, int limit)
	{
		var smallLimit = Math.Min(limit, maxNotLohLength);
		buffer = new char[smallLimit];
		var resultLength = await reader.ReadBlockAsync(buffer, 0, smallLimit);
		if (resultLength < maxNotLohLength - 1)
			return resultLength;
		var bigBuffer = new char[limit];
		Array.Copy(buffer, bigBuffer, resultLength);
		buffer = bigBuffer;
		var bigLength = await reader.ReadBlockAsync(buffer, resultLength, limit - resultLength);
		return resultLength + bigLength;
	}

	public async Task<string> GetDataAsync()
	{
		var length = await readerTask;
		return new string(buffer, 0, length);
	}
}