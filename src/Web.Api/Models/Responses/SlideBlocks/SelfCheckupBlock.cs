using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Database.Models;

namespace Ulearn.Web.Api.Models.Responses.SlideBlocks
{
	[DataContract]
	[DisplayName("selfCheckups")]
	public class SelfCheckupBlockResponse : IApiSlideBlock
	{
		public SelfCheckupBlockResponse(List<ISelfCheckup> slideCheckups, List<SelfCheckup> userCheckups)
		{
			Checkups = BuildCheckups(slideCheckups, userCheckups);
		}

		[DataMember]
		public List<SelfCheckupResponse> Checkups { get; set; }

		public bool Hide { get; set; }

		public static List<SelfCheckupResponse> BuildCheckups(List<ISelfCheckup> slideCheckups, List<SelfCheckup> userCheckups)
		{
			var userCheckupsById = userCheckups.ToDictionary(c => c.CheckupId, c => c);

			return slideCheckups
				.Select(c => new SelfCheckupResponse
				{
					Content = c.Content,
					IsChecked = userCheckupsById.ContainsKey(c.CheckupId) && userCheckupsById[c.CheckupId].IsChecked,
					Id = c.CheckupId
				})
				.ToList();
		}
	}

	public interface ISelfCheckup
	{
		public string Content { get; set; }
		public string CheckupId { get; set; }
	}

	public class SlideSelfCheckup : ISelfCheckup
	{
		public SlideSelfCheckup(string content)
		{
			Content = content;
			CheckupId = $"Slide_{GetDeterministicHashCode(content).ToString()}";
		}

		public string Content { get; set; }
		public string CheckupId { get; set; }

		//We need same hash code across each run, otherwise it will forget all user progress due to same checkup having new id
		//https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
		private static int GetDeterministicHashCode(string str)
		{
			unchecked
			{
				var hash1 = (5381 << 16) + 5381;
				var hash2 = hash1;

				for (var i = 0; i < str.Length; i += 2)
				{
					hash1 = ((hash1 << 5) + hash1) ^ str[i];
					if (i == str.Length - 1)
						break;
					hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
				}

				return hash1 + hash2 * 1566083941;
			}
		}
	}

	public class ExerciseSelfCheckup : ISelfCheckup
	{
		public ExerciseSelfCheckup(int submissionId)
		{
			Content = "Исправьте замечания от преподавателя";
			CheckupId = $"Exercise_{submissionId}";
		}

		public string Content { get; set; }
		public string CheckupId { get; set; }
	}
}