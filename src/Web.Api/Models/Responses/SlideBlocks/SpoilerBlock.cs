using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Ulearn.Core.Courses.Slides.Blocks;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks;

[DataContract]
[DisplayName("spoiler")]
public class SpoilerBlockResponse : IApiSlideBlock
{
	public SpoilerBlockResponse(SpoilerBlock spoilerBlock, List<IApiSlideBlock> innerBlocks)
	{
		Hide = spoilerBlock.Hide;
		Text = spoilerBlock.Text;
		HideQuizButton = spoilerBlock.HideQuizButton;
		InnerBlocks = innerBlocks;
	}

	[DataMember]
	public string Text { get; set; }

	[DataMember(Name = "hideQuizButton", EmitDefaultValue = false)]
	public bool HideQuizButton { get; set; }

	[DataMember(Name = "blocks")]
	public List<IApiSlideBlock> InnerBlocks { get; set; }

	[DefaultValue(false)]
	[DataMember(Name = "hide", EmitDefaultValue = false)]
	public bool Hide { get; set; }
}