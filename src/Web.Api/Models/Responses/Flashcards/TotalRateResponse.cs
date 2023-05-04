using System.Runtime.Serialization;
using Database.Models;

namespace Ulearn.Web.Api.Models.Responses.Flashcards;

[DataContract]
public class TotalRateResponse
{
	[DataMember]
	public int Rate1;

	[DataMember]
	public int Rate2;

	[DataMember]
	public int Rate3;

	[DataMember]
	public int Rate4;

	[DataMember]
	public int Rate5;


	public void Add(Rate rate)
	{
		switch (rate)
		{
			case Rate.Rate1:
				Rate1++;
				break;
			case Rate.Rate2:
				Rate2++;
				break;
			case Rate.Rate3:
				Rate3++;
				break;
			case Rate.Rate4:
				Rate4++;
				break;
			case Rate.Rate5:
				Rate5++;
				break;
		}
	}
}