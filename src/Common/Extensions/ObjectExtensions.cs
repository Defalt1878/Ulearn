using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
using WriteState = System.Xml.WriteState;

namespace Ulearn.Common.Extensions;

public static class ObjectExtensions
{
	private static readonly XmlWriterSettings defaultSettings = new() { OmitXmlDeclaration = true, Indent = true };
	private static readonly XmlWriterSettings withoutSpacesSettings = new() { OmitXmlDeclaration = true, Indent = false, NewLineHandling = NewLineHandling.None };
	private static readonly XmlSerializerNamespaces ns = new(new[] { new XmlQualifiedName("", "") });

	public static bool IsOneOf<T>(this T o, params T[] validValues)
	{
		return validValues.Contains(o);
	}

	[NotNull]
	public static T EnsureNotNull<T>([CanBeNull] this T o, string exceptionMessageIfNull = "can't be null")
	{
		if (o is null)
			throw new ArgumentException(exceptionMessageIfNull);
		return o;
	}

	public static string XmlSerialize(this object o, bool removeWhitespaces = false, bool expandEmptyTags = false)
	{
		var settings = removeWhitespaces ? withoutSpacesSettings : defaultSettings;

		using var ms = StaticRecyclableMemoryStreamManager.Manager.GetStream();
		using var innerWriter = XmlWriter.Create(ms, settings);
		using var writer = expandEmptyTags ? new XmlTextWriterPreventAutoClosingEmptyTags(innerWriter) : innerWriter;
		var s = new XmlSerializer(o.GetType());
		s.Serialize(writer, o, ns);
		ms.Flush();
		ms.Seek(0, SeekOrigin.Begin);
		var sr = new StreamReader(ms);
		return sr.ReadToEnd();
	}

	public static string JsonSerialize(this object o, Formatting formatting = Formatting.None)
	{
		return JsonConvert.SerializeObject(o, formatting, new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Include,
			DefaultValueHandling = DefaultValueHandling.Include
		});
	}
}

/* This Xml Writer don't collapse empty tags, because self-closed tags are not valid for HTML.
	Standard Xml Writer collapse empty tags: <iframe .../>, this one makes it empty: <iframe ...></iframe>*/
public class XmlTextWriterPreventAutoClosingEmptyTags : XmlWriterDecorator
{
	public XmlTextWriterPreventAutoClosingEmptyTags(XmlWriter baseWriter)
		: base(baseWriter)
	{
	}

	public override void WriteEndElement()
	{
		base.WriteFullEndElement();
	}
}

public class XmlWriterDecorator : XmlWriter
{
	private readonly XmlWriter baseWriter;

	public XmlWriterDecorator(XmlWriter baseWriter)
	{
		this.baseWriter = baseWriter ?? throw new ArgumentNullException(nameof(baseWriter));
	}

	protected virtual bool IsSuspended => false;

	public override WriteState WriteState => baseWriter.WriteState;

	public override void Close()
	{
		baseWriter.Close();
	}

	public override void Flush()
	{
		baseWriter.Flush();
	}

	public override string LookupPrefix(string ns)
	{
		return baseWriter.LookupPrefix(ns);
	}

	public override void WriteBase64(byte[] buffer, int index, int count)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteBase64(buffer, index, count);
	}

	public override void WriteCData(string text)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteCData(text);
	}

	public override void WriteCharEntity(char ch)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteCharEntity(ch);
	}

	public override void WriteChars(char[] buffer, int index, int count)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteChars(buffer, index, count);
	}

	public override void WriteComment(string text)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteComment(text);
	}

	public override void WriteDocType(string name, string pubid, string sysid, string subset)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteDocType(name, pubid, sysid, subset);
	}

	public override void WriteEndAttribute()
	{
		if (IsSuspended)
			return;
		baseWriter.WriteEndAttribute();
	}

	public override void WriteEndDocument()
	{
		if (IsSuspended)
			return;
		baseWriter.WriteEndDocument();
	}

	public override void WriteEndElement()
	{
		if (IsSuspended)
			return;
		baseWriter.WriteEndElement();
	}

	public override void WriteEntityRef(string name)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteEntityRef(name);
	}

	public override void WriteFullEndElement()
	{
		if (IsSuspended)
			return;
		baseWriter.WriteFullEndElement();
	}

	public override void WriteProcessingInstruction(string name, string text)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteProcessingInstruction(name, text);
	}

	public override void WriteRaw(string data)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteRaw(data);
	}

	public override void WriteRaw(char[] buffer, int index, int count)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteRaw(buffer, index, count);
	}

	public override void WriteStartAttribute(string prefix, string localName, string ns)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteStartAttribute(prefix, localName, ns);
	}

	public override void WriteStartDocument(bool standalone)
	{
		baseWriter.WriteStartDocument(standalone);
	}

	public override void WriteStartDocument()
	{
		baseWriter.WriteStartDocument();
	}

	public override void WriteStartElement(string prefix, string localName, string ns)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteStartElement(prefix, localName, ns);
	}

	public override void WriteString(string text)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteString(text);
	}

	public override void WriteSurrogateCharEntity(char lowChar, char highChar)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteSurrogateCharEntity(lowChar, highChar);
	}

	public override void WriteWhitespace(string ws)
	{
		if (IsSuspended)
			return;
		baseWriter.WriteWhitespace(ws);
	}
}