using System;

namespace Ulearn.Common.Api
{
	public class StatusCodeException : Exception
	{
		public StatusCodeException(int code, string message)
			: base(message)
		{
			Code = code;
		}

		public int Code { get; }
	}
}