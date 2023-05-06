using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Ulearn.Common.Extensions;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Model.Edx
{
	public class OlxPatcher
	{
		public readonly string OlxPath;

		public OlxPatcher(string olxPath)
		{
			OlxPath = olxPath;
		}

		public void PatchComponents(EdxCourse course, IEnumerable<Component> components, bool replaceExisting = true)
		{
			var newVerticals = new List<Vertical>();
			foreach (var component in components)
			{
				var filename = $"{OlxPath}/{component.SubfolderName}/{component.UrlName}.xml";
				if (File.Exists(filename))
				{
					if (replaceExisting)
					{
						string displayName;
						try
						{
							var xml = new XmlDocument();
							xml.Load(filename);
							displayName = xml.DocumentElement.Attributes["display_name"].InnerText;
						}
						catch (Exception)
						{
							displayName = component.DisplayName;
						}

						component.DisplayName = displayName;
						component.Save(OlxPath);
					}
				}
				else
				{
					newVerticals.Add(new Vertical(Utils.NewNormalizedGuid(), component.DisplayName, new[] { component }));
				}
			}

			Add(course, newVerticals.ToArray());
		}

		public void PatchVerticals(EdxCourse course, IEnumerable<Vertical[]> verticals, bool replaceExisting = true)
		{
			var newVerticals = new List<Vertical>();
			foreach (var subverticals in verticals)
			{
				var existsMap = subverticals.ToDictionary(sv => sv, sv => File.Exists($"{OlxPath}/vertical/{sv.UrlName}.xml"));
				if (subverticals.Any(x => existsMap[x]))
				{
					if (replaceExisting)
					{
						foreach (var subvertical in subverticals)
							subvertical.Save(OlxPath);

						if (subverticals.Length > 1)
							SaveSequentialContainingSubverticals(
								course,
								subverticals.Where(x => !existsMap[x]).ToArray(),
								subverticals.First(x => existsMap[x])
							);
					}
				}
				else
				{
					newVerticals.AddRange(subverticals);
				}
			}

			Add(course, newVerticals.ToArray());
		}

		public void Add(EdxCourse course, Vertical[] verticals)
		{
			if (verticals.Length != 0)
				course.CreateUnsortedChapter(OlxPath, verticals);
		}

		private void SaveSequentialContainingSubverticals(EdxCourse course, IEnumerable<Vertical> verticalsToAdd, Vertical afterThisVertical)
		{
			var sequential = course.GetSequentialContainingVertical(afterThisVertical.UrlName);
			var filename = $"{OlxPath}/sequential/{sequential.UrlName}.xml";
			var sequentialXml = XDocument.Load(filename).Root ?? new XElement("sequential");
			var refs = sequentialXml.Elements("vertical").ToList();
			var insertIndex = refs
				.Select((v, i) => new { urlName = v.Attribute("url_name").Value, i })
				.First(v => v.urlName == afterThisVertical.UrlName).i + 1;
			refs.InsertRange(insertIndex, verticalsToAdd.Select(v => new XElement("vertical", new XAttribute("url_name", v.UrlName))));
			sequentialXml.ReplaceNodes(refs);
			sequentialXml.Save(filename);
			new FileInfo(filename).RemoveXmlDeclaration();
		}
	}
}