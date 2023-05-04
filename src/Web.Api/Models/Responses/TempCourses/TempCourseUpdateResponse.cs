using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ulearn.Web.Api.Models.Responses.TempCourses;

[DataContract]
public class TempCourseUpdateResponse
{
	[DataMember]
	public string Message;

	[DataMember]
	public ErrorType ErrorType;

	[DataMember]
	public DateTime LastUploadTime;
}

[JsonConverter(typeof(StringEnumConverter), true)]
public enum ErrorType
{
	NoErrors,
	Forbidden,
	Conflict,
	NotFound,
	CourseError
}