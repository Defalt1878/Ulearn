using AntiPlagiarism.Api.Models.Results;
using AntiPlagiarism.Web.Database.Models;

namespace AntiPlagiarism.Web.Extensions;

public static class SubmissionExtensions
{
	public static SubmissionInfo GetSubmissionInfoForApi(this Submission submission)
	{
		/* We do submission.ProgramText.TrimStart() because of issue in way of passing code ot codemirror on ulearn's frontend. We insert data into <textarea> which loses first spaces.
			We can remove it after migrating to new, React-based frontend. */
		return new SubmissionInfo
		{
			AntiplagiarismId = submission.Id,
			TaskId = submission.TaskId,
			Language = submission.Language,
			AuthorId = submission.AuthorId,
			Code = submission.ProgramText.TrimStart(),
			AdditionalInfo = submission.AdditionalInfo,
			ClientSubmissionId = submission.ClientSubmissionId
		};
	}
}