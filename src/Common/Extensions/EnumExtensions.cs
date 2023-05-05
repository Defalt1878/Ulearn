using System;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace Ulearn.Common.Extensions
{
	public static class EnumExtensions
	{
		/// <summary>
		///     Gets an attribute on an enum field value
		/// </summary>
		public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
		{
			var type = value.GetType();
			var memberInfo = type.GetMember(value.ToString())[0];
			var attributes = memberInfo.GetCustomAttributes(typeof(TAttribute), false);
			return attributes.Length > 0 ? (TAttribute)attributes[0] : null;
		}

		public static bool TryParseToNullableEnum<TEnum>(this string value, out TEnum? result) where TEnum : struct
		{
			if (Enum.TryParse(value, out TEnum localResult))
			{
				result = localResult;
				return true;
			}

			result = null;
			return false;
		}

		/* Mark your enum value with [Display(Name = "any text")] and retrieve Name via this method */
		public static string GetDisplayName(this Enum type)
		{
			return type.GetAttribute<DisplayAttribute>().GetName();
		}

		/* Mark you enum value with [XmlEnum(Name = "any text")] and retrieve Name via this method */
		public static string GetXmlEnumName(this Enum type)
		{
			return type.GetAttribute<XmlEnumAttribute>().Name;
		}
	}
}