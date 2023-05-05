using System.Runtime.Serialization;

namespace Ulearn.Common.Api.Models.Responses;

[DataContract]
public class PaginatedResponse : SuccessResponse
{
	[DataMember(Order = 100)]
	public PaginationResponse Pagination { get; set; }
}

[DataContract]
public class PaginationResponse
{
	[DataMember]
	public int Offset { get; set; }

	[DataMember]
	public int Count { get; set; }

	[DataMember(EmitDefaultValue = false)]
	public int? TotalCount { get; set; }
}