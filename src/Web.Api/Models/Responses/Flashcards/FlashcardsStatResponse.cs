using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class FlashcardsStatResponse
{
	[DataMember]
	public int TotalFlashcardsCount;

	[DataMember(Name = "statistics")]
	public TotalRateResponse TotalRateResponse;
}