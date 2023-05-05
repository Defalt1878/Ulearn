using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ulearn.Common.Api.Swagger;

public class PolymorphismDocumentFilter<T> : IDocumentFilter
{
	public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
	{
		var schemaGenerator = context.SchemaGenerator;
		var derivedTypes = DerivedTypesHelper.GetDerivedTypes(typeof(T));
		foreach (var type in derivedTypes)
			schemaGenerator.GenerateSchema(type, context.SchemaRepository);
	}
}