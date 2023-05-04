using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Parameters.Exercise;

[DataContract]
public class PythonVisualizerRunParameters
{
	[DataMember(Name = "code")]
	public string Code { get; set; }

	[DataMember(Name = "inputData")]
	public string InputData { get; set; }
}