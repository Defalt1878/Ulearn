using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace AntiPlagiarism.Api.Models.Results;

[DataContract]
public class GetSubmissionPlagiarismsResponse : SuccessResponse
{
	public GetSubmissionPlagiarismsResponse()
	{
		Plagiarisms = new List<Plagiarism>();
	}

	[DataMember(Name = "submission")]
	public SubmissionInfo SubmissionInfo { get; set; }

	[DataMember(Name = "plagiarisms")]
	public List<Plagiarism> Plagiarisms { get; set; }

	[DataMember(Name = "tokensPositions")]
	public List<TokenPosition> TokensPositions { get; set; }

	[DataMember(Name = "suspicionLevels")]
	public SuspicionLevels SuspicionLevels { get; set; }

	[DataMember(Name = "analyzedCodeUnits")]
	public List<AnalyzedCodeUnit> AnalyzedCodeUnits { get; set; }

	public override string GetShortLogString()
	{
		return new GetSubmissionPlagiarismsResponse
		{
			SubmissionInfo = SubmissionInfo.CloneWithoutCode(),
			SuspicionLevels = SuspicionLevels,
			Plagiarisms = Plagiarisms.Select(p => new Plagiarism
			{
				Weight = p.Weight,
				SubmissionInfo = p.SubmissionInfo.CloneWithoutCode()
			}).ToList()
		}.ToString();
	}
}