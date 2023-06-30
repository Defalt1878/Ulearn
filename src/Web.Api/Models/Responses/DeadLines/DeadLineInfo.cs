using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Responses.DeadLines
{
	[DataContract]
	public class DeadLineInfo
	{
		[DataMember]
		public Guid Id { get; set; }

		[DataMember]
		public DateTime Date { get; set; }

		[DataMember]
		public string CourseId { get; set; }

		[DataMember]
		public int GroupId { get; set; }

		[DataMember]
		public Guid UnitId { get; set; }

		[DataMember]
		public DeadLineSlideType SlideType { get; set; }
		
		[DataMember]
		public string? SlideValue { get; set; }

		[DataMember]
		public List<Guid>? UserIds { get; set; }

		[DataMember]
		public int ScorePercent { get; set; }

		public static DeadLineInfo BuildDeadLineInfo(DeadLine deadLine)
		{
			return new DeadLineInfo
			{
				Id = deadLine.Id,
				Date = deadLine.Date,
				CourseId = deadLine.CourseId,
				GroupId = deadLine.GroupId,
				UnitId = deadLine.UnitId,
				SlideType = deadLine.SlideType,
				SlideValue = deadLine.SlideValue,
				UserIds = deadLine.UserIds,
				ScorePercent = deadLine.ScorePercent,
			};
		}
	}
}