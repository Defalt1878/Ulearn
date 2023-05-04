using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards
{
	[DataContract]
	public class FlashcardStatistic
	{
		[DataMember]
		public string FlashcardId;

		[DataMember]
		public TotalRateResponse Statistics;

		[DataMember]
		public int UniqueVisitCount;

		[DataMember]
		public Guid UnitId;

		[DataMember]
		public string UnitTitle;

		[DataMember]
		public int VisitCount;

		public FlashcardStatistic()
		{
			Statistics = new TotalRateResponse();
		}
	}
}