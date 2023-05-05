using System;
using System.Collections.Generic;
using System.Linq;
using AntiPlagiarism.Web.CodeAnalyzing.Hashers;
using AntiPlagiarism.Web.Database.Models;

namespace AntiPlagiarism.Web.CodeAnalyzing
{
	public class SnippetsExtractorOptions
	{
		public ISequenceHasher SequenceHasher;

		public static SnippetsExtractorOptions Default => new()
		{
			SequenceHasher = new PolynomialSequenceHasher(137, new StableStringHasher())
		};
	}

	public class SnippetsExtractor
	{
		public SnippetsExtractor(SnippetsExtractorOptions options)
		{
			Options = options;
		}

		public SnippetsExtractor()
			: this(SnippetsExtractorOptions.Default)
		{
		}

		private SnippetsExtractorOptions Options { get; }

		public IEnumerable<Snippet> GetSnippets(IEnumerable<IToken> tokens, int snippetTokensCount, ITokenInSnippetConverter converter)
		{
			if (snippetTokensCount <= 0)
				throw new ArgumentException(@"Tokens count in snippet must be positive", nameof(snippetTokensCount));

			var tokensQueue = new Queue<string>();

			var hasher = Options.SequenceHasher;
			hasher.Reset();

			foreach (var token in tokens.Select(converter.Convert))
			{
				if (tokensQueue.Count == snippetTokensCount)
				{
					hasher.Dequeue();
					tokensQueue.Dequeue();
				}

				hasher.Enqueue(token);
				tokensQueue.Enqueue(token);

				if (tokensQueue.Count == snippetTokensCount)
					yield return new Snippet
					{
						SnippetType = converter.SnippetType,
						TokensCount = snippetTokensCount,
						Hash = hasher.GetCurrentHash()
					};
			}
		}
	}
}