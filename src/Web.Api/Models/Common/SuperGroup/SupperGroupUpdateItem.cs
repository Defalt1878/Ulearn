using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Common.SuperGroup;

[DataContract]
public class SupperGroupUpdateItem
{
	/// <summary>
	///     Null if group doesn't exists
	/// </summary>
	[DataMember]
	public int? GroupId { get; set; }

	[DataMember]
	public string GroupName { get; set; }
}