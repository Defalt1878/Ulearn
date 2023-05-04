using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class FlashcardResponseByUnits
{
	[DataMember]
	public List<UnitFlashcardsResponse> Units = new();
}