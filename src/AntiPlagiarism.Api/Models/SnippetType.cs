using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AntiPlagiarism.Api.Models;

[JsonConverter(typeof(StringEnumConverter), true)]
public enum SnippetType : short
{
	TokensKinds = 1,
	TokensValues = 2
}