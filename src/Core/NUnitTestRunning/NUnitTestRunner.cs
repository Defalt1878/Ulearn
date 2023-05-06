// ReSharper disable All
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text.RegularExpressions;
using NUnit.Framework.Api;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

[assembly: InternalsVisibleTo("NUnit.Framework")]

namespace Ulearn.Core.NUnitTestRunning
{
	/// <summary>
	/// Этот файл включен в ресурсы и добавляется в пришедший на проверку проект с задачей, если у нее в качестве способа проверки
	/// установлен nunitTestClass, а не startupObject
	/// </summary>
	public class NUnitTestRunner
	{
		public static void WillBeMain()
		{
			string[] testClassesToLaunch = { "SHOULD_BE_REPLACED" };
			var listener = new TestListener();
			var assembly = Assembly.GetExecutingAssembly();
			ReportOnNonexistentTestClasses(assembly, testClassesToLaunch);
			RunAllTests(listener, assembly, testClassesToLaunch);
		}

		public static void RunAllTests(ITestListener testListener, Assembly executingAssembly, params string[] testClassesToLaunch)
		{
			var runner = new NUnitTestAssemblyRunner(new DefaultTestAssemblyBuilder());
			runner.Load(executingAssembly, new Dictionary<string, object> { { "StopOnError", true } });
			foreach (var testClass in testClassesToLaunch)
			{
				RunTestBatch(testClass, runner, testListener);
				if (runner.Result.ResultState.Status == TestStatus.Failed)
					break;
			}
		}

		private static void RunTestBatch(string testClass, NUnitTestAssemblyRunner runner, ITestListener listener)
		{
			var testFilter = new ClassNameFilter(testClass);
			runner.Run(listener, testFilter);
		}

		public static void ReportOnNonexistentTestClasses(Assembly assembly, params string[] testClassesToLaunch)
		{
			foreach (var testClass in GetNonexistentTestClasses(assembly, testClassesToLaunch))
			{
				var errorMessage = $"Error in checking system: test class {testClass} does not exist.";
				throw new ArgumentException(errorMessage);
			}
		}

		private static IEnumerable<string> GetNonexistentTestClasses(Assembly executingAssembly, params string[] testClasses)
		{
			var classes = executingAssembly.GetTypes().Select(x => x.FullName).ToList();
			var intersection = classes.Intersect(testClasses);
			return testClasses.Except(intersection);
		}

		// должен остаться в этом же файле
		public class TestListener : ITestListener
		{
			public IReadOnlyCollection<ITestResult> Results => results;

			private readonly List<ITestResult> results = new List<ITestResult>();

			public void TestStarted(ITest test)
			{
			}

			public void TestFinished(ITestResult result)
			{
				results.Add(result);

				if (result.ResultState.Status == TestStatus.Failed && result.ResultState.Site != FailureSite.Child
																	&& result.ResultState.Label != "Cancelled")
				{
					Console.WriteLine($"Как минимум один из тестов не пройден!\nНазвание теста: {result.Name}\nСообщение:\n{result.Message}\nСтек вызовов:\n{result.StackTrace}");
				}
			}

			public void TestOutput(TestOutput output)
			{
			}
		}
	}

	/* Based on internal classes ClassNameFilter and ValueMatchFilter from NUnit */
	[Serializable]
	internal class ClassNameFilter : TestFilter
	{
		/// <summary>
		/// Returns the value matched by the filter - used for testing
		/// </summary>
		private string ExpectedValue { get; }

		/// <summary>Indicates whether the value is a regular expression</summary>
		public bool IsRegex { get; set; }

		/// <summary>Construct a ValueMatchFilter for a single value.</summary>
		/// <param name="expectedValue">The value to be included.</param>
		public ClassNameFilter(string expectedValue)
		{
			ExpectedValue = expectedValue;
		}

		/// <inheritdoc />
		/// <summary>Adds an XML node</summary>
		/// <param name="parentNode">Parent node</param>
		/// <param name="recursive">True if recursive</param>
		/// <returns>The added XML node</returns>
		public override TNode AddToXml(TNode parentNode, bool recursive)
		{
			var tnode = parentNode.AddElement(ElementName, ExpectedValue);
			if (IsRegex)
				tnode.AddAttribute("re", "1");
			return tnode;
		}

		/// <summary>Match the input provided by the derived class</summary>
		/// <param name="input">The value to be matched</param>
		/// <returns>True for a match, false otherwise.</returns>
		private bool Match(string input)
		{
			if (!IsRegex)
				return ExpectedValue == input;
			if (input != null)
				return new Regex(ExpectedValue).IsMatch(input);
			return false;
		}

		/// <inheritdoc />
		/// <summary>Match a test against a single value.</summary>
		public override bool Match(ITest test)
		{
			if (!test.IsSuite || test is ParameterizedMethodSuite || test.ClassName == null)
				return false;
			return Match(test.ClassName);
		}

		private string ElementName => "class";
	}
}