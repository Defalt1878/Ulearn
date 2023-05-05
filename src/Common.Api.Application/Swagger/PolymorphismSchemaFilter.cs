using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ulearn.Common.Extensions;

namespace Ulearn.Common.Api.Swagger
{
	public class PolymorphismSchemaFilter<T> : ISchemaFilter
	{
		private const string discriminatorName = "$type";
		private readonly Lazy<HashSet<Type>> derivedTypes = new(DerivedTypesInit);
		private readonly Lazy<Dictionary<Type, string>> type2Name = new(Type2NameInit);

		public void Apply(OpenApiSchema schema, SchemaFilterContext context)
		{
			var type = context.Type;
			if (type == typeof(T))
				ApplyToBaseType(schema);
			if (derivedTypes.Value.Contains(context.Type))
				ApplyToDerivedType(schema, context);
		}

		private void ApplyToBaseType(OpenApiSchema schema)
		{
			var clonedSchema = new OpenApiSchema
			{
				Properties = schema.Properties,
				Type = schema.Type,
				Required = schema.Required
			};

			var derivedTypesSchema = new OpenApiSchema
			{
				OneOf = derivedTypes.Value.Select(
					t => new OpenApiSchema { Reference = new OpenApiReference { Id = t.Name, Type = ReferenceType.Schema } }
				).ToList(),
				Discriminator = new OpenApiDiscriminator
				{
					PropertyName = discriminatorName,
					Mapping = derivedTypes.Value.ToDictionary(t => type2Name.Value.GetOrDefault(t) ?? t.Name, t => $"#/components/schemas/{t.Name}")
				},
				Required = new HashSet<string> { discriminatorName },
				Properties = new Dictionary<string, OpenApiSchema>
				{
					{
						discriminatorName, new OpenApiSchema
						{
							Type = "string",
							Pattern = string.Join("|", derivedTypes.Value.Select(GetTypeDiscriminatorValue))
						}
					}
				}
			};

			schema.AllOf = new List<OpenApiSchema>
			{
				derivedTypesSchema,
				clonedSchema
			};

			// reset properties for they are included in allOf, should be null but code does not handle it
			schema.Properties = new Dictionary<string, OpenApiSchema>();
		}

		private void ApplyToDerivedType(OpenApiSchema schema, SchemaFilterContext context)
		{
			schema.Properties.Add(discriminatorName, new OpenApiSchema
			{
				Type = "string",
				Pattern = GetTypeDiscriminatorValue(context.Type),
				Example = new OpenApiString(GetTypeDiscriminatorValue(context.Type))
			});
		}

		private string GetTypeDiscriminatorValue(Type t)
		{
			return type2Name.Value.GetOrDefault(t) ?? t.Name;
		}

		private static HashSet<Type> DerivedTypesInit()
		{
			return new HashSet<Type>(DerivedTypesHelper.GetDerivedTypes(typeof(T)));
		}

		private static Dictionary<Type, string> Type2NameInit()
		{
			return DerivedTypesHelper.GetType2JsonTypeName(DerivedTypesInit());
		}
	}
}