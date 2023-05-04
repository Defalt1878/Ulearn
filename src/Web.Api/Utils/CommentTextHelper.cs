using Ulearn.Common.Api.Helpers;
using Ulearn.Common.Extensions;

namespace Ulearn.Web.Api.Utils;

public static class CommentTextHelper
{
	public static string RenderCommentTextToHtml(string commentText)
	{
		var encodedText = HtmlTransformations.EncodeMultiLineText(commentText);
		var renderedText = encodedText.RenderSimpleMarkdown();
		var textWithLinks = HtmlTransformations.HighlightLinks(renderedText);
		return textWithLinks;
	}
}