using System;

namespace Ulearn.Common.Api;

public class ApiClientException : Exception
{
	public ApiClientException()
	{
	}

	public ApiClientException(string message)
		: base(message)
	{
	}

	public ApiClientException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}