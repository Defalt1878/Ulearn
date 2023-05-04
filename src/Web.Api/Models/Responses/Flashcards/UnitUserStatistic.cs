using System;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class UnitUserStatistic
{
	[DataMember]
	public int Rate5Count;

	[DataMember]
	public int TotalFlashcardsCount;

	[DataMember]
	public int TotalFlashcardVisits;

	[DataMember]
	public int UniqueFlashcardVisits;

	[DataMember]
	public Guid UnitId;

	[DataMember]
	public string UnitTitle;
}