using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace Ulearn.Core
{
	public class XmlValidator
	{
		private readonly XmlReaderSettings settings;

		public XmlValidator(string schemaPath)
		{
			AppContext.SetSwitch("Switch.System.Xml.AllowDefaultResolver", true);

			XmlReaderSettings xmlReaderSettings = new()
			{
				CloseInput = true,
				ValidationType = ValidationType.Schema,
				ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
								XmlSchemaValidationFlags.ProcessIdentityConstraints |
								XmlSchemaValidationFlags.ProcessInlineSchema |
								XmlSchemaValidationFlags.ProcessSchemaLocation,
				XmlResolver = new XmlUrlResolver()
			};

			using (var r = XmlReader.Create(schemaPath))
			{
				xmlReaderSettings.Schemas.Add(XmlSchema.Read(r, null));
			}

			settings = xmlReaderSettings;
		}

		public string ValidateSlideFile(FileInfo file)
		{
			var log = new List<string>
			{
				file.Directory != null
					? $"Ошибки в слайде {file.Directory.Name}/{file.Name}:"
					: $"Ошибки в слайде {file.FullName}:"
			};

			void Action(object sender, ValidationEventArgs e)
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (e.Exception is not null)
					log.Add($"	[Line: {e.Exception.LineNumber}, Column: {e.Exception.LinePosition}]: {e.Message}");
			}

			settings.ValidationEventHandler += Action;
			using (var validatingReader = XmlReader.Create(file.FullName, settings))
			{
				var x = new XmlDocument();
				x.Load(validatingReader);

				while (validatingReader.Read())
				{
				}
			}

			return log.Count > 1 ? string.Join("\n", log) : null;
		}

		public string ValidateSlidesFiles(List<FileInfo> files)
		{
			return string.Join("\n", files.Select(ValidateSlideFile).Where(x => x != null));
		}
	}
}