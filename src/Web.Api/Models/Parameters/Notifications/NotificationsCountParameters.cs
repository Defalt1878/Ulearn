using System;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Common.Api.Models.Parameters;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Ulearn.Web.Api.Models.Parameters.Notifications;

public class NotificationsCountParameters : ApiParameters
{
	[FromQuery(Name = "lastTimestamp")]
	public DateTime? LastTimestamp { get; set; }
}