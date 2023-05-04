using System;
using System.Runtime.Serialization;
using Database.Models;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class FlashcardResponse
{
	[DataMember]
	public string Answer;

	[DataMember]
	public string Id;

	[DataMember]
	public int LastRateIndex;

	[DataMember]
	public string Question;

	[DataMember]
	public Rate Rate;

	[DataMember]
	public Guid[] TheorySlidesIds;

	[DataMember]
	public Guid UnitId;

	[DataMember]
	public string UnitTitle;
}