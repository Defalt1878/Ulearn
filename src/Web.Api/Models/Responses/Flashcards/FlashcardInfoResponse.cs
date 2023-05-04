using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class FlashcardInfoResponse
{
	[DataMember]
	public List<FlashcardsUnitInfo> FlashcardsInfo = new();
}