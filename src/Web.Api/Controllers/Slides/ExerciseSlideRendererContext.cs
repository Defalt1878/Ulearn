using System.Collections.Generic;
using Ulearn.Core.Markdown;
using Ulearn.Web.Api.Models.Responses.Exercise;
using Ulearn.Web.Api.Models.Responses.SlideBlocks;

namespace Ulearn.Web.Api.Controllers.Slides;

public class ExerciseSlideRendererContext
{
	public ExerciseAttemptsStatistics AttemptsStatistics;
	public bool CanSeeCheckerLogs;
	public MarkdownRenderContext markdownRenderContext;
	public List<SelfCheckupResponse> Checkups;
}