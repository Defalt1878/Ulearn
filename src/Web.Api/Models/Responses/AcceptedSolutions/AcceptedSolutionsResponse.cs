using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Ulearn.Common;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.AcceptedSolutions
{
	[DataContract]
	public class AcceptedSolutionsResponse
	{
		// Списки содержат разные решения
		[DataMember]
		public List<AcceptedSolution> PromotedSolutions { get; set; }

		[DataMember]
		public List<AcceptedSolution> RandomLikedSolutions { get; set; }

		[DataMember]
		public List<AcceptedSolution> NewestSolutions { get; set; }
	}

	[DataContract]
	public class LikedAcceptedSolutionsResponse
	{
		[DataMember]
		public List<AcceptedSolution> LikedSolutions { get; set; }
	}

	[DataContract]
	public class AcceptedSolution
	{
		public AcceptedSolution(int submissionId, string code, Language language, int? likesCount, bool? likedByMe, [CanBeNull] ShortUserInfo promotedBy)
		{
			SubmissionId = submissionId;
			Code = code;
			Language = language;
			LikesCount = likesCount;
			LikedByMe = likedByMe;
			PromotedBy = promotedBy;
		}

		[DataMember]
		public int SubmissionId { get; set; }

		[DataMember]
		public string Code { get; set; }

		[DataMember]
		public Language Language { get; set; }

		[DataMember]
		[CanBeNull]
		public int? LikesCount { get; set; }

		[DataMember]
		[CanBeNull]
		public bool? LikedByMe { get; set; }

		[DataMember(EmitDefaultValue = false)]
		[CanBeNull]
		public ShortUserInfo PromotedBy { get; set; }
	}
}