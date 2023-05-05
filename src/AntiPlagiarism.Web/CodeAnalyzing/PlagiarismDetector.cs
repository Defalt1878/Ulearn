using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api.Models;
using AntiPlagiarism.Api.Models.Results;
using AntiPlagiarism.Web.Configuration;
using AntiPlagiarism.Web.Database.Models;
using AntiPlagiarism.Web.Database.Repos;
using AntiPlagiarism.Web.Extensions;
using Microsoft.Extensions.Options;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.CodeAnalyzing;

public class PlagiarismDetector
{
	private readonly ISnippetsRepo snippetsRepo;
	private readonly ISubmissionsRepo submissionsRepo;
	private readonly IMostSimilarSubmissionsRepo mostSimilarSubmissionsRepo;
	private readonly CodeUnitsExtractor codeUnitsExtractor;
	private readonly AntiPlagiarismConfiguration configuration;

	private static ILog Log => LogProvider.Get().ForContext(typeof(PlagiarismDetector));

	public PlagiarismDetector(
		ISnippetsRepo snippetsRepo, ISubmissionsRepo submissionsRepo,
		IMostSimilarSubmissionsRepo mostSimilarSubmissionsRepo,
		CodeUnitsExtractor codeUnitsExtractor,
		IOptions<AntiPlagiarismConfiguration> options)
	{
		this.snippetsRepo = snippetsRepo;
		this.submissionsRepo = submissionsRepo;
		this.mostSimilarSubmissionsRepo = mostSimilarSubmissionsRepo;
		this.codeUnitsExtractor = codeUnitsExtractor;
		configuration = options.Value;
	}

	public async Task<double> GetWeightAsync(Submission firstSubmission, Submission secondSubmission)
	{
		Log.Info($"Вычисляю коэффициент похожести решения #{firstSubmission.Id} и #{secondSubmission.Id}");
		var maxSnippetsCount = configuration.AntiPlagiarism.PlagiarismDetector.CountOfColdestSnippetsUsedToSecondSearch;
		var authorsCountThreshold = configuration.AntiPlagiarism.PlagiarismDetector.SnippetAuthorsCountThreshold;
		var snippetsOccurrencesOfFirstSubmission = await snippetsRepo.GetSnippetsOccurrencesForSubmissionAsync(firstSubmission, maxSnippetsCount, 0, authorsCountThreshold).ConfigureAwait(false);
		Log.Debug($"Сниппеты первого решения: [{string.Join(", ", snippetsOccurrencesOfFirstSubmission)}]");
		var snippetsOccurrencesOfSecondSubmission = await snippetsRepo.GetSnippetsOccurrencesForSubmissionAsync(secondSubmission, maxSnippetsCount, 0, authorsCountThreshold).ConfigureAwait(false);
		Log.Debug($"Сниппеты второго решения: [{string.Join(", ", snippetsOccurrencesOfSecondSubmission)}]");

		/* Group by snippets from the second submissions by snippetId for fast searching */
		var snippetsOccurrencesOfSecondSubmissionBySnippet = snippetsOccurrencesOfSecondSubmission
			.GroupBy(o => o.SnippetId)
			.ToDictionary(g => g.Key, g => g.ToList())
			.ToDefaultDictionary();

		var tokensMatchedInFirstSubmission = new DefaultDictionary<SnippetType, HashSet<int>>();
		var tokensMatchedInSecondSubmission = new DefaultDictionary<SnippetType, HashSet<int>>();
		foreach (var snippetOccurence in snippetsOccurrencesOfFirstSubmission)
		{
			var snippet = snippetOccurence.Snippet;
			foreach (var otherOccurence in snippetsOccurrencesOfSecondSubmissionBySnippet[snippet.Id])
			{
				Log.Debug($"Нашёл совпадающий сниппет в обоих решениях: {snippet}");
				for (var i = 0; i < snippet.TokensCount; i++)
				{
					tokensMatchedInFirstSubmission[snippet.SnippetType].Add(snippetOccurence.FirstTokenIndex + i);
					tokensMatchedInSecondSubmission[snippet.SnippetType].Add(otherOccurence.FirstTokenIndex + i);
				}
			}
		}

		Log.Debug("Закончил поиск совпадающих сниппетов");

		var unionLength = 0;
		var allSnippetTypes = GetAllSnippetTypes();
		foreach (var snippetType in allSnippetTypes)
		{
			if (!tokensMatchedInFirstSubmission.ContainsKey(snippetType))
				continue;

			unionLength += tokensMatchedInFirstSubmission[snippetType].Count;
			unionLength += tokensMatchedInSecondSubmission[snippetType].Count;
		}

		var totalLength = GetTokensCountFromSnippetOccurrences(snippetsOccurrencesOfFirstSubmission) +
						GetTokensCountFromSnippetOccurrences(snippetsOccurrencesOfSecondSubmission);
		var weight = totalLength == 0 ? 0 : (double)unionLength / totalLength;

		/* Normalize weight */
		weight /= allSnippetTypes.Count;

		Log.Info($"Совпавших токенов {unionLength}, всего токенов {totalLength}, итоговый коэффициент {weight}");
		return weight;
	}

