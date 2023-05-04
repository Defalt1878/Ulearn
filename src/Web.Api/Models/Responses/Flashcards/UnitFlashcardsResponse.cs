using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards
{
	[DataContract]
	public class UnitFlashcardsResponse
	{
		[DataMember]
		public List<FlashcardResponse> Flashcards;

		[DataMember]
		public Guid UnitId;

		[DataMember]
		public string UnitTitle;

		[DataMember]
		public bool Unlocked;

		public UnitFlashcardsResponse()
		{
			Flashcards = new List<FlashcardResponse>();
		}
	}
}