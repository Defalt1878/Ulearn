using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Notifications;
using Ulearn.Web.Api.Models.Responses.Notifications;
using Vostok.Logging.Abstractions;
using Web.Api.Configuration;

namespace Ulearn.Web.Api.Controllers.Notifications
{
	[Route("/notifications")]
	public class NotificationsController : BaseController
	{
		private static int? commentsFeedNotificationTransportId;
		private readonly WebApiConfiguration configuration;
		private readonly IFeedRepo feedRepo;
		private readonly INotificationDataPreloader notificationDataPreloader;
		private readonly IServiceProvider serviceProvider;

		public NotificationsController(ICourseStorage courseStorage, UlearnDb db,
			IUsersRepo usersRepo,
			IFeedRepo feedRepo,
			IServiceProvider serviceProvider,
			INotificationDataPreloader notificationDataPreloader,
			IOptions<WebApiConfiguration> options)
			: base(courseStorage, db, usersRepo)
		{
			this.feedRepo = feedRepo;
			this.serviceProvider = serviceProvider;
			this.notificationDataPreloader = notificationDataPreloader;
			configuration = options.Value;
		}

		private new static ILog Log => LogProvider.Get().ForContext(typeof(NotificationsController));

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			commentsFeedNotificationTransportId ??= await feedRepo.GetCommentsFeedNotificationTransportId();

			var userId = User.GetUserId();
			await feedRepo.AddFeedNotificationTransportIfNeeded(userId);

			await next();
		}

		/// <summary>
		///     Список уведомлений и комментариев
		/// </summary>
		[HttpGet]
		[Authorize]
		public async Task<ActionResult<NotificationListResponse>> NotificationList()
		{
			var userId = User.GetUserId();
			var (importantNotificationList, commentsNotificationList) = await GetNotificationListsAsync(userId);

			return new NotificationListResponse
			{
				Important = importantNotificationList,
				Comments = commentsNotificationList
			};
		}

		/// <summary>
		///     Число непрочитанных уведомлений
		/// </summary>
		[HttpGet("count")]
		[Authorize]
		public async Task<ActionResult<NotificationsCountResponse>> NotificationsCount([FromQuery] NotificationsCountParameters parameters)
		{
			var userId = User.GetUserId();
			var userNotificationTransportId = await feedRepo.GetUsersFeedNotificationTransportId(userId);
			var unreadCountAndLastTimestamp = await GetUnreadNotificationsCountAndLastTimestampAsync(userId, userNotificationTransportId!.Value, parameters.LastTimestamp);

			return new NotificationsCountResponse
			{
				Count = unreadCountAndLastTimestamp.Item1,
				LastTimestamp = unreadCountAndLastTimestamp.Item2
			};
		}

		/// <summary>
		///     Уведомление в плашке под шапкой
		/// </summary>
		[HttpGet("global")]
		public ActionResult<NotificationBarResponse> GlobalNotification()
		{
			return new NotificationBarResponse
			{
				Message = configuration.NotificationBar,
				Force = configuration.ForceNotificationBar,
				Overlap = configuration.OverlapNotificationBar
			};
		}

		private async Task<Tuple<int, DateTime?>> GetUnreadNotificationsCountAndLastTimestampAsync(string userId, int transportId, DateTime? from = null)
		{
			var realFrom = from ?? await feedRepo.GetFeedViewTimestamp(userId, transportId) ?? DateTime.MinValue;
			var unreadCount = await feedRepo.GetNotificationsCount(userId, realFrom, transportId);
			if (unreadCount > 0)
				from = await feedRepo.GetLastDeliveryTimestamp(transportId);

			return Tuple.Create(unreadCount, from);
		}

