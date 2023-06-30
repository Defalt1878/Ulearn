using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Database
{
	public abstract class AbstractFilterOptionByCourseAndUsers
	{
		public string CourseId { get; set; }

		/* If true, search only users which ids IS NOT in UsersIds*/
		public bool IsUserIdsSupplement { get; set; }

		public List<string> UserIds { get; set; }
	}

	public class ManualCheckingQueueFilterOptionsObsolete : AbstractFilterOptionByCourseAndUsers
	{
		public ManualCheckingQueueFilterOptionsObsolete()
		{
			OnlyChecked = false;

			From = DateTime.MinValue;
			To = DateTime.MaxValue;
		}

		public IEnumerable<Guid> SlidesIds { get; set; }
		public bool? OnlyChecked { get; set; }
		public int Count { get; set; }

		public DateTime From { get; set; }
		public DateTime To { get; set; }
	}

	public class ManualCheckingQueueFilterOptions : AbstractFilterOptionByCourseAndUsers
	{
		public ManualCheckingQueueFilterOptions()
		{
			IsReviewed = false;
			DateSort = DateSort.Ascending;
		}

		[CanBeNull]
		public Guid[] SlideIds { get; set; }

		public bool IsReviewed { get; set; }

		public DateSort DateSort { get; set; }

		public int Count { get; set; }
	}

	public enum DateSort
	{
		Ascending,
		Descending
	}

	public class VisitsFilterOptions : AbstractFilterOptionByCourseAndUsers
	{
		public List<Guid> SlidesIds { get; set; }
		public DateTime PeriodStart { get; set; }
		public DateTime PeriodFinish { get; set; }

		public VisitsFilterOptions WithPeriodStart(DateTime newPeriodStart)
		{
			var copy = (VisitsFilterOptions)MemberwiseClone();
			copy.PeriodStart = newPeriodStart;
			return copy;
		}

		public VisitsFilterOptions WithPeriodFinish(DateTime newPeriodFinish)
		{
			var copy = (VisitsFilterOptions)MemberwiseClone();
			copy.PeriodFinish = newPeriodFinish;
			return copy;
		}

		public VisitsFilterOptions WithSlidesIds(List<Guid> newSlidesIds)
		{
			var copy = (VisitsFilterOptions)MemberwiseClone();
			copy.SlidesIds = newSlidesIds;
			return copy;
		}
	}

	public class SubmissionsFilterOptions : AbstractFilterOptionByCourseAndUsers
	{
		public IEnumerable<Guid> SlideIds { get; set; }
	}
}