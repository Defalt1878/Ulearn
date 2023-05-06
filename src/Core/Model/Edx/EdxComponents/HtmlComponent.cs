using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Ulearn.Core.Model.Edx.EdxComponents
{
	public record StaticFileForEdx(FileInfo StaticFile, string EdxFileName);

	[XmlRoot("html")]
	public class HtmlComponent : Component
	{
		[XmlIgnore]
		public string HtmlContent;

		[XmlIgnore]
		public List<StaticFileForEdx> StaticFiles;

		[XmlAttribute("filename")]
		public string Filename;

		[XmlIgnore]
		public override string SubfolderName => "html";

		[XmlIgnore]
		public Component[] Subcomponents;

		public HtmlComponent()
		{
		}

		public HtmlComponent(string urlName, string displayName, string filename, string htmlContent)
		{
			UrlName = urlName;
			DisplayName = displayName;
			Filename = filename;
			HtmlContent = htmlContent;
		}

		public HtmlComponent(string urlName, string displayName, string filename, string htmlContent, List<StaticFileForEdx> staticFiles)
		{
			UrlName = urlName;
			DisplayName = displayName;
			Filename = filename;
			HtmlContent = htmlContent;
			StaticFiles = staticFiles;
		}

		public override void Save(string folderName)
		{
			base.Save(folderName);
			File.WriteAllText($"{folderName}/{SubfolderName}/{UrlName}.html", HtmlContent);
		}

		public override void SaveAdditional(string folderName)
		{
			if (Subcomponents != null)
				foreach (var subcomponent in Subcomponents)
					subcomponent.SaveAdditional(folderName);
			SaveStaticFiles(folderName, StaticFiles);
		}

		private static void SaveStaticFiles(string folderName, List<StaticFileForEdx> staticFiles)
		{
			try
			{
				foreach (var (file, edxFileName) in staticFiles.EmptyIfNull())
					File.Copy(file.FullName, $"{folderName}/static/{edxFileName}", true);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		public override EdxReference GetReference()
		{
			return new HtmlComponentReference { UrlName = UrlName };
		}

		public override string AsHtmlString()
		{
			return HtmlContent;
		}

		public static HtmlComponent Load(string folderName, string urlName, EdxLoadOptions options)
		{
			return Load<HtmlComponent>(folderName, "html", urlName, options,
				c => { c.HtmlContent = File.ReadAllText($"{folderName}/html/{c.Filename}.html"); });
		}
	}
}