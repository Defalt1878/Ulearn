using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Notifications;

[DataContract]
public class NotificationListResponse : SuccessResponse
{
	[DataMember]
	public NotificationList Important { get; set; }

	[DataMember]
	public NotificationList Comments { get; set; }
}

[DataContract]
public class NotificationList
{
	[DataMember]
	public DateTime? LastViewTimestamp { get; set; }

	[DataMember]
	public List<NotificationInfo> Notifications { get; set; }
}

[DataContract]
public class NotificationInfo
{
	[DataMember]
	public int Id { get; set; }

	[DataMember]
	public string Type { get; set; }

	[DataMember]
	public ShortUserInfo Author { get; set; }

	[DataMember]
	public DateTime CreateTime { get; set; }

	[DataMember]
	public string CourseId { get; set; }

	[DataMember]
	public NotificationData Data { get; set; }
}

[DataContract]
public class NotificationData
{
	[DataMember(EmitDefaultValue = false)]
	public NotificationCommentInfo Comment { get; set; }
}

[DataContract]
public class NotificationCommentInfo
{
	[DataMember]
	public int Id { get; set; }

	[DataMember]
	public ShortUserInfo Author { get; set; }

	[DataMember]
	public string CourseId { get; set; }

	[DataMember]
	public Guid SlideId { get; set; }

	[DataMember]
	public string Text { get; set; }

	[DataMember]
	public DateTime PublishTime { get; set; }
}