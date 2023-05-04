using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards
{
	[DataContract]
	public class UserFlashcardStatistics
	{
		[DataMember]
		public int GroupId;

		[DataMember]
		public string GroupName;

		[DataMember]
		public List<UnitUserStatistic> UnitUserStatistics;

		[DataMember]
		public string UserId;

		[DataMember]
		public string UserName;

		public UserFlashcardStatistics()
		{
			UnitUserStatistics = new List<UnitUserStatistic>();
		}
	}
}