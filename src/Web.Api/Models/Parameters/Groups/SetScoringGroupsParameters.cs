using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Parameters;

namespace Ulearn.Web.Api.Models.Parameters.Groups;

[DataContract]
public class SetScoringGroupsParameters : ApiParameters
{
	/// <summary>
	///     ScoringGroupIds
	/// </summary>
	[DataMember(IsRequired = true)]
	public List<string> Scores { get; set; }
}