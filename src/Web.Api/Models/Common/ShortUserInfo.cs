using System.Runtime.Serialization;
using Ulearn.Common;

namespace Ulearn.Web.Api.Models.Common;

[DataContract]
public class ShortUserInfo
{
	[DataMember]
	public string Id { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public string Login { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public string Email { get; set; }

	[DataMember]
	public string FirstName { get; set; }

	[DataMember]
	public string LastName { get; set; }

	[DataMember]
	public string VisibleName { get; set; }

	[DataMember]
	public string AvatarUrl { get; set; }

	[DataMember]
	public Gender? Gender { get; set; }
}