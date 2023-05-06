using System;
using System.Collections.Generic;
using Ulearn.Common.Extensions;

namespace Ulearn.Core
{
	public static class RegionsParser
	{
		public static Dictionary<string, Region> GetRegions(string code)
		{
			var regions = new Dictionary<string, Region>();
			var opened = new Dictionary<string, Tuple<int, int>>();
			var current = 0;

			foreach (var line in code.SplitToLinesWithEoln())
			{
				if (line.Contains("endregion"))
				{
					var name = GetRegionName(line);
					if (opened.ContainsKey(name))
					{
						var start = opened[name];
						regions[name] = new Region(start.Item1, current - start.Item1, start.Item2, current - start.Item2 + line.Length);
						opened.Remove(name);
					}

					current += line.Length;
					continue;
				}

				current += line.Length;

				if (line.Contains("region"))
				{
					var name = GetRegionName(line);
					opened[name] = Tuple.Create(current, current - line.Length);
				}
			}

			return regions;
		}

		private static string GetRegionName(string line)
		{
			var regionIndex = line.LastIndexOf("region ", StringComparison.Ordinal);
			if (regionIndex == -1)
				return "";
			return line[(regionIndex + "region ".Length)..].Trim();
		}

		public class Region : IEquatable<Region>
		{
			public readonly int dataStart;
			public readonly int dataLength;
			public readonly int fullStart;
			public readonly int fullLength;

			public Region(int dataStart, int dataLength, int fullStart, int fullLength)
			{
				this.dataStart = dataStart;
				this.dataLength = dataLength;
				this.fullStart = fullStart;
				this.fullLength = fullLength;
			}

			#region EqualsAndToString

			public bool Equals(Region other)
			{
				if (ReferenceEquals(null, other))
					return false;
				if (ReferenceEquals(this, other))
					return true;
				return dataStart == other.dataStart && dataLength == other.dataLength && fullStart == other.fullStart && fullLength == other.fullLength;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				if (ReferenceEquals(this, obj))
					return true;
				if (obj.GetType() != GetType())
					return false;
				return Equals((Region)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = dataStart;
					hashCode = (hashCode * 397) ^ dataLength;
					hashCode = (hashCode * 397) ^ fullStart;
					hashCode = (hashCode * 397) ^ fullLength;
					return hashCode;
				}
			}

			public override string ToString()
			{
				return $"data: ({dataStart}, {dataLength}); full: ({fullStart}, {fullLength})";
			}

			#endregion
		}
	}
}