using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using AntiPlagiarism.Api.Models.Results;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Responses.AntiPlagiarism;

namespace Ulearn.Web.Api.Controllers.AntiPlagiarism;

[Route("/antiplagiarism")]
public class AntiPlagiarismController : BaseController
{
	private static readonly ConcurrentDictionary<Tuple<Guid, Guid>, Tuple<DateTime, GetAuthorPlagiarismsResponse>> plagiarismsCache = new();
	private static readonly TimeSpan cacheLifeTime = TimeSpan.FromMinutes(10);
	private readonly IAntiPlagiarismClient antiPlagiarismClient;
	private readonly IUserSolutionsRepo userSolutionsRepo;

	public AntiPlagiarismController(
		ICourseStorage courseStorage,
		UlearnDb db,
		IUsersRepo usersRepo,
		IAntiPlagiarismClient antiPlagiarismClient,
		IUserSolutionsRepo userSolutionsRepo
	)
		: base(courseStorage, db, usersRepo)
	{
		this.userSolutionsRepo = userSolutionsRepo;
		this.antiPlagiarismClient = antiPlagiarismClient;
	}

	[Authorize(Policy = "Instructors")]
	[HttpGet("{submissionId}")]
	public async Task<ActionResult<AntiPlagiarismInfoResponse>> Info([FromRoute] int submissionId, [FromQuery] string courseId)
	{
		var submission = await userSolutionsRepo.FindSubmissionById(submissionId);
		if (courseStorage.FindCourse(courseId)?.FindSlideByIdNotSafe(submission.SlideId) is not ExerciseSlide slide)
			return NotFound();

		if (!slide.Exercise.CheckForPlagiarism)
			return new AntiPlagiarismInfoResponse
			{
				Status = "notChecked"
			};

		var antiPlagiarismsResult = await GetAuthorPlagiarismsAsync(submission);

		var info = new AntiPlagiarismInfoResponse
		{
			Status = "checked",
			SuspicionLevel = SuspicionLevel.None,
			SuspiciousAuthorsCount = 0
		};
		var faintSuspicionAuthorsIds = new HashSet<Guid>();
		var strongSuspicionAuthorsIds = new HashSet<Guid>();
		foreach (var researchedSubmission in antiPlagiarismsResult.ResearchedSubmissions)
		foreach (var plagiarism in researchedSubmission.Plagiarisms)
			if (plagiarism.Weight >= antiPlagiarismsResult.SuspicionLevels.StrongSuspicion)
			{
				strongSuspicionAuthorsIds.Add(plagiarism.SubmissionInfo.AuthorId);
				info.SuspicionLevel = SuspicionLevel.Strong;
			}
			else if (plagiarism.Weight >= antiPlagiarismsResult.SuspicionLevels.FaintSuspicion && info.SuspicionLevel != SuspicionLevel.Strong)
			{
				faintSuspicionAuthorsIds.Add(plagiarism.SubmissionInfo.AuthorId);
				info.SuspicionLevel = SuspicionLevel.Faint;
			}

		info.SuspiciousAuthorsCount = info.SuspicionLevel == SuspicionLevel.Faint ? faintSuspicionAuthorsIds.Count : strongSuspicionAuthorsIds.Count;

		return info;
	}

	private async Task<GetAuthorPlagiarismsResponse> GetAuthorPlagiarismsAsync(UserExerciseSubmission submission)
	{
		RemoveOldValuesFromCache();
		var userId = Guid.Parse(submission.UserId);
		var taskId = submission.SlideId;
		var cacheKey = Tuple.Create(userId, taskId);
		if (plagiarismsCache.TryGetValue(cacheKey, out var cachedValue))
			return cachedValue.Item2;

		var value = await antiPlagiarismClient.GetAuthorPlagiarismsAsync(new GetAuthorPlagiarismsParameters
		{
			AuthorId = userId,
			TaskId = taskId,
			Language = submission.Language
		});

		plagiarismsCache.AddOrUpdate(cacheKey,
			_ => Tuple.Create(DateTime.Now, value),
			(_, _) => Tuple.Create(DateTime.Now, value));

		return value;
	}

	private static void RemoveOldValuesFromCache()
	{
		foreach (var key in plagiarismsCache.Keys.ToList())
		{
			if (!plagiarismsCache.TryGetValue(key, out var cachedValue))
				continue;
			/* Remove cached value if it is too old */
			if (DateTime.Now.Subtract(cachedValue.Item1) > cacheLifeTime)
				plagiarismsCache.TryRemove(key, out _);
		}
	}
}