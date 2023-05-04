using System;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Notifications;

[DataContract]
public class NotificationsCountResponse : SuccessResponse
{
	[DataMember]
	public int Count { get; set; }

	[DataMember]
	public DateTime? LastTimestamp { get; set; }
}