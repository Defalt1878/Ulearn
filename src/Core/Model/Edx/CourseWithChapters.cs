using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.Model.Edx
{
	[XmlRoot("course")]
	public class CourseWithChapters : EdxItem
	{
		[XmlIgnore]
		public override string SubfolderName => "course";

		[XmlAttribute("advanced_modules")]
		public string AdvancedModules;

		[XmlAttribute("lti_passports")]
		public string LtiPassports;

		[XmlAttribute("use_latex_compiler")]
		public bool UseLatexCompiler;

		[XmlElement("chapter", Order = 1)]
		public ChapterReference[] ChapterReferences
		{
			get { return chapterReferences ??= Array.Empty<ChapterReference>(); }
			set => chapterReferences = value;
		}

		[XmlElement("wiki", Order = 2)]
		public Wiki[] Wiki;

		[XmlIgnore]
		public Chapter[] Chapters;

		private ChapterReference[] chapterReferences;

		public CourseWithChapters()
		{
			ChapterReferences = Array.Empty<ChapterReference>();
		}

		public CourseWithChapters(string urlName, string displayName, string[] advancedModules, string[] ltiPassports, bool useLatexCompiler, Chapter[] chapters)
		{
			UrlName = urlName;
			DisplayName = displayName;
			AdvancedModules = advancedModules == null ? null : JsonConvert.SerializeObject(advancedModules);
			LtiPassports = ltiPassports == null ? null : JsonConvert.SerializeObject(ltiPassports);
			UseLatexCompiler = useLatexCompiler;
			Chapters = chapters;
			ChapterReferences = chapters.Select(x => x.GetReference()).ToArray();
		}

		public override void Save(string folderName, bool withAdditionals)
		{
			var courseFile = $"{folderName}/{SubfolderName}/{UrlName}.xml";
			if (File.Exists(courseFile))
			{
				var doc = new XmlDocument();
				doc.LoadXml(File.ReadAllText(courseFile));

				XmlNode root = doc.DocumentElement;

				var count = root.ChildNodes.Count;
				for (var i = 0; i < count; i++)
					foreach (XmlElement childNode in root.ChildNodes)
						if (childNode.Name is "chapter" or "wiki")
							root.RemoveChild(childNode);

				foreach (var chapter in Chapters)
				{
					var elem = doc.CreateElement("chapter");
					elem.SetAttribute("url_name", chapter.UrlName);
					root.AppendChild(elem);
				}

				foreach (var wiki in Wiki.EmptyIfNull())
				{
					var elem = doc.CreateElement("wiki");
					elem.SetAttribute("slug", wiki.Slug);
					root.AppendChild(elem);
				}

				File.WriteAllText(courseFile, doc.XmlSerialize());
				if (withAdditionals)
					SaveAdditional(folderName);
			}
			else
			{
				base.Save(folderName, withAdditionals);
			}
		}

		public override void SaveAdditional(string folderName)
		{
			foreach (var chapter in Chapters)
				chapter.Save(folderName);
		}

		public static CourseWithChapters Load(string folderName, string urlName, EdxLoadOptions options)
		{
			return Load<CourseWithChapters>(folderName, "course", urlName, options, c =>
			{
				c.Chapters = c.ChapterReferences.Select(x => Chapter.Load(folderName, x.UrlName, options)).ExceptNulls().ToArray();
				c.ChapterReferences = c.Chapters.Select(v => v.GetReference()).ToArray();
			});
		}
	}

	public class Wiki
	{
		[XmlAttribute("slug")]
		public string Slug;
	}
}