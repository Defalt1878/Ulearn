using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Common.SuperGroup;

[DataContract]
public class SuperGroupItemInfo
{
	[DataMember]
	public SuperGroupItemActions? NeededAction { get; set; }

	/// <summary>Null if group is not created</summary>
	[DataMember]
	public int? GroupId { get; set; }

	/// <summary>Students which should be in group. Null if group should be deleted</summary>
	[DataMember]
	public List<string>? StudentNames { get; set; }

	/// <summary>Students which have joined group. Not null if group status is created</summary>
	[DataMember]
	public List<GroupMemberInfo>? JoinedStudents { get; set; }
}