using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class GroupStudentsResponse : SuccessResponse
{
	[DataMember]
	public List<GroupStudentInfo> Students { get; set; }
}

[DataContract]
public class GroupStudentInfo
{
	[DataMember]
	public ShortUserInfo User { get; set; }

	[DataMember]
	public DateTime? AddingTime { get; set; }
}