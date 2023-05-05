using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Ulearn.Common;

namespace AntiPlagiarism.Api.Models.Results
{
	[DataContract]
	public class Plagiarism
	{
		[DataMember(Name = "submission")]
		public SubmissionInfo SubmissionInfo { get; set; }

		[DataMember(Name = "weight")]
		public double Weight { get; set; }

		[DataMember(Name = "analyzedCodeUnits")]
		public List<AnalyzedCodeUnit> AnalyzedCodeUnits { get; set; }

		[DataMember(Name = "tokensPositions")]
		public List<TokenPosition> TokensPositions { get; set; }

		[DataMember(Name = "matchedSnippets")]
		public List<MatchedSnippet> MatchedSnippets { get; set; }
	}

	[DataContract]
	public class SubmissionInfo
	{
		[DataMember(Name = "id")]
		public int AntiplagiarismId { get; set; }

		[DataMember(Name = "code")]
		public string Code { get; set; }

		[DataMember(Name = "taskId")]
		public Guid TaskId { get; set; }

		[DataMember(Name = "language")]
		public Language Language { get; set; }

		[DataMember(Name = "authorId")]
		public Guid AuthorId { get; set; }

		[DataMember(Name = "additionalInfo")]
		public string AdditionalInfo { get; set; }

		[DataMember(Name = "clientSubmissionId")]
		public string ClientSubmissionId { get; set; }

		public SubmissionInfo CloneWithoutCode()
		{
			var r = (SubmissionInfo)MemberwiseClone();
			r.Code = null;
			return r;
		}
	}

	[DataContract]
	public class AnalyzedCodeUnit
	{
		[DataMember(Name = "name")]
		public string Name { get; set; }

		[DataMember(Name = "firstTokenIndex")]
		public int FirstTokenIndex { get; set; }

		[DataMember(Name = "tokensCount")]
		public int TokensCount { get; set; }
	}

	[DataContract]
	public class TokenPosition
	{
		[DataMember(Name = "tokenIndex")]
		public int TokenIndex { get; set; }

		[DataMember(Name = "startPosition")]
		public int StartPosition { get; set; }

		[DataMember(Name = "length")]
		public int Length { get; set; }
	}

	[DataContract]
	public class MatchedSnippet
	{
		[DataMember(Name = "snippetType")]
		public SnippetType SnippetType { get; set; }

		[DataMember(Name = "snippetTokensCount")]
		public int TokensCount { get; set; }

		[DataMember(Name = "originalSubmissionFirstTokenIndex")]
		public int OriginalSubmissionFirstTokenIndex { get; set; }

		[DataMember(Name = "plagiarismSubmissionFirstTokenIndex")]
		public int PlagiarismSubmissionFirstTokenIndex { get; set; }

		[DataMember(Name = "snippetFrequency")]
		public double SnippetFrequency { get; set; }
	}

	[DataContract]
	public class SuspicionLevels
	{
		[DataMember(Name = "faintSuspicion")]
		public double FaintSuspicion { get; set; }

		[DataMember(Name = "strongSuspicion")]
		public double StrongSuspicion { get; set; }

		[DataMember(Name = "automaticFaintSuspicion")]
		public double? AutomaticFaintSuspicion { get; set; }

		[DataMember(Name = "automaticStrongSuspicion")]
		public double? AutomaticStrongSuspicion { get; set; }

		[DataMember(Name = "manualFaintSuspicion")]
		public double? ManualFaintSuspicion { get; set; }

		[DataMember(Name = "manualStrongSuspicion")]
		public double? ManualStrongSuspicion { get; set; }
	}
}