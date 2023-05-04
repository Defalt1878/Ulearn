using System;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Ulearn.Common.Api;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Telegram;
using Ulearn.Web.Api.Models.Parameters.Exercise;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Controllers
{
	[Route("/python-visualizer")]
	public class PythonVisualizerController : BaseController
	{
		private readonly ErrorsBot errorsBot;
		private readonly IPythonVisualizerClient pythonVisualizerClient;

		public PythonVisualizerController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo, ErrorsBot errorsBot,
			IPythonVisualizerClient pythonVisualizerClient)
			: base(courseStorage, db, usersRepo)
		{
			this.pythonVisualizerClient = pythonVisualizerClient;
			this.errorsBot = errorsBot;
		}

		private new static ILog Log => LogProvider.Get().ForContext(typeof(PythonVisualizerController));

		/// <summary>
		///     Получить результат отладки кода на python
		/// </summary>
		[Authorize]
		[HttpPost("run")]
		public async Task<ActionResult> Run([FromBody] PythonVisualizerRunParameters parameters)
		{
			var response = await pythonVisualizerClient.GetResult(parameters);
			if (response is null)
				return StatusCode((int)HttpStatusCode.InternalServerError);
			if (response.Code == ResponseCode.RequestTimeout)
			{
				Log.Error("Python Visualizer request timed out, posting data to errors bot");
				await errorsBot.PostToChannelAsync($"Визуализатор питона не смог обработать запрос для code=[{parameters.Code}] input=[{parameters.InputData}]");
			}

			if (response.Code != ResponseCode.Ok)
				return StatusCode((int)response.Code);
			if (response.HasStream)
				return new FileStreamResult(response.Stream, "application/json");
			if (response.HasContent)
				return new FileContentResult(response.Content.ToArray(), "application/json");
			return StatusCode((int)HttpStatusCode.InternalServerError);
		}
	}

	public interface IPythonVisualizerClient
	{
		Task<Response> GetResult(PythonVisualizerRunParameters parameters);
	}

	public class PythonVisualizerClient : BaseApiClient, IPythonVisualizerClient
	{
		private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30);
		private readonly string endpointUrl;

		public PythonVisualizerClient(string endpointUrl)
			: base(new ApiClientSettings(endpointUrl)
			{
				ServiceName = "ulearn.python-visualizer",
				DefaultTimeout = defaultTimeout
			})
		{
			this.endpointUrl = endpointUrl;
		}

		public async Task<Response> GetResult(PythonVisualizerRunParameters parameters)
		{
			var builder = new UriBuilder(endpointUrl + "run");
			var json = JsonConvert.SerializeObject(parameters);
			var request = Request.Post(builder.Uri)
				.WithHeader("Content-Type", "application/json")
				.WithContent(json);
			return (await MakeRequestAsync(request)).Response;
		}
	}
}