using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Api.ModelBinders;

public class JsonModelBinder : IModelBinder
{
	private static ILog Log => LogProvider.Get().ForContext(typeof(JsonModelBinder));

	public async Task BindModelAsync(ModelBindingContext bindingContext)
	{
		if (bindingContext == null)
			throw new ArgumentNullException(nameof(bindingContext));

		var request = bindingContext.HttpContext.Request;
		if (request.ContentLength > 0 && !request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
		{
			/* If not JSON request */
			bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Content-Type should be application/json");
			return;
		}

		var reader = new StreamReader(request.Body);
		var incomingData = await reader.ReadToEndAsync().ConfigureAwait(false);
		if (string.IsNullOrEmpty(incomingData))
		{
			/* If no JSON data */
			bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Body can not be empty");
			return;
		}

		object model;
		try
		{
			model = JsonConvert.DeserializeObject(incomingData, bindingContext.ModelType);
		}
		catch (Exception e) when (e is JsonSerializationException or JsonReaderException)
		{
			Log.Warn($"Can't deserialize json request: {e.Message}");
			bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, e.Message);
			return;
		}

		bindingContext.Result = ModelBindingResult.Success(model);
	}
}