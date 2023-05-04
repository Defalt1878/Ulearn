using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Parameters.Groups.SuperGroup;

[DataContract]
public class ResortStudentRequestParameters
{
	/// <summary>
	///     Contains expected groups members after sorting
	/// </summary>
	[DataMember]
	public Dictionary<string, List<string>> StudentNamesByGroupsName { get; set; }
}