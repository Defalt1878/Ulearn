using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.Courses
{
	/*
* These settings are loading from course.xml from the root folder of the course. 
*/
	[XmlRoot("course", IsNullable = false, Namespace = "https://ulearn.me/schema/v2")]
	public class CourseSettings
	{
		[XmlIgnore]
		private static readonly Regex scoringGroupIdRegex = new("^[a-z0-9_]+$", RegexOptions.IgnoreCase);

		public CourseSettings()
		{
		}

		public CourseSettings(string title, Language? defaultLanguage, PreludeFile[] preludes)
			: this()
		{
			Title = title;
			DefaultLanguage = defaultLanguage;
			Preludes = preludes;
		}

		public CourseSettings(CourseSettings other)
			: this()
		{
			Title = other.Title;
			DefaultLanguage = other.DefaultLanguage;
			Preludes = (PreludeFile[])other.Preludes.Clone();
			DictionaryFile = other.DictionaryFile;
		}

		[XmlAttribute("title")]
		public string Title { get; set; }

		[XmlElement("defaultLanguage")]
		public Language? DefaultLanguage { get; set; }

		[XmlElement("videoAnnotationsGoogleDoc")]
		public string VideoAnnotationsGoogleDoc { get; set; }

		[XmlElement("enableCodeReviewAndQuizManualCheckForEveryone")]
		public bool IsManualCheckingEnabled { get; set; }

		[XmlElement("scoring")]
		public ScoringSettings Scoring { get; set; } = new();

		[XmlArray("units")]
		[XmlArrayItem("add")]
		public string[] UnitPaths { get; set; } = Array.Empty<string>();

		[XmlArray("preludes")]
		[XmlArrayItem("prelude")]
		public PreludeFile[] Preludes { get; set; }

		[XmlElement("dictionaryFile")]
		public string DictionaryFile { get; set; }

		[XmlElement("description")]
		public string Description { get; set; }

		public static CourseSettings DefaultSettings => new(
			null,
			null,
			Array.Empty<PreludeFile>()
		);

		public static CourseSettings Load(DirectoryInfo dir)
		{
			var file = dir.GetFile("course.xml");
			if (!file.Exists)
				return new CourseSettings(DefaultSettings);

			var settings = file.DeserializeXml<CourseSettings>();
			settings.Preludes ??= Array.Empty<PreludeFile>();

			foreach (var scoringGroup in settings.Scoring.Groups.Values)
				if (!scoringGroupIdRegex.IsMatch(scoringGroup.Id))
					throw new CourseLoadingException(
						$"Некорректный идентификатор группы баллов <group id={scoringGroup.Id}> (файл course.xml). " +
						"Идентификатор группы баллов может состоить только из латинских букв, цифр и подчёркивания, а также не может быть пустым. " +
						"Понятное человеку название используйте в аббревиатуре и имени группы."
					);

			return settings;
		}

		public string GetPrelude(Language? language)
		{
			var prelude = Preludes.FirstOrDefault(file => file.Language == language);
			if (prelude == null && Title != null && this != DefaultSettings)
				return DefaultSettings.GetPrelude(language);
			return prelude?.File;
		}
	}

	public class PreludeFile
	{
		public PreludeFile()
		{
		}

		public PreludeFile(Language language, string file)
		{
			Language = language;
			File = file;
		}

		[XmlAttribute("language")]
		public Language Language { get; set; }

		[XmlText]
		public string File { get; set; }

		#region Equals

		protected bool Equals(PreludeFile other)
		{
			return Language == other.Language && string.Equals(File, other.File);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((PreludeFile)obj);
		}

		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
		public override int GetHashCode()
		{
			unchecked
			{
				return ((int)Language * 397) ^ (File != null ? File.GetHashCode() : 0);
			}
		}

		#endregion
	}
}