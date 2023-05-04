using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Ulearn.Web.Api.Models.Responses.AntiPlagiarism
{
	[JsonConverter(typeof(StringEnumConverter), true)]
	public enum SuspicionLevel : short
	{
		None = 0,
		Faint = 1,
		Strong = 2
	}

	[DataContract]
	public class AntiPlagiarismInfoResponse
	{
		[DataMember(Name = "status")]
		public string Status { get; set; }

		[DataMember(Name = "suspicionLevel")]
		public SuspicionLevel SuspicionLevel { get; set; }

		[DataMember(Name = "suspiciousAuthorsCount")]
		public int SuspiciousAuthorsCount { get; set; }
	}
}