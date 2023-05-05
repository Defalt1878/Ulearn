using AntiPlagiarism.Web.CodeAnalyzing;
using NUnit.Framework;

namespace AntiPlagiarism.Tests.CodeAnalyzing
{
	[TestFixture]
	public class SuspicionLevels_should
	{
		[Test]
		[TestCase(0.121332, 0.095524,
			3.7, 4.65,
			0.6484, 0.7757)]
		[TestCase(0.388527478644123, 0.525986874590113, // Даст отрицательные параметры бета-распределения, так что будет использоваться нормальное
			3.7, 4.65,
			2.33467, 2.8343)]
		public void SuspicionLevelsTest(double mean, double sigma,
			double faintSuspicionCoefficient, double strongSuspicionCoefficient,
			double faintSuspicionExpected, double strongSuspicionExpected)
		{
			var (faintSuspicion, strongSuspicion)
				= StatisticsParametersFinder.GetSuspicionLevels(mean, sigma, faintSuspicionCoefficient, strongSuspicionCoefficient);
			Assert.AreEqual(faintSuspicionExpected, faintSuspicion, 1e-4);
			Assert.AreEqual(strongSuspicionExpected, strongSuspicion, 1e-4);
		}
	}
}