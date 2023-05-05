using NUnit.Framework;
using Ulearn.Core.Logging;

namespace AntiPlagiarism.Tests;

[SetUpFixture]
public class TestsSetupClass
{
	[OneTimeSetUp]
	public void GlobalSetup()
	{
		LoggerSetup.SetupForTests();
	}
}