	private static int GetTokensCountFromSnippetOccurrences(IEnumerable<SnippetOccurence> occurrences)
	{
		var tokens = new HashSet<int>();
		foreach (var occurrence in occurrences)
			for (var i = 0; i < occurrence.Snippet.TokensCount; i++)
				tokens.Add(occurrence.FirstTokenIndex + i);

		return tokens.Count;
	}

	// Работа метода описана в классе PlagiarismDetectorConfiguration
	public async Task<List<Plagiarism>> GetPlagiarismsAsync(Submission submission, SuspicionLevels suspicionLevels, int submissionInfluenceLimitInMonths)
	{
		/* Dictionaries by submission id and snippet type */
		var tokensMatchedInThisSubmission = new DefaultDictionary<Tuple<int, SnippetType>, HashSet<int>>();
		var tokensMatchedInOtherSubmissions = new DefaultDictionary<Tuple<int, SnippetType>, HashSet<int>>();

		var maxSnippetsCountFirstSearch = configuration.AntiPlagiarism.PlagiarismDetector.CountOfColdestSnippetsUsedToFirstSearch;
		var maxSnippetsCountSecondSearch = configuration.AntiPlagiarism.PlagiarismDetector.CountOfColdestSnippetsUsedToSecondSearch;
		var maxSubmissionsAfterFirstSearch = configuration.AntiPlagiarism.PlagiarismDetector.MaxSubmissionsAfterFirstSearch;
		var authorsCountThreshold = configuration.AntiPlagiarism.PlagiarismDetector.SnippetAuthorsCountThreshold;

		/* We make two queries for finding suspicion submissions: first query is more limited by snippets count (`maxSnippetsCountFirstSearch` from configuration).
			For the first query we are looking for all submissions which are similar to our submission and filter only top-`maxSubmissionsAfterFirstSearch` by matched snippets count */
		var snippetsOccurrencesFirstSearch = await snippetsRepo.GetSnippetsOccurrencesForSubmissionAsync(
			submission,
			maxSnippetsCountFirstSearch,
			2,
			authorsCountThreshold
		).ConfigureAwait(false);
		var snippetsIdsFirstSearch = new HashSet<int>(snippetsOccurrencesFirstSearch.Select(o => o.SnippetId));
		Log.Info($"Found following snippets after first search: {string.Join(", ", snippetsIdsFirstSearch)}");
		var useSubmissionsFromDate = DateTime.Now.AddMonths(-submissionInfluenceLimitInMonths);
		var suspicionSubmissionIds = await snippetsRepo.GetSubmissionIdsWithSameSnippets(
			snippetsIdsFirstSearch,
			/* Filter only  submissions BY THIS client, THIS task, THIS language and NOT BY THIS author */
			o => o.Submission.ClientId == submission.ClientId &&
				o.Submission.TaskId == submission.TaskId &&
				o.Submission.Language == submission.Language &&
				o.Submission.AuthorId != submission.AuthorId &&
				o.Submission.AddingTime > useSubmissionsFromDate,
			maxSubmissionsAfterFirstSearch
		);
		Log.Info($"Found following submissions after first search: {string.Join(", ", suspicionSubmissionIds)}");

		var snippetsOccurrences = await snippetsRepo.GetSnippetsOccurrencesForSubmissionAsync(submission, maxSnippetsCountSecondSearch, 0, authorsCountThreshold).ConfigureAwait(false);
		var snippetsIds = new HashSet<int>(snippetsOccurrences.Select(o => o.SnippetId));

		var allOtherOccurrences = (await snippetsRepo.GetSnippetsOccurrences(
			snippetsIds,
			/* Filter only snippet occurrences in submissions BY THIS client, THIS task, THIS language and NOT BY THIS author */
			o => o.Submission.ClientId == submission.ClientId &&
				o.Submission.TaskId == submission.TaskId &&
				o.Submission.Language == submission.Language &&
				o.Submission.AuthorId != submission.AuthorId &&
				/* ... and only in submissions filterer by first query */
				suspicionSubmissionIds.Contains(o.SubmissionId)
		)).GroupBy(o => o.SnippetId).ToDictionary(kvp => kvp.Key, kvp => kvp.ToList());

		var snippetsStatistics = await snippetsRepo.GetSnippetsStatisticsAsync(submission.ClientId, submission.TaskId, submission.Language, snippetsIds).ConfigureAwait(false);

		var matchedSnippets = new DefaultDictionary<int, List<MatchedSnippet>>();
		var authorsCount = await submissionsRepo.GetAuthorsCountAsync(submission.ClientId, submission.TaskId, submission.Language, submissionInfluenceLimitInMonths).ConfigureAwait(false);
		foreach (var snippetOccurrence in snippetsOccurrences)
		{
			var otherOccurrences = allOtherOccurrences.GetOrDefault(snippetOccurrence.SnippetId, new List<SnippetOccurence>());

			var snippet = snippetOccurrence.Snippet;
			var snippetType = snippet.SnippetType;

			foreach (var otherOccurence in otherOccurrences)
			{
				for (var i = 0; i < snippet.TokensCount; i++)
				{
					var tokenIndexInThisSubmission = snippetOccurrence.FirstTokenIndex + i;
					var tokenIndexInOtherSubmission = otherOccurence.FirstTokenIndex + i;
					tokensMatchedInThisSubmission[Tuple.Create(otherOccurence.SubmissionId, snippetType)].Add(tokenIndexInThisSubmission);
					tokensMatchedInOtherSubmissions[Tuple.Create(otherOccurence.SubmissionId, snippetType)].Add(tokenIndexInOtherSubmission);
				}

				matchedSnippets[otherOccurence.SubmissionId].Add(new MatchedSnippet
				{
					SnippetType = snippetType,
					TokensCount = snippet.TokensCount,
					OriginalSubmissionFirstTokenIndex = snippetOccurrence.FirstTokenIndex,
					PlagiarismSubmissionFirstTokenIndex = otherOccurence.FirstTokenIndex,
					SnippetFrequency = GetSnippetFrequency(snippetsStatistics[snippet.Id], authorsCount)
				});
			}
		}

		var plagiarismSubmissionIds = tokensMatchedInOtherSubmissions.Keys.Select(tuple => tuple.Item1).ToList();
		var plagiarismSubmissions = await submissionsRepo.GetSubmissionsByIdsAsync(plagiarismSubmissionIds).ConfigureAwait(false);

		var plagiarisms = new List<Plagiarism>();

		var allSnippetTypes = GetAllSnippetTypes();
		var thisSubmissionLength = submission.TokensCount;
		MostSimilarSubmission mostSimilarSubmission = null;
		foreach (var plagiarismSubmission in plagiarismSubmissions)
		{
			var unionLength = 0;
			foreach (var snippetType in allSnippetTypes)
			{
				var submissionIdWithSnippetType = Tuple.Create(plagiarismSubmission.Id, snippetType);
				if (!tokensMatchedInThisSubmission.ContainsKey(submissionIdWithSnippetType))
					continue;

				unionLength += tokensMatchedInThisSubmission[submissionIdWithSnippetType].Count;
				unionLength += tokensMatchedInOtherSubmissions[submissionIdWithSnippetType].Count;
			}

			var plagiarismSubmissionLength = plagiarismSubmission.TokensCount;
			var totalLength = thisSubmissionLength + plagiarismSubmissionLength;
			var weight = totalLength == 0 ? 0 : (double)unionLength / totalLength;
			/* Normalize weight */
			weight /= allSnippetTypes.Count;

			if (mostSimilarSubmission is null || mostSimilarSubmission.Weight < weight)
				mostSimilarSubmission = new MostSimilarSubmission
				{
					Weight = weight,
					SubmissionId = submission.Id,
					SimilarSubmissionId = plagiarismSubmission.Id,
					Timestamp = DateTime.Now
				};
			Log.Info($"Link weight between submissions {submission.Id} and {plagiarismSubmission.Id} is {weight}. Union length is {unionLength}.");

			if (weight < suspicionLevels.FaintSuspicion)
				continue;

			plagiarisms.Add(BuildPlagiarismInfo(plagiarismSubmission, weight, matchedSnippets[plagiarismSubmission.Id]));
		}

		if (mostSimilarSubmission is not null)
			await mostSimilarSubmissionsRepo.TrySaveMostSimilarSubmissionAsync(mostSimilarSubmission).ConfigureAwait(false);

		return plagiarisms;
	}

