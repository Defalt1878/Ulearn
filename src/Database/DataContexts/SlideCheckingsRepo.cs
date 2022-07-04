using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Database.Extensions;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Core.Courses;
using Vostok.Logging.Abstractions;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Extensions;

namespace Database.DataContexts
{
	public class SlideCheckingsRepo
	{
		private readonly ULearnDb db;
		private static ILog log => LogProvider.Get().ForContext(typeof(SlideCheckingsRepo));

		public SlideCheckingsRepo(ULearnDb db)
		{
			this.db = db;
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
			await db.SaveChangesAsync().ConfigureAwait(false);

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
			await db.SaveChangesAsync().ConfigureAwait(false);

			return automaticChecking;
		}

		public async Task<ManualExerciseChecking> AddManualExerciseChecking(string courseId, Guid slideId, string userId, UserExerciseSubmission submission)
		{
			var manualChecking = new ManualExerciseChecking
			{
				Id = submission.Id,
				CourseId = courseId,
				SlideId = slideId,
				UserId = userId,
				Timestamp = DateTime.Now,
			};
			db.ManualExerciseCheckings.Add(manualChecking);

			await db.SaveChangesAsync().ConfigureAwait(false);

			return manualChecking;
		}

		public async Task RemoveWaitingManualCheckings<T>(string courseId, Guid slideId, string userId, bool startTransaction = true) where T : AbstractManualSlideChecking
		{
			using (var transaction = startTransaction ? db.Database.BeginTransaction() : null)
			{
				var checkings = GetSlideCheckingsByUser<T>(courseId, slideId, userId, noTracking: false)
					.AsEnumerable()
					.Where(c => !c.IsChecked && c.LockedById == null)
					.ToList();
				foreach (var checking in checkings)
				{
					checking.PreRemove(db);
					db.Set<T>().Remove(checking);
				}

				await db.SaveChangesAsync().ConfigureAwait(false);
				transaction?.Commit();
			}
		}

		private IQueryable<T> GetSlideCheckingsByUser<T>(string courseId, Guid slideId, string userId, bool noTracking = true) where T : AbstractSlideChecking
		{
			IQueryable<T> dbRef = db.Set<T>();
			if (noTracking)
				dbRef = dbRef.AsNoTracking();
			var items = dbRef.Where(c => c.CourseId == courseId && c.SlideId == slideId && c.UserId == userId);
			return items;
		}

		public async Task RemoveAttempts(string courseId, Guid slideId, string userId, bool saveChanges = true)
		{
			db.ManualQuizCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.AutomaticQuizCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.ManualExerciseCheckings.RemoveSlideAction(courseId, slideId, userId);
			db.AutomaticExerciseCheckings.RemoveSlideAction(courseId, slideId, userId);
			if (saveChanges)
				await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public bool IsSlidePassed(string courseId, Guid slideId, string userId)
		{
			return GetSlideCheckingsByUser<ManualQuizChecking>(courseId, slideId, userId).Any() ||
					GetSlideCheckingsByUser<AutomaticQuizChecking>(courseId, slideId, userId).Any() ||
					GetSlideCheckingsByUser<ManualExerciseChecking>(courseId, slideId, userId).Any() ||
					GetSlideCheckingsByUser<AutomaticExerciseChecking>(courseId, slideId, userId).Any(c => c.IsRightAnswer);
		}

		#region Slide Score Calculating

		public (int Score, int? Percent, bool IsReviewScore) GetExerciseSlideScoreAndPercent(string courseId, ExerciseSlide slide, string userId, int maxAutomaticScorePercent = 100)
		{
			var hasAutomaticChecking = slide.Exercise.HasAutomaticChecking();
			if (hasAutomaticChecking)
			{
				var isRightAnswer = GetSlideCheckingsByUser<AutomaticExerciseChecking>(courseId, slide.Id, userId)
					.Any(c => c.IsRightAnswer);
				if (!isRightAnswer)
					return (0, null, false);
			}

			var percent = GetLastReviewPercentForExerciseSlide(courseId, slide.Id, userId);
			var automaticScore = slide.Scoring.PassedTestsScore;
			return percent == null
				? (ConvertAutomaticScoreWithDeadLinePercentToScore(automaticScore, maxAutomaticScorePercent), null, false)
				: (ConvertExerciseManualCheckingPercentToScore(percent.Value, slide.Scoring.ScoreWithCodeReview), percent, true);
		}
		
		public static int ConvertAutomaticScoreWithDeadLinePercentToScore(int automaticScore, int maxAutomaticScorePercent)
		{
			return (int)Math.Ceiling(automaticScore * maxAutomaticScorePercent / 100m);
		}

		public static int ConvertExerciseManualCheckingPercentToScore(int manualCheckingPercent, int scoreWithCodeReview)
		{
			return (int)Math.Ceiling(manualCheckingPercent / 100m * scoreWithCodeReview);
		}

		public int? GetLastReviewPercentForExerciseSlide(string courseId, Guid slideId, string userId, DateTime? submissionBefore = null)
		{
			var query = db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.SlideId == slideId && c.UserId == userId && c.IsChecked)
				.Where(c => c.IsChecked);
			if (submissionBefore != null)
				query = query.Where(c => c.Submission.Timestamp < submissionBefore);
			var lastChecking = query
				.OrderByDescending(c => c.Timestamp)
				.FirstOrDefault();
			return lastChecking?.Percent;
		}

