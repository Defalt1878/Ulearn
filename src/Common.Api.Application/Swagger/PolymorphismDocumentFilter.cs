using System;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Ulearn.Common.Api.Swagger;

public class PolymorphismDocumentFilter<T> : IDocumentFilter
{
	public void Apply(OpenApiDocument openApiDoc, DocumentFilterContext context)
	{
		RegisterSubClasses(context, typeof(T));
	}

	private static void RegisterSubClasses(DocumentFilterContext context, Type baseType)
	{
		var schemaGenerator = context.SchemaGenerator;
		var derivedTypes = DerivedTypesHelper.GetDerivedTypes(typeof(T));
		foreach (var type in derivedTypes)
			schemaGenerator.GenerateSchema(type, context.SchemaRepository);
	}
}