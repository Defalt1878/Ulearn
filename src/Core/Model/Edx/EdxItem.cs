using System;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using Ulearn.Common.Extensions;

namespace Ulearn.Core.Model.Edx
{
	public abstract class EdxItem
	{
		[XmlAttribute("url_name")]
		public string UrlName { get; set; }

		[XmlAttribute("display_name")]
		public string DisplayName { get; set; }

		[XmlIgnore]
		public virtual string SubfolderName { get; set; }

		public virtual bool ShouldSerializeUrlName()
		{
			return false;
		}

		public virtual void Save(string folderName)
		{
			Save(folderName, true);
		}

		public virtual void Save(string folderName, bool withAdditionals)
		{
			var path = Path.Combine(folderName, SubfolderName);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			var filename = Path.Combine(path, UrlName + ".xml");
			var converted = this.XmlSerialize();
			var originalXDoc = File.Exists(filename) ? XDocument.Load(filename) : null;
			if (originalXDoc != null)
			{
				var convertedXDoc = XDocument.Load(new StringReader(converted));
				foreach (var attribute in originalXDoc.Root!.Attributes())
				{
					if (convertedXDoc.Root!.Attribute(attribute.Name) is null)
						convertedXDoc.Root.SetAttributeValue(attribute.Name, attribute.Value);
					converted = convertedXDoc.ToString();
				}
			}

			File.WriteAllText(filename, converted);
			if (withAdditionals)
				SaveAdditional(folderName);
		}

		public virtual void SaveAdditional(string folderName)
		{
		}

		public static TComponent Load<TComponent>(string folderName, string type, string urlName, EdxLoadOptions options,
			Action<TComponent> loadInner = null, Func<TComponent> customDeserialize = null) where TComponent : EdxItem
		{
			try
			{
				var fileInfo = new FileInfo($"{folderName}/{type}/{urlName}.xml");
				if (!fileInfo.Exists)
				{
					if (options.FailOnNonExistingItem)
					{
						throw new FileNotFoundException($"File {fileInfo.FullName} not found.");
					}

					options.HandleNonExistentItemTypeName?.Invoke(type, urlName);
					return null;
				}

				options.OnLoadExistingEdxItem?.Invoke(new FileInEdxCourse(type, urlName, "xml"));

				var component = customDeserialize == null
					? fileInfo.DeserializeXml<TComponent>()
					: customDeserialize();
				component.UrlName = urlName;
				loadInner?.Invoke(component);
				return component;
			}
			catch (Exception e)
			{
				throw new Exception($"{type} {urlName} load error", e);
			}
		}
	}
}