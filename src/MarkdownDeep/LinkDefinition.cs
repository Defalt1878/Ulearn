﻿//   MarkdownDeep - http://www.toptensoftware.com/markdowndeep
//	 Copyright (C) 2010-2011 Topten Software
// 
//   Licensed under the Apache License, Version 2.0 (the "License"); you may not use this product except in 
//   compliance with the License. You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software distributed under the License is 
//   distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
//   See the License for the specific language governing permissions and limitations under the License.
//

using System.Text;

namespace MarkdownDeep;

public class LinkDefinition
{
	public LinkDefinition(string id)
	{
		this.id= id;
	}

	public LinkDefinition(string id, string url)
	{
		this.id = id;
		this.url = url;
	}

	public LinkDefinition(string id, string url, string title)
	{
		this.id = id;
		this.url = url;
		this.title = title;
	}

	public string id
	{
		get;
		set;
	}

	public string url
	{
		get;
		set;
	}

	public string title
	{
		get;
		set;
	}


	internal void RenderLink(Markdown m, StringBuilder b, string linkText)
	{
		if (url.StartsWith("mailto:"))
		{
			b.Append("<a href=\"");
			Utils.HtmlRandomize(b, url);
			b.Append('\"');
			if (!string.IsNullOrEmpty(title))
			{
				b.Append(" title=\"");
				Utils.SmartHtmlEncodeAmpsAndAngles(b, title);
				b.Append('\"');
			}
			b.Append('>');
			Utils.HtmlRandomize(b, linkText);
			b.Append("</a>");
		}
		else
		{
			var tag = new HtmlTag("a");

			// encode url
			var sb = m.GetStringBuilder();
			Utils.SmartHtmlEncodeAmpsAndAngles(sb, url);
			tag.Attributes["href"] = sb.ToString();

			// encode title
			if (!string.IsNullOrEmpty(title ))
			{
				sb.Length = 0;
				Utils.SmartHtmlEncodeAmpsAndAngles(sb, title);
				tag.Attributes["title"] = sb.ToString();
			}

			// Do user processing
			m.OnPrepareLink(tag);

			// Render the opening tag
			tag.RenderOpening(b);

			b.Append(linkText);	  // Link text already escaped by SpanFormatter
			b.Append("</a>");
		}
	}

	internal void RenderImg(Markdown m, StringBuilder b, string altText)
	{
		var tag = new HtmlTag("img");

		// encode url
		var sb = m.GetStringBuilder();
		Utils.SmartHtmlEncodeAmpsAndAngles(sb, url);
		tag.Attributes["src"] = sb.ToString();

		// encode alt text
		if (!string.IsNullOrEmpty(altText))
		{
			sb.Length = 0;
			Utils.SmartHtmlEncodeAmpsAndAngles(sb, altText);
			tag.Attributes["alt"] = sb.ToString();
		}

		// encode title
		if (!string.IsNullOrEmpty(title))
		{
			sb.Length = 0;
			Utils.SmartHtmlEncodeAmpsAndAngles(sb, title);
			tag.Attributes["title"] = sb.ToString();
		}

		tag.Closed = true;

		m.OnPrepareImage(tag, m.RenderingTitledImage);

		tag.RenderOpening(b);
	}


	// Parse a link definition from a string (used by test cases)
	public static LinkDefinition ParseLinkDefinition(string str, bool extraMode)
	{
		var p = new StringScanner(str);
		return ParseLinkDefinitionInternal(p, extraMode);
	}

	// Parse a link definition
	internal static LinkDefinition ParseLinkDefinition(StringScanner p, bool extraMode)
	{
		var savepos=p.Position;
		var l = ParseLinkDefinitionInternal(p, extraMode);
		if (l==null)
			p.Position = savepos;
		return l;

	}

	internal static LinkDefinition ParseLinkDefinitionInternal(StringScanner p, bool extraMode)
	{
		// Skip leading white space
		p.SkipWhitespace();

		// Must start with an opening square bracket
		if (!p.SkipChar('['))
			return null;

		// Extract the id
		p.Mark();
		if (!p.Find(']'))
			return null;
		var id = p.Extract();
		if (id.Length == 0)
			return null;
		if (!p.SkipString("]:"))
			return null;

		// Parse the url and title
		var link=ParseLinkTarget(p, id, extraMode);

		// and trailing whitespace
		p.SkipLineSpace();

		// Trailing crap, not a valid link reference...
		return !p.Eol ? null : link;
	}

	// Parse just the link target
	// For reference link definition, this is the bit after "[id]: thisbit"
	// For inline link, this is the bit in the parens: [link text](thisbit)
	internal static LinkDefinition ParseLinkTarget(StringScanner p, string id, bool extraMode)
	{
		// Skip whitespace
		p.SkipWhitespace();

		// End of string?
		if (p.Eol)
			return null;

		// Create the link definition
		var r = new LinkDefinition(id);

		// Is the url enclosed in angle brackets
		if (p.SkipChar('<'))
		{
			// Extract the url
			p.Mark();

			// Find end of the url
			while (p.Current != '>')
			{
				if (p.Eof)
					return null;
				p.SkipEscapableChar(extraMode);
			}

			var url = p.Extract();
			if (!p.SkipChar('>'))
				return null;

			// Unescape it
			r.url = Utils.UnescapeString(url.Trim(), extraMode);

			// Skip whitespace
			p.SkipWhitespace();
		}
		else
		{
			// Find end of the url
			p.Mark();
			var paren_depth = 1;
			while (!p.Eol)
			{
				var ch=p.Current;
				if (char.IsWhiteSpace(ch))
					break;
				if (id == null)
				{
					if (ch == '(')
						paren_depth++;
					else if (ch == ')')
					{
						paren_depth--;
						if (paren_depth==0)
							break;
					}
				}

				p.SkipEscapableChar(extraMode);
			}

			r.url = Utils.UnescapeString(p.Extract().Trim(), extraMode);
		}

		p.SkipLineSpace();

		// End of inline target
		if (p.DoesMatch(')'))
			return r;

		var bOnNewLine = p.Eol;
		var posLineEnd = p.Position;
		if (p.Eol)
		{
			p.SkipEol();
			p.SkipLineSpace();
		}

		// Work out what the title is delimited with
		char delim;
		switch (p.Current)
		{
			case '\'':  
			case '\"':
				delim = p.Current;
				break;

			case '(':
				delim = ')';
				break;

			default:
				if (bOnNewLine)
				{
					p.Position = posLineEnd;
					return r;
				}
				else
					return null;
		}

		// Skip the opening title delimiter
		p.SkipForward(1);

		// Find the end of the title
		p.Mark();
		while (true)
		{
			if (p.Eol)
				return null;

			if (p.Current == delim)
			{

				if (delim != ')')
				{
					var savePos = p.Position;

					// Check for embedded quotes in title

					// Skip the quote and any trailing whitespace
					p.SkipForward(1);
					p.SkipLineSpace();

					// Next we expect either the end of the line for a link definition
					// or the close bracket for an inline link
					if ((id == null && p.Current != ')') ||
					    (id != null && !p.Eol))
					{
						continue;
					}

					p.Position = savePos;
				}

				// End of title
				break;
			}

			p.SkipEscapableChar(extraMode);
		}

		// Store the title
		r.title = Utils.UnescapeString(p.Extract(), extraMode);

		// Skip closing quote
		p.SkipForward(1);

		// Done!
		return r;
	}
}