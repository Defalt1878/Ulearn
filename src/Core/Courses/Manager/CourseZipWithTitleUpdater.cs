using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Ionic.Zip;
using Ulearn.Common;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.Courses.Manager
{
	public static class CourseZipWithTitleUpdater
	{
		public static void Update(FileInfo exampleZipToModify, string courseTitle)
		{
			var nsResolver = new XmlNamespaceManager(new NameTable());
			nsResolver.AddNamespace("ulearn", "https://ulearn.me/schema/v2");
			using var zip = ZipFile.Read(exampleZipToModify.FullName, new ReadOptions { Encoding = ZipUtils.Cp866 });
			var courseXml = zip.Entries.FirstOrDefault(e => !e.IsDirectory && (Path.GetFileName(e.FileName)?.Equals("course.xml", StringComparison.OrdinalIgnoreCase) ?? false));
			if (courseXml != null)
				UpdateXmlAttribute(zip[courseXml.FileName], "//ulearn:course", "title", courseTitle, zip, nsResolver);
		}

		private static void UpdateXmlAttribute(ZipEntry entry, string selector, string attribute, string value, ZipFile zip, IXmlNamespaceResolver nsResolver)
		{
			UpdateXmlEntity(entry, selector, element =>
			{
				var elementAttribute = element.Attribute(attribute);
				if (elementAttribute != null)
					elementAttribute.Value = value;
			}, zip, nsResolver);
		}

		private static void UpdateXmlEntity(ZipEntry entry, string selector, Action<XElement> update, ZipFile zip, IXmlNamespaceResolver nsResolver)
		{
			using var output = StaticRecyclableMemoryStreamManager.Manager.GetStream();
			using (var entryStream = entry.OpenReader())
			{
				var xml = XDocument.Load(entryStream);
				var element = xml.XPathSelectElement(selector, nsResolver);
				update(element.EnsureNotNull($"no element [{selector}] in zip entry {entry.FileName}"));
				xml.Save(output);
			}

			output.Position = 0;
			zip.UpdateEntry(entry.FileName, output.ToArray());
			zip.Save();
		}
	}
}