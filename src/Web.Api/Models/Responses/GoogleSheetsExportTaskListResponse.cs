using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses;

[DataContract]
public class GoogleSheetsExportTaskListResponse
{
	[DataMember]
	public List<GoogleSheetsExportTaskResponse> GoogleSheetsExportTasks { get; set; }
}