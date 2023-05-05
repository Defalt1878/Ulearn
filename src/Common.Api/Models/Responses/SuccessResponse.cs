using System.Runtime.Serialization;

namespace Ulearn.Common.Api.Models.Responses
{
	[DataContract]
	public class SuccessResponse : ApiResponse
	{
		[DataMember(Name = "status", Order = 0)]
		public ResponseStatus Status { get; } = ResponseStatus.Ok;
	}

	[DataContract]
	public class SuccessResponseWithMessage : SuccessResponse
	{
		public SuccessResponseWithMessage(string message)
		{
			Message = message;
		}

		[DataMember(Name = "message", EmitDefaultValue = false, Order = 0)]
		public string Message { get; set; }
	}
}