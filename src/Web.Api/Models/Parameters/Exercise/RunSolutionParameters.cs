using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Parameters.Exercise;

[DataContract]
public class RunSolutionParameters
{
	[DataMember(IsRequired = true)]
	public string Solution { get; set; }
}