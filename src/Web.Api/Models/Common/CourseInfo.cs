using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Common
{
	[DataContract]
	public class CourseInfo
	{
		[DataMember]
		public string Id { get; set; }

		[DataMember]
		public string Title { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public List<UnitInfo> Units { get; set; }

		[DataMember]
		public DateTime? NextUnitPublishTime { get; set; }

		[DataMember]
		public ScoringSettingsModel Scoring { get; set; }

		[DataMember]
		public bool ContainsFlashcards { get; set; }
		
		[DataMember]
		public bool IsTempCourse { get; set; }
		
		[DataMember]
		public string TempCourseError { get; set; }
	}

	public class ScoringSettingsModel
	{
		public List<ScoringGroupModel> Groups { get; set; }
	}

	public class ScoringGroupModel
	{
		[DataMember]
		public string Id { get; set; }
		
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string? Abbr { get; set; }

		[DataMember]
		public string? Description { get; set; }

		[DataMember]
		public decimal Weight { get; set; } = 1;
	}
}