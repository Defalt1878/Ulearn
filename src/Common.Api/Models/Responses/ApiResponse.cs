using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Ulearn.Common.Extensions;

namespace Ulearn.Common.Api.Models.Responses
{
	public class ApiResponse
	{
		public override string ToString()
		{
			return this.JsonSerialize();
		}

		public virtual string GetShortLogString()
		{
			return ToString();
		}
	}

	[JsonConverter(typeof(StringEnumConverter), true)]
	public enum ResponseStatus
	{
		Ok,
		Error
	}
}