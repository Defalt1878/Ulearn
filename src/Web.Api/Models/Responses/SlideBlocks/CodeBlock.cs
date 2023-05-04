using System.ComponentModel;
using System.Runtime.Serialization;
using Ulearn.Common;
using Ulearn.Core.Courses.Slides.Blocks;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks
{
	[DataContract]
	[DisplayName("code")]
	public class CodeBlockResponse : IApiSlideBlock
	{
		public CodeBlockResponse(CodeBlock codeBlock)
		{
			Hide = codeBlock.Hide;
			Code = codeBlock.Code;
			Language = codeBlock.Language;
		}

		public CodeBlockResponse()
		{
		}

		[DataMember(Name = "code")]
		public string Code { get; set; }

		[DataMember(Name = "language")]
		public Language? Language { get; set; }

		[DefaultValue(false)]
		[DataMember(Name = "hide", EmitDefaultValue = false)]
		public bool Hide { get; set; }
	}
}