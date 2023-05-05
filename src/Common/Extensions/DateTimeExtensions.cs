using System;
using System.Globalization;

namespace Ulearn.Common.Extensions;

public static class DateTimeExtensions
{
	public static string ToSortable(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-ddTHH.mm.ss.ff");
	}

	public static DateTime FromSortable(string str)
	{
		return DateTime.ParseExact(str, "yyyy-MM-ddTHH.mm.ss.ff", CultureInfo.InvariantCulture);
	}

	public static string ToSortableDate(this DateTime dateTime)
	{
		return dateTime.ToString("yyyy-MM-dd");
	}
}