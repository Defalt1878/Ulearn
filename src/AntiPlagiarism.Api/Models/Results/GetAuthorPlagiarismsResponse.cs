using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;

namespace AntiPlagiarism.Api.Models.Results;

[DataContract]
public class GetAuthorPlagiarismsResponse : SuccessResponse
{
	[DataMember(Name = "submissions")]
	public List<ResearchedSubmission> ResearchedSubmissions { get; set; }

	[DataMember(Name = "suspicionLevels")]
	public SuspicionLevels SuspicionLevels { get; set; }

	public GetAuthorPlagiarismsResponse()
	{
		ResearchedSubmissions = new List<ResearchedSubmission>();
	}

	public override string GetShortLogString()
	{
		return new GetAuthorPlagiarismsResponse
		{
			ResearchedSubmissions = ResearchedSubmissions.Select(s =>
			{
				return new ResearchedSubmission
				{
					SubmissionInfo = s.SubmissionInfo.CloneWithoutCode(),
					Plagiarisms = s.Plagiarisms.Select(p => new Plagiarism
					{
						Weight = p.Weight,
						SubmissionInfo = p.SubmissionInfo.CloneWithoutCode(),
					}).ToList()
				};
			}).ToList(),
			SuspicionLevels = SuspicionLevels,
		}.ToString();
	}
}

[DataContract]
public class ResearchedSubmission
{
	[DataMember(Name = "submission")]
	public SubmissionInfo SubmissionInfo { get; set; }

	[DataMember(Name = "plagiarisms")]
	public List<Plagiarism> Plagiarisms { get; set; }

	[DataMember(Name = "tokensPositions")]
	public List<TokenPosition> TokensPositions { get; set; }

	[DataMember(Name = "analyzedCodeUnits")]
	public List<AnalyzedCodeUnit> AnalyzedCodeUnits { get; set; }
}