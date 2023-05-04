using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Web.Api.Models.Common.SuperGroup;

namespace Ulearn.Web.Api.Models.Responses.Groups;

[DataContract]
public class SuperGroupSheetExtractionResult
{
	[DataMember]
	public List<ValidatingResult> ValidatingResults { get; set; }

	[DataMember]
	public Dictionary<string, SuperGroupItemInfo> Groups { get; set; }
}