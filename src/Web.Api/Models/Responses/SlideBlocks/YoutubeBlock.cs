using System.ComponentModel;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.VideoAnnotations.Api.Models.Responses.Annotations;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks;

[DataContract]
[DisplayName("youtube")]
public class YoutubeBlockResponse : IApiSlideBlock
{
	public YoutubeBlockResponse(YoutubeBlock youtubeBlock, [CanBeNull] Annotation annotation, [CanBeNull] string googleDocLink)
	{
		Hide = youtubeBlock.Hide;
		VideoId = youtubeBlock.VideoId;
		Annotation = annotation;
		GoogleDocLink = googleDocLink;
	}

	[XmlText]
	[DataMember(Name = "videoId")]
	public string VideoId { get; set; }

	[DataMember(Name = "annotation")]
	[CanBeNull]
	public Annotation Annotation { get; set; }

	[DataMember(Name = "googleDocLink")]
	[CanBeNull]
	public string GoogleDocLink { get; set; }

	[DefaultValue(false)]
	[DataMember(Name = "hide", EmitDefaultValue = false)]
	public bool Hide { get; set; }
}