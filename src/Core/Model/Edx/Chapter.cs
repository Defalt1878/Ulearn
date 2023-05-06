using System;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Model.Edx
{
	[XmlRoot("chapter")]
	public class Chapter : EdxItem
	{
		[XmlIgnore]
		public override string SubfolderName => "chapter";

		[XmlAttribute("start")]
		public string StartDateAsString { get; set; }

		[XmlIgnore]
		public DateTime? Start
		{
			get => StartDateAsString == null ? null : DateTime.Parse(StartDateAsString, CultureInfo.InvariantCulture);
			set => StartDateAsString = value?.ToString("O");
		}

		[XmlIgnore]
		public SequentialReference[] sequentialReferences;

		[XmlElement("sequential")]
		public SequentialReference[] SequentialReferences
		{
			get { return sequentialReferences ??= Array.Empty<SequentialReference>(); }
			set => sequentialReferences = value;
		}

		[XmlIgnore]
		public Sequential[] Sequentials;

		public Chapter()
		{
		}

		public Chapter(string urlName, string displayName, DateTime? start, Sequential[] sequentials)
		{
			DisplayName = displayName;
			Start = start;
			UrlName = urlName;
			Sequentials = sequentials;
			SequentialReferences = sequentials.Select(x => x.GetReference()).ToArray();
		}

		public ChapterReference GetReference()
		{
			return new ChapterReference { UrlName = UrlName };
		}

		public override void SaveAdditional(string folderName)
		{
			foreach (var sequential in Sequentials)
				sequential.Save(folderName);
		}

		public static Chapter Load(string folderName, string urlName, EdxLoadOptions options)
		{
			return Load<Chapter>(folderName, "chapter", urlName, options, c =>
			{
				c.Sequentials = c.SequentialReferences.Select(x => Sequential.Load(folderName, x.UrlName, options)).ExceptNulls().ToArray();
				c.SequentialReferences = c.Sequentials.Select(v => v.GetReference()).ToArray();
			});
		}
	}
}