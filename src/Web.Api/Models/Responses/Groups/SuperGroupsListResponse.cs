using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class SuperGroupsListResponse : PaginatedResponse
{
	[DataMember]
	public List<GroupSettings> SuperGroups { get; set; }

	[DataMember]
	public Dictionary<int, List<GroupSettings>> SubGroupsBySuperGroupId { get; set; }
}