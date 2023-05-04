using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class FlashcardsStatistics
{
	[DataMember]
	public List<FlashcardStatistic> Statistics;

	public FlashcardsStatistics()
	{
		Statistics = new List<FlashcardStatistic>();
	}
}