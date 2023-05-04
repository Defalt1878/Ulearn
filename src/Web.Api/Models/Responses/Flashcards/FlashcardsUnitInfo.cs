using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards
{
	[DataContract]
	public class FlashcardsUnitInfo
	{
		[DataMember]
		public int CardsCount;

		[DataMember]
		public Guid UnitId;

		[DataMember]
		public string UnitTitle;

		[DataMember]
		public bool Unlocked;
	}
}