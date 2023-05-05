using System;
using System.IO;
using System.Xml.Serialization;

namespace Ulearn.Common.Extensions
{
	public static class XmlExtensions
	{
		public static T DeserializeXml<T>(this FileInfo file)
		{
			var serializer = new XmlSerializer(typeof(T));
			using var stream = file.OpenRead();
			return (T)serializer.Deserialize(stream);
		}

		public static object DeserializeXml(this FileInfo file, Type type)
		{
			var serializer = new XmlSerializer(type);
			using var stream = file.OpenRead();
			return serializer.Deserialize(stream);
		}

		public static T DeserializeXml<T>(this string content)
		{
			var serializer = new XmlSerializer(typeof(T));
			using var stream = new StringReader(content);
			return (T)serializer.Deserialize(stream);
		}

		public static object DeserializeXml(this string content, Type type)
		{
			var serializer = new XmlSerializer(type);
			using var stream = new StringReader(content);
			return serializer.Deserialize(stream);
		}

		public static T DeserializeXml<T>(this byte[] content)
		{
			var serializer = new XmlSerializer(typeof(T));
			using var stream = StaticRecyclableMemoryStreamManager.Manager.GetStream(content);
			return (T)serializer.Deserialize(stream);
		}

		public static object DeserializeXml(this byte[] content, Type type)
		{
			var serializer = new XmlSerializer(type);
			using var stream = StaticRecyclableMemoryStreamManager.Manager.GetStream(content);
			return serializer.Deserialize(stream);
		}
	}
}