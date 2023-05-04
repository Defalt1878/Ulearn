using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Common;

[DataContract]
public class AdditionalContent
{
	[DataMember]
	public bool IsAdditionalContent { get; set; }

	[DataMember]
	[CanBeNull]
	public string PublicationDate { get; set; }
}