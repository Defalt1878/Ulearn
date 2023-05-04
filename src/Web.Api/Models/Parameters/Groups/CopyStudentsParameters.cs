using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Parameters.Groups;

[DataContract]
public class CopyStudentsParameters
{
	[DataMember(IsRequired = true)]
	public List<string> StudentIds { get; set; }
}