using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Ulearn.Common.Api.Models.Parameters
{
	public class ApiParameters
	{
		public override string ToString()
		{
			return ConstructQueryString(ToNameValueCollection());
		}

		public NameValueCollection ToNameValueCollection(string iEnumerableValuesSeparator = ",")
		{
			/* Get all properties of the object */
			var properties = GetType().GetProperties()
				.Where(p => p.CanRead)
				.Where(p => p.GetValue(this, null) != null)
				.ToDictionary(GetPropertyNameForQueryString, p => p.GetValue(this, null));

			/* Get names for all IEnumerable properties (excl. string) */
			var iEnumerablePropertiesNames = properties
				.Where(p => !(p.Value is string) && p.Value is IEnumerable)
				.Select(p => p.Key)
				.ToList();

			/* Concat all IEnumerable properties into a comma separated string */
			foreach (var propertyName in iEnumerablePropertiesNames)
			{
				var valueType = properties[propertyName].GetType();
				var valueElemType = valueType.IsGenericType
					? valueType.GetGenericArguments()[0]
					: valueType.GetElementType();
				if (valueElemType is { IsPrimitive: true } || valueElemType == typeof(string))
				{
					var enumerable = properties[propertyName] as IEnumerable;
					properties[propertyName] = string.Join(iEnumerableValuesSeparator, enumerable!.Cast<object>());
				}
			}

			var nameValueCollection = new NameValueCollection();
			foreach (var key in properties.Keys)
				nameValueCollection.Add(key, properties[key].ToString());

			return nameValueCollection;
		}

		private static string GetPropertyNameForQueryString(PropertyInfo property)
		{
			var fromQueryAttribute = property.GetCustomAttribute<FromQueryAttribute>();
			return fromQueryAttribute is not null 
				? fromQueryAttribute.Name 
				: property.Name;
		}

		private static string ConstructQueryString(NameValueCollection parameters)
		{
			var items = new List<string>();

			foreach (string name in parameters)
				items.Add(string.Concat(name, "=", WebUtility.UrlEncode(parameters[name])));

			return string.Join("&", items);
		}
	}
}