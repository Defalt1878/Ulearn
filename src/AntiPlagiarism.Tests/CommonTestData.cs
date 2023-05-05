namespace AntiPlagiarism.Tests;

public static class CommonTestData
{
	public const string SimpleProgramWithMethodAndProperty = @"using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace HelloWorld.Namespace
{
	/* 
	Комментарий
	*/
	class Program
	{
		static void Main(){ Console.WriteLine(@""
A"");}

		static int A { get { return 2; } set { Console.WriteLine(value); }}
	}
}";
}