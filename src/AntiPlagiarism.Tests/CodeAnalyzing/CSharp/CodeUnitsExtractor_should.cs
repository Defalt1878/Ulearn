using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AntiPlagiarism.Web.CodeAnalyzing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Ulearn.Common.Extensions;

namespace AntiPlagiarism.Tests.CodeAnalyzing.CSharp;

[TestFixture]
public class CodeUnitsExtractor_should
{
	private CSharpCodeUnitsExtractor extractor;

	private static DirectoryInfo TestDataDir => new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CodeAnalyzing", "CSharp", "TestData"));

	[SetUp]
	public void SetUp()
	{
		extractor = new CSharpCodeUnitsExtractor();
	}

	[Test]
	public void ExtractCodeUnits()
	{
		var codeUnits = extractor.Extract(CommonTestData.SimpleProgramWithMethodAndProperty);

		Assert.AreEqual(3, codeUnits.Count);
		CollectionAssert.AreEqual(new List<int> { 31, 45, 51 }, codeUnits.Select(u => u.FirstTokenIndex));

		var methodBodyUnit = codeUnits[0];
		var methodBodyExpectedKinds = new List<SyntaxKind>
		{
			SyntaxKind.OpenBraceToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.DotToken,
			SyntaxKind.IdentifierToken,
			SyntaxKind.OpenParenToken,
			SyntaxKind.StringLiteralToken,
			SyntaxKind.CloseParenToken,
			SyntaxKind.SemicolonToken,
			SyntaxKind.CloseBraceToken
		}.Select(k => k.ToString()).ToList();
		Assert.AreEqual(methodBodyExpectedKinds.Count, methodBodyUnit.Tokens.Count);
		CollectionAssert.AreEqual(methodBodyExpectedKinds, methodBodyUnit.Tokens.Select(t => t.Type));
	}

	[Test]
	public void GiveCorrectNodeNames()
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(CommonTestData.SimpleProgramWithMethodAndProperty);
		var syntaxTreeRoot = syntaxTree.GetRoot();

		Assert.AreEqual("ROOT", CSharpCodeUnitsExtractor.GetNodeName(syntaxTreeRoot));

		var namespaceDeclaration = syntaxTreeRoot.ChildNodes().First(n => n.IsKind(SyntaxKind.NamespaceDeclaration));
		Assert.AreEqual("HelloWorld.Namespace", CSharpCodeUnitsExtractor.GetNodeName(namespaceDeclaration));

		var classDeclaration = namespaceDeclaration.ChildNodes().First(n => n.IsKind(SyntaxKind.ClassDeclaration));
		Assert.AreEqual("Program", CSharpCodeUnitsExtractor.GetNodeName(classDeclaration));

		var methodDeclaration = classDeclaration.ChildNodes().First(n => n.IsKind(SyntaxKind.MethodDeclaration));
		Assert.AreEqual("Main", CSharpCodeUnitsExtractor.GetNodeName(methodDeclaration));

		var propertyDeclaration = classDeclaration.ChildNodes().First(n => n.IsKind(SyntaxKind.PropertyDeclaration));
		Assert.AreEqual("A", CSharpCodeUnitsExtractor.GetNodeName(propertyDeclaration));
	}

	[Test]
	public void ExtractInnerClass()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("NestedClasses.cs");

		/* At least one code unit is inside InnerClass */
		Assert.IsTrue(codeUnits.Any(u => u.Path.Parts.Any(p => p.Name == "InnerClass")));
	}

	[Test]
	public void ExtractConstructors()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("Constructors.cs");

		Assert.AreEqual(5, codeUnits.Count);

		/* One code unit should be ThisConstructorInitializer */
		Assert.AreEqual(1, codeUnits.Count(u => u.Path.Parts.Any(p => p.Name == "ThisConstructorInitializer")));
	}

	[Test]
	public void ExtractOperators()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("Operators.cs");

		Assert.AreEqual(4, codeUnits.Count(u => u.Path.Parts.Any(p => p.Name.StartsWith("Operator"))));
		Assert.AreEqual(3, codeUnits.Count(u => u.Path.Parts.Any(p => p.Name.StartsWith("Conversion-"))));
	}

	[Test]
	public void ExtractInterfaces()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("Interface.cs");

		Assert.AreEqual(1, codeUnits.Count);
		CollectionAssert.AreEqual(
			new[] { 22 },
			codeUnits.Select(u => u.FirstTokenIndex)
		);
		Assert.AreEqual("Interface.DefaultMethod.Block", string.Join(".", codeUnits[0].Path.Parts.Skip(2)));
	}

	[Test]
	public void ExtractStruct()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("Struct.cs");

		Assert.AreEqual(2, codeUnits.Count);
		CollectionAssert.AreEqual(
			new[] { 23, 30 },
			codeUnits.Select(u => u.FirstTokenIndex)
		);
		CollectionAssert.AreEqual(
			new[] { "Struct.Method1.Block", "Struct.Method2.Block" },
			codeUnits.Select(u => string.Join(".", u.Path.Parts.Skip(2)))
		);
	}

	[Test]
	public void ExtractRecord()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("Record.cs");

		Assert.AreEqual(2, codeUnits.Count);
		CollectionAssert.AreEqual(
			new[] { 30, 37 },
			codeUnits.Select(u => u.FirstTokenIndex)
		);
		CollectionAssert.AreEqual(
			new[] { "Record.Method1.Block", "Record.Method2.Block" },
			codeUnits.Select(u => string.Join(".", u.Path.Parts.Skip(2)))
		);
	}

	[Test]
	public void ExtractRecordStruct()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("RecordStruct.cs");

		Assert.AreEqual(2, codeUnits.Count);
		CollectionAssert.AreEqual(
			new[] { 31, 38 },
			codeUnits.Select(u => u.FirstTokenIndex)
		);
		CollectionAssert.AreEqual(
			new[] { "RecordStruct.Method1.Block", "RecordStruct.Method2.Block" },
			codeUnits.Select(u => string.Join(".", u.Path.Parts.Skip(2)))
		);
	}

	[Test]
	public void EmptyRecord()
	{
		var codeUnits = ExtractCodeUnitsFromTestFile("EmptyRecord.cs");

		Assert.AreEqual(0, codeUnits.Count);
	}

	private List<CodeUnit> ExtractCodeUnitsFromTestFile(string filename)
	{
		var testFile = TestDataDir.GetFile(filename);
		var testContent = File.ReadAllText(testFile.FullName);
		return extractor.Extract(testContent);
	}
}