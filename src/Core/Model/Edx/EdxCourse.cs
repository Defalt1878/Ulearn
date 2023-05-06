using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ulearn.Common.Extensions;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Model.Edx
{
	public delegate void NonExistentItemHandler(string type, string urlName);

	public record FileInEdxCourse(string Directory, string FileName, string Extension); // Строки в record сравниваются по значению

	public class EdxLoadOptions
	{
		public bool FailOnNonExistingItem = true;
		public NonExistentItemHandler HandleNonExistentItemTypeName;
		public Action<FileInEdxCourse> OnLoadExistingEdxItem; // Соответствует загруженным xml файлам
	}

	[XmlRoot("course")]
	public class EdxCourse
	{
		[XmlAttribute("url_name")]
		public string UrlName { get; set; }

		[XmlAttribute("org")]
		public string Organization;

		[XmlAttribute("course")]
		public string CourseName;

		[XmlIgnore]
		public string[] StaticFiles;

		[XmlIgnore]
		public CourseWithChapters CourseWithChapters;

		public EdxCourse()
		{
		}

		public EdxCourse(string courseId, string organization, string courseTitle, string[] advancedModules, string[] ltiPassports, Chapter[] chapters)
		{
			CourseName = courseId;
			UrlName = courseId;
			Organization = organization;
			CourseWithChapters = new CourseWithChapters(courseId, courseTitle, advancedModules, ltiPassports, true, chapters);
			StaticFiles = Directory.GetFiles($"{Utils.GetRootDirectory()}/static");
		}

		private static void CreateDirectories(string rootDir, params string[] subDirs)
		{
			if (!Directory.Exists(rootDir))
				Directory.CreateDirectory(rootDir);
			foreach (var subdir in subDirs.Select(subDir => $"{rootDir}/{subDir}").Where(subdir => !Directory.Exists(subdir)))
				Directory.CreateDirectory(subdir);
		}

		public void Save(string folderName)
		{
			CreateDirectories(folderName, "static", "course", "chapter", "sequential", "vertical", "video", "html", "lti", "problem");
			foreach (var file in StaticFiles)
			{
				var newName = $"{folderName}/static/{Path.GetFileName(file)}";
				if (!Path.GetFullPath(file).Equals(Path.GetFullPath(newName), StringComparison.OrdinalIgnoreCase))
					File.Copy(file, newName, true);
			}

			var courseFile = $"{folderName}/course.xml";
			if (File.Exists(courseFile))
				CourseWithChapters.UrlName = new FileInfo(courseFile).DeserializeXml<EdxCourse>().UrlName;
			else
				File.WriteAllText(courseFile, this.XmlSerialize());

			CourseWithChapters.Save(folderName);
		}

		public static EdxCourse Load(string folderName, EdxLoadOptions options = null)
		{
			options ??= new EdxLoadOptions();
			var course = new FileInfo($"{folderName}/course.xml").DeserializeXml<EdxCourse>();
			course.StaticFiles = $"{folderName}/static".GetFiles();
			course.CourseWithChapters = CourseWithChapters.Load(folderName, course.UrlName, options);
			return course;
		}

		public void CreateUnsortedChapter(string folderName, Vertical[] verticals)
		{
			if (CourseWithChapters.Chapters.All(x => x.UrlName != "Unsorted"))
			{
				var chapters = new List<Chapter>(CourseWithChapters.Chapters);
				var newChapter = new Chapter("Unsorted", "Unsorted", DateTime.MaxValue, new[] { new Sequential(Utils.NewNormalizedGuid(), "Unsorted " + DateTime.Now, verticals) { VisibleToStaffOnly = true } });
				chapters.Add(newChapter);
				CourseWithChapters.Chapters = chapters.ToArray();
				CourseWithChapters.ChapterReferences = CourseWithChapters.Chapters.Select(x => new ChapterReference { UrlName = x.UrlName }).ToArray();
				CourseWithChapters.Save(folderName, false);
				newChapter.Save(folderName);
			}
			else
			{
				var unsortedChapter = CourseWithChapters.Chapters.Single(x => x.UrlName == "Unsorted");
				var filename = $"{folderName}/chapter/{unsortedChapter.UrlName}.xml";
				var chapterXml = XDocument.Load(filename).Root ?? new XElement("chapter");
				var newSequential = new Sequential(Utils.NewNormalizedGuid(), "Unsorted " + DateTime.Now, verticals);
				chapterXml.Add(new XElement("sequential", new XAttribute("url_name", newSequential.UrlName)));
				chapterXml.Save(filename);
				new FileInfo(filename).RemoveXmlDeclaration();
				newSequential.Save(folderName);
			}
		}

		public Sequential GetSequentialContainingVertical(string verticalId)
		{
			var sequentials = CourseWithChapters.Chapters.SelectMany(
				x => x.Sequentials.Where(y => y.Verticals.Any(z => z.UrlName == verticalId))).ToList();
			if (sequentials.Count > 1)
				throw new Exception(
					$"Vertical {verticalId} are in several sequentials {string.Join(", ", sequentials.Select(s => s.UrlName))}");
			if (sequentials.Count == 0)
				throw new Exception(
					$"Vertical {verticalId} are not in any sequential!");

			return sequentials.Single();
		}

		public Vertical GetVerticalById(string id)
		{
			return CourseWithChapters
				.Chapters
				.SelectMany(x => x.Sequentials.SelectMany(y => y.Verticals))
				.FirstOrDefault(x => x.UrlName == id);
		}

		public VideoComponent GetVideoBySlideId(string slideId)
		{
			return CourseWithChapters
				.Chapters
				.SelectMany(x => x.Sequentials.SelectMany(y => y.Verticals.SelectMany(z => z.Components)))
				.OfType<VideoComponent>()
				.FirstOrDefault(x => x.UrlName.StartsWith(slideId));
		}
	}
}