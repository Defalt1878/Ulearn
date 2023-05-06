using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Courses.Manager
{
	public class CourseStorage : ICourseStorage, ICourseStorageUpdater
	{
		private readonly ConcurrentDictionary<string, Course> courses = new(StringComparer.InvariantCultureIgnoreCase);

		private static ILog Log => LogProvider.Get().ForContext(typeof(CourseStorage));

		public event CourseChangedEventHandler CourseChangedEvent;

		public Course GetCourse(string courseId)
		{
			if (courses.TryGetValue(courseId, out var course))
				return course;
			throw new CourseNotFoundException(courseId);
		}

		[CanBeNull]
		public Course FindCourse(string courseId)
		{
			return courses.TryGetValue(courseId, out var course) ? course : null;
		}

		public bool HasCourse(string courseId)
		{
			return FindCourse(courseId) != null;
		}

		public IEnumerable<Course> GetCourses()
		{
			return courses.Select(kvp => kvp.Value).OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase);
		}

		public void AddOrUpdateCourse(Course course)
		{
			courses.AddOrUpdate(course.Id, _ => course, (_, _) => course);
			Log.Info($"В CourseStorage загружен курс {course.Id} версии {course.CourseVersionToken}");
			CourseChangedEvent?.Invoke(course.Id);
		}

		public void TryRemoveCourse(string courseId)
		{
			if (courses.TryRemove(courseId, out _))
				Log.Warn($"Из CourseStorage удален курс {courseId}");
		}
	}
}