	public List<TokenPosition> GetNeededTokensPositions(string program, Language language)
	{
		return GetNeededTokensPositions(codeUnitsExtractor.Extract(program, language));
	}

	private static List<TokenPosition> GetNeededTokensPositions(IEnumerable<CodeUnit> codeUnits)
	{
		return codeUnits.SelectMany(u => u.Tokens.Enumerate(u.FirstTokenIndex)).Select(t => new TokenPosition
		{
			TokenIndex = t.Index,
			StartPosition = t.Item.Position,
			Length = t.Item.Value.Length
		}).ToList();
	}

	private Plagiarism BuildPlagiarismInfo(Submission submission, double weight, List<MatchedSnippet> matchedSnippets)
	{
		/* We do TrimStart() because of issue in a way of passing code to codemirror on ulearn's frontend. We insert data into <textarea> which loses first spaces.
			We can remove it after migrating to new, React-based frontend. */
		var codeUnits = codeUnitsExtractor.Extract(submission.ProgramText.TrimStart(), submission.Language);
		return new Plagiarism
		{
			SubmissionInfo = submission.GetSubmissionInfoForApi(),
			Weight = weight,
			AnalyzedCodeUnits = codeUnits.Select(
				u => new AnalyzedCodeUnit
				{
					Name = u.Path.ToString(),
					FirstTokenIndex = u.FirstTokenIndex,
					TokensCount = u.Tokens.Count
				}).ToList(),
			TokensPositions = GetNeededTokensPositions(codeUnits),
			MatchedSnippets = matchedSnippets
		};
	}

	private static List<SnippetType> GetAllSnippetTypes()
	{
		return Enum.GetValues(typeof(SnippetType)).Cast<SnippetType>().ToList();
	}

	private static double GetSnippetFrequency(SnippetStatistics snippet, int authorsCount)
	{
		return (double)snippet.AuthorsCount / (authorsCount == 0 ? 1 : authorsCount);
	}
}