		public int GetUserScoreForQuizSlide(string courseId, Guid slideId, string userId, int maxAutomaticScorePercent = 10)
		{
			var manualScore = GetSlideCheckingsByUser<ManualQuizChecking>(courseId, slideId, userId).Select(c => c.Score).DefaultIfEmpty(0).Max();
			var automaticScore = GetSlideCheckingsByUser<AutomaticQuizChecking>(courseId, slideId, userId).Select(c => c.Score).DefaultIfEmpty(0).Max();
			return ConvertAutomaticScoreWithDeadLinePercentToScore(automaticScore, maxAutomaticScorePercent) + manualScore;
		}

		public List<(Guid SlideId, int Percent)> GetPassedManualExerciseCheckingsAndPercents(Course course, string userId, IEnumerable<Guid> visibleUnits)
		{
			var checkings = db.ManualExerciseCheckings
				.Where(c => c.CourseId == course.Id && c.UserId == userId && c.IsChecked)
				.Select(c => new { c.SlideId, c.Timestamp, c.Percent })
				.ToList();
			var slides = course.GetSlides(false, visibleUnits).OfType<ExerciseSlide>().Select(s => s.Id).ToHashSet();
			return checkings.GroupBy(s => s.SlideId)
				.Where(s => slides.Contains(s.Key))
				.Select(g =>
				{
					var lastPercent = g.Deprecated_MaxBy(c => c.Timestamp).Percent.Value;
					return (g.Key, lastPercent);
				}).ToList();
		}

		#endregion

		public IEnumerable<T> GetManualCheckingQueue<T>(ManualCheckingQueueFilterOptions options) where T : AbstractManualSlideChecking
		{
			var query = GetManualCheckingQueueFilterQuery<T>(options);
			query = query.OrderByDescending(c => c.Timestamp);

			const int reserveForStartedOrDoubleCheckedReviews = 100;
			if (options.Count > 0)
				query = query.Take(options.Count + reserveForStartedOrDoubleCheckedReviews);

			var enumerable = query.AsEnumerable();
			// Метод RemoveWaitingManualCheckings удалил непосещенные преподавателем старые ManualChecking. Здесь отфильтровываются посещенные преподавателем.
			enumerable = enumerable
				.GroupBy(g => new { g.UserId, g.SlideId })
				.Select(g => g.First())
				.OrderByDescending(c => c.Timestamp);

			if (options.Count > 0)
				enumerable = enumerable.Take(options.Count);

			return enumerable;
		}

