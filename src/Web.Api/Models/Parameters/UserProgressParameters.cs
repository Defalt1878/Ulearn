using System.Collections.Generic;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Ulearn.Web.Api.Models.Parameters;

[DataContract]
public class UserProgressParameters
{
	[DataMember]
	[CanBeNull]
	public List<string> UserIds { get; set; }
}