using Microsoft.AspNetCore.Mvc;
using Ulearn.Core.Courses.Slides;

namespace Ulearn.Web.Api.Controllers.Slides;

public class SlideRenderContext
{
	public readonly string CourseId;
	public readonly Slide Slide;
	public readonly string BaseUrlApi;
	public readonly string BaseUrlWeb;
	public readonly bool RemoveHiddenBlocks;
	public readonly string VideoAnnotationsGoogleDoc;
	public readonly IUrlHelper UrlHelper;
	public readonly string UserId;

	public SlideRenderContext(string courseId, Slide slide, string userId, string baseUrlApi, string baseUrlWeb, bool removeHiddenBlocks,
		string videoAnnotationsGoogleDoc, IUrlHelper urlHelper)
	{
		CourseId = courseId;
		Slide = slide;
		BaseUrlApi = baseUrlApi;
		BaseUrlWeb = baseUrlWeb;
		RemoveHiddenBlocks = removeHiddenBlocks;
		VideoAnnotationsGoogleDoc = videoAnnotationsGoogleDoc;
		UrlHelper = urlHelper;
		UserId = userId;
	}
}