		private IQueryable<T> GetManualCheckingQueueFilterQuery<T>(ManualCheckingQueueFilterOptions options) where T : AbstractManualSlideChecking
		{
			var query = db.Set<T>().Include(c => c.User).Where(c => c.CourseId == options.CourseId);
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
		public int GetQuizManualCheckingCount(string courseId, Guid slideId, string userId, DateTime? beforeSubmissionTimestamp)
		{
			var queue = db.ManualQuizCheckings.Where(c =>
				c.CourseId == courseId
				&& c.SlideId == slideId
				&& c.UserId == userId
				&& !c.IgnoreInAttemptsCount);
			if (beforeSubmissionTimestamp != null)
				queue = queue.Where(s => s.Submission.Timestamp < beforeSubmissionTimestamp);
			return queue.Count();
		}

		public HashSet<Guid> GetManualCheckingQueueSlideIds<T>(ManualCheckingQueueFilterOptions options) where T : AbstractManualSlideChecking
		{
			return GetManualCheckingQueue<T>(options).Select(c => c.SlideId).Distinct().AsEnumerable().ToHashSet();
		}

		public T FindManualCheckingById<T>(int id) where T : AbstractManualSlideChecking
		{
			return db.Set<T>().Find(id);
		}

		public async Task LockManualChecking<T>(T checkingItem, string lockedById) where T : AbstractManualSlideChecking
		{
			checkingItem.LockedById = lockedById;
			checkingItem.LockedUntil = DateTime.Now.Add(TimeSpan.FromMinutes(30));
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public async Task MarkManualQuizCheckingAsChecked(ManualQuizChecking queueItem, int score)
		{
			queueItem.LockedUntil = null;
			queueItem.IsChecked = true;
			queueItem.Score = score;
			await db.SaveChangesAsync().ConfigureAwait(false);
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
			var itemsForMark = db.Set<ManualExerciseChecking>()
				.Include(c => c.Submission)
				.Where(c => c.CourseId == queueItem.CourseId && c.UserId == queueItem.UserId && c.SlideId == queueItem.SlideId && c.Timestamp < queueItem.Timestamp)
				.OrderBy(c => c.Timestamp)
				.ToList();
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
				await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public async Task ProhibitFurtherExerciseManualChecking(string courseId, string userId, Guid slideId)
		{
			var checkings = db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.SlideId == slideId && c.ProhibitFurtherManualCheckings)
				.ToList();
			if (checkings.Count == 0)
				return;
			foreach (var checking in checkings)
				checking.ProhibitFurtherManualCheckings = true;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public async Task ResetManualCheckingLimitsForUser(string courseId, string userId)
		{
			await DisableProhibitFurtherManualCheckings(courseId, userId).ConfigureAwait(false);
			await NotCountOldAttemptsToQuizzesWithManualChecking(courseId, userId).ConfigureAwait(false);
		}

		public async Task DisableProhibitFurtherManualCheckings(string courseId, string userId, Guid? slideId = null)
		{
			var query = db.ManualExerciseCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.ProhibitFurtherManualCheckings);
			if (slideId != null)
				query = query.Where(c => c.SlideId == slideId);
			var checkingsWithProhibitFurther = query.ToList();
			if (checkingsWithProhibitFurther.Count == 0)
				return;
			foreach (var checking in checkingsWithProhibitFurther)
				checking.ProhibitFurtherManualCheckings = false;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public async Task NotCountOldAttemptsToQuizzesWithManualChecking(string courseId, string userId)
		{
			var checkings = db.ManualQuizCheckings
				.Where(c => c.CourseId == courseId && c.UserId == userId && c.IsChecked)
				.ToList();
			foreach (var checking in checkings)
				checking.IgnoreInAttemptsCount = true;
			await db.SaveChangesAsync().ConfigureAwait(false);
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
				AddingTime = setAddingTime ? DateTime.Now : null
			});

			await db.SaveChangesAsync().ConfigureAwait(false);

			/* Extract review from database to fill review.Author by EF's DynamicProxy */
			return db.ExerciseCodeReviews.AsNoTracking().FirstOrDefault(r => r.Id == review.Id);
		}

		public Task<ExerciseCodeReview> AddExerciseCodeReview(ManualExerciseChecking checking, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment, bool setAddingTime = true)
		{
			return AddExerciseCodeReview(null, checking, userId, startLine, startPosition, finishLine, finishPosition, comment, setAddingTime);
		}

		public Task<ExerciseCodeReview> AddExerciseCodeReview(UserExerciseSubmission submission, string userId, int startLine, int startPosition, int finishLine, int finishPosition, string comment, bool setAddingTime = false)
		{
			return AddExerciseCodeReview(submission, null, userId, startLine, startPosition, finishLine, finishPosition, comment, setAddingTime);
		}

		public ExerciseCodeReview FindExerciseCodeReviewById(int reviewId)
		{
			return db.ExerciseCodeReviews.Find(reviewId);
		}

		public async Task DeleteExerciseCodeReview(ExerciseCodeReview review)
		{
			review.IsDeleted = true;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public async Task UpdateExerciseCodeReview(ExerciseCodeReview review, string newComment)
		{
			review.Comment = newComment;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public Dictionary<int, List<ExerciseCodeReview>> GetExerciseCodeReviewForCheckings(IEnumerable<int> checkingsIds)
		{
			return db.ExerciseCodeReviews
				.Where(r => r.ExerciseCheckingId.HasValue && checkingsIds.Contains(r.ExerciseCheckingId.Value) && !r.IsDeleted)
				.GroupBy(r => r.ExerciseCheckingId)
				.ToDictionary(g => g.Key.Value, g => g.ToList());
		}

		public List<string> GetTopUserReviewComments(string courseId, Guid slideId, string userId, int count)
		{
			var sw = Stopwatch.StartNew();
			var result = db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId == userId && !r.HiddenFromTopComments && !r.IsDeleted)
				.ToList()
				.GroupBy(r => r.Comment)
				.OrderByDescending(g => g.Count())
				.ThenByDescending(g => g.Max(r => r.AddingTime))
				.Take(count)
				.Select(g => g.Key)
				.ToList();
			log.Info("GetTopUserReviewComments " + sw.ElapsedMilliseconds + " ms");
			return result;
		}

		public List<string> GetTopOtherUsersReviewComments(string courseId, Guid slideId, string userId, int count, List<string> excludeComments)
		{
			var sw = Stopwatch.StartNew();
			var excludeCommentsSet = excludeComments.ToHashSet();
			var result = db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId != userId && !r.HiddenFromTopComments && !r.IsDeleted)
				.GroupBy(r => r.Comment)
				.OrderByDescending(g => g.Count())
				.Take(count * 2)
				.Select(g => g.Key)
				.ToList()
				.Where(c => !excludeCommentsSet.Contains(c))
				.Take(count)
				.ToList();
			log.Info("GetTopOtherUsersReviewComments " + sw.ElapsedMilliseconds + " ms");
			return result;
		}

		public async Task HideFromTopCodeReviewComments(string courseId, Guid slideId, string userId, string comment)
		{
			var reviews = db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && r.AuthorId == userId && r.Comment == comment && !r.IsDeleted)
				.ToList();
			foreach (var review in reviews)
				review.HiddenFromTopComments = true;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}

		public List<ExerciseCodeReview> GetLastYearReviewComments(string courseId, Guid slideId)
		{
			var sw = Stopwatch.StartNew();
			var lastYear = DateTime.Today.AddYears(-1);
			var result = db.ExerciseCodeReviews
				.Where(r => r.CourseId == courseId && r.SlideId == slideId && !r.IsDeleted && r.AddingTime > lastYear)
				.ToList();
			log.Info("GetLastYearReviewComments " + sw.ElapsedMilliseconds + " ms");
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
			await db.SaveChangesAsync().ConfigureAwait(false);

			/* Extract review from database to fill review.Author by EF's DynamicProxy */
			return db.ExerciseCodeReviewComments.AsNoTracking().FirstOrDefault(r => r.Id == codeReviewComment.Id);
		}

		public ExerciseCodeReviewComment FindExerciseCodeReviewCommentById(int commentId)
		{
			return db.ExerciseCodeReviewComments.Find(commentId);
		}

		public async Task DeleteExerciseCodeReviewComment(ExerciseCodeReviewComment comment)
		{
			comment.IsDeleted = true;
			await db.SaveChangesAsync().ConfigureAwait(false);
		}
	}
}