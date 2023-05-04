using System.Collections.Generic;
using Database.Models.Comments;

namespace Ulearn.Web.Api.Controllers.Notifications;

public class NotificationDataStorage
{
	public Dictionary<int, Comment> CommentsByIds;
}