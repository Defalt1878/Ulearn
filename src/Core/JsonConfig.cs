using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ulearn.Core
{
	public static class JsonConfig
	{
		public static JsonSerializerSettings GetSettings(params Type[] baseTypes)
		{
			return new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				SerializationBinder = new DisplayNameSerializationBinder(DisplayNameSerializationBinder.GetSubtypes(baseTypes)),
				Converters = new List<JsonConverter> { new StringEnumConverter() }
			};
		}
	}
}