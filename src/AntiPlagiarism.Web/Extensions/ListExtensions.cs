using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiPlagiarism.Web.Extensions;

public static class ListExtensions
{
	public static double Mean(this List<double> values)
	{
		var count = values.Count;
		if (count == 0)
			return 0;
		return values.Sum() / count;
	}

	public static double Deviation(this List<double> values, double mean)
	{
		return Math.Sqrt(values.Variance(mean));
	}

	public static double Variance(this List<double> values, double mean)
	{
		var count = values.Count;
		if (count <= 1)
			return 0;
		return values.Sum(value => (value - mean) * (value - mean) / (count - 1));
	}

	public static double Deviation(this List<double> values)
	{
		return Deviation(values, values.Mean());
	}
}