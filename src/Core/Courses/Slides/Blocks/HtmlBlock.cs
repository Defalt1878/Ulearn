using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Courses.Slides.Blocks
{
	//[XmlType("html")]
	public class HtmlBlock : SlideBlock, IXmlSerializable, IConvertibleToEdx
	{
		public const string BaseUrlApiPlaceholder = "%BaseUrlApiPlaceholder%";

		public HtmlBlock()
		{
		}

		public HtmlBlock(string content)
		{
			Content = content;
		}

		private string Content { get; set; } = "";

		public Component ToEdxComponent(EdxComponentBuilderContext context)
		{
			var urlName = context.Slide.NormalizedGuid + context.ComponentIndex;
			return new HtmlComponent(urlName, context.DisplayName, urlName, GetContent(context.UlearnBaseUrlApi));
		}

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			reader.MoveToContent();

			if (reader.IsEmptyElement)
			{
				reader.Read();
				return;
			}

			var name = reader.Name;
			var hide = reader.GetAttribute("hide") == "true";
			var innerXml = reader.ReadInnerXml();
			if (name == "html")
				Hide = hide;
			Content = RemoveXmlNamespacesAndAutoExpandEmptyTags(innerXml.RemoveCommonNesting());
		}

		public void WriteXml(XmlWriter writer)
		{
			if (Hide)
				writer.WriteAttributeString("hide", "true");
			writer.WriteRaw(Content);
		}

		public string GetContent(string ulearnBaseUrlApi)
		{
			return Content.Replace(BaseUrlApiPlaceholder, ulearnBaseUrlApi);
		}

		public override string ToString()
		{
			return $"Html {Content[..50]}";
		}

		/* Андрей Гейн: We need to remove xml namespaces (which are inherited from ulearn's xml) and
			to expand empty tags (i.e. replace auto-collapsed <iframe ... /> to <iframe ...></iframe>) */
		/* Антон Федоров: Не понял, чем мешают теги вида <tag/>. Браузер понимает тег <br></br> как два. */
		private string RemoveXmlNamespacesAndAutoExpandEmptyTags(string innerXmlContent)
		{
			/* Add outer tag, otherwise XML is not correct and XmlUtils can't parse it */
			var xml = $"<node>{innerXmlContent}</node>";

			var resultXml = XmlUtils.RemoveAllNamespaces(xml);
			//resultXml = XmlUtils.ExpandEmptyTags(resultXml);

			/* Delete outer tag */
			if (resultXml.StartsWith("<node>"))
				resultXml = resultXml.Remove(0, "<node>".Length);
			if (resultXml.EndsWith("</node>"))
				resultXml = resultXml.Remove(resultXml.Length - "</node>".Length);

			return resultXml;
		}
	}
}