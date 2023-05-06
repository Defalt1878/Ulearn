using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace Ulearn.Core
{
	public class DisplayNameSerializationBinder : DefaultSerializationBinder
	{
		private readonly Dictionary<string, Type> nameToType;
		private readonly Dictionary<Type, string> typeToName;

		public DisplayNameSerializationBinder(IEnumerable<Type> types)
		{
			var customDisplayNameTypes = types
				.Where(x => x
					.GetCustomAttributes(false)
					.Any(y => y is DisplayNameAttribute));

			nameToType = customDisplayNameTypes
				.ToDictionary(t => t
						.GetCustomAttributes(false)
						.OfType<DisplayNameAttribute>()
						.First()
						.DisplayName,
					t => t);

			typeToName = nameToType
				.ToDictionary(t => t.Value, t => t.Key);
		}

		public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
		{
			if (!typeToName.ContainsKey(serializedType))
			{
				base.BindToName(serializedType, out assemblyName, out typeName);
				return;
			}

			var name = typeToName[serializedType];

			assemblyName = null;
			typeName = name;
		}

		public override Type BindToType(string assemblyName, string typeName)
		{
			if (nameToType.TryGetValue(typeName, out var type))
				return type;

			return base.BindToType(assemblyName, typeName);
		}

		public static IEnumerable<Type> GetSubtypes(params Type[] baseTypes) =>
			baseTypes
				.SelectMany(baseType => Assembly.GetAssembly(baseType)?.GetTypes()
					.EmptyIfNull()
					.Where(t =>
						t is { IsClass: true, IsAbstract: false } &&
						(baseType.IsInterface ? baseType.IsAssignableFrom(t) : t.IsSubclassOf(baseType))
					)
				);
	}
}