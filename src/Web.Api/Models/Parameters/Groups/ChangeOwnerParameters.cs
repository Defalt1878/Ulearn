using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Parameters.Groups;

[DataContract]
public class ChangeOwnerParameters
{
	[DataMember(IsRequired = true)]
	public string OwnerId { get; set; }
}