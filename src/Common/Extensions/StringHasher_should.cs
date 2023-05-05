using NUnit.Framework;

namespace Ulearn.Common.Extensions;

[TestFixture]
public class StringHasher_should
{
	[TestCase("hello", -327419862)]
	[TestCase("", 371857150)]
	[TestCase("DiFfErEnT chars", 675565598)]
	public void TestString(string s, int expectedHash)
	{
		Assert.AreEqual(s.GetStableHashCode(), expectedHash);
	}
}