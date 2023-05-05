using NUnit.Framework;

namespace Ulearn.Common.Extensions;

[TestFixture]
public class StringExtensions_should
{
	[Test]
	public void RenderSimpleMarkdownWithInlineCode_Simple_Test()
	{
		const string text = "a `b c` d";
		var rendered = text.RenderSimpleMarkdown();
		Assert.AreEqual("a <span class='inline-pre'>b c</span> d", rendered);
	}

	[Test]
	public void RenderSimpleMarkdownWithInlineCode_LineBreak_Test()
	{
		const string text = "a `b\n c` d";
		var rendered = text.RenderSimpleMarkdown();
		Assert.AreEqual(text, rendered);
	}

	[Test]
	public void RenderSimpleMarkdownWithInlineCode_QuotesInEmptyLines_Test()
	{
		const string text = "`\nb c\n`\n";
		var rendered = text.RenderSimpleMarkdown();
		Assert.AreEqual(text, rendered);
	}

	[Test]
	public void RenderSimpleMarkdownWithInlineCode_Br_Test()
	{
		const string text = "a `b<br/> c` d";
		var rendered = text.RenderSimpleMarkdown();
		Assert.AreEqual("a <span class='inline-pre'>b<br/> c</span> d", rendered);
	}

	[Test]
	public void RenderSimpleMarkdownWithInlineCode_WithoutSpaces_Test()
	{
		const string text = "a`b`c";
		var rendered = text.RenderSimpleMarkdown();
		Assert.AreEqual("a<span class='inline-pre'>b</span>c", rendered);
	}

	[Test]
	public void TestRenderSimpleMarkdownInTelegramMode()
	{
		const string text = @"Выделен весь цикл `for`. Стоило бы написать:

			```
			for (int i = 0; i < 10; i++)
			{
				inputInt[i] = double.Parse(userStr[i]);
			}
			```
			";
		var rendered = text.RenderSimpleMarkdown(false, true);
		Assert.True(rendered.IndexOf("<pre>") >= 0);
		Assert.True(rendered.IndexOf("</pre>") >= 0);
		Assert.True(rendered.IndexOf("<code>") >= 0);
		Assert.True(rendered.IndexOf("</code>") >= 0);
	}
}