using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class GroupSettings : ChangeableGroupSettings
{
	[DataMember]
	public int Id { get; set; }

	[DataMember]
	public string CourseTitle { get; set; }

	[DataMember]
	public string CourseId { get; set; }

	[DataMember]
	public GroupType GroupType { get; set; }

	[DataMember]
	public DateTime? CreateTime { get; set; }

	[DataMember]
	public string Name { get; set; }

	[DataMember]
	public bool IsArchived { get; set; }

	[DataMember]
	public ShortUserInfo Owner { get; set; }

	[DataMember]
	public Guid InviteHash { get; set; }

	[DataMember]
	public bool IsInviteLinkEnabled { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public bool? AreYouStudent { get; set; }


	[DataMember(EmitDefaultValue = false)]
	public int? StudentsCount { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public List<GroupAccessesInfo> Accesses { get; set; }


	[DataMember(EmitDefaultValue = false)]
	public string ApiUrl { get; set; }

	[DataMember]
	public int? SuperGroupId { get; set; }

	[DataMember]
	public string SuperGroupName { get; set; }

	[DataMember]
	public string DistributionTableLink { get; set; }
}

[DataContract]
public class ChangeableGroupSettings : SuccessResponse
{
	[DataMember]
	public bool? IsManualCheckingEnabled { get; set; }

	[DataMember]
	public bool? IsManualCheckingEnabledForOldSolutions { get; set; }

	[DataMember]
	public bool? DefaultProhibitFurtherReview { get; set; }

	[DataMember]
	public bool? CanStudentsSeeGroupProgress { get; set; }
}