using System;
using System.Net.Http;
using System.Threading.Tasks;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;
using Ulearn.Common;
using Ulearn.Common.Api;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Api;

public class AntiPlagiarismClient : BaseApiClient, IAntiPlagiarismClient
{
	private static readonly TimeSpan defaultTimeout = TimeSpan.FromMinutes(10);

	private readonly string token;

	public AntiPlagiarismClient(string endpointUrl, string token)
		: base(new ApiClientSettings(endpointUrl)
		{
			ServiceName = "ulearn.antiplagiarism-web",
			DefaultTimeout = defaultTimeout
		})
	{
		this.token = token;
	}

	public Task<AddSubmissionResponse> AddSubmissionAsync(AddSubmissionParameters parameters)
	{
		return MakeRequestAsync<AddSubmissionParameters, AddSubmissionResponse>(HttpMethod.Post, Urls.AddSubmission, parameters);
	}

	public Task<GetSubmissionPlagiarismsResponse> GetSubmissionPlagiarismsAsync(GetSubmissionPlagiarismsParameters parameters)
	{
		return MakeRequestAsync<GetSubmissionPlagiarismsParameters, GetSubmissionPlagiarismsResponse>(HttpMethod.Get, Urls.GetSubmissionPlagiarisms, parameters);
	}

	public Task<GetAuthorPlagiarismsResponse> GetAuthorPlagiarismsAsync(GetAuthorPlagiarismsParameters parameters)
	{
		return MakeRequestAsync<GetAuthorPlagiarismsParameters, GetAuthorPlagiarismsResponse>(HttpMethod.Get, Urls.GetAuthorPlagiarisms, parameters);
	}

	public Task<GetMostSimilarSubmissionsResponse> GetMostSimilarSubmissionsAsync(GetMostSimilarSubmissionsParameters parameters)
	{
		return MakeRequestAsync<GetMostSimilarSubmissionsParameters, GetMostSimilarSubmissionsResponse>(HttpMethod.Get, Urls.GetMostSimilarSubmissions, parameters);
	}

	public Task<GetSuspicionLevelsResponse> GetSuspicionLevelsAsync(GetSuspicionLevelsParameters parameters)
	{
		return MakeRequestAsync<GetSuspicionLevelsParameters, GetSuspicionLevelsResponse>(HttpMethod.Get, Urls.GetSuspicionLevels, parameters);
	}

	public Task<GetSuspicionLevelsResponse> SetSuspicionLevelsAsync(SetSuspicionLevelsParameters parameters)
	{
		return MakeRequestAsync<SetSuspicionLevelsParameters, GetSuspicionLevelsResponse>(HttpMethod.Post, Urls.SetSuspicionLevels, parameters);
	}

	public Task<GetProcessingStatusResponse> GetProcessingStatusAsync(GetProcessingStatusParameters parameters)
	{
		return MakeRequestAsync<GetProcessingStatusParameters, GetProcessingStatusResponse>(HttpMethod.Post, Urls.GetProcessingStatus, parameters);
	}

	protected override Uri AddCustomParametersToUrl(Uri url)
	{
		var builder = new UriBuilder(url);
		var queryString = WebUtils.ParseQueryString(builder.Query);
		queryString["token"] = token;
		builder.Query = queryString.ToQueryString();
		return builder.Uri;
	}
}