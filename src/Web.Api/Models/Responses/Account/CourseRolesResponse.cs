using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Account;

[DataContract]
public class CourseRolesResponse : SuccessResponse
{
	[DataMember]

	public bool IsSystemAdministrator { get; set; }

	[DataMember]
	public List<CourseRoleResponse> CourseRoles { get; set; }

	[DataMember]
	public List<CourseAccessResponse> CourseAccesses { get; set; }

	[DataMember]
	public List<ShortGroupInfo> GroupsAsStudent { get; set; }
}

[DataContract]
public class CourseRoleResponse
{
	[DataMember]
	public string CourseId { get; set; }

	[DataMember]
	public CourseRoleType Role { get; set; }
}

[DataContract]
public class CourseAccessResponse
{
	[DataMember]
	public string CourseId { get; set; }

	[DataMember]
	public List<CourseAccessType> Accesses { get; set; }
}