using System;
using System.Threading.Tasks;
using Database.Repos;
using LtiLibrary.Core.Outcomes.v1;
using Newtonsoft.Json;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Model;
using Ulearn.Web.Api.Controllers;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Utils.LTI;

public static class LtiUtils
{
	private static ILog Log => LogProvider.Get().ForContext(typeof(LtiUtils));

	public static async Task SubmitScore(Slide slide, string userId, int score,
		string ltiRequestJson, ILtiConsumersRepo consumersRepo)
	{
		var ltiRequest = JsonConvert.DeserializeObject<LtiRequest>(ltiRequestJson);
		Log.Info($"Нашёл Lti запрос с ConsumerKey = {ltiRequest.ConsumerKey}, ищу secret для этого consumer-а");

		var consumerSecret = (await consumersRepo.Find(ltiRequest.ConsumerKey)).Secret;

		Log.Info($"Надо отправить результаты слайда {slide.Id} пользователя {userId} по LTI. Нашёл LtiRequest: {ltiRequestJson}");
		UriBuilder uri;
		try
		{
			uri = new UriBuilder(ltiRequest.LisOutcomeServiceUrl);
		}
		catch (Exception e)
		{
			Log.Error(e, $"Неверный адрес отправки результатов по LTI: {ltiRequest.LisOutcomeServiceUrl}");
			throw;
		}

		if (uri.Host == "localhost")
		{
			uri.Host = "192.168.33.10";
			uri.Port = 80;
			uri.Scheme = "http";
		}

		var maxScore = BaseController.GetMaxScoreForUsersSlide(slide, true, false, false);
		var outputScore = score / (double)maxScore;
		Log.Info($"Отправляю результаты на {ltiRequest.LisOutcomeServiceUrl}: {score} из {maxScore} ({outputScore})");

		/* Sometimes score is bigger then slide's MaxScore, i.e. in case of manual checking */
		if (score > maxScore)
			outputScore = 1;
		var result = OutcomesClient.PostScore(uri.ToString(), ltiRequest.ConsumerKey, consumerSecret,
			ltiRequest.LisResultSourcedId, outputScore);

		if (!result.IsValid)
			throw new Exception(uri + "\r\n\r\n" + result.Message);
	}
}