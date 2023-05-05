using System.Runtime.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ulearn.Common.Api.Models.Parameters;

namespace AntiPlagiarism.Api.Models.Parameters;

public class AntiPlagiarismApiParameters : ApiParameters
{
	[BindRequired]
	[FromQuery(Name = "token")]
	[IgnoreDataMember]
	// Токен проверяется в BaseController OnActionExecutionAsync. Здесь указан только чтобы было поле в swagger
	public string Token { get; set; }
}