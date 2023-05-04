using System.ComponentModel;
using System.Runtime.Serialization;
using Ulearn.Core.Courses.Slides.Blocks;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks
{
	[DataContract]
	[DisplayName("tex")]
	public class TexBlockResponse : IApiSlideBlock
	{
		public TexBlockResponse(TexBlock texBlock)
		{
			Hide = texBlock.Hide;
			TexLines = texBlock.TexLines;
		}

		[DataMember(Name = "lines")]
		public string[] TexLines { get; set; }

		[DefaultValue(false)]
		[DataMember(Name = "hide", EmitDefaultValue = false)]
		public bool Hide { get; set; }
	}
}