using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks;

[DataContract]
public class SelfCheckupResponse
{
	[DataMember]
	public string Content { get; set; }

	[DataMember]
	public string Id { get; set; }

	[DataMember]
	public bool IsChecked { get; set; }
}