		private async Task<(NotificationList, NotificationList)> GetNotificationListsAsync(string userId)
		{
			var notificationTransportId = await feedRepo.GetUsersFeedNotificationTransportId(userId);

			var importantNotifications = new List<Notification>();
			if (notificationTransportId is not null)
				importantNotifications = await feedRepo.GetNotificationForFeedNotificationDeliveries(userId, n => n.InitiatedBy, notificationTransportId.Value);

			var commentsNotifications = new List<Notification>();
			if (commentsFeedNotificationTransportId is not null)
				commentsNotifications = await feedRepo.GetNotificationForFeedNotificationDeliveries(userId, n => n.InitiatedBy, commentsFeedNotificationTransportId.Value);

			Log.Info($"[GetNotificationList] Step 1 done: found {importantNotifications.Count} important notifications and {commentsNotifications.Count} comment notifications");

			importantNotifications = RemoveBlockedNotifications(importantNotifications).ToList();
			commentsNotifications = RemoveBlockedNotifications(commentsNotifications, importantNotifications).ToList();

			Log.Info($"[GetNotificationList] Step 2 done, removed blocked notifications: left {importantNotifications.Count} important notifications and {commentsNotifications.Count} comment notifications");

			importantNotifications = RemoveNotActualNotifications(importantNotifications).ToList();
			commentsNotifications = RemoveNotActualNotifications(commentsNotifications).ToList();

			Log.Info($"[GetNotificationList] Step 3 done, removed not actual notifications: left {importantNotifications.Count} important notifications and {commentsNotifications.Count} comment notifications");

			var importantLastViewTimestamp = await feedRepo.GetFeedViewTimestamp(userId, notificationTransportId ?? -1);
			var commentsLastViewTimestamp = await feedRepo.GetFeedViewTimestamp(userId, commentsFeedNotificationTransportId ?? -1);

			Log.Info("[GetNotificationList] Step 4, building models");

			var allNotifications = importantNotifications.Concat(commentsNotifications).ToList();
			var notificationsData = await notificationDataPreloader.LoadAsync(allNotifications);

			var importantNotificationList = new NotificationList
			{
				LastViewTimestamp = importantLastViewTimestamp,
				Notifications = importantNotifications.Select(notification => BuildNotificationInfo(notification, notificationsData)).ToList()
			};
			var commentsNotificationList = new NotificationList
			{
				LastViewTimestamp = commentsLastViewTimestamp,
				Notifications = commentsNotifications.Select(notification => BuildNotificationInfo(notification, notificationsData)).ToList()
			};

			return (importantNotificationList, commentsNotificationList);
		}

		private IEnumerable<Notification> RemoveBlockedNotifications(IReadOnlyCollection<Notification> notifications, IReadOnlyCollection<Notification> searchBlockersAlsoIn = null)
		{
			var allNotifications = notifications.ToList();
			if (searchBlockersAlsoIn is not null)
				allNotifications = allNotifications.Concat(searchBlockersAlsoIn).ToList();

			foreach (var notification in notifications)
			{
				if (notification.IsBlockedByAnyNotificationFrom(serviceProvider, allNotifications))
					continue;
				yield return notification;
			}
		}

		private IEnumerable<Notification> RemoveNotActualNotifications(IEnumerable<Notification> notifications)
		{
			return notifications.Where(notification =>
			{
				Log.Info($"Checking actuality of notification #{notification.Id}: {notification} ({notification.GetNotificationType().ToString()})");
				return notification.IsActual();
			});
		}

		private NotificationInfo BuildNotificationInfo(Notification notification, NotificationDataStorage notificationsData)
		{
			return new NotificationInfo
			{
				Id = notification.Id,
				Author = BuildShortUserInfo(notification.InitiatedBy),
				Type = notification.GetNotificationType().ToString(),
				CreateTime = notification.CreateTime,
				CourseId = notification.CourseId,
				Data = BuildNotificationData(notification, notificationsData)
			};
		}

		private NotificationData BuildNotificationData(Notification notification, NotificationDataStorage notificationsData)
		{
			var data = new NotificationData();
			if (notification is AbstractCommentNotification commentNotification)
				data.Comment = BuildNotificationCommentInfo(notificationsData.CommentsByIds.GetOrDefault(commentNotification.CommentId));
			return data;
		}
	}
}