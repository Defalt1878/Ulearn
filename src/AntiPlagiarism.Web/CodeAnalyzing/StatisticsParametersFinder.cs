using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Accord.Statistics.Distributions.Univariate;
using AntiPlagiarism.Web.Database.Models;
using AntiPlagiarism.Web.Extensions;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.CodeAnalyzing
{
	public class StatisticsParametersFinder
	{
		private readonly PlagiarismDetector plagiarismDetector;

		private static ILog Log => LogProvider.Get().ForContext(typeof(StatisticsParametersFinder));

		public StatisticsParametersFinder(PlagiarismDetector plagiarismDetector)
		{
			this.plagiarismDetector = plagiarismDetector;
		}

		public async Task<(List<TaskStatisticsSourceData>, TaskStatisticsParameters)> FindStatisticsParametersAsync(List<Submission> submissions)
		{
			var pairs = submissions.SelectMany(_ => submissions, Tuple.Create).Where(pair => pair.Item1.Id < pair.Item2.Id).ToList();

			var taskStatisticsSourceData = new List<TaskStatisticsSourceData>();
			var pairIndex = 0;
			foreach (var (firstSubmission, secondSubmission) in pairs)
				taskStatisticsSourceData.Add(new TaskStatisticsSourceData
				{
					Submission1Id = firstSubmission.Id,
					Submission2Id = secondSubmission.Id,
					Weight = await GetLinkWeightAsync(firstSubmission, secondSubmission, pairIndex++, pairs.Count).ConfigureAwait(false)
				});

			/* Remove all 1's from weights list.
				Ones are weights for pairs of absolutely identical solutions (i.e. copied from the internet or shared between students).
				They are negatively affect to the mean and standard deviation of weights. So we decided just remove them from the array before
				calculation mean and deviation. See https://yt.skbkontur.ru/issue/ULEARN-78 for details. */
			taskStatisticsSourceData = taskStatisticsSourceData.Where(d => d.Weight < 1 - 1e-6).ToList();
			var weights = taskStatisticsSourceData.Select(d => d.Weight).ToList();
			Log.Info($"Пересчитываю статистические параметры задачи (TaskStatisticsParameters) по следующему набору весов: [{string.Join(", ", weights)}]");

			var mean = weights.Mean();
			var deviation = weights.Deviation(mean);
			return (taskStatisticsSourceData, new TaskStatisticsParameters
			{
				Mean = mean,
				Deviation = deviation
			});
		}

		private Task<double> GetLinkWeightAsync(Submission first, Submission second, int index, int totalCount)
		{
			Log.Info($"Вычисляю коэффициент похожести решения #{first.Id} и #{second.Id} ({index} из {totalCount})");
			return plagiarismDetector.GetWeightAsync(first, second);
		}

		public static (double faintSuspicion, double strongSuspicion) GetSuspicionLevels(double mean, double sigma, double faintSuspicionCoefficient, double strongSuspicionCoefficient)
		{
			if (Math.Abs(sigma) < 1e-7)
				return (1, 1);
			var (alpha, beta) = GetBetaParameters(mean, sigma);
			UnivariateContinuousDistribution distribution = alpha > 0 && beta > 0 // иное запрещено по определению BetaDistribution
				? new BetaDistribution(alpha, beta)
				: new NormalDistribution(mean, sigma);
			var faintSigmaToProbability = new NormalDistribution(0, 1).DistributionFunction(faintSuspicionCoefficient);
			var strongSigmaToProbability = new NormalDistribution(0, 1).DistributionFunction(strongSuspicionCoefficient);
			var faintSuspicion = distribution.InverseDistributionFunction(faintSigmaToProbability);
			var strongSuspicion = distribution.InverseDistributionFunction(strongSigmaToProbability);
			return (faintSuspicion, strongSuspicion);
		}

		private static (double, double) GetBetaParameters(double mean, double sigma)
		{
			var alpha = -mean * (sigma * sigma + mean * mean - mean) / (sigma * sigma);
			var beta = (sigma * sigma + mean * mean - mean) * (mean - 1) / (sigma * sigma);
			return (alpha, beta);
		}
	}
}