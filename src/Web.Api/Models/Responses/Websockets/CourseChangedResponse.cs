using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Websockets;

[DataContract]
public class CourseChangedResponse
{
	[DataMember(Name = "courseId")]
	public string CourseId { get; set; }
}