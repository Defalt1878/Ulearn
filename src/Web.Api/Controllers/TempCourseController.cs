using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Web.Api.Models.Responses.TempCourses;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Utils.Courses;
using Vostok.Logging.Abstractions;


namespace Ulearn.Web.Api.Controllers
{
	[Route("/temp-courses")]
	[Route("/tempCourses")] // Obsolete
	public class TempCourseController : BaseController
	{
		private readonly ITempCoursesRepo tempCoursesRepo;
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly IMasterCourseManager courseManager;
		private static ILog log => LogProvider.Get().ForContext(typeof(TempCourseController));

		public TempCourseController(ICourseStorage courseStorage, IMasterCourseManager courseManager, UlearnDb db, IUsersRepo? usersRepo, ITempCoursesRepo tempCoursesRepo, ICourseRolesRepo courseRolesRepo)
			: base(courseStorage, db, usersRepo)
		{
			this.tempCoursesRepo = tempCoursesRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.courseManager = courseManager;
		}

		/// <summary>
		/// Создать временный курс для базового курса с courseId
		/// </summary>
		[Authorize]
		[HttpPost("{courseId}")]
		public async Task<ActionResult<TempCourseUpdateResponse>> CreateCourse([FromRoute] string courseId)
		{
			if (!courseStorage.HasCourse(courseId))
			{
				return new TempCourseUpdateResponse
				{
					ErrorType = ErrorType.NotFound,
					Message = $"Не существует курса {courseId}"
				};
			}

			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
			{
				return new TempCourseUpdateResponse
				{
					ErrorType = ErrorType.Forbidden,
					Message = $"Необходимо быть администратором курса {courseId}"
				};
			}

			var tempCourse = await courseManager.CreateTempCourse(courseId, UserId);

			return new TempCourseUpdateResponse
			{
				Message = $"Временный курс с id {tempCourse.CourseId} успешно создан.",
				LastUploadTime = tempCourse.LoadingTime
			};
		}

		/// <summary>
		/// Ошибки при загрузке временного курса c courseId
		/// </summary>
		[Authorize(Policy = "Instructors")]
		[HttpGet("{courseId}/errors")]
		public async Task<ActionResult<TempCourseErrorsResponse>> GetError([FromRoute] string courseId)
		{
			var tmpCourseError = await tempCoursesRepo.GetCourseError(courseId);
			return new TempCourseErrorsResponse
			{
				TempCourseError = tmpCourseError?.Error
			};
		}

		/// <summary>
		/// Загрузить изменения временного курса для базового курса с courseId
		/// </summary>
		[Authorize]
		[HttpPatch("{courseId}")]
		public async Task<ActionResult<TempCourseUpdateResponse>> UploadCourse([FromRoute] string courseId, List<IFormFile> files)
		{
			return await UploadCourse(courseId, files, false);
		}

		/// <summary>
		/// Загрузить целиком временный курс для базового курса с courseId
		/// </summary>
		[HttpPut("{courseId}")]
		[Authorize]
		public async Task<ActionResult<TempCourseUpdateResponse>> UploadFullCourse([FromRoute] string courseId, List<IFormFile> files)
		{
			return await UploadCourse(courseId, files, true);
		}

		private async Task<TempCourseUpdateResponse> UploadCourse(string courseId, List<IFormFile> files, bool isFull)
		{
			var tmpCourseId = courseManager.GetTempCourseId(courseId, UserId);
			var tmpCourse = await tempCoursesRepo.Find(tmpCourseId);
			if (tmpCourse == null)
			{
				return new TempCourseUpdateResponse
				{
					ErrorType = ErrorType.NotFound,
					Message = $"Вашей временной версии курса {courseId} не существует. Для создания испрользуйте метод Create"
				};
			}

			if (!await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.CourseAdmin))
			{
				return new TempCourseUpdateResponse
				{
					ErrorType = ErrorType.Forbidden,
					Message = $"Необходимо быть администратором курса {tmpCourseId}"
				};
			}

			if (files.Count != 1)
			{
				throw new Exception($"Expected count of files is 1, but was {files.Count}");
			}

			var file = files.Single();

			if (file == null || file.Length <= 0)
				throw new Exception("The file is empty");
			var fileName = Path.GetFileName(file.FileName);
			if (fileName == null || !fileName.ToLower().EndsWith(".zip"))
				throw new Exception("The file should have .zip extension");

			var (tempCourse, error) = await courseManager.UpdateTempCourseFromUserZipStream(tmpCourseId, file.OpenReadStream(), isFull);

			if (error != null)
			{
				return new TempCourseUpdateResponse
				{
					Message = error,
					ErrorType = ErrorType.CourseError
				};
			}

			return new TempCourseUpdateResponse
			{
				Message = $"Временный курс {tmpCourseId} успешно обновлен",
				LastUploadTime = tempCourse.LoadingTime
			};
		}
	}
}