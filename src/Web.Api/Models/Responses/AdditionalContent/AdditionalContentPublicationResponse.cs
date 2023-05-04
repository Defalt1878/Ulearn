using System;
using System.Runtime.Serialization;
using Database.Models;
using JetBrains.Annotations;
using Ulearn.Web.Api.Controllers;
using Ulearn.Web.Api.Models.Common;

namespace Ulearn.Web.Api.Models.Responses.AdditionalContent;

[DataContract]
public class AdditionalContentPublicationResponse
{
	[DataMember]
	public Guid Id;

	[DataMember]
	public string CourseId;

	[DataMember]
	public int GroupId;

	[DataMember]
	public Guid UnitId;

	[DataMember]
	[CanBeNull]
	public Guid? SlideId;

	[DataMember]
	public DateTime Date;

	[DataMember]
	public ShortUserInfo Author;

	public static AdditionalContentPublicationResponse Build(AdditionalContentPublication p, ApplicationUser user)
	{
		return new AdditionalContentPublicationResponse
		{
			Id = p.Id,
			Author = BaseController.BuildShortUserInfo(user),
			Date = p.Date,
			UnitId = p.UnitId,
			SlideId = p.SlideId,
			CourseId = p.CourseId,
			GroupId = p.GroupId
		};
	}
}