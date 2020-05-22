﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;

namespace Database.DataContexts
{
	public class TempCoursesRepo
	{
		private readonly ULearnDb db;

		public TempCoursesRepo()
			: this(new ULearnDb())
		{
		}

		public TempCoursesRepo(ULearnDb db)
		{
			this.db = db;
		}

		public TempCourse Find(string courseId)
		{
			return db.TempCourses.SingleOrDefault(course => course.CourseId == courseId);
		}
		
		public TempCourseError FindError(string courseId)
		{
			return db.TempCourseErrors.SingleOrDefault(course => course.CourseId == courseId);
		}

		public async Task<TempCourse> AddTempCourse(string courseId, string authorId)
		{
			var tempCourse = new TempCourse()
			{
				CourseId = courseId,
				AuthorId = authorId,
				LoadingTime = DateTime.Now,
				LastUpdateTime = DateTime.Now
			};
			var result = db.TempCourses.Add(tempCourse);
			await db.SaveChangesAsync();
			return result;
		}

		public async Task UpdateTempCourseLoadingTime(string courseId)
		{
			var course = db.TempCourses.Find(courseId);
			if (course == null)
				return;

			course.LoadingTime = DateTime.Now;
			await db.SaveChangesAsync();
		}

		public async Task UpdateTempCourseLastUpdateTime(string courseId)
		{
			var course = db.TempCourses.Find(courseId);
			if (course == null)
				return;

			course.LastUpdateTime = DateTime.Now;
			await db.SaveChangesAsync();
		}

		public async Task<TempCourseError> UpdateOrAddTempCourseError(string courseId, string error)
		{
			var course = db.TempCourses.Find(courseId);
			if (course == null)
				return null;
			var existingError = db.TempCourseErrors.Find(courseId);
			TempCourseError result;
			if (existingError == null)
			{
				var errorEntity = new TempCourseError() { CourseId = courseId, Error = error };
				result = db.TempCourseErrors.Add(errorEntity);
			}
			else
			{
				existingError.Error = error;
				result = existingError;
			}

			await db.SaveChangesAsync();
			return result;
		}

		public async Task MarkTempCourseAsNotErrored(string courseId)
		{
			var course = db.TempCourses.Find(courseId);
			if (course == null)
				return;
			var error = db.TempCourseErrors.Find(courseId);
			if (error == null)
			{
				await UpdateOrAddTempCourseError(courseId, null);
				return;
			}

			error.Error = null;
			await db.SaveChangesAsync();
		}
	}
}