using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ulearn.Common.Api.Swagger;

public class BadRequestResponseOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		if (context.ApiDescription.ParameterDescriptions.Any())
			operation.Responses.Add("400", new OpenApiResponse { Description = "Bad request. Invalid parameters in url or body" });
	}
}