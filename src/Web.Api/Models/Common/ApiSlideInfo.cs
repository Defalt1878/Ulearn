using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Web.Api.Models.Responses.SlideBlocks;

namespace Ulearn.Web.Api.Models.Common;

[DataContract]
public class ApiSlideInfo : ShortSlideInfo
{
	[DataMember]
	public List<IApiSlideBlock> Blocks { get; set; }
}