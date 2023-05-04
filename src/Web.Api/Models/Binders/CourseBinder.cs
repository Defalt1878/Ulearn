using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;

namespace Ulearn.Web.Api.Models.Binders;

/*
* This class allows to pass courses to actions like
* public void IActionResult GetCourseInfo(Course course) {}
* and call them i.e. as /GetCourseInfo?courseId=BasicProgramming or /courses/BasicProgramming
*
* So it converts 'courseId' parameter (name can be overriden) to Course instance loaded from courseManager.
* See https://docs.microsoft.com/ru-ru/aspnet/core/mvc/advanced/custom-model-binding for details
*/
public class CourseBinder : IModelBinder
{
	private readonly ICourseStorage courseStorage;

	public CourseBinder(ICourseStorage courseStorage)
	{
		this.courseStorage = courseStorage;
	}

	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext is null)
			throw new ArgumentNullException(nameof(bindingContext));

		// Specify a default argument name if none is set by ModelBinderAttribute
		var modelName = bindingContext.BinderModelName;
		if (string.IsNullOrEmpty(modelName))
			modelName = "courseId";

		// Try to fetch the value of the argument by name
		var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

		if (valueProviderResult == ValueProviderResult.None)
			return Task.CompletedTask;

		bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
		var value = valueProviderResult.FirstValue;

		// Check if the argument value is null or empty
		if (string.IsNullOrEmpty(value))
			return Task.CompletedTask;

		var model = courseStorage.FindCourse(value);
		if (model is null)
			bindingContext.ModelState.TryAddModelError(modelName, $"Course {value} not found");
		bindingContext.Result = model is null ? ModelBindingResult.Failed() : ModelBindingResult.Success(model);
		return Task.CompletedTask;
	}
}

public class CourseBinderProvider : IModelBinderProvider
{
	public IModelBinder GetBinder(ModelBinderProviderContext context)
	{
		if (context is null)
			throw new ArgumentNullException(nameof(context));

		if (context.Metadata.ModelType == typeof(Course) || context.Metadata.ModelType == typeof(ICourse))
			return new BinderTypeModelBinder(typeof(CourseBinder));

		return null;
	}
}