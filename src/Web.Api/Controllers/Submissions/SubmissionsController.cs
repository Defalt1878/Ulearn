using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Web.Api.Models.Responses.Submissions;

namespace Ulearn.Web.Api.Controllers.Submissions
{
	[Route("/submissions")]
	public class SubmissionsController : BaseController
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IGroupAccessesRepo groupAccessesRepo;
		private readonly INotificationsRepo notificationsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly IUnitsRepo unitsRepo;
		private readonly IUserSolutionsRepo userSolutionsRepo;
		private readonly IVisitsRepo visitsRepo;

		public SubmissionsController(
			ICourseStorage courseStorage,
			UlearnDb db,
			IUsersRepo usersRepo,
			IUserSolutionsRepo userSolutionsRepo,
			ICourseRolesRepo courseRolesRepo,
			ISlideCheckingsRepo slideCheckingsRepo,
			IGroupAccessesRepo groupAccessesRepo,
			IVisitsRepo visitsRepo,
			INotificationsRepo notificationsRepo,
			IUnitsRepo unitsRepo
		)
			: base(courseStorage, db, usersRepo)
		{
			this.courseRolesRepo = courseRolesRepo;
			this.userSolutionsRepo = userSolutionsRepo;
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.unitsRepo = unitsRepo;
			this.groupAccessesRepo = groupAccessesRepo;
			this.visitsRepo = visitsRepo;
			this.notificationsRepo = notificationsRepo;
		}

		[HttpGet]
		[Authorize]
		public async Task<ActionResult<SubmissionsResponse>> GetSubmissions([FromQuery] [CanBeNull] string userId, [FromQuery] string courseId, [FromQuery] Guid slideId)
		{
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin);
			if (userId is not null && !isCourseAdmin && !userId.Equals(UserId, StringComparison.OrdinalIgnoreCase))
			{
				var isInstructor = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Instructor);
				if (!isInstructor)
					return StatusCode((int)HttpStatusCode.Forbidden, new ErrorResponse("You don't have access to view submissions"));
			}
			else
			{
				userId ??= UserId;
			}

			var submissions = await userSolutionsRepo
				.GetAllSubmissionsByUserAllInclude(courseId, slideId, userId)
				.OrderByDescending(s => s.Timestamp)
				.ToListAsync();

			if (!courseStorage.HasCourse(courseId))
				return NotFound($"Course {courseId} not found");

			if (courseStorage
					.GetCourse(courseId)
					.GetSlideByIdNotSafe(slideId) is not ExerciseSlide)
				return NotFound($"Slide with id {slideId} not found");

			var codeReviewComments = await slideCheckingsRepo.GetExerciseCodeReviewComments(courseId, slideId, userId);
			var reviewId2Comments = codeReviewComments
				?.GroupBy(c => c.ReviewId)
				.ToDictionary(g => g.Key, g => g.AsEnumerable());
			var prohibitFurtherManualChecking = submissions.Any(s => s.ManualChecking?.ProhibitFurtherManualCheckings ?? false);

			return SubmissionsResponse.Build(submissions, reviewId2Comments, isCourseAdmin, prohibitFurtherManualChecking);
		}

		[HttpPost("{submissionId}/manual-checking")]
		[Authorize]
		public async Task<ActionResult> EnableManualChecking([FromRoute] string submissionId)
		{
			var submission = await userSolutionsRepo.FindSubmissionById(submissionId);

			if (!await groupAccessesRepo.HasInstructorEditAccessToStudentGroup(UserId, submission.UserId))
				return StatusCode((int)HttpStatusCode.Forbidden, "You don't have access to edit ProhibitFurtherReview flag for this submission");

			if (submission.ManualChecking is not null)
				Ok($"Manual checking already enabled for submission {submissionId}");

			var checking = await slideCheckingsRepo.AddManualExerciseChecking(submission.CourseId, submission.SlideId, submission.UserId, submission.Id);
			await visitsRepo.MarkVisitsAsWithManualChecking(submission.CourseId, submission.SlideId, submission.UserId);
			await slideCheckingsRepo.LockManualChecking(checking, UserId);

			return Ok($"Manual checking enabled for submission {submissionId}");
		}

		[HttpPost("{submissionId}/score")]
		[Authorize]
		public async Task<ActionResult> Score([FromRoute] string submissionId, [FromQuery] int percent)
		{
			var submission = await userSolutionsRepo.FindSubmissionById(submissionId);
			var courseId = submission.CourseId;
			var slideId = submission.SlideId;
			var userId = submission.UserId;

			if (!await groupAccessesRepo.HasInstructorEditAccessToStudentGroup(User.GetUserId(), userId))
				return StatusCode((int)HttpStatusCode.Forbidden, "You don't have access to score this submission");

			/* Invalid form: score isn't from range 0..100 */
			if (percent is < 0 or > 100)
				return StatusCode((int)HttpStatusCode.BadRequest, $"Неверное количество процентов: {percent}");

			var course = courseStorage.GetCourse(courseId);
			var slide = course.FindSlideByIdNotSafe(slideId);

			if (submission.ManualChecking is null)
			{
				var lastAcceptedSubmission = await userSolutionsRepo
					.GetAllAcceptedSubmissionsByUser(courseId, slideId, userId)
					.OrderByDescending(s => s.Timestamp)
					.FirstOrDefaultAsync();
				if (lastAcceptedSubmission is not null && lastAcceptedSubmission.Id != submission.Id)
					return StatusCode((int)HttpStatusCode.BadRequest,
						new
						{
							Status = "error",
							Error = "has_newest_submission",
							SubmissionId = lastAcceptedSubmission.Id,
							SubmissionDate = lastAcceptedSubmission.Timestamp
						});
			}

			var checking = submission.ManualChecking;

			await using (var transaction = await db.Database.BeginTransactionAsync())
			{
				await slideCheckingsRepo.LockManualChecking(checking, UserId);
				await slideCheckingsRepo.MarkManualExerciseCheckingAsChecked(checking, percent);
				await slideCheckingsRepo.MarkManualExerciseCheckingAsCheckedBeforeThis(checking);
				await visitsRepo.UpdateScoreForVisit(courseId, slide, userId);

				var visibleUnits = await unitsRepo.GetPublishedUnitIds(course);
				if (course.FindSlideById(slideId, false, visibleUnits) is not null)
					await NotifyAboutManualExerciseChecking(checking);

				await transaction.CommitAsync();
			}

			return Ok($"Submission {submissionId} scored {percent} successfully");
		}

		private async Task NotifyAboutManualExerciseChecking(ManualExerciseChecking checking)
		{
			var isRecheck = (await notificationsRepo
					.FindNotifications<PassedManualExerciseCheckingNotification>(n => n.CheckingId == checking.Id))
				.Any();

			var notification = new PassedManualExerciseCheckingNotification
			{
				Checking = checking,
				IsRecheck = isRecheck
			};

			await notificationsRepo.AddNotification(checking.CourseId, notification, UserId);
		}
	}
}