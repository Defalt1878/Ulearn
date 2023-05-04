using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Ulearn.Web.Api.Models.Responses.Groups
{
	[DataContract]
	public class SuperGroupMoveUserResponse
	{
		/// <summary>
		///     Contains all users which been moved
		/// </summary>
		[DataMember]
		public List<MoveUserInfo> MovedUsers { get; set; }
	}
}