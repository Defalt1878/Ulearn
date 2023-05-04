using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using Database.Models.Comments;
using Database.Repos.Comments;

namespace Ulearn.Web.Api.Controllers.Notifications;

public class NotificationDataPreloader : INotificationDataPreloader
{
	private readonly ICommentsRepo commentsRepo;

	public NotificationDataPreloader(ICommentsRepo commentsRepo)
	{
		this.commentsRepo = commentsRepo;
	}

	public async Task<NotificationDataStorage> LoadAsync(IEnumerable<Notification> notifications)
	{
		return new NotificationDataStorage
		{
			CommentsByIds = await LoadCommentsAsync(notifications).ConfigureAwait(false)
		};
	}

	private async Task<Dictionary<int, Comment>> LoadCommentsAsync(IEnumerable<Notification> notifications)
	{
		/* Preload comments */
		var commentNotifications = notifications.OfType<AbstractCommentNotification>();
		var commentIds = commentNotifications.Select(n => n.CommentId);
		return (await commentsRepo.GetCommentsByIdsAsync(commentIds).ConfigureAwait(false)).ToDictionary(c => c.Id);
	}
}