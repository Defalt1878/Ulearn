using System;
using System.Collections.Generic;
using System.Linq;

namespace AntiPlagiarism.Web.CodeAnalyzing
{
	public class CodePath
	{
		public List<CodePathPart> Parts { get; }

		public CodePath(IEnumerable<CodePathPart> parts)
		{
			Parts = parts.ToList();
		}

		public override string ToString()
		{
			return string.Join(".", Parts.Select(p => p.ToString()));
		}
	}

	public class CodePathPart
	{
		public string Name { get; }
		public Type ContainingType { get; private set; }

		public CodePathPart(object obj, string name)
		{
			Name = name;
			ContainingType = obj.GetType();
		}

		public override string ToString()
		{
			return Name;
		}
	}
}