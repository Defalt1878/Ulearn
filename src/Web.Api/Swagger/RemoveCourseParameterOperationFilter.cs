using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ulearn.Core.Courses;

namespace Ulearn.Web.Api.Swagger;

public class RemoveCourseParameterOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		// При использовании CourseBinder (конвертация courseId в Course),
		// Swagger разворачивает Course, создавая множество параметров, а также добавляет сам courseId из route, которого на деле в аргументах,
		// в итоге мы получаем null в ParameterDescriptor
		// ReSharper disable once ConditionIsAlwaysTrueOrFalse
		var parametersToDiscard = context.ApiDescription
			.ParameterDescriptions
			.Where(desc =>
				desc.ParameterDescriptor is null ||
				desc.ParameterDescriptor.ParameterType == typeof(Course) ||
				desc.ParameterDescriptor.ParameterType == typeof(ICourse)
			)
			.ToList();

		parametersToDiscard
			.ForEach(param =>
			{
				var toRemove = operation.Parameters.SingleOrDefault(p => p.Name == param.Name);

				if (toRemove is not null)
					operation.Parameters.Remove(toRemove);
			});
	}
}