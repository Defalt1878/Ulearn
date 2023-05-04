using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards
{
	[DataContract]
	public class UserFlashcardStatisticResponse
	{
		[DataMember]
		public List<UserFlashcardStatistics> UsersFlashcardsStatistics;

		public UserFlashcardStatisticResponse()
		{
			UsersFlashcardsStatistics = new List<UserFlashcardStatistics>();
		}
	}
}