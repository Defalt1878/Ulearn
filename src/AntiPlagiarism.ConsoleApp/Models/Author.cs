using System;

namespace AntiPlagiarism.ConsoleApp.Models
{
	public class Author
	{
		public readonly string Name;
		public Guid Id;

		public Author(string name)
		{
			Name = name;
			Id = Guid.NewGuid();
		}
	}
}