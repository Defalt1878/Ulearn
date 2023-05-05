using System;
using System.Collections.Generic;

namespace AntiPlagiarism.Web.CodeAnalyzing.Hashers
{
	public class PolynomialSequenceHasher : ISequenceHasher
	{
		private readonly IObjectHasher hasher;
		private readonly int polynomBase;

		private int currentHash;
		private readonly Queue<int> objectsHashes = new();
		private int[] basePowers;

		public PolynomialSequenceHasher(int polynomBase, IObjectHasher hasher = null, int defaultCapacity = 10000)
		{
			this.polynomBase = polynomBase;
			this.hasher = hasher ?? new DefaultObjectHasher();
			InitBasePowers(defaultCapacity);
			Reset();
		}

		public void Enqueue(object obj)
		{
			var objectHashCode = hasher.GetHashCode(obj);
			currentHash = unchecked(currentHash * polynomBase + objectHashCode);
			objectsHashes.Enqueue(objectHashCode);
		}

		public void Dequeue()
		{
			if (objectsHashes.Count == 0)
				throw new InvalidOperationException("Can't dequeue from empty sequence");
			var valueToSubtract = GetBasePower(objectsHashes.Count - 1);
			currentHash = unchecked(currentHash - valueToSubtract * objectsHashes.Dequeue());
		}

		public int GetCurrentHash()
		{
			return currentHash;
		}

		public void Reset()
		{
			currentHash = 0;
			objectsHashes.Clear();
		}

		private void InitBasePowers(int capacity)
		{
			basePowers = new int[capacity];
			basePowers[0] = 1;
			for (var i = 1; i < capacity; i++)
				basePowers[i] = unchecked(basePowers[i - 1] * polynomBase);
		}

		private int GetBasePower(int power)
		{
			var result = 1;
			while (power >= basePowers.Length)
			{
				result = unchecked(result * basePowers[^1]);
				power -= basePowers.Length - 1;
			}

			return unchecked(result * basePowers[power]);
		}
	}
}