using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AntiPlagiarism.Api;
using AntiPlagiarism.Api.Models.Parameters;
using Database;
using Database.Models;
using Database.Models.Comments;
using Database.Repos;
using Database.Repos.Comments;
using Database.Repos.Groups;
using Database.Repos.SystemAccessesRepo;
using Database.Repos.Users;
using GitCourseUpdater;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core;
using Ulearn.Core.Configuration;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Units;
using Ulearn.Core.CSharp;
using Ulearn.Core.Extensions;
using Ulearn.Core.Helpers;
using uLearn.Web.Core.Authorization;
using uLearn.Web.Core.Extensions;
using uLearn.Web.Core.Models;
using uLearn.Web.Core.Utils;
using Vostok.Logging.Abstractions;

namespace uLearn.Web.Core.Controllers;

[Authorize(Policy = UlearnAuthorizationConstants.InstructorsPolicyName)]
public class AdminController : Controller
{
	private static ILog log => LogProvider.Get().ForContext(typeof(AdminController));

	private readonly IWebCourseManager courseManager;
	private readonly ICourseStorage courseStorage;
	private readonly UlearnDb db;
	private readonly IUsersRepo usersRepo;
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly ICommentsRepo commentsRepo;
	private readonly ICommentPoliciesRepo commentPoliciesRepo;
	private readonly UlearnUserManager userManager;
	private readonly ICoursesRepo coursesRepo;
	private readonly IGroupsRepo groupsRepo;
	private readonly IGroupAccessesRepo groupAccessesRepo;
	private readonly ISlideCheckingsRepo slideCheckingsRepo;
	private readonly IUserSolutionsRepo userSolutionsRepo;
	private readonly ICertificatesRepo certificatesRepo;
	private readonly IAdditionalScoresRepo additionalScoresRepo;
	private readonly INotificationsRepo notificationsRepo;
	private readonly ISystemAccessesRepo systemAccessesRepo;
	private readonly IStyleErrorsRepo styleErrorsRepo;
	private readonly CertificateGenerator certificateGenerator;
	private readonly string gitSecret;
	private readonly DirectoryInfo reposDirectory;
	private readonly IAntiPlagiarismClient antiPlagiarismClient;
	private readonly ITempCoursesRepo tempCoursesRepo;

	public AdminController(
		UlearnDb db,
		IUsersRepo usersRepo,
		ICourseRolesRepo courseRolesRepo,
		ICommentsRepo commentsRepo,
		UlearnUserManager userManager,
		ICoursesRepo coursesRepo,
		IGroupsRepo groupsRepo,
		IGroupAccessesRepo groupAccessesRepo,
		ISlideCheckingsRepo slideCheckingsRepo,
		IUserSolutionsRepo userSolutionsRepo,
		ICertificatesRepo certificatesRepo,
		IAdditionalScoresRepo additionalScoresRepo,
		ICommentPoliciesRepo commentPoliciesRepo,
		INotificationsRepo notificationsRepo,
		ISystemAccessesRepo systemAccessesRepo,
		IStyleErrorsRepo styleErrorsRepo,
		CertificateGenerator certificateGenerator,
		// string gitSecret,
		// DirectoryInfo reposDirectory,
		IWebCourseManager webCourseManager,
		ITempCoursesRepo tempCoursesRepo
	)
	{
		this.db = db;
		this.usersRepo = usersRepo;
		this.courseRolesRepo = courseRolesRepo;
		this.commentsRepo = commentsRepo;
		this.userManager = userManager;
		this.coursesRepo = coursesRepo;
		this.groupsRepo = groupsRepo;
		this.groupAccessesRepo = groupAccessesRepo;
		this.commentPoliciesRepo = commentPoliciesRepo;
		this.slideCheckingsRepo = slideCheckingsRepo;
		this.userSolutionsRepo = userSolutionsRepo;
		this.certificatesRepo = certificatesRepo;
		this.additionalScoresRepo = additionalScoresRepo;
		this.notificationsRepo = notificationsRepo;
		this.systemAccessesRepo = systemAccessesRepo;
		this.styleErrorsRepo = styleErrorsRepo;
		this.certificateGenerator = certificateGenerator;
		this.tempCoursesRepo = tempCoursesRepo;
		this.courseManager = webCourseManager;
		this.courseStorage = WebCourseManager.CourseStorageInstance;

		this.reposDirectory = CourseManager.CoursesDirectory.GetSubdirectory("Repos");
		var configuration = ApplicationConfiguration.Read<UlearnConfiguration>();
		this.gitSecret = configuration.Git.Webhook.Secret;
		var antiplagiarismClientConfiguration = ApplicationConfiguration.Read<UlearnConfiguration>().AntiplagiarismClient;
		antiPlagiarismClient = new AntiPlagiarismClient(antiplagiarismClientConfiguration.Endpoint, antiplagiarismClientConfiguration.Token);
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> SpellingErrors(Guid versionId)
	{
		var versionFile = await coursesRepo.GetVersionFile(versionId);
		using (var courseDirectory = await courseManager.ExtractCourseVersionToTemporaryDirectory(versionFile.CourseId, new CourseVersionToken(versionFile.CourseVersionId), versionFile.File))
		{
			var (course, exception) = courseManager.LoadCourseFromDirectory(versionFile.CourseId, courseDirectory.DirectoryInfo);
			if (exception != null)
				throw exception;
			var model = course.SpellCheck(courseDirectory.DirectoryInfo.FullName);
			return PartialView(model);
		}
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> Units(string courseId)
	{
		var course = courseStorage.GetCourse(courseId);
		var appearances = await db.UnitAppearances.Where(u => u.CourseId == course.Id).ToListAsync();
		var unitAppearances = course.GetUnitsNotSafe()
			.Select(unit => Tuple.Create(unit, appearances.FirstOrDefault(a => a.UnitId == unit.Id)))
			.ToList();
		return View(new UnitsListViewModel(course.Id, course.Title, unitAppearances, DateTime.Now));
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<RedirectToActionResult> SetPublishTime(string courseId, Guid unitId, string publishTime)
	{
		var oldInfo = await db.UnitAppearances.Where(u => u.CourseId == courseId && u.UnitId == unitId).ToListAsync();
		db.UnitAppearances.RemoveRange(oldInfo);
		var unitAppearance = new UnitAppearance
		{
			CourseId = courseId,
			UnitId = unitId,
			UserName = User.Identity.Name,
			PublishTime = DateTime.Parse(publishTime),
		};
		db.UnitAppearances.Add(unitAppearance);
		await db.SaveChangesAsync();
		return RedirectToAction("Units", new { courseId });
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<RedirectToActionResult> RemovePublishTime(string courseId, Guid unitId)
	{
		var unitAppearance = await db.UnitAppearances.FirstOrDefaultAsync(u => u.CourseId == courseId && u.UnitId == unitId);
		if (unitAppearance != null)
		{
			db.UnitAppearances.Remove(unitAppearance);
			await db.SaveChangesAsync();
		}

		return RedirectToAction("Units", new { courseId });
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> DownloadPackage(string courseId)
	{
		var course = courseStorage.GetCourse(courseId);
		byte[] content;
		if (course.IsTempCourse())
			content = await courseManager.GetTempCourseZipBytes(courseId).ConfigureAwait(false);
		else
		{
			var publishedVersionFile = await coursesRepo.GetPublishedVersionFile(courseId);
			content = publishedVersionFile.File;
		}

		return File(content, "application/zip", courseId + ".zip");
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> DownloadVersion(string courseId, Guid versionId)
	{
		var publishedVersionFile = await coursesRepo.GetVersionFile(versionId);
		return File(publishedVersionFile.File, "application/zip", courseId + ".zip");
	}

	private async Task NotifyAboutCourseVersion(string courseId, Guid versionId, string userId)
	{
		var notification = new UploadedPackageNotification
		{
			CourseVersionId = versionId,
		};
		await notificationsRepo.AddNotification(courseId, notification, userId);
	}

	private async Task NotifyAboutCourseUploadFromRepoError(string courseId, string commitHash, string repoUrl)
	{
		var notification = new NotUploadedPackageNotification
		{
			CommitHash = commitHash,
			RepoUrl = repoUrl
		};
		var bot = await usersRepo.GetUlearnBotUser();
		await notificationsRepo.AddNotification(courseId, notification, bot.Id);
	}

	private static readonly Regex httpsGitLinkRegex = new Regex(@"https://(?<host>.+)/(?<login>.+)/(?<repo>.+)\.git", RegexOptions.Compiled);

	[HttpPost]
	[ValidateAntiForgeryToken]
	[HandleHttpAntiForgeryException]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> SaveCourseRepoSettings(string courseId, string repoUrl, string branch, string pathToCourseXml, bool isWebhookEnabled, string submitButton)
	{
		if (submitButton == "Save")
		{
			repoUrl = repoUrl.NullIfEmptyOrWhitespace();
			if (repoUrl != null)
			{
				var match = httpsGitLinkRegex.Match(repoUrl);
				if (match.Success)
					repoUrl = $"git@{match.Groups["host"]}:{match.Groups["login"]}/{match.Groups["repo"]}.git";
			}

			pathToCourseXml = pathToCourseXml.NullIfEmptyOrWhitespace()?.Replace("\"", "/").Trim('/');
			branch = branch.NullIfEmptyOrWhitespace() ?? "master";
			var oldRepoSettings = await coursesRepo.GetCourseRepoSettings(courseId);
			var settings = oldRepoSettings != null && oldRepoSettings.RepoUrl == repoUrl ? oldRepoSettings : new CourseGit { CourseId = courseId };
			settings.RepoUrl = repoUrl;
			settings.PathToCourseXml = pathToCourseXml;
			settings.Branch = branch;
			settings.IsWebhookEnabled = isWebhookEnabled;
			if (settings.PrivateKey == null && repoUrl != null)
			{
				var coursesWithSameRepo = (await coursesRepo.FindCoursesByRepoUrl(repoUrl))
					.Where(r => r.PrivateKey != null)
					.ToList();
				if (coursesWithSameRepo.Any())
				{
					settings.PrivateKey = coursesWithSameRepo[0].PrivateKey;
					settings.PublicKey = coursesWithSameRepo[0].PublicKey;
				}
				else
				{
					var keys = SshKeyGenerator.Generate();
					settings.PrivateKey = keys.PrivatePEM;
					settings.PublicKey = keys.PublicSSH;
				}
			}

			await coursesRepo.SetCourseRepoSettings(settings).ConfigureAwait(false);
			return await PackagesInternal(courseId, openStep1: true, openStep2: true);
		}

		if (submitButton == "Remove")
		{
			await coursesRepo.RemoveCourseRepoSettings(courseId).ConfigureAwait(false);
		}

		return RedirectToAction("Packages", new { courseId });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[HandleHttpAntiForgeryException]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> GenerateCourseRepoKey(string courseId, string repoUrl)
	{
		var keys = SshKeyGenerator.Generate();
		await coursesRepo.UpdateKeysByRepoUrl(repoUrl, keys.PublicSSH, keys.PrivatePEM).ConfigureAwait(false);
		return await PackagesInternal(courseId, openStep1: true, openStep2: true);
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> UploadCourse(string courseId, IFormFile file)
	{
		if (file == null || file.Length <= 0)
			return RedirectToAction("Packages", new { courseId });

		var fileName = Path.GetFileName(file.FileName);
		if (fileName == null || !fileName.ToLower().EndsWith(".zip"))
			return RedirectToAction("Packages", new { courseId });

		using (var tempFile = courseManager.SaveVersionZipToTemporaryDirectory(courseId, new CourseVersionToken(new Guid()), file.OpenReadStream()))
		{
			Guid versionId;
			Exception error;
			using (var inputStream = ZipUtils.GetZipWithFileWithNameInRoot(tempFile.FileInfo.FullName, "course.xml"))
			{
				(versionId, error) = await UploadCourse(courseId, inputStream, User.GetUserId()).ConfigureAwait(false);
			}

			if (error != null)
			{
				var errorMessage = error.Message.ToLowerFirstLetter();
				while (error.InnerException != null)
				{
					errorMessage += $"\n\n{error.InnerException.Message}";
					error = error.InnerException;
				}

				return await Packages(courseId, errorMessage);
			}

			return RedirectToAction("Diagnostics", new { courseId, versionId });
		}
	}

	public async Task UploadCoursesWithGit(string repoUrl, string branch)
	{
		var courses = (await coursesRepo.FindCoursesByRepoUrl(repoUrl))
			.Where(r => r.IsWebhookEnabled && (r.Branch == branch || branch == "master" && r.Branch == null))
			.ToList();
		if (courses.Count == 0)
		{
			log.Warn($"Repo '{repoUrl}' is not expected");
			return;
		}

		log.Info($"Start update repo '{repoUrl}'");
		var userId = (await usersRepo.GetUlearnBotUser()).Id;
		var publicKey = courses[0].PublicKey; // у всех курсов одинаковый repoUrl и ключ
		var privateKey = courses[0].PrivateKey;
		var infoForUpload = new List<(string CourseId, MemoryStream Zip, CommitInfo CommitInfo, string PathToCourseXml)>();
		try
		{
			using (IGitRepo git = new GitRepo(repoUrl, reposDirectory, publicKey, privateKey, new DirectoryInfo(TempDirectory.TempDirectoryPath)))
			{
				// В GitRepo используется Monitor. Он должен быть освобожден в том же потоке, что и взят.
				git.Checkout(branch);
				var commitInfo = git.GetCurrentCommitInfo();
				foreach (var courseRepo in courses)
				{
					var (zip, pathToCourseXml) = git.GetCurrentStateAsZip(courseRepo.PathToCourseXml);
					var hasChanges = true;
					if (courses.Count > 1)
					{
						var publishedVersion = await coursesRepo.GetPublishedCourseVersion(courseRepo.CourseId);
						if (publishedVersion?.CommitHash != null)
						{
							var changedFiles = git.GetChangedFiles(publishedVersion.CommitHash, commitInfo.Hash, pathToCourseXml);
							hasChanges = changedFiles?.Any() ?? true;
						}
					}

					if (hasChanges)
					{
						log.Info($"Course '{courseRepo.CourseId}' has changes in '{repoUrl}'");
						infoForUpload.Add((courseRepo.CourseId, zip, commitInfo, pathToCourseXml));
					}
					else
					{
						log.Info($"Course '{courseRepo.CourseId}' has not changes in '{repoUrl}'");
					}
				}
			}

			foreach (var info in infoForUpload)
			{
				var (courseId, zip, commitInfo, pathToCourseXml) = info;
				var (_, error) = await UploadCourse(courseId, zip, userId, repoUrl, commitInfo, pathToCourseXml).ConfigureAwait(false);
				if (error != null)
					await NotifyAboutCourseUploadFromRepoError(courseId, commitInfo.Hash, repoUrl).ConfigureAwait(false);
			}
		}
		finally
		{
			infoForUpload.ForEach(i => i.Zip.Dispose());
		}
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[HandleHttpAntiForgeryException]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> UploadCourseWithGit(string courseId)
	{
		var courseRepo = await coursesRepo.GetCourseRepoSettings(courseId);
		if (courseRepo == null)
			return RedirectToAction("Packages", new { courseId, error = "Course repo settings not found" });

		var publicKey = courseRepo.PublicKey; // у всех курсов одинаковый repoUrl и ключ
		var privateKey = courseRepo.PrivateKey;
		var pathToCourseXml = courseRepo.PathToCourseXml;

		Exception error = null;
		MemoryStream zip = null;
		CommitInfo commitInfo = null;
		try
		{
			using (IGitRepo git = new GitRepo(courseRepo.RepoUrl, reposDirectory, publicKey, privateKey, new DirectoryInfo(TempDirectory.TempDirectoryPath)))
			{
				git.Checkout(courseRepo.Branch);
				commitInfo = git.GetCurrentCommitInfo();
				(zip, pathToCourseXml) = git.GetCurrentStateAsZip(pathToCourseXml);
			}
		}
		catch (GitException ex)
		{
			if (ex.MayBeSSHException)
			{
				log.Error(ex.InnerException);
				error = new Exception("Не удалось получить данные из репозитория. Вероятно не настроен деплой ключ. Исходный текст ошибки:", ex.InnerException);
			}
			else
				throw;
		}

		var versionId = new Guid();
		if (error == null)
			using (zip)
				(versionId, error) = await UploadCourse(courseId, zip, User.GetUserId(), courseRepo.RepoUrl, commitInfo, pathToCourseXml);

		if (error != null)
		{
			var errorMessage = error.Message.ToLowerFirstLetter();
			while (error.InnerException != null)
			{
				errorMessage += $"\n\n{error.InnerException.Message}";
				error = error.InnerException;
			}

			return await Packages(courseId, errorMessage);
		}

		return RedirectToAction("Diagnostics", new { courseId, versionId });
	}


	private async Task<(Guid versionId, Exception error)> UploadCourse(string courseId, Stream content, string userId,
		string uploadedFromRepoUrl = null, CommitInfo commitInfo = null, string pathToCourseXmlInRepo = null)
	{
		log.Info($"Start upload course '{courseId}'");
		var versionId = Guid.NewGuid();

		using (var zipOnDisk = courseManager.SaveVersionZipToTemporaryDirectory(courseId, new CourseVersionToken(versionId), content))
		{
			string courseName;
			try
			{
				using (var courseDirectory = await courseManager.ExtractCourseVersionToTemporaryDirectory(courseId, new CourseVersionToken(versionId), await zipOnDisk.FileInfo.ReadAllContentAsync()))
				{
					var (course, exception) = courseManager.LoadCourseFromDirectory(courseId, courseDirectory.DirectoryInfo);
					if (exception != null)
						throw exception;
					courseName = course.Title;
				}
			}
			catch (Exception e)
			{
				log.Warn(e, $"Upload course exception '{courseId}'");
				return (versionId, e);
			}

			log.Info($"Successfully update course files '{courseId}'");

			await coursesRepo.AddCourseVersion(courseId, courseName, versionId, userId,
				pathToCourseXmlInRepo, uploadedFromRepoUrl, commitInfo?.Hash, commitInfo?.Message, await zipOnDisk.FileInfo.ReadAllContentAsync());
			await NotifyAboutCourseVersion(courseId, versionId, userId);
			try
			{
				var courseVersions = await coursesRepo.GetCourseVersions(courseId);
				var previousUnpublishedVersions = courseVersions.Where(v => v.PublishTime == null && v.Id != versionId).ToList();
				foreach (var unpublishedVersion in previousUnpublishedVersions)
					await RemoveCourseVersion(courseId, unpublishedVersion.Id).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				log.Warn(ex, "Error during delete previous unpublished versions");
			}
		}

		return (versionId, null);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Policy = UlearnAuthorizationConstants.SysAdminsPolicyName)]
	[HandleHttpAntiForgeryException]
	public async Task<ActionResult> CreateCourse(string courseId, string courseTitle)
	{
		var versionId = Guid.NewGuid();
		var userId = User.GetUserId();

		if (!courseManager.IsCourseIdAllowed(courseId))
			throw new Exception("CourseId contains forbidden characters");

		var createdNew = await courseManager.CreateCourseIfNotExists(courseId, versionId, courseTitle, userId);
		if (!createdNew)
			return RedirectToAction("Courses", "Course", new { courseId = courseId, courseTitle = courseTitle });

		await NotifyAboutPublishedCourseVersion(courseId, versionId, userId).ConfigureAwait(false);

		Thread.Sleep(TimeSpan.FromSeconds(3)); // Чтобы с большой вероятностью курс уже загрузился.

		return RedirectToAction("Packages", new { courseId, onlyPrivileged = true });
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> Packages(string courseId, string error = "")
	{
		var isTempCourse = courseStorage.GetCourse(courseId).IsTempCourse();
		if (isTempCourse)
			return null;
		return await PackagesInternal(courseId, error);
	}

	private async Task<ActionResult> PackagesInternal(string courseId, string error = "", bool openStep1 = false, bool openStep2 = false)
	{
		var course = courseStorage.GetCourse(courseId);
		var courseVersions = (await coursesRepo.GetCourseVersions(courseId)).ToList();
		var publishedVersion = await coursesRepo.GetPublishedCourseVersion(courseId);
		var courseRepo = await coursesRepo.GetCourseRepoSettings(courseId);
		return View("Packages", model: new PackagesViewModel
		{
			Course = course,
			HasPackage = true,
			Versions = courseVersions,
			PublishedVersion = publishedVersion,
			CourseGit = courseRepo,
			OpenStep1 = openStep1,
			OpenStep2 = openStep2,
			GitSecret = gitSecret,
			Error = error,
		});
	}

	public async Task<ActionResult> Comments(string courseId)
	{
		const int commentsCountLimit = 500;

		var userId = User.GetUserId();

		var course = courseStorage.GetCourse(courseId);
		var commentsPolicy = await commentPoliciesRepo.GetCommentsPolicyAsync(courseId);

		var comments = (await commentsRepo.GetCourseCommentsAsync(courseId))
			.Where(c => !c.IsForInstructorsOnly)
			.OrderByDescending(x => x.PublishTime)
			.ToList();
		var commentsLikes = commentsRepo.GetCommentsLikesCounts(comments);
		var commentsLikedByUser = await commentsRepo.GetCourseCommentsLikedByUserAsync(courseId, userId);
		var commentsById = comments.ToDictionary(x => x.Id);

		var canViewProfiles = await systemAccessesRepo.HasSystemAccessAsync(userId, SystemAccessType.ViewAllProfiles) || User.IsSystemAdministrator();

		return View(new AdminCommentsViewModel
		{
			CourseId = courseId,
			IsCommentsEnabled = commentsPolicy.IsCommentsEnabled,
			ModerationPolicy = commentsPolicy.ModerationPolicy,
			OnlyInstructorsCanReply = commentsPolicy.OnlyInstructorsCanReply,
			Comments = (from c in comments.Take(commentsCountLimit)
				let slide = course.FindSlideByIdNotSafe(c.SlideId)
				where slide != null
				select
					new CommentViewModel
					{
						Comment = c,
						LikesCount = commentsLikes.GetOrDefault(c.Id),
						IsLikedByUser = commentsLikedByUser.Contains(c.Id),
						Replies = new List<CommentViewModel>(),
						CanEditAndDeleteComment = true,
						CanModerateComment = true,
						IsCommentVisibleForUser = true,
						ShowContextInformation = true,
						ContextSlideTitle = slide.Title,
						ContextParentComment = c.IsTopLevel ? null : commentsById.ContainsKey(c.ParentCommentId) ? commentsById[c.ParentCommentId].Text : null,
						CanViewAuthorProfile = canViewProfiles,
					}).ToList()
		});
	}

	private async Task<ManualCheckingQueueFilterOptionsObsolete> GetManualCheckingFilterOptionsByGroup(string courseId, List<string> groupsIds)
	{
		return await ControllerUtils.GetFilterOptionsByGroup<ManualCheckingQueueFilterOptionsObsolete>(groupsRepo, groupAccessesRepo, User, courseId, groupsIds);
	}

	/* Returns merged checking queue for exercises (code reviews) as well as for quizzes */
	private async Task<List<AbstractManualSlideChecking>> GetMergedCheckingQueue(ManualCheckingQueueFilterOptionsObsolete filterOptions)
	{
		var result = (await slideCheckingsRepo.GetManualCheckingQueue<ManualExerciseChecking>(filterOptions)).Cast<AbstractManualSlideChecking>().ToList();
		result.AddRange(await slideCheckingsRepo.GetManualCheckingQueue<ManualQuizChecking>(filterOptions));

		result = result.OrderByDescending(c => c.Timestamp).ToList();
		if (filterOptions.Count > 0)
			result = result.Take(filterOptions.Count).ToList();

		return result;
	}

	private async Task<HashSet<Guid>> GetMergedCheckingQueueSlideIds(ManualCheckingQueueFilterOptionsObsolete filterOptions)
	{
		var result = await slideCheckingsRepo.GetManualCheckingQueueSlideIds<ManualExerciseChecking>(filterOptions);
		result.UnionWith(await slideCheckingsRepo.GetManualCheckingQueueSlideIds<ManualQuizChecking>(filterOptions));
		return result;
	}

	private async Task<ActionResult> InternalCheckingQueue(string courseId, bool done, List<string> groupsIds, string userId = "", Guid? slideId = null, string message = "")
	{
		const int maxShownQueueSize = 500;
		var course = courseStorage.GetCourse(courseId);

		var filterOptions = await GetManualCheckingFilterOptionsByGroup(courseId, groupsIds);
		if (filterOptions.UserIds == null)
			groupsIds = new List<string> { "all" };

		if (!string.IsNullOrEmpty(userId))
			filterOptions.UserIds = new List<string> { userId };
		if (slideId.HasValue)
			filterOptions.SlidesIds = new List<Guid> { slideId.Value };

		filterOptions.OnlyChecked = done;
		filterOptions.Count = maxShownQueueSize + 1;
		var checkings = await GetMergedCheckingQueue(filterOptions);

		if (!checkings.Any() && !string.IsNullOrEmpty(message))
			return RedirectToAction("CheckingQueue", new { courseId, group = string.Join(",", groupsIds) });

		var groups = (await groupAccessesRepo.GetAvailableForUserGroupsAsync(courseId, User.GetUserId(), true, true, false, GroupQueryType.SingleGroup)).AsGroups().ToList();
		var groupsAccesses = await groupAccessesRepo.GetGroupAccessesAsync(groups.Select(g => g.Id));

		var alreadyChecked = done;
		Dictionary<int, List<ExerciseCodeReview>> reviews = null;
		Dictionary<int, string> solutions = null;
		if (alreadyChecked)
		{
			reviews = await slideCheckingsRepo.GetExerciseCodeReviewForCheckings(checkings.Select(c => c.Id));
			var submissionsIds = checkings.Select(c => (c as ManualExerciseChecking)?.Id).Where(s => s.HasValue).Select(s => s.Value);
			solutions = await userSolutionsRepo.GetSolutionsForSubmissions(submissionsIds);
		}

		var allCheckingsSlides = await GetAllCheckingsSlides(course, groupsIds, filterOptions);

		return View("CheckingQueue", new ManualCheckingQueueViewModel
		{
			CourseId = courseId,
			/* TODO (andgein): Merge FindSlideById() and following GetSlideById() calls */
			Checkings = checkings.Take(maxShownQueueSize).Where(c => course.FindSlideByIdNotSafe(c.SlideId) != null).Select(c =>
			{
				var slide = course.GetSlideByIdNotSafe(c.SlideId);
				return new ManualCheckingQueueItemViewModel
				{
					CheckingQueueItem = c,
					ContextSlideId = slide.Id,
					ContextSlideTitle = slide.Title,
					ContextMaxScore = (slide as ExerciseSlide)?.Scoring.ScoreWithCodeReview ?? slide.MaxScore,
					ContextTimestamp = c.Timestamp,
					ContextReviews = alreadyChecked ? reviews.GetOrDefault(c.Id, new List<ExerciseCodeReview>()) : new List<ExerciseCodeReview>(),
					ContextExerciseSolution = alreadyChecked && c is ManualExerciseChecking checking ?
						solutions.GetOrDefault(checking.Id, "") :
						"",
				};
			}).ToList(),
			Groups = groups,
			GroupsAccesses = groupsAccesses,
			SelectedGroupsIds = groupsIds,
			Message = message,
			AlreadyChecked = alreadyChecked,
			ExistsMore = checkings.Count > maxShownQueueSize,
			ShowFilterForm = string.IsNullOrEmpty(userId),
			Slides = allCheckingsSlides,
			QueueSlideId = slideId,
		});
	}

	// Возвращает слайды, по которым есть работы (проверенные или непроверенные, зависит от галочки), разделитель и оставшиеся слайды (не важно проверенные или нет).
	private async Task<List<KeyValuePair<Guid, Slide>>> GetAllCheckingsSlides(Course course, List<string> groupsIds, ManualCheckingQueueFilterOptionsObsolete filterOptions)
	{
		filterOptions.SlidesIds = null;
		var usedSlidesIds = await GetMergedCheckingQueueSlideIds(filterOptions);

		filterOptions = await GetManualCheckingFilterOptionsByGroup(course.Id, groupsIds);
		filterOptions.OnlyChecked = null;
		var allCheckingsSlidesIds = await GetMergedCheckingQueueSlideIds(filterOptions);
		// +1 to prevent first slide in course to be AFTER emptySlideMock below
		// GetOrDefault will return 0 for mock slide, so if slide indexed with 0 it can lead to bad ordering below
		var slideId2Index = course.GetSlidesNotSafe().Select((s, i) => (s.Id, i + 1))
			.ToDictionary(p => p.Item1, p => p.Item2);

		var emptySlideMock = new Slide { Title = "", Id = Guid.Empty };
		var allCheckingsSlides = allCheckingsSlidesIds
			.Select(s => new KeyValuePair<Guid, Slide>(s, course.FindSlideByIdNotSafe(s)))
			.Where(kvp => kvp.Value != null)
			.Union(new List<KeyValuePair<Guid, Slide>>
			{
				/* Divider between used slides and another ones */
				new KeyValuePair<Guid, Slide>(Guid.Empty, emptySlideMock)
			})
			.OrderBy(s => usedSlidesIds.Contains(s.Key) ? 0 : 1)
			.ThenBy(s => slideId2Index.GetOrDefault(s.Key))
			.Select(s => new KeyValuePair<Guid, Slide>(s.Key, s.Value))
			.ToList();

		/* Remove divider iff it is first or last item */
		if (allCheckingsSlides.First().Key == Guid.Empty || allCheckingsSlides.Last().Key == Guid.Empty)
			allCheckingsSlides.RemoveAll(kvp => kvp.Key == Guid.Empty);

		return allCheckingsSlides;
	}

	public async Task<ActionResult> CheckingQueue(string courseId, bool done = false, string userId = "", Guid? slideId = null, string message = "")
	{
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");
		return await InternalCheckingQueue(courseId, done, groupsIds, userId, slideId, message);
	}

	private async Task<ActionResult> InternalManualChecking<T>(string courseId, int queueItemId, bool ignoreLock = false, List<string> groupsIds = null, bool recheck = false, string queueSlideId = null) where T : AbstractManualSlideChecking
	{
		T checking;
		var joinedGroupsIds = string.Join(",", groupsIds ?? new List<string>());
		await using (var transaction = await db.Database.BeginTransactionAsync())
		{
			checking = await slideCheckingsRepo.FindManualCheckingById<T>(queueItemId);
			if (checking == null)
				return RedirectToAction("CheckingQueue",
					new
					{
						courseId = courseId,
						group = joinedGroupsIds,
						done = recheck,
						message = "already_checked",
					});

			if (!User.HasAccessFor(checking.CourseId, CourseRoleType.Instructor))
				return new ForbidResult();

			if (checking.IsChecked && !recheck)
				return RedirectToAction("CheckingQueue",
					new
					{
						courseId = checking.CourseId,
						group = joinedGroupsIds,
						done = recheck,
						message = "already_checked",
					});

			if (!recheck)
				await slideCheckingsRepo.LockManualChecking(checking, User.GetUserId()).ConfigureAwait(false);
			await transaction.CommitAsync();
		}

		return RedirectToRoute("Course.SlideById", new
		{
			checking.CourseId,
			SlideId = checking.SlideId.ToString(),
			UserId = checking.UserId,
			CheckQueueItemId = checking.Id,
			SubmissionId = checking.Id,
			Group = joinedGroupsIds,
			QueueSlideId = queueSlideId,
			Done = recheck,
		});
	}

	private async Task<ActionResult> CheckNextManualCheckingForSlide<T>(string courseId, Guid slideId, List<string> groupsIds, int previousCheckingId) where T : AbstractManualSlideChecking
	{
		int itemToCheckId;
		using (var transaction = db.Database.BeginTransaction())
		{
			var filterOptions = await GetManualCheckingFilterOptionsByGroup(courseId, groupsIds);
			if (filterOptions.UserIds == null)
				groupsIds = new List<string> { "all" };
			filterOptions.SlidesIds = new List<Guid> { slideId };
			var checkings = (await slideCheckingsRepo.GetManualCheckingQueue<T>(filterOptions)).ToList();

			/* First of all try to find checking with Id < previousCheckingId (early) */
			var itemToCheck = checkings.FirstOrDefault(c => !c.IsLocked && c.Id < previousCheckingId) ?? checkings.FirstOrDefault(c => !c.IsLocked);
			if (itemToCheck == null)
				return RedirectToAction("CheckingQueue", new { courseId, group = string.Join(",", groupsIds), message = "slide_checked" });

			await slideCheckingsRepo.LockManualChecking(itemToCheck, User.GetUserId()).ConfigureAwait(false);
			itemToCheckId = itemToCheck.Id;
			transaction.Commit();
		}

		return await InternalManualChecking<T>(courseId, itemToCheckId, ignoreLock: true, groupsIds: groupsIds).ConfigureAwait(false);
	}

	public Task<ActionResult> QuizChecking(string courseId, int id, bool recheck = false, string queueSlideId = null)
	{
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");
		return InternalManualChecking<ManualQuizChecking>(courseId, id, ignoreLock: false, groupsIds: groupsIds, recheck: recheck, queueSlideId);
	}

	public Task<ActionResult> ExerciseChecking(string courseId, int id, bool recheck = false, string queueSlideId = null)
	{
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");
		return InternalManualChecking<ManualExerciseChecking>(courseId, id, ignoreLock: false, groupsIds: groupsIds, recheck: recheck, queueSlideId);
	}

	public Task<ActionResult> CheckNextQuizForSlide(string courseId, Guid slideId, int previous)
	{
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");
		return CheckNextManualCheckingForSlide<ManualQuizChecking>(courseId, slideId, groupsIds, previous);
	}

	public Task<ActionResult> CheckNextExerciseForSlide(string courseId, Guid slideId, int previous)
	{
		var groupsIds = Request.GetMultipleValuesFromQueryString("group");
		return CheckNextManualCheckingForSlide<ManualExerciseChecking>(courseId, slideId, groupsIds, previous);
	}

	public async Task<ActionResult> GetNextManualCheckingExerciseForSlide(string courseId, Guid slideId, int previous)
	{
		var action = await CheckNextExerciseForSlide(courseId, slideId, previous).ConfigureAwait(false);
		if (!(action is RedirectToRouteResult redirect))
			return action;
		var url = Url.RouteUrl(redirect.RouteName, redirect.RouteValues);
		return Json(new { url });
	}

	public async Task<ActionResult> GetNextManualCheckingQuizForSlide(string courseId, Guid slideId, int previous)
	{
		var action = await CheckNextQuizForSlide(courseId, slideId, previous).ConfigureAwait(false);
		if (!(action is RedirectToRouteResult redirect))
			return action;
		var url = Url.RouteUrl(redirect.RouteName, redirect.RouteValues);
		return Json(new { url });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	[HandleHttpAntiForgeryException]
	public async Task<ActionResult> SaveCommentsPolicy(AdminCommentsViewModel model)
	{
		var courseId = model.CourseId;
		var commentsPolicy = new CommentsPolicy
		{
			CourseId = courseId,
			IsCommentsEnabled = model.IsCommentsEnabled,
			ModerationPolicy = model.ModerationPolicy,
			OnlyInstructorsCanReply = model.OnlyInstructorsCanReply
		};
		await commentPoliciesRepo.SaveCommentsPolicyAsync(commentsPolicy);
		return RedirectToAction("Comments", new { courseId });
	}

	[Authorize(Policy = UlearnAuthorizationConstants.InstructorsPolicyName)]
	public async Task<ActionResult> Users(UserSearchQueryModel queryModel)
	{
		var isCourseAdmin = User.HasAccessFor(queryModel.CourseId, CourseRoleType.CourseAdmin);
		var canAddInstructors = await coursesRepo.HasCourseAccess(User.GetUserId(), queryModel.CourseId, CourseAccessType.AddAndRemoveInstructors);
		if (!isCourseAdmin && !canAddInstructors)
			return NotFound();

		if (string.IsNullOrEmpty(queryModel.CourseId))
			return RedirectToAction("Courses", "Course");
		return View(queryModel);
	}

	//[ChildActionOnly]
	public async Task<ActionResult> UsersPartial(UserSearchQueryModel queryModel)
	{
		var userRolesByEmail = User.IsSystemAdministrator() ? await courseRolesRepo.FilterUsersByEmail(queryModel) : null;
		var userRoles = await courseRolesRepo.FilterUsers(queryModel);
		var allTempCourses = (await tempCoursesRepo.GetAllTempCourses())
			.ToDictionary(t => t.CourseId, t => t, StringComparer.InvariantCultureIgnoreCase);
		var courses = courseStorage.GetCourses()
			.ToDictionary(c => c.Id, c => (c, allTempCourses.GetValueOrDefault(c.Id)), StringComparer.OrdinalIgnoreCase);
		var model = await GetUserListModel(userRolesByEmail.EmptyIfNull().Concat(userRoles).Deprecated_DistinctBy(r => r.UserId).ToList(),
			courses,
			queryModel.CourseId);

		return PartialView("_UserListPartial", model);
	}

	private async Task<UserListModel> GetUserListModel(List<UserRolesInfo> userRoles,
		Dictionary<string, (Course Course, TempCourse TempCourse)> courses,
		string courseId)
	{
		var rolesForUsers = await courseRolesRepo.GetRolesByUsers(courseId);
		var currentUserId = User.GetUserId();
		var isCourseAdmin = User.HasAccessFor(courseId, CourseRoleType.CourseAdmin);
		var canAddInstructors = await coursesRepo.HasCourseAccess(currentUserId, courseId, CourseAccessType.AddAndRemoveInstructors);
		var model = new UserListModel
		{
			CanToggleRoles = isCourseAdmin || canAddInstructors,
			ShowDangerEntities = false,
			Users = new List<UserModel>(),
			CanViewAndToggleCourseAccesses = isCourseAdmin,
			CanViewAndToogleSystemAccesses = false,
			CanViewProfiles = await systemAccessesRepo.HasSystemAccessAsync(currentUserId, SystemAccessType.ViewAllProfiles) || User.IsSystemAdministrator(),
		};
		var userIds = new HashSet<string>(userRoles.Select(r => r.UserId));
		var usersCourseAccesses = (await coursesRepo.GetCourseAccesses(courseId)).Where(a => userIds.Contains(a.UserId))
			.GroupBy(a => a.UserId)
			.ToDictionary(g => g.Key, g => g.Select(a => a.AccessType).ToList());

		foreach (var userRolesInfo in userRoles)
		{
			var user = new UserModel(userRolesInfo);

			if (!rolesForUsers.TryGetValue(userRolesInfo.UserId, out var roles))
				roles = new List<CourseRoleType>();

			string getVisibleCourseName(string s)
			{
				var (course, tempCourse) = courses.GetOrDefault(s);
				string visibleCourseName = null;
				if (course != null)
					visibleCourseName = tempCourse != null
						? tempCourse.GetVisibleName(course.Title)
						: courseStorage.GetCourse(course.Id).Title;
				return visibleCourseName;
			}

			user.CourseRoles = Enum.GetValues(typeof(CourseRoleType))
				.Cast<CourseRoleType>()
				.Where(courseRole => courseRole != CourseRoleType.Student)
				.ToDictionary(
					courseRole => courseRole.ToString(),
					courseRole => (ICoursesRolesListModel)new SingleCourseRolesModel
					{
						HasAccess = roles.Contains(courseRole),
						ToggleUrl = Url.Action("ToggleRole", "Account", new { courseId, userId = user.UserId, role = courseRole }),
						UserName = user.UserVisibleName,
						Role = courseRole,
						VisibleCourseName = getVisibleCourseName(courseId)
					});

			var courseAccesses = usersCourseAccesses.ContainsKey(user.UserId) ? usersCourseAccesses[user.UserId] : null;
			user.CourseAccesses[courseId] = Enum.GetValues(typeof(CourseAccessType))
				.Cast<CourseAccessType>()
				.ToDictionary(
					a => a,
					a => new CourseAccessModel
					{
						CourseId = courseId,
						HasAccess = courseAccesses?.Contains(a) ?? false,
						ToggleUrl = Url.Action("ToggleCourseAccess", "Admin", new { courseId = courseId, userId = user.UserId, accessType = a }),
						UserName = user.UserVisibleName,
						AccessType = a,
						VisibleCourseName = getVisibleCourseName(courseId)
					}
				);

			model.Users.Add(user);
		}

		model.UsersGroups = await groupAccessesRepo.GetUsersGroupsNamesAsStrings(courseId, userIds, User.GetUserId(), actual: true, archived: false);
		model.UsersArchivedGroups = await groupAccessesRepo.GetUsersGroupsNamesAsStrings(courseId, userIds, User.GetUserId(), actual: false, archived: true);

		return model;
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> Diagnostics(string courseId, Guid? versionId)
	{
		var course = courseStorage.GetCourse(courseId);
		if (versionId == null)
		{
			return View(new DiagnosticsModel
			{
				CourseId = courseId,
				IsTempCourse = course.IsTempCourse()
			});
		}

		var versionFile = await coursesRepo.GetVersionFile(versionId.Value);
		using (var courseDirectory = await courseManager.ExtractCourseVersionToTemporaryDirectory(versionFile.CourseId, new CourseVersionToken(versionFile.CourseVersionId), versionFile.File))
		{
			var (version, exception) = courseManager.LoadCourseFromDirectory(versionFile.CourseId, courseDirectory.DirectoryInfo);
			if (exception != null)
				throw exception;

			var courseDiff = new CourseDiff(course, version);
			var schemaPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "schema.xsd"); //HttpRuntime.BinDirectory
			var validator = new XmlValidator(schemaPath);
			var warnings = validator.ValidateSlidesFiles(version.GetSlidesNotSafe()
				.Select(s => new FileInfo(Path.Combine(courseDirectory.DirectoryInfo.FullName, s.SlideFilePathRelativeToCourse))).ToList());

			return View(new DiagnosticsModel
			{
				CourseId = courseId,
				IsDiagnosticsForVersion = true,
				VersionId = versionId.Value,
				CourseDiff = courseDiff,
				Warnings = warnings,
				IsTempCourse = course.IsTempCourse()
			});
		}
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> TempCourseDiagnostics(string courseId)
	{
		var authorId = (await tempCoursesRepo.Find(courseId)).AuthorId;
		var baseCourseId = courseId.Replace($"_{authorId}", ""); // todo добавить поле baseCourseId в сущность tempCourse
		var baseCourseVersion = coursesRepo.GetPublishedCourseVersion(baseCourseId).Id;
		return RedirectToAction("Diagnostics", new { courseId, versionId = baseCourseVersion });
	}

	public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
	{
		/* Check that one directory is not a parent of another one */
		if (source.FullName.StartsWith(target.FullName) || target.FullName.StartsWith(source.FullName))
			throw new Exception("Can\'t copy files recursively from parent to child directory or from child to parent");

		foreach (var subDirectory in source.GetDirectories())
			CopyFilesRecursively(subDirectory, target.CreateSubdirectory(subDirectory.Name));
		foreach (var file in source.GetFiles())
			file.CopyTo(Path.Combine(target.FullName, file.Name), true);
	}

	private async Task NotifyAboutPublishedCourseVersion(string courseId, Guid versionId, string userId)
	{
		var notification = new PublishedPackageNotification
		{
			CourseVersionId = versionId,
		};
		await notificationsRepo.AddNotification(courseId, notification, userId);
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> PublishVersion(string courseId, Guid versionId)
	{
		log.Info($"Публикую версию курса {courseId}. ID версии: {versionId}");
		var oldCourse = courseStorage.GetCourse(courseId);

		log.Info($"Помечаю версию {versionId} как опубликованную версию курса {courseId}");
		await coursesRepo.MarkCourseVersionAsPublished(versionId);
		await NotifyAboutPublishedCourseVersion(courseId, versionId, User.GetUserId());

		Course version;
		var versionFile = await coursesRepo.GetVersionFile(versionId);
		using (var courseDirectory = await courseManager.ExtractCourseVersionToTemporaryDirectory(versionFile.CourseId, new CourseVersionToken(versionFile.CourseVersionId), versionFile.File))
		{
			Exception exception;
			(version, exception) = courseManager.LoadCourseFromDirectory(versionFile.CourseId, courseDirectory.DirectoryInfo);
			if (exception != null)
				throw exception;
		}

		var courseDiff = new CourseDiff(oldCourse, version);

		return View("Diagnostics", new DiagnosticsModel
		{
			CourseId = courseId,
			IsDiagnosticsForVersion = true,
			IsVersionPublished = true,
			VersionId = versionId,
			CourseDiff = courseDiff,
		});
	}

	// Удаляет версии, которые старше на 2 месяца даты загрузки текущей опубликованной (если не использовался git), но не 3 версии, идущие после опубликованной.
	private async Task RemoveOldCourseVersions(string courseId)
	{
		var allVersions = (await coursesRepo.GetCourseVersions(courseId)).ToList();
		var publishedCourseVersion = allVersions
			.Where(v => v.CourseId == courseId && v.PublishTime != null)
			.MaxBy(v => v.PublishTime);
		if (publishedCourseVersion == null)
			return;
		var isPublishedCourseVersionFound = false;
		var timeLimit = publishedCourseVersion.LoadingTime.Subtract(TimeSpan.FromDays(60));
		const int versionsCountLimit = 3;
		var versionsAfterPublishedCount = 0;
		foreach (var version in allVersions)
		{
			if (!isPublishedCourseVersionFound)
			{
				isPublishedCourseVersionFound |= publishedCourseVersion.Id == version.Id;
				continue;
			}

			versionsAfterPublishedCount++;
			if (version.CommitHash == null && (version.LoadingTime > timeLimit || (version.PublishTime.HasValue && version.PublishTime.Value > timeLimit)))
				continue;
			if (versionsAfterPublishedCount <= versionsCountLimit)
				continue;
			await RemoveCourseVersion(courseId, version.Id);
		}
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> DeleteVersion(string courseId, Guid versionId)
	{
		await RemoveCourseVersion(courseId, versionId);
		return RedirectToAction("Packages", new { courseId });
	}

	private async Task RemoveCourseVersion(string courseId, Guid versionId)
	{
		log.Warn($"Remove course version {courseId} {versionId}");

		try
		{
			await coursesRepo.DeleteCourseVersion(courseId, versionId);
		}
		catch (Exception ex)
		{
			log.Error(ex, "Can't remove course version {VersionId}", versionId);
		}
	}

	public ActionResult Groups(string courseId)
	{
		/* This action is moved to react-based frontend application */
		return Redirect($"/{courseId.ToLower(CultureInfo.InvariantCulture)}/groups");
	}

	public class UserSearchResultModel
	{
		public string id { get; set; }
		public string value { get; set; }
	}

	public async Task<ActionResult> FindUsers(string courseId, string term, bool onlyInstructors = true, bool withGroups = true)
	{
		/* Only sysadmins can search ordinary users */
		// This limitation is disabled now for certificates generation. Waits for futher investigation
		// if (!User.IsSystemAdministrator() && !onlyInstructors)
		//   	return Json(new { status = "error", message = "Вы не можете искать среди всех пользователей" }, JsonRequestBehavior.AllowGet);

		var query = new UserSearchQueryModel { NamePrefix = term };
		if (onlyInstructors)
		{
			query.CourseRole = CourseRoleType.Instructor;
			query.CourseId = courseId;
			query.IncludeHighCourseRoles = true;
		}

		var users = (await courseRolesRepo.FilterUsers(query, 10)).ToList();
		var usersList = users.Select(ur => new UserSearchResultModel
		{
			id = ur.UserId,
			value = $"{ur.UserVisibleName} ({ur.UserName})"
		}).ToList();

		if (withGroups)
		{
			var usersIds = users.Select(u => u.UserId);
			var groupsNames = await groupAccessesRepo.GetUsersGroupsNamesAsStrings(courseId, usersIds, User.GetUserId(), actual: true, archived: false);
			foreach (var user in usersList)
				if (groupsNames.ContainsKey(user.id) && !string.IsNullOrEmpty(groupsNames[user.id]))
					user.value += $": {groupsNames[user.id]}";
		}

		return Json(usersList); //JsonRequestBehavior.AllowGet
	}

	public async Task<ActionResult> Certificates(string courseId)
	{
		var course = courseStorage.GetCourse(courseId);
		var certificateTemplates = (await certificatesRepo.GetTemplates(courseId)).ToDictionary(t => t.Id);
		var certificates = await certificatesRepo.GetCertificates(courseId);
		var templateParameters = certificateTemplates.ToDictionary(
			kv => kv.Key,
			kv => certificateGenerator.GetTemplateParametersWithoutBuiltins(kv.Value).ToList()
		);

		return View(new CertificatesViewModel
		{
			Course = course,
			Templates = certificateTemplates,
			TemplateParameters = templateParameters,
			Certificates = certificates
		});
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> CreateCertificateTemplate(string courseId, string name, IFormFile archive)
	{
		if (archive == null || archive.Length <= 0)
		{
			log.Error("Создание шаблона сертификата: ошибка загрузки архива");
			throw new Exception("Ошибка загрузки архива");
		}

		log.Info($"Создаю шаблон сертификата «{name}» для курса {courseId}");
		var archiveName = await SaveUploadedTemplate(archive);
		var template = await certificatesRepo.AddTemplate(courseId, name, archiveName);
		await LoadUploadedTemplateToBD(archiveName, template.Id).ConfigureAwait(false);
		log.Info($"Создал шаблон, Id = {template.Id}, путь к архиву {template.ArchiveName}");
		return RedirectToAction("Certificates", new { courseId });
	}

	private async Task<string> SaveUploadedTemplate(IFormFile archive)
	{
		var archiveName = Ulearn.Core.Utils.NewNormalizedGuid();
		var templateArchivePath = certificateGenerator.GetTemplateArchivePath(archiveName);
		try
		{
			await using Stream fileStream = new FileStream(templateArchivePath.FullName, FileMode.Create);
			await archive.CopyToAsync(fileStream);
		}
		catch (Exception e)
		{
			log.Error(e, "Создание шаблона сертификата: не могу сохранить архив");
			throw;
		}

		return archiveName;
	}

	private async Task LoadUploadedTemplateToBD(string archiveName, Guid templateId)
	{
		var content = await certificateGenerator.GetTemplateArchivePath(archiveName).ReadAllContentAsync().ConfigureAwait(false);
		await certificatesRepo.AddCertificateTemplateArchive(archiveName, templateId, content).ConfigureAwait(false);
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> EditCertificateTemplate(string courseId, Guid templateId, string name, IFormFile archive)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		log.Info($"Обновляю шаблон сертификата «{template.Name}» (Id = {template.Id}) для курса {courseId}");

		if (archive != null && archive.Length > 0)
		{
			var archiveName = await SaveUploadedTemplate(archive);
			await LoadUploadedTemplateToBD(archiveName, template.Id).ConfigureAwait(false);
			log.Info($"Загружен новый архив в {archiveName}");
			await certificatesRepo.ChangeTemplateArchiveName(templateId, archiveName);
		}

		await certificatesRepo.ChangeTemplateName(templateId, name);

		return RedirectToAction("Certificates", new { courseId });
	}

	[HttpPost]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	public async Task<ActionResult> RemoveCertificateTemplate(string courseId, Guid templateId)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		log.Info($"Удаляю шаблон сертификата «{template.Name}» (Id = {template.Id}) для курса {courseId}");
		await certificatesRepo.RemoveTemplate(template);

		return RedirectToAction("Certificates", new { courseId });
	}

	private async Task NotifyAboutCertificate(Certificate certificate)
	{
		var notification = new ReceivedCertificateNotification
		{
			Certificate = certificate,
		};
		var ulearnBotUserId = await usersRepo.GetUlearnBotUserId();
		await notificationsRepo.AddNotification(certificate.Template.CourseId, notification, ulearnBotUserId);
	}

	[HttpPost]
	public async Task<ActionResult> AddCertificate(string courseId, Guid templateId, string userId, bool isPreview = false)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		var certificateParameters = GetCertificateParametersFromRequest(template);
		if (certificateParameters == null)
			return RedirectToAction("Certificates", new { courseId });

		var certificate = await certificatesRepo.AddCertificate(templateId, userId, User.GetUserId(), certificateParameters, isPreview);

		if (isPreview)
			return RedirectToRoute("Certificate", new { certificateId = certificate.Id.ToString() });

		await NotifyAboutCertificate(certificate);

		return Redirect(Url.Action("Certificates", new { courseId }) + "#template-" + templateId);
	}

	private Dictionary<string, string> GetCertificateParametersFromRequest(CertificateTemplate template)
	{
		var templateParameters = certificateGenerator.GetTemplateParametersWithoutBuiltins(template);
		var certificateParameters = new Dictionary<string, string>();
		foreach (var parameter in templateParameters)
		{
			if (!Request.Form.ContainsKey("parameter-" + parameter))
				return null;
			certificateParameters[parameter] = Request.Form["parameter-" + parameter];
		}

		return certificateParameters;
	}

	[HttpPost]
	public async Task<ActionResult> RemoveCertificate(string courseId, Guid certificateId)
	{
		var certificate = await certificatesRepo.FindCertificateById(certificateId);
		if (certificate == null)
			return NotFound();

		if (!User.HasAccessFor(certificate.Template.CourseId, CourseRoleType.CourseAdmin) &&
			certificate.InstructorId != User.GetUserId())
			return NotFound();

		await certificatesRepo.RemoveCertificate(certificate);
		return Json(new { status = "ok" });
	}

	public async Task<ActionResult> DownloadCertificateTemplate(string courseId, Guid templateId)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		return RedirectPermanent($"/Certificates/{template.ArchiveName}.zip");
	}

	public async Task<ActionResult> PreviewCertificates(string courseId, Guid templateId, IFormFile certificatesData)
	{
		const string namesColumnName = "Фамилия Имя";

		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		var notBuiltinTemplateParameters = certificateGenerator.GetTemplateParametersWithoutBuiltins(template).ToList();
		var builtinTemplateParameters = certificateGenerator.GetBuiltinTemplateParameters(template).ToList();
		builtinTemplateParameters.Sort();

		var model = new PreviewCertificatesViewModel
		{
			CourseId = courseId,
			Template = template,
			NotBuiltinTemplateParameters = notBuiltinTemplateParameters,
			BuiltinTemplateParameters = builtinTemplateParameters,
			Certificates = new List<PreviewCertificatesCertificateModel>(),
		};

		if (certificatesData == null || certificatesData.Length <= 0)
		{
			return View(model.WithError("Ошибка загрузки файла с данными для сертификатов"));
		}

		var allUsersIds = new HashSet<string>();

		using (var parser = new TextFieldParser(new StreamReader(certificatesData.OpenReadStream())))
		{
			parser.TextFieldType = FieldType.Delimited;
			parser.SetDelimiters(",");
			if (parser.EndOfData)
				return View(model.WithError("Пустой файл? В файле с данными должна присутствовать строка с заголовком"));

			string[] headers;
			try
			{
				headers = parser.ReadFields();
			}
			catch (MalformedLineException e)
			{
				return View(model.WithError($"Ошибка в файле с данными: в строке {parser.ErrorLineNumber}: \"{parser.ErrorLine}\". {e}"));
			}

			var namesColumnIndex = headers.FindIndex(namesColumnName);
			if (namesColumnIndex < 0)
				return View(model.WithError($"Одно из полей должно иметь имя \"{namesColumnName}\", в нём должны содержаться фамилия и имя студента. Смотрите пример файла с данными"));

			var parametersIndeces = new Dictionary<string, int>();
			foreach (var parameter in notBuiltinTemplateParameters)
			{
				parametersIndeces[parameter] = headers.FindIndex(parameter);
				if (parametersIndeces[parameter] < 0)
					return View(model.WithError($"Одно из полей должно иметь имя \"{parameter}\", в нём должна содержаться подстановка для шаблона. Смотрите пример файла с данными"));
			}

			while (!parser.EndOfData)
			{
				string[] fields;
				try
				{
					fields = parser.ReadFields();
				}
				catch (MalformedLineException e)
				{
					return View(model.WithError($"Ошибка в файле с данными: в строке {parser.ErrorLineNumber}: \"{parser.ErrorLine}\". {e}"));
				}

				if (fields.Length != headers.Length)
				{
					return View(model.WithError($"Ошибка в файле с данными: в строке {parser.ErrorLineNumber}: \"{parser.ErrorLine}\". Количество ячеек в строке не совпадает с количеством столбцов в заголовке"));
				}

				var userNames = fields[namesColumnIndex];

				var query = new UserSearchQueryModel { NamePrefix = userNames };
				var users = (await courseRolesRepo.FilterUsers(query)).Take(10).ToList();

				var exportedParameters = parametersIndeces.ToDictionary(kv => kv.Key, kv => fields[kv.Value]);
				var certificateModel = new PreviewCertificatesCertificateModel
				{
					UserNames = userNames,
					Users = users,
					Parameters = exportedParameters,
				};
				allUsersIds = users.Select(u => u.UserId).ToHashSet();
				model.Certificates.Add(certificateModel);
			}
		}

		model.GroupsNames = await groupAccessesRepo.GetUsersGroupsNamesAsStrings(courseId, allUsersIds, User.GetUserId(), actual: true, archived: false);

		return View(model);
	}

	public async Task<ActionResult> GenerateCertificates(string courseId, Guid templateId, int maxCertificateId)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		var templateParameters = certificateGenerator.GetTemplateParametersWithoutBuiltins(template).ToList();
		var certificateRequests = new List<CertificateRequest>();

		for (var certificateIndex = 0; certificateIndex < maxCertificateId; certificateIndex++)
		{
			var userId = Request.Form[$"user-{certificateIndex}"];
			if (userId.Count == 0)
				continue;
			if (string.IsNullOrEmpty(userId))
			{
				return View("GenerateCertificatesError", (object)"Не все пользователи выбраны");
			}

			var parameters = new Dictionary<string, string>();
			foreach (var parameterName in templateParameters)
			{
				var parameterValue = Request.Form[$"parameter-{certificateIndex}-{parameterName}"];
				parameters[parameterName] = parameterValue;
			}

			certificateRequests.Add(new CertificateRequest
			{
				UserId = userId,
				Parameters = parameters,
			});
		}

		foreach (var certificateRequest in certificateRequests)
		{
			var certificate = await certificatesRepo.AddCertificate(templateId, certificateRequest.UserId, User.GetUserId(), certificateRequest.Parameters);
			await NotifyAboutCertificate(certificate);
		}

		return Redirect(Url.Action("Certificates", new { courseId }) + "#template-" + templateId);
	}

	public async Task<ActionResult> GetBuiltinCertificateParametersForUser(string courseId, Guid templateId, string userId)
	{
		var template = await certificatesRepo.FindTemplateById(templateId);
		if (template == null || !template.CourseId.EqualsIgnoreCase(courseId))
			return NotFound();

		var user = await userManager.FindByIdAsync(userId);
		if (user == null)
			return NotFound();
		var instructor = await userManager.FindByIdAsync(User.GetUserId());
		var course = courseStorage.GetCourse(courseId);

		var builtinParameters = certificateGenerator.GetBuiltinTemplateParameters(template);
		var builtinParametersValues = builtinParameters.ToDictionary(
			p => p,
			p => certificateGenerator.GetTemplateBuiltinParameterForUser(template, course, user, instructor, p)
		);

		return Json(builtinParametersValues); //JsonRequestBehavior.AllowGet);
	}

	private async Task NotifyAboutAdditionalScore(AdditionalScore score)
	{
		var notification = new ReceivedAdditionalScoreNotification
		{
			Score = score
		};
		await notificationsRepo.AddNotification(score.CourseId, notification, score.InstructorId);
	}

	[HttpPost]
	public async Task<ActionResult> SetAdditionalScore(string courseId, Guid unitId, string userId, string scoringGroupId, string score)
	{
		var course = courseStorage.GetCourse(courseId);
		if (!course.Settings.Scoring.Groups.ContainsKey(scoringGroupId))
			return NotFound();
		var unit = course.GetUnitsNotSafe().FirstOrDefault(u => u.Id == unitId);
		if (unit == null)
			return NotFound();

		var scoringGroup = unit.Scoring.Groups[scoringGroupId];
		if (string.IsNullOrEmpty(score))
		{
			await additionalScoresRepo.RemoveAdditionalScores(courseId, unitId, userId, scoringGroupId).ConfigureAwait(false);
			return Json(new { status = "ok", score = "" });
		}

		if (!int.TryParse(score, out int scoreInt))
			return Json(new { status = "error", error = "Введите целое число" });
		if (scoreInt < 0 || scoreInt > scoringGroup.MaxAdditionalScore)
			return Json(new { status = "error", error = $"Баллы должны быть от 0 до {scoringGroup.MaxAdditionalScore}" });

		var (additionalScore, oldScore) =
			await additionalScoresRepo.SetAdditionalScore(courseId, unitId, userId, scoringGroupId, scoreInt, User.GetUserId()).ConfigureAwait(false);
		if (!oldScore.HasValue || oldScore.Value != scoreInt)
			await NotifyAboutAdditionalScore(additionalScore).ConfigureAwait(false);

		return Json(new { status = "ok", score = scoreInt });
	}

	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	[HttpPost]
	public async Task<ActionResult> ToggleCourseAccess(string courseId, string userId, CourseAccessType accessType, bool isEnabled)
	{
		var currentUserId = User.GetUserId();
		var comment = Request.Form["comment"];
		var userRoles = await courseRolesRepo.GetRoles(userId);
		var errorMessage = "Выдавать дополнительные права можно только преподавателям. Сначала назначьте пользователя администратором курса или преподавателем";
		if (!userRoles.ContainsKey(courseId))
			return Json(new { status = "error", message = errorMessage });
		if (userRoles[courseId] > CourseRoleType.Instructor)
			return Json(new { status = "error", message = errorMessage });

		if (isEnabled)
			await coursesRepo.GrantAccess(courseId, userId, accessType, currentUserId, comment);
		else
			await coursesRepo.RevokeAccess(courseId, userId, accessType, currentUserId, comment);

		return Json(new { status = "ok" });
	}

	[SysAdminsOnly]
	public async Task<ActionResult> StyleValidations()
	{
		return View(await styleErrorsRepo.GetStyleErrorSettings());
	}

	[SysAdminsOnly]
	[HttpPost]
	public async Task<ActionResult> EnableStyleValidation(StyleErrorType errorType, bool isEnabled)
	{
		await styleErrorsRepo.EnableStyleError(errorType, isEnabled);
		return Json(new { status = "ok" });
	}

	[ValidateAntiForgeryToken]
	[HandleHttpAntiForgeryException]
	[Authorize(Policy = UlearnAuthorizationConstants.CourseAdminsPolicyName)]
	[HttpPost]
	public async Task<ActionResult> SetSuspicionLevels(string courseId, Guid slideId, Language language, string faintSuspicion = null, string strongSuspicion = null)
	{
		var course = courseStorage.GetCourse(courseId);
		if (course.FindSlideByIdNotSafe(slideId) == null)
			return Forbid("Course does not contain a slide");

		if (!TryParseNullableDouble(faintSuspicion, out var faintSuspicionParsed)
			|| !TryParseNullableDouble(strongSuspicion, out var strongSuspicionParsed))
			return BadRequest("faintSuspicion or strongSuspicion not in double");

		if (faintSuspicion != null && (faintSuspicionParsed < 0 || faintSuspicionParsed > 100)
			|| strongSuspicion != null && (strongSuspicionParsed < 0 || strongSuspicionParsed > 100)
			|| faintSuspicionParsed > strongSuspicionParsed)
			return BadRequest("faintSuspicion < strongSuspicion and in [0, 100]");

		await antiPlagiarismClient.SetSuspicionLevelsAsync(new SetSuspicionLevelsParameters
		{
			TaskId = slideId,
			Language = language,
			FaintSuspicion = faintSuspicionParsed / 100d,
			StrongSuspicion = strongSuspicionParsed / 100d,
		});
		return Json(new { status = "ok" });
	}

	public static bool TryParseNullableDouble(string str, out double? result)
	{
		result = null;
		if (string.IsNullOrWhiteSpace(str))
			return true;
		str = str.Replace(',', '.');
		if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
		{
			result = d;
			return true;
		}

		return false;
	}
}

public class CertificateRequest
{
	public string UserId;
	public Dictionary<string, string> Parameters;
}

public class PreviewCertificatesCertificateModel
{
	public string UserNames { get; set; }
	public List<UserRolesInfo> Users { get; set; }
	public Dictionary<string, string> Parameters { get; set; }
}

public class PreviewCertificatesViewModel
{
	public string Error { get; set; }

	public string CourseId { get; set; }
	public CertificateTemplate Template { get; set; }
	public List<PreviewCertificatesCertificateModel> Certificates { get; set; }
	public List<string> NotBuiltinTemplateParameters { get; set; }
	public List<string> BuiltinTemplateParameters { get; set; }
	public Dictionary<string, string> GroupsNames { get; set; }

	public PreviewCertificatesViewModel WithError(string error)
	{
		return new PreviewCertificatesViewModel
		{
			CourseId = CourseId,
			Template = Template,
			Error = error,
		};
	}
}

public class CertificatesViewModel
{
	public Course Course { get; set; }

	public Dictionary<Guid, CertificateTemplate> Templates { get; set; }
	public Dictionary<Guid, List<Certificate>> Certificates { get; set; }
	public Dictionary<Guid, List<string>> TemplateParameters { get; set; }
}

public class UnitsListViewModel
{
	public string CourseId;
	public string CourseTitle;
	public DateTime CurrentDateTime;
	public List<Tuple<Unit, UnitAppearance>> Units;

	public UnitsListViewModel(string courseId, string courseTitle, List<Tuple<Unit, UnitAppearance>> units,
		DateTime currentDateTime)
	{
		CourseId = courseId;
		CourseTitle = courseTitle;
		Units = units;
		CurrentDateTime = currentDateTime;
	}
}

public class CourseListViewModel
{
	public List<CourseViewModel> Courses;
	public string LastTryCourseId { get; set; }
	public string LastTryCourseTitle { get; set; }
	public string InvalidCharacters { get; set; }
}

public class CourseViewModel
{
	public string Title { get; set; }
	public string Id { get; set; }

	[CanBeNull]
	public TempCourse TempCourse { get; set; }
}

public class PackagesViewModel
{
	public Course Course { get; set; }
	public bool HasPackage { get; set; }
	public List<CourseVersion> Versions { get; set; }
	public CourseVersion PublishedVersion { get; set; }
	public CourseGit CourseGit { get; set; }
	public bool OpenStep1 { get; set; }
	public bool OpenStep2 { get; set; }
	public string GitSecret { get; set; }

	public string Error { get; set; }
	public string HelpUrl { get; set; } = "https://docs.google.com/document/d/1tL_D2SGIv163GpVVr5HrZTBEgcMk5shCKN5J6le4pTc/edit?usp=sharing";
}

public class AdminCommentsViewModel
{
	public string CourseId { get; set; }
	public bool IsCommentsEnabled { get; set; }
	public CommentModerationPolicy ModerationPolicy { get; set; }
	public bool OnlyInstructorsCanReply { get; set; }
	public List<CommentViewModel> Comments { get; set; }
}

public class ManualCheckingQueueViewModel
{
	public string CourseId { get; set; }
	public Guid? QueueSlideId { get; set; }
	public List<ManualCheckingQueueItemViewModel> Checkings { get; set; }
	public string Message { get; set; }
	public List<SingleGroup> Groups { get; set; }
	public Dictionary<int, List<GroupAccess>> GroupsAccesses { get; set; }
	public List<string> SelectedGroupsIds { get; set; }
	public string SelectedGroupsIdsJoined => string.Join(",", SelectedGroupsIds);
	public bool AlreadyChecked { get; set; }
	public bool ExistsMore { get; set; }
	public bool ShowFilterForm { get; set; }
	public List<KeyValuePair<Guid, Slide>> Slides { get; set; }
}

public class ManualCheckingQueueItemViewModel
{
	public AbstractManualSlideChecking CheckingQueueItem { get; set; }

	public Guid ContextSlideId { get; set; }
	public string ContextSlideTitle { get; set; }
	public int ContextMaxScore { get; set; }
	public List<ExerciseCodeReview> ContextReviews { get; set; }
	public string ContextExerciseSolution { get; set; }
	public DateTime ContextTimestamp { get; set; }
}

public class DiagnosticsModel
{
	public string CourseId { get; set; }
	public bool IsDiagnosticsForVersion { get; set; }
	public bool IsVersionPublished { get; set; }
	public Guid VersionId { get; set; }
	public CourseDiff CourseDiff { get; set; }
	public string Warnings { get; set; }
	public bool IsTempCourse { get; set; }
}