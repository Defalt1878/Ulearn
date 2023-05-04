using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class GroupAccessesResponse : SuccessResponse
{
	[DataMember]
	public List<GroupAccessesInfo> Accesses { get; set; }
}

[DataContract]
public class GroupAccessesInfo
{
	[DataMember]
	public ShortUserInfo User { get; set; }

	[DataMember]
	public GroupAccessType AccessType { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public ShortUserInfo GrantedBy { get; set; }

	[DataMember]
	public DateTime GrantTime { get; set; }
}