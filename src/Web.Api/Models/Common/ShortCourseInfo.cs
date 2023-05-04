using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Common;

[DataContract]
public class ShortCourseInfo
{
	[DataMember]
	public string Id { get; set; }

	[DataMember]
	public string Title { get; set; }

	[DataMember]
	public string ApiUrl { get; set; }

	[DataMember]
	public bool IsTempCourse { get; set; }

	[DataMember]
	public DateTime? Timestamp { get; set; }
}