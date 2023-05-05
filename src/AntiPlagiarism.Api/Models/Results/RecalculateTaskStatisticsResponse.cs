using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common;
using Ulearn.Common.Api.Models.Responses;

namespace AntiPlagiarism.Api.Models.Results;

[DataContract]
public class RecalculateTaskStatisticsResponse : SuccessResponse
{
	[DataMember(Name = "taskIds")]
	public List<Guid> TaskIds { get; set; }

	public Language Language { get; set; }

	[DataMember(Name = "weights")]
	public Dictionary<Guid, List<double>> Weights { get; set; }
}