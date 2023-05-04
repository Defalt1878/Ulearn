using System.Runtime.Serialization;
using Database.Models;
using Ulearn.Common.Api.Models.Validations;

namespace Ulearn.Web.Api.Models.Parameters.Groups
{
	[DataContract]
	public class CreateGroupParameters
	{
		[DataMember(IsRequired = true)]
		[NotEmpty(ErrorMessage = "Group name can not be empty")]
		public string Name { get; set; }

		[DataMember(IsRequired = true)]
		public GroupType GroupType { get; set; }
	}
}