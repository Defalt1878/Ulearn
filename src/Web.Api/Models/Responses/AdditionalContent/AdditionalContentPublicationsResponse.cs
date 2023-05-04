using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.AdditionalContent;

[DataContract]
public class AdditionalContentPublicationsResponse
{
	[DataMember]
	public List<AdditionalContentPublicationResponse> Publications;
}