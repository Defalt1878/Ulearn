using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ulearn.Common.Api.Models.Parameters;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Criteria;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Strategies;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Transport;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Tracing;
using Vostok.Telemetry.Kontur;

namespace Ulearn.Common.Api
{
	public class BaseApiClient
	{
		private readonly ApiClientSettings settings;
		private readonly ClusterClient clusterClient;

		protected BaseApiClient(ApiClientSettings settings)
		{
			this.settings = settings;

			clusterClient = new ClusterClient(Log, config =>
			{
				config.SetupUniversalTransport();
				config.DefaultTimeout = settings.DefaultTimeout;
				config.SetupDistributedKonturTracing();
				config.ClusterProvider = new FixedClusterProvider(settings.EndpointUrl);
				config.TargetServiceName = settings.ServiceName;
				config.DefaultRequestStrategy = Strategy.SingleReplica;
				config.SetupResponseCriteria(
					//new AcceptNonRetriableCriterion(), // Пока у нас одна реплика, используем результат, какой есть
					//new RejectNetworkErrorsCriterion(),
					//new RejectServerErrorsCriterion(),
					//new RejectThrottlingErrorsCriterion(),
					//new RejectUnknownErrorsCriterion(),
					//new RejectStreamingErrorsCriterion(),
					new AlwaysAcceptCriterion()
				);
			});
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(BaseApiClient)).WithTracingProperties(KonturTracerProvider.Get());

		protected async Task<TResult> MakeRequestAsync<TParameters, TResult>(HttpMethod method, string url, TParameters parameters)
			where TParameters : ApiParameters
			where TResult : ApiResponse
		{
			ClusterResult response;
			if (settings.LogRequestsAndResponses)
				Log.Info("Send {method} request to {serviceName} ({url}) with parameters: {parameters}", method.Method, settings.ServiceName, url, parameters.ToString());

			try
			{
				if (method == HttpMethod.Get)
				{
					var request = Request.Get(BuildUrl(settings.EndpointUrl + url));
					var parametersNameValueCollection = parameters.ToNameValueCollection();
					request = parametersNameValueCollection.AllKeys
						.Aggregate(
							request,
							(current, key) => current.WithAdditionalQueryParameter(key, parametersNameValueCollection[key])
						);
					response = await MakeRequestAsync(request).ConfigureAwait(false);
				}
				else if (method == HttpMethod.Post)
				{
					var serializedPayload = JsonConvert.SerializeObject(parameters, Formatting.Indented);
					var request = Request
						.Post(BuildUrl(settings.EndpointUrl + url))
						.WithContent(serializedPayload, Encoding.UTF8)
						.WithContentTypeHeader("application/json");
					response = await MakeRequestAsync(request).ConfigureAwait(false);
				}
				else
				{
					throw new ApiClientException($"Internal error: unsupported http method: {method.Method}");
				}
			}
			catch (Exception e)
			{
				Log.Error(e, "Can't send request to {serviceName}: {message}", settings.ServiceName, e.Message);
				throw new ApiClientException($"Can't send request to {settings.ServiceName}: {e.Message}", e);
			}

			if (response.Status != ClusterResultStatus.Success)
			{
				Log.Error("Bad response status from {serviceName}: {status}", settings.ServiceName, response.Status.ToString());
				throw new ApiClientException($"Bad response status from {settings.ServiceName}: {response.Status}");
			}

			if (!response.Response.IsSuccessful)
			{
				Log.Error("Bad response code from {serviceName}: {statusCode} {statusCodeDescription}", settings.ServiceName, (int)response.Response.Code, response.Response.Code.ToString());
				throw new ApiClientException($"Bad response code from {settings.ServiceName}: {(int)response.Response.Code} {response.Response.Code}");
			}

			TResult result;
			string jsonResult = null;
			try
			{
				if (response.Response.HasStream)
				{
					var reader = new StreamReader(response.Response.Stream, Encoding.UTF8);
					jsonResult = await reader.ReadToEndAsync();
				}
				else if (response.Response.HasContent)
				{
					var content = response.Response.Content;
					jsonResult = Encoding.UTF8.GetString(content.Buffer, content.Offset, content.Length);
				}

				result = JsonConvert.DeserializeObject<TResult>(jsonResult!);
			}
			catch (Exception e)
			{
				Log.Error(e, "Can't parse response from {serviceName}: {message}", settings.ServiceName, e.Message);
				throw new ApiClientException($"Can't parse response from {settings.ServiceName}: {e.Message}", e);
			}

			if (settings.LogRequestsAndResponses)
			{
				var shortened = false;
				var logResult = jsonResult;
				if (jsonResult.Length > 8 * 1024)
				{
					logResult = result.GetShortLogString();
					shortened = true;
				}

				Log.Info($"Received response from \"{settings.ServiceName}\"{(shortened ? " (сокращенный)" : "")}: {logResult}");
			}

			return result;
		}

		private Uri BuildUrl(string url, NameValueCollection parameters = null)
		{
			parameters ??= new NameValueCollection();

			var builder = new UriBuilder(url);
			var queryString = WebUtils.ParseQueryString(builder.Query);
			foreach (var parameterName in parameters.AllKeys)
				queryString[parameterName] = parameters[parameterName];

			builder.Query = queryString.ToQueryString();
			return AddCustomParametersToUrl(builder.Uri);
		}

		protected virtual Uri AddCustomParametersToUrl(Uri url)
		{
			return url;
		}

		public async Task<ClusterResult> MakeRequestAsync(Request request)
		{
			var response = await clusterClient.SendAsync(request).ConfigureAwait(false);

			if (response.Status != ClusterResultStatus.Success)
			{
				Log.Error("Bad response status from {serviceName}: {status}", settings.ServiceName, response.Status.ToString());
				throw new ApiClientException($"Bad response status from {settings.ServiceName}: {response.Status}");
			}

			return response;
		}
	}

	public class ApiClientSettings
	{
		public readonly Uri EndpointUrl;

		public TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

		public bool LogRequestsAndResponses = true;

		public string ServiceName = "ulearn.service";

		public ApiClientSettings(string endpointUrl)
		{
			endpointUrl = endpointUrl.Replace("localhost", Environment.MachineName.ToLowerInvariant());
			EndpointUrl = new Uri(endpointUrl);
		}
	}
}