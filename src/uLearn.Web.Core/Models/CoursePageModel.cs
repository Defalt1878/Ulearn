﻿using Database.Models;
using Ulearn.Core.Courses.Slides;

namespace uLearn.Web.Core.Models;

public class CoursePageModel
{
	public string CourseId;
	public string UserId { get; set; }
	public string CourseTitle;
	public Slide Slide;
	public BlockRenderContext BlockRenderContext { get; set; }
	public AbstractManualSlideChecking ManualChecking { get; set; }
	public string ContextManualCheckingUserGroups { get; set; }
	public string ContextManualCheckingUserArchivedGroups { get; set; }
	public bool IsGuest { get; set; }
	public string Error { get; set; }
}