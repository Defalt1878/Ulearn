using System;
using AntiPlagiarism.Web.CodeAnalyzing.Hashers;
using NUnit.Framework;

namespace AntiPlagiarism.Tests.CodeAnalyzing.Hashers
{
	[TestFixture]
	public class PolynomialSequenceHasher_should
	{
		[Test]
		public void WorkWithPolynomBase1()
		{
			var hasher = new PolynomialSequenceHasher(1);
			hasher.Enqueue("first");
			Assert.AreEqual("first".GetHashCode(), hasher.GetCurrentHash());
			hasher.Enqueue("second");
			Assert.AreEqual("first".GetHashCode() + "second".GetHashCode(), hasher.GetCurrentHash());
			hasher.Dequeue();
			Assert.AreEqual("second".GetHashCode(), hasher.GetCurrentHash());
		}

		[TestCase(2)]
		[TestCase(5)]
		[TestCase(137)]
		public void WorkWithPolynomBase(int polynomBase)
		{
			var hasher = new PolynomialSequenceHasher(polynomBase);
			hasher.Enqueue("first");
			Assert.AreEqual("first".GetHashCode(), hasher.GetCurrentHash());
			hasher.Enqueue("second");
			Assert.AreEqual(unchecked(polynomBase * "first".GetHashCode() + "second".GetHashCode()), hasher.GetCurrentHash());
			hasher.Dequeue();
			Assert.AreEqual("second".GetHashCode(), hasher.GetCurrentHash());
		}

		[Test]
		public void DequeueElements()
		{
			const int count = 100;
			var hasher = new PolynomialSequenceHasher(137);
			for (var i = 0; i < count; i++)
				hasher.Enqueue("some_string");
			for (var i = 0; i < count - 1; i++)
				hasher.Dequeue();
			Assert.AreEqual("some_string".GetHashCode(), hasher.GetCurrentHash());
		}

		[TestCase(1000, 2)]
		[TestCase(2000, 5)]
		[TestCase(3000, 137)]
		public void WorkWithManyAdds(int count, int polynomBase)
		{
			var hasher = new PolynomialSequenceHasher(polynomBase);
			var currentHash = 0;
			for (var i = 0; i < count; i++)
			{
				hasher.Enqueue("some_string");
				currentHash = unchecked(currentHash * polynomBase + "some_string".GetHashCode());
				Assert.AreEqual(currentHash, hasher.GetCurrentHash());
			}
		}

		[TestCase(100)]
		public void Reset(int count)
		{
			var hasher = new PolynomialSequenceHasher(137);
			for (var i = 0; i < count; i++)
			{
				hasher.Enqueue("some_string");
				Assert.AreEqual("some_string".GetHashCode(), hasher.GetCurrentHash());
				hasher.Reset();
			}
		}

		[Test]
		public void NotDequeFromEmptyQueue()
		{
			var hasher = new PolynomialSequenceHasher(137);
			hasher.Enqueue("some_string");
			hasher.Dequeue();
			Assert.Throws<InvalidOperationException>(() => hasher.Dequeue());
		}

		[Test]
		public void TestIntegerOverflow()
		{
			const int polynomBase = 137;
			var hash = 1;
			for (var i = 0; i < 100; i++)
				hash = unchecked(hash * polynomBase);
			Assert.AreEqual(656577185, hash);
		}
	}
}