using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.Courses;

[DataContract]
public class CoursesListResponse : SuccessResponse
{
	[DataMember]
	public List<ShortCourseInfo> Courses { get; set; }
}