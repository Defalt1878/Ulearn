using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Extensions;
using Database.Models;
using Database.Models.Quizzes;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Extensions;

namespace Database.Repos
{
	public class SlideCheckingsRepo : ISlideCheckingsRepo
	{
		private readonly UlearnDb db;

		[ItemCanBeNull]
		private readonly Lazy<IVisitsRepo> visitsRepo;

		public SlideCheckingsRepo(UlearnDb db, [CanBeNull] IServiceProvider serviceProvider)
		{
			this.db = db;
			visitsRepo = new Lazy<IVisitsRepo>(() => serviceProvider?.GetRequiredService<IVisitsRepo>());
		}

		public async Task<ManualQuizChecking> AddManualQuizChecking(UserQuizSubmission submission, string courseId, Guid slideId, string userId)
		{
			var manualChecking = new ManualQuizChecking
			{
				Submission = submission,
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
			};
			db.ManualQuizCheckings.Add(manualChecking);
			await db.SaveChangesAsync();

			return manualChecking;
		}

		public async Task<AutomaticQuizChecking> AddAutomaticQuizChecking(UserQuizSubmission submission, string courseId, Guid slideId, string userId, int automaticScore)
		{
			var automaticChecking = new AutomaticQuizChecking
			{
				Submission = submission,
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
				Score = automaticScore,
			};
			db.AutomaticQuizCheckings.Add(automaticChecking);
			await db.SaveChangesAsync();

			return automaticChecking;
		}

		public async Task<bool> HasManualExerciseChecking(string courseId, Guid slideId, string userId, int submissionId)
		{
			return await db.ManualExerciseCheckings
				.AnyAsync(c => c.CourseId == courseId && c.UserId == userId && c.SlideId == slideId && c.Id == submissionId);
		}

		public async Task<ManualExerciseChecking> AddManualExerciseChecking(string courseId, Guid slideId, string userId, int submissionId)
		{
			var manualChecking = new ManualExerciseChecking
			{
				Id = submissionId,
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
			};
			db.ManualExerciseCheckings.Add(manualChecking);

			await db.SaveChangesAsync();

			return manualChecking;
		}

		public async Task RemoveWaitingManualCheckings<T>(string courseId, Guid slideId, string userId, bool startTransaction = true) where T : AbstractManualSlideChecking
		{
			using (var transaction = startTransaction ? db.Database.BeginTransaction() : null)
			{
				var checkings = (await GetSlideCheckingsByUser<T>(courseId, slideId, userId)
						.ToListAsync())
					.Where(c => !c.IsChecked && c.LockedById == null)
					.ToList();
				foreach (var checking in checkings)
				{
					checking.PreRemove(db);
					db.Set<T>().Remove(checking);
				}

				await db.SaveChangesAsync();
				if (transaction != null)
					await transaction.CommitAsync();
			}
		}

		private IQueryable<T> GetSlideCheckingsByUser<T>(string courseId, Guid slideId, string userId) where T : AbstractSlideChecking
		{
			return db
				.Set<T>()
				.Where(c => c.CourseId == courseId && c.SlideId == slideId && c.UserId == userId);
		}

		public async Task RemoveAttempts(string courseId, Guid slideId, string userId, bool saveChanges = true)
		{
			db.ManualQuizCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.AutomaticQuizCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.ManualExerciseCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.AutomaticExerciseCheckings.RemoveSlideAction(courseId, slideId, userId);
			if (saveChanges)
				await db.SaveChangesAsync();
		}

		public async Task<bool> IsSlidePassed(string courseId, Guid slideId, string userId)
		{
			return await GetSlideCheckingsByUser<ManualQuizChecking>(courseId, slideId, userId).AnyAsync() ||
					await GetSlideCheckingsByUser<AutomaticQuizChecking>(courseId, slideId, userId).AnyAsync() ||
					await GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId).AnyAsync() ||
					await GetSlideCheckingsByUser<AutomaticExerciseChecking>(courseId, slideId, userId).AnyAsync(c => c.IsRightAnswer);
		}

		#region Slide Score Calculating

		public async Task<(int Score, int? Percent, bool IsReviewScore)> GetExerciseSlideScoreAndPercent(string courseId, ExerciseSlide slide, string userId, int maxAutomaticScorePercent = 100)
		{
			var hasAutomaticChecking = slide.Exercise.HasAutomaticChecking();
			if (hasAutomaticChecking)
			{
				var isRightAnswer = await GetSlideCheckingsByUser<AutomaticExerciseChecking>(courseId, slide.Id, userId)
					.AnyAsync(c => c.IsRightAnswer);
				if (!isRightAnswer)
					return (0, null, false);
			}

			var percent = await GetLastReviewPercentForExerciseSlide(courseId, slide.Id, userId);
			var automaticScore = slide.Scoring.PassedTestsScore;
			return percent == null
				? (ConvertAutomaticScoreWithDeadLinePercentToScore(automaticScore, maxAutomaticScorePercent), null, false)
				: (ConvertExerciseManualCheckingPercentToScore(percent.Value, slide.Scoring.ScoreWithCodeReview), percent, true);
		}

		public static int ConvertExerciseManualCheckingPercentToScore(int manualCheckingPercent, int scoreWithCodeReview)
		{
			return (int)Math.Ceiling(manualCheckingPercent / 100m * scoreWithCodeReview);
		}

		public static int ConvertAutomaticScoreWithDeadLinePercentToScore(int automaticScore, int maxAutomaticScorePercent)
		{
			return (int)Math.Ceiling(automaticScore * maxAutomaticScorePercent / 100m);
		}

		public async Task<int?> GetLastReviewPercentForExerciseSlide(string courseId, Guid slideId, string userId, DateTime? submissionBefore = null)
		{
			var query = db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.IsChecked && c.UserId == userId && c.SlideId == slideId);
			if (submissionBefore != null)
				query = query.Where(c => c.Submission.Timestamp < submissionBefore);
			var lastChecking = await query
				.OrderByDescending(c => c.Timestamp)
				.FirstOrDefaultAsync();
			return lastChecking?.Percent;
		}

		public async Task<int> GetUserScoreForQuizSlide(string courseId, Guid slideId, string userId, int maxAutomaticScorePercent = 100)
		{
			var manualScore = await GetSlideCheckingsByUser<ManualQuizChecking>(courseId, slideId, userId)
				.Select(c => c.Score)
				.DefaultIfEmpty()
				.MaxAsync();
			var automaticScore = await GetSlideCheckingsByUser<AutomaticQuizChecking>(courseId, slideId, userId)
				.Select(c => c.Score)
				.DefaultIfEmpty()
				.MaxAsync();
			return ConvertAutomaticScoreWithDeadLinePercentToScore(automaticScore, maxAutomaticScorePercent) + manualScore;
		}

		public List<(Guid SlideId, int Percent)> GetPassedManualExerciseCheckingsAndPercents(Course course, string userId, IEnumerable<Guid> visibleUnits)
		{
			var checkings = db.ManualExerciseCheckings
				.Where(c => c.CourseId == course.Id && c.IsChecked && c.UserId == userId)
				.Select(c => new { c.SlideId, c.Timestamp, c.Percent })
				.ToList();
			var slides = course.GetSlides(false, visibleUnits).OfType<ExerciseSlide>().Select(s => s.Id).ToHashSet();
			return checkings.GroupBy(s => s.SlideId)
				.Where(s => slides.Contains(s.Key))
				.Select(g =>
				{
					var lastPercent = g.MaxBy(c => c.Timestamp).Percent.Value;
					return (g.Key, lastPercent);
				}).ToList();
		}

		#endregion

		public async Task<List<T>> GetManualCheckingQueue<T>(ManualCheckingQueueFilterOptionsObsolete options) where T : AbstractManualSlideChecking
		{
			var query = GetManualCheckingQueueFilterQuery<T>(options);
			query = query.OrderByDescending(c => c.Timestamp);

			const int reserveForStartedOrDoubleCheckedReviews = 100;
			if (options.Count > 0)
				query = query.Take(options.Count + reserveForStartedOrDoubleCheckedReviews);

			IEnumerable<T> enumerable = await query.ToListAsync();
			// Метод RemoveWaitingManualCheckings удалил непосещенные преподавателем старые ManualChecking. Здесь отфильтровываются посещенные преподавателем.
			enumerable = enumerable
				.GroupBy(g => new { g.UserId, g.SlideId })
				.Select(g => g.First())
				.OrderByDescending(c => c.Timestamp);

			if (options.Count > 0)
				enumerable = enumerable.Take(options.Count);

			return enumerable.ToList();
		}

		public async Task<IEnumerable<T>> GetManualCheckingQueue<T>(ManualCheckingQueueFilterOptions options) where T : AbstractManualSlideChecking
		{
			var query = db.Set<T>()
				.Where(c => c.CourseId == options.CourseId);

			query = options.IsReviewed
				? query.Where(c => c.IsChecked)
				: query.Where(c => !c.IsChecked);

			if (options.UserIds is not null)
				query = options.IsUserIdsSupplement
					? query.Where(c => !options.UserIds.Contains(c.UserId))
					: query.Where(c => options.UserIds.Contains(c.UserId));
			
			if (options.SlideIds is not null)
				query = query.Where(c => !options.SlideIds.Contains(c.SlideId));

			var groupQuery = query
				.GroupBy(c => new { c.UserId, c.SlideId })
				.Select(g => new
				{
					LastTimeStamp = g.OrderByDescending(c => c.Timestamp).First().Timestamp,
					LastEntry = g.OrderByDescending(c => c.Timestamp).First()
				});
			
			groupQuery = options.DateSort is DateSort.Ascending
				? groupQuery.OrderBy(c => c.LastTimeStamp)
				: groupQuery.OrderByDescending(c => c.LastTimeStamp);

			if (options.Count > 0)
				groupQuery = groupQuery.Take(options.Count);

			return (await groupQuery.ToListAsync())
				.Select(r => r.LastEntry);
		}

		public IQueryable<T> GetManualCheckingQueueFilterQuery<T>(ManualCheckingQueueFilterOptionsObsolete options) where T : AbstractManualSlideChecking
		{
			var query = db.Set<T>()
				.Include(c => c.User)
				.Where(c => c.CourseId == options.CourseId);
			if (options.OnlyChecked.HasValue)
				query = options.OnlyChecked.Value ? query.Where(c => c.IsChecked) : query.Where(c => !c.IsChecked);
			if (options.SlidesIds != null)
				query = query.Where(c => options.SlidesIds.Contains(c.SlideId));
			if (options.UserIds != null)
			{
				if (options.IsUserIdsSupplement)
					query = query.Where(c => !options.UserIds.Contains(c.UserId));
				else
					query = query.Where(c => options.UserIds.Contains(c.UserId));
			}

			return query;
		}

		/// For calculating not checked submissions as well as checked ones
		public async Task<int> GetQuizManualCheckingCount(string courseId, Guid slideId, string userId, DateTime? beforeTimestamp)
		{
			var queue = db.ManualQuizCheckings.Where(c =>
				c.CourseId == courseId
				&& c.SlideId == slideId
				&& c.UserId == userId
				&& !c.IgnoreInAttemptsCount);
			if (beforeTimestamp != null)
				queue = queue.Where(s => s.Timestamp < beforeTimestamp);
			return await queue.CountAsync();
		}

		public async Task<T> FindManualCheckingById<T>(int id) where T : AbstractManualSlideChecking
		{
			return await db.Set<T>().FindAsync(id);
		}

		public async Task<bool> IsProhibitedToSendExerciseToManualChecking(string courseId, Guid slideId, string userId)
		{
			return await GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId)
				.AnyAsync(c => c.ProhibitFurtherManualCheckings);
		}

		public async Task LockManualChecking<T>(T checkingItem, string lockedById) where T : AbstractManualSlideChecking
		{
			checkingItem.LockedById = lockedById;
			checkingItem.LockedUntil = DateTime.Now.Add(TimeSpan.FromMinutes(30));
			await db.SaveChangesAsync();
		}

		public async Task MarkManualQuizCheckingAsChecked(ManualQuizChecking queueItem, int score)
		{
			queueItem.LockedUntil = null;
			queueItem.IsChecked = true;
			queueItem.Score = score;
			await db.SaveChangesAsync();
		}

		public async Task MarkManualExerciseCheckingAsChecked(ManualExerciseChecking queueItem, int percent)
		{
			queueItem.LockedUntil = null;
			queueItem.IsChecked = true;
			queueItem.Percent = percent;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		// Помечает оцененными посещенные но не оцененные старые ревью. Большинство удаляется RemoveWaitingManualCheckings
		public async Task MarkManualExerciseCheckingAsCheckedBeforeThis(ManualExerciseChecking queueItem)
		{
			var itemsForMark = (await db.Set<ManualExerciseChecking>()
					.Include(c => c.Submission)
					.Where(c => c.CourseId == queueItem.CourseId && c.SlideId == queueItem.SlideId && c.UserId == queueItem.UserId && c.Timestamp < queueItem.Timestamp)
					.ToListAsync())
				.OrderBy(c => c.Submission.Timestamp)
				.ThenBy(c => c.Timestamp);
			int? percent = 0;
			var changed = false;
			foreach (var item in itemsForMark)
			{
				if (item.IsChecked)
				{
					percent = item.Percent;
				}
				else
				{
					item.LockedUntil = null;
					item.IsChecked = true;
					item.Percent = percent;
					changed = true;
				}
			}

			if (changed)
				await db.SaveChangesAsync();
		}

		public async Task ProhibitFurtherExerciseManualChecking(string courseId, string userId, Guid slideId)
		{
			var checkings = await db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.SlideId == slideId && !c.ProhibitFurtherManualCheckings)
				.ToListAsync();
			if (checkings.Count == 0)
				return;
			foreach (var checking in checkings)
				checking.ProhibitFurtherManualCheckings = true;
			await db.SaveChangesAsync();
		}

		public async Task ResetManualCheckingLimitsForUser(string courseId, string userId)
		{
			await EnableFurtherManualCheckings(courseId, userId);
			await NotCountOldAttemptsToQuizzesWithManualChecking(courseId, userId);
		}

		public async Task ResetAutomaticCheckingLimitsForUser(string courseId, string userId)
		{
			await NotCountOldAttemptsToQuizzesWithAutomaticChecking(courseId, userId);
			if (visitsRepo.Value != null)
				await visitsRepo.Value.UnskipAllSlides(courseId, userId);
		}

		public async Task EnableFurtherManualCheckings(string courseId, string userId, Guid? slideId = null)
		{
			var query = db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.ProhibitFurtherManualCheckings);
			if (slideId != null)
				query = query.Where(c => c.SlideId == slideId);
			var checkingsWithProhibitFurther = await query.ToListAsync();
			if (checkingsWithProhibitFurther.Count == 0)
				return;
			foreach (var checking in checkingsWithProhibitFurther)
				checking.ProhibitFurtherManualCheckings = false;
			await db.SaveChangesAsync();
		}

		public async Task NotCountOldAttemptsToQuizzesWithManualChecking(string courseId, string userId)
		{
			var checkings = await db.ManualQuizCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.IsChecked)
				.ToListAsync();
			foreach (var checking in checkings)
				checking.IgnoreInAttemptsCount = true;
			await db.SaveChangesAsync();
		}

		public async Task NotCountOldAttemptsToQuizzesWithAutomaticChecking(string courseId, string userId)
		{
			var checkings = await db.AutomaticQuizCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId)
				.ToListAsync();
			foreach (var checking in checkings)
				checking.IgnoreInAttemptsCount = true;
			await db.SaveChangesAsync();
		}

		private async Task<ExerciseCodeReview> AddExerciseCodeReview([CanBeNull] UserExerciseSubmission submission, [CanBeNull] ManualExerciseChecking checking, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment, bool setAddingTime)
		{
			var review = db.ExerciseCodeReviews.Add(new ExerciseCodeReview
			{
				CourseId = submission?.CourseId ?? checking?.CourseId,
				SlideId = (submission?.SlideId ?? checking?.SlideId)!.Value,
				SubmissionAuthorId = submission?.UserId ?? checking?.UserId,
				AuthorId = userId,
				Comment = comment,
				ExerciseCheckingId = checking?.Id,
				SubmissionId = submission?.Id,
				StartLine = startLine,
				StartPosition = startPosition,
				FinishLine = finishLine,
				FinishPosition = finishPosition,
				AddingTime = setAddingTime ? DateTime.Now : null,
			});

			await db.SaveChangesAsync();

			return await db.ExerciseCodeReviews.FirstOrDefaultAsync(r => r.Id == review.Entity.Id);
		}

		public Task<ExerciseCodeReview> AddExerciseCodeReview(ManualExerciseChecking checking, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment, bool setAddingTime = true)
		{
			return AddExerciseCodeReview(null, checking, userId, startLine, startPosition, finishLine, finishPosition, comment, setAddingTime);
		}

		public Task<ExerciseCodeReview> AddExerciseCodeReview([CanBeNull] UserExerciseSubmission submission, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment, bool setAddingTime = false)
		{
			return AddExerciseCodeReview(submission, null, userId, startLine, startPosition, finishLine, finishPosition, comment, setAddingTime);
		}

		public async Task<ExerciseCodeReview> FindExerciseCodeReviewById(int reviewId)
		{
			return await db.ExerciseCodeReviews.FindAsync(reviewId);
		}

		public async Task DeleteExerciseCodeReview(ExerciseCodeReview review)
		{
			review.IsDeleted = true; //TODO delete instead IsDeleted
			await db.SaveChangesAsync();
		}

		public async Task UpdateExerciseCodeReview(ExerciseCodeReview review, string newComment)
		{
			review.Comment = newComment;
			await db.SaveChangesAsync();
		}

		public async Task<Dictionary<int, List<ExerciseCodeReview>>> GetExerciseCodeReviewForCheckings(IEnumerable<int> checkingsIds)
		{
			return (await db.ExerciseCodeReviews
					.Where(r => r.ExerciseCheckingId.HasValue && checkingsIds.Contains(r.ExerciseCheckingId.Value) && !r.IsDeleted)
					.ToListAsync())
				.GroupBy(r => r.ExerciseCheckingId)
				.ToDictionary(g => g.Key.Value, g => g.ToList());
		}

		public async Task<List<string>> GetTopUserReviewComments(string courseId, Guid slideId, string userId, int count)
		{
			var result = await db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId == userId && !r.HiddenFromTopComments && !r.IsDeleted)
				.Select(r => new { r.Comment, r.AddingTime })
				.GroupBy(r => r.Comment)
				.Select(g => new
				{
					g.Key,
					Count = g.Count(),
					Timestamp = g.Max(r => r.AddingTime)
				})
				.OrderByDescending(g => g.Count)
				.ThenByDescending(g => g.Timestamp)
				.Take(count)
				.Select(g => g.Key)
				.ToListAsync();
			return result;
		}

		public async Task<List<string>> GetTopOtherUsersReviewComments(string courseId, Guid slideId, string userId, int count, List<string> excludeComments)
		{
			var excludeCommentsSet = excludeComments.ToHashSet();
			var result = (await db.ExerciseCodeReviews
					.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId != userId && !r.HiddenFromTopComments && !r.IsDeleted)
					.GroupBy(r => r.Comment)
					.Select(g => new { g.Key, Count = g.Count() })
					.OrderByDescending(g => g.Count)
					.Take(count * 2)
					.Select(g => g.Key)
					.ToListAsync())
				.Where(c => !excludeCommentsSet.Contains(c))
				.Take(count)
				.ToList();
			return result;
		}

		public async Task<Dictionary<string, List<Guid>>> GetSlideIdsWaitingForManualExerciseCheckAsync(string courseId, IEnumerable<string> userIds)
		{
			return (await db.ManualExerciseCheckings
					.Where(c => c.CourseId == courseId && userIds.Contains(c.UserId) && !c.IsChecked)
					.Select(c => new { c.UserId, c.SlideId })
					.Distinct()
					.ToListAsync())
				.GroupBy(c => c.UserId)
				.ToDictionary(g => g.Key, g => g.Select(c => c.SlideId).ToList());
		}

		public async Task<Dictionary<string, List<Guid>>> GetProhibitFurtherManualCheckingSlides(string courseId, IEnumerable<string> userIds)
		{
			return (await db.ManualExerciseCheckings
					.Where(c => c.CourseId == courseId && userIds.Contains(c.UserId) && c.ProhibitFurtherManualCheckings)
					.Select(c => new { c.UserId, c.SlideId })
					.Distinct()
					.ToListAsync())
				.GroupBy(c => c.UserId)
				.ToDictionary(g => g.Key, g => g.Select(c => c.SlideId).ToList());
		}

		public async Task HideFromTopCodeReviewComments(string courseId, Guid slideId, string userId, string comment)
		{
			var reviews = await db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId == userId && r.Comment == comment && !r.IsDeleted)
				.ToListAsync();
			foreach (var review in reviews)
				review.HiddenFromTopComments = true;
			await db.SaveChangesAsync();
		}

		public async Task<List<ExerciseCodeReview>> GetLastYearReviewComments(string courseId, Guid slideId)
		{
			var lastYear = DateTime.Today.AddYears(-1);
			var result = await db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && !r.IsDeleted && r.AddingTime > lastYear)
				.ToListAsync();
			return result;
		}

		public async Task<ExerciseCodeReviewComment> AddExerciseCodeReviewComment(string authorId, int reviewId, string text)
		{
			var codeReviewComment = new ExerciseCodeReviewComment
			{
				AuthorId = authorId,
				ReviewId = reviewId,
				Text = text,
				IsDeleted = false,
				AddingTime = DateTime.Now,
			};

			db.ExerciseCodeReviewComments.Add(codeReviewComment);
			await db.SaveChangesAsync();

			return await db.ExerciseCodeReviewComments.FirstOrDefaultAsync(r => r.Id == codeReviewComment.Id);
		}

		public async Task EditExerciseCodeReviewComment(ExerciseCodeReviewComment codeReviewComment, string text)
		{
			codeReviewComment.Text = text;
			await db.SaveChangesAsync();
		}

		public async Task<ExerciseCodeReviewComment> FindExerciseCodeReviewCommentById(int commentId)
		{
			return await db.ExerciseCodeReviewComments.FindAsync(commentId);
		}

		public async Task<List<ExerciseCodeReviewComment>> GetExerciseCodeReviewComments(string courseId, Guid slideId, string userId)
		{
			return await db.ExerciseCodeReviewComments
				.Include(c => c.Author)
				.Where(c =>
					c.Review.CourseId == courseId
					&& c.Review.SlideId == slideId
					&& c.Review.SubmissionAuthorId == userId
					&& !c.Review.IsDeleted
					&& !c.IsDeleted)
				.ToListAsync();
		}

		public async Task<List<string>> GetLastUsedExerciseCodeReviewsTexts(string courseId, Guid slideId, string instructorId, int count, List<string> skipReviews = null)
		{
			IEnumerable<string> codeReviews = (await db.ExerciseCodeReviews
					.Where(c =>
						c.CourseId == courseId
						&& c.SlideId == slideId
						&& c.AuthorId == instructorId
						&& c.AddingTime != null
						&& !c.IsDeleted)
					.OrderByDescending(c => c.AddingTime)
					.Select(c => new { c.Comment, c.AddingTime })
					.Take(count + 40)
					.ToListAsync())
				.GroupBy(c => c.Comment)
				.Select(group => (group.Key, group.OrderByDescending(c => c.AddingTime)))
				.OrderByDescending(group => group.Item2.First().AddingTime)
				.Select(g => g.Key);

			if (skipReviews != null)
			{
				var skipReviewsHashSet = skipReviews.ToHashSet();
				codeReviews = codeReviews.Where(c => !skipReviewsHashSet.Contains(c));
			}

			return codeReviews
				.Take(count)
				.ToList();
		}

		public async Task DeleteExerciseCodeReviewComment(ExerciseCodeReviewComment comment)
		{
			comment.IsDeleted = true;
			await db.SaveChangesAsync();
		}

		public async Task<DateTime?> GetExerciseLastRightAnswerDate(string courseId, Guid slideId)
		{
			return await db.AutomaticExerciseCheckings
				.Where(c => c.CourseId == courseId && c.SlideId == slideId)
				.Select(c => (DateTime?)c.Timestamp)
				.OrderByDescending(t => t)
				.FirstOrDefaultAsync();
		}

		public async Task<int> GetExerciseUsersCount(string courseId, Guid slideId)
		{
			return await db.ExerciseAttemptedUsersCounts
				.Where(c => c.CourseId == courseId && c.SlideId == slideId)
				.Select(c => c.AttemptedUsersCount)
				.FirstOrDefaultAsync();
		}

		public async Task<int> GetExerciseUsersWithRightAnswerCount(string courseId, Guid slideId)
		{
			return await db.ExerciseUsersWithRightAnswerCounts
				.Where(c => c.CourseId == courseId && c.SlideId == slideId)
				.Select(c => c.UsersWithRightAnswerCount)
				.FirstOrDefaultAsync();
		}

		public async Task RefreshExerciseStatisticsMaterializedViews()
		{
			db.Database.SetCommandTimeout(TimeSpan.FromMinutes(2));
			await db.RefreshMaterializedView(ExerciseAttemptedUsersCount.ViewName);
			await db.RefreshMaterializedView(ExerciseUsersWithRightAnswerCount.ViewName);
			db.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
		}

		public async Task<HashSet<Guid>> GetManualCheckingQueueSlideIds<T>(ManualCheckingQueueFilterOptionsObsolete options) where T : AbstractManualSlideChecking
		{
			return (await GetManualCheckingQueue<T>(options))
				.Select(c => c.SlideId)
				.Distinct()
				.AsEnumerable()
				.ToHashSet();
		}
	}
}