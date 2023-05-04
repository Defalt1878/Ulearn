using System;
using System.IO;
using System.Threading.Tasks;
using Database.Models;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;

namespace Ulearn.Web.Api.Utils.Courses;

public interface IMasterCourseManager : ICourseUpdater
{
	Task<FileInfo> GenerateOrFindStudentZip(string courseId, Slide slide);
	Task<bool> CreateCourseIfNotExists(string courseId, Guid versionId, string courseTitle, string userId);
	Task<TempCourse> CreateTempCourse(string baseCourseId, string userId);
	string GetTempCourseId(string baseCourseId, string userId);
	Task<(TempCourse Course, string Error)> UpdateTempCourseFromUserZipStream(string tmpCourseId, Stream updateZipContent, bool isFullCourse);
	DirectoryInfo GetExtractedCourseDirectory(string courseId);
}