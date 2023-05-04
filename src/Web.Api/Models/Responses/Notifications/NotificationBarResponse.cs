using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Notifications;

[DataContract]
public class NotificationBarResponse : SuccessResponse
{
	[DataMember]
	public string Message { get; set; }

	[DataMember]
	public bool Force { get; set; }

	[DataMember]
	public bool Overlap { get; set; }
}