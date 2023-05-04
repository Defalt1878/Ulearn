using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Core.Courses;

namespace Ulearn.Web.Api.Models.Responses.Groups
{
	[DataContract]
	public class GroupScoringGroupsResponse : SuccessResponse
	{
		[DataMember]
		public List<GroupScoringGroupInfo> Scores { get; set; }
	}

	[DataContract]
	public abstract class AbstractScoringGroupInfo
	{
		public AbstractScoringGroupInfo(ScoringGroup scoringGroup)
		{
			Id = scoringGroup.Id;
			Name = scoringGroup.Name ?? "";
			Abbreviation = scoringGroup.Abbreviation ?? "";
			Description = scoringGroup.Description ?? "";
			Weight = scoringGroup.Weight;
		}

		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string Abbreviation { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		// ReSharper disable once MemberInitializerValueIgnored
		public decimal Weight { get; set; } = 1;
	}

	[DataContract]
	public class GroupScoringGroupInfo : AbstractScoringGroupInfo
	{
		public GroupScoringGroupInfo(ScoringGroup scoringGroup)
			: base(scoringGroup)
		{
		}

		[DataMember]
		public bool AreAdditionalScoresEnabledForAllGroups { get; set; }

		[DataMember]
		public bool CanInstructorSetAdditionalScoreInSomeUnit { get; set; }

		[DataMember]
		public bool? AreAdditionalScoresEnabledInThisGroup { get; set; }
	}

	[DataContract]
	public class UnitScoringGroupInfo : AbstractScoringGroupInfo
	{
		public UnitScoringGroupInfo(ScoringGroup scoringGroup)
			: base(scoringGroup)
		{
			MaxAdditionalScore = scoringGroup.MaxAdditionalScore;
			CanInstructorSetAdditionalScore = scoringGroup.CanBeSetByInstructor;
		}

		[DataMember]
		public bool CanInstructorSetAdditionalScore { get; set; }

		[DataMember]
		public int MaxAdditionalScore { get; set; }
	}
}