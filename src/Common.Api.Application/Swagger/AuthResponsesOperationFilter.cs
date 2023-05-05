using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ulearn.Common.Api.Swagger;

public class AuthResponsesOperationFilter : IOperationFilter
{
	public void Apply(OpenApiOperation operation, OperationFilterContext context)
	{
		var authAttributes = new List<AuthorizeAttribute>();
		if (context.ApiDescription.TryGetMethodInfo(out var methodInfo))
		{
			authAttributes.AddRange(methodInfo.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType<AuthorizeAttribute>());
			var controllerAttributes = methodInfo.DeclaringType?.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType<AuthorizeAttribute>();
			if (controllerAttributes != null)
				authAttributes.AddRange(controllerAttributes);
		}

		if (authAttributes.Any())
			operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized. Authorization header is not set or JWT bearer token is invalid" });
	}
}