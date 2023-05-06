using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Ulearn.Common.Extensions;
using Vostok.Logging.Abstractions;

namespace Ulearn.Core.Courses
{
	// Это данные, которые добавляются на сервере а автору курса никогда не видны
	[DataContract]
	public class CourseVersionToken : IEqualityComparer<CourseVersionToken>
	{
		public const string VersionFileName = ".version";

		// При передаче в базу время немного меняется, так что сравниваем с погрешностью
		[DataMember(Name = "loadingTime", EmitDefaultValue = false)]
		private string loadingTimeStr;

		public CourseVersionToken() // Для десериализации
		{
		}

		public CourseVersionToken(Guid version)
		{
			Version = version;
		}

		public CourseVersionToken(DateTime tempCourseLoadingTime)
		{
			LoadingTime = tempCourseLoadingTime;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(CourseVersionToken));

		[DataMember(Name = "version", EmitDefaultValue = false)]
		public Guid? Version { get; set; } // null для временных курсов и проверяемых курсов, для которых еще нет версии

		// То, что вместо версии во временном курсе, дата LoadingTime
		// Не сравнивать даты напрямую, сравнивать CourseVersionToken
		[IgnoreDataMember]
		public DateTime? LoadingTime
		{
			get => DateTimeExtensions.FromSortable(loadingTimeStr);
			init => loadingTimeStr = value?.ToSortable();
		}

		public bool Equals(CourseVersionToken x, CourseVersionToken y)
		{
			if (ReferenceEquals(x, y))
				return true;
			if (ReferenceEquals(x, null))
				return false;
			if (ReferenceEquals(y, null))
				return false;
			if (x.GetType() != y.GetType())
				return false;
			return Nullable.Equals(x.Version, y.Version) && Equals(x.loadingTimeStr, y.loadingTimeStr);
		}

		public int GetHashCode(CourseVersionToken obj)
		{
			if (obj.Version != null)
				return obj.Version.GetHashCode();
			if (obj.loadingTimeStr != null)
				return obj.loadingTimeStr.GetHashCode();
			return 0;
		}

		[NotNull]
		// Из общей папки читать CourseVersionToken нужно под дисковым локом на курс
		public static CourseVersionToken Load(DirectoryInfo courseDirectory)
		{
			var versionFile = courseDirectory.GetFile(VersionFileName);
			if (!versionFile.Exists)
			{
				Log.Error($".version not exists in {courseDirectory.FullName}");
				return new CourseVersionToken(default(Guid)); // Допустимо только в CourseTool
			}

			return JsonConvert.DeserializeObject<CourseVersionToken>(versionFile.ContentAsUtf8());
		}

		// Из общей папки писать CourseVersionToken нужно под дисковым локом на курс
		public async Task Save(DirectoryInfo directory)
		{
			var fullName = Path.Combine(directory.FullName, VersionFileName);
			await using var file = File.Open(fullName, FileMode.Create, FileAccess.Write, FileShare.None);
			var json = JsonConvert.SerializeObject(this, Formatting.Indented);
			var bytes = Encoding.UTF8.GetBytes(json);
			await file.WriteAsync(bytes, 0, bytes.Length);
		}

		public static void RemoveFile(DirectoryInfo courseDirectory)
		{
			var versionFile = courseDirectory.GetFile(VersionFileName);
			if (versionFile.Exists)
				versionFile.Delete();
		}

		public bool IsTempCourse()
		{
			return loadingTimeStr != null;
		}

		public override bool Equals(object obj)
		{
			return Equals(this, obj as CourseVersionToken);
		}

		public override int GetHashCode()
		{
			return GetHashCode(this);
		}

		public static bool operator ==(CourseVersionToken x, CourseVersionToken y)
		{
			if (x is null)
				return y is null;
			return x.Equals(y);
		}

		public static bool operator !=(CourseVersionToken x, CourseVersionToken y)
		{
			return !(x == y);
		}

		public override string ToString()
		{
			if (Version != null)
				return Version.ToString();
			return LoadingTime is null ? "" : loadingTimeStr;
		}
	}
}