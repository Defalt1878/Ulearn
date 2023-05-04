using System.ComponentModel;
using System.Runtime.Serialization;
using Ulearn.Core.Courses.Slides.Blocks;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks
{
	[DataContract]
	[DisplayName("html")]
	public class HtmlBlockResponse : IApiSlideBlock
	{
		public HtmlBlockResponse(HtmlBlock htmlBlock, bool fromMarkdown, string ulearnBaseUrlApi)
		{
			Hide = htmlBlock.Hide;
			Content = htmlBlock.GetContent(ulearnBaseUrlApi);
			FromMarkdown = fromMarkdown;
		}

		public HtmlBlockResponse()
		{
		}

		[DataMember(Name = "content")]
		public string Content { get; set; }

		[DataMember(Name = "fromMarkdown")]
		public bool FromMarkdown { get; set; }

		[DefaultValue(false)]
		[DataMember(Name = "hide", EmitDefaultValue = false)]
		public bool Hide { get; set; }
	}
}