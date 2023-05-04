using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ulearn.Core.Courses;

namespace Ulearn.Web.Api.Swagger;

public class RemoveCourseParameterOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var parametersWithCourseType = context.ApiDescription
			.ParameterDescriptions
			.Where(desc => desc.ParameterDescriptor.ParameterType == typeof(Course) || desc.ParameterDescriptor.ParameterType == typeof(ICourse))
			.ToList();

		parametersWithCourseType
			.ForEach(param =>
			{
				var toRemove = operation.Parameters.SingleOrDefault(p => p.Name == param.Name);

				if (toRemove is not null)
					operation.Parameters.Remove(toRemove);
			});
	}
}