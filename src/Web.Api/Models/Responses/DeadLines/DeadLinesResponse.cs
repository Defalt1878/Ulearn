using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Responses;

namespace Ulearn.Web.Api.Models.Responses.DeadLines;

[DataContract]
public class DeadLinesResponse : SuccessResponse
{
	[DataMember]
	public List<DeadLineInfo> DeadLines;

	public static DeadLinesResponse BuildDeadLinesInfo(List<DeadLine> deadLines)
	{
		return new DeadLinesResponse
		{
			DeadLines = deadLines.Select(DeadLineInfo.BuildDeadLineInfo).ToList()
		};
	}
}