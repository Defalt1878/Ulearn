using System;
using System.Runtime.Serialization;
using Vostok.Context;
using Vostok.Tracing.Abstractions;

namespace Ulearn.Common.Api.Models.Responses
{
	[DataContract]
	public class ErrorResponse : ApiResponse
	{
		public ErrorResponse(string message)
		{
			Message = message;
		}

		[DataMember(Name = "status")]
		public ResponseStatus Status { get; } = ResponseStatus.Error;

		[DataMember(Name = "message")]
		public string Message { get; set; }

		[DataMember(Name = "traceId")]
		public Guid TraceId => FlowingContext.Globals.Get<TraceContext>()!.TraceId;

		[DataMember(Name = "timestamp")]
		public DateTime Timestamp { get; } = DateTime.Now;
	}
}