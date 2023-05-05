using System.Collections.Generic;
using AntiPlagiarism.Web.Extensions;
using NUnit.Framework;

namespace AntiPlagiarism.Tests.Extensions;

[TestFixture]
public class ListExtensions_should
{
	[Test]
	public void CalculateMeanOnEmptyList()
	{
		var emptyList = new List<double>();
		Assert.AreEqual(0.0, emptyList.Mean());
	}

	[Test]
	public void CalculateIntegerMean()
	{
		var data = new List<double> { 1.0, 2.0, 3.0 };
		Assert.AreEqual(2.0, data.Mean());
	}

	[Test]
	public void CalculaterDoubleMean()
	{
		var data = new List<double> { 1.5, 2.5, 4.5 };
		Assert.AreEqual(8.5 / 3, data.Mean(), 1e-6);
	}

	[TestCase(1000)]
	[TestCase(12345)]
	public void CalculateMeanOfLargeList(int count)
	{
		var data = new List<double>();
		const double factor = 15.6;
		for (var i = 1; i <= count; i++)
			data.Add(i * factor);
		Assert.AreEqual((count + 1.0) / 2 * factor, data.Mean(), 1e-6);
	}

	[Test]
	public void CalculaterDeviationOfIntegers()
	{
		var data = new List<double> { 489, 490, 490, 491, 494, 499, 499, 500, 501, 505 };
		Assert.AreEqual(5.672545, data.Deviation(), 1e-6);
	}
}