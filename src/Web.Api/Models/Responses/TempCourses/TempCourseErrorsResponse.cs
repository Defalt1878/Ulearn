using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.TempCourses;

[DataContract]
public class TempCourseErrorsResponse : SuccessResponse
{
	[DataMember]
	public string TempCourseError { get; set; }
}