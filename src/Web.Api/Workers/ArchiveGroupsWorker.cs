using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Applications.Scheduled;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Abstractions;

namespace Ulearn.Web.Api.Workers
{
	public class ArchiveGroupsWorker : VostokScheduledApplication
	{
		private readonly IServiceScopeFactory serviceScopeFactory;

		public ArchiveGroupsWorker(IServiceScopeFactory serviceScopeFactory)
		{
			this.serviceScopeFactory = serviceScopeFactory;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(ArchiveGroupsWorker));

		public override void Setup(IScheduledActionsBuilder builder, IVostokHostingEnvironment environment)
		{
			var scheduler = Scheduler.Crontab("0 9 * * 1"); // По пн в 9
			builder.Schedule("ArchiveGroupsWorker", scheduler, ArchiveAllOldGroups);
		}

		private async Task ArchiveAllOldGroups()
		{
			Log.Info("Start ArchiveGroupsWorker");
			using var scope = serviceScopeFactory.CreateScope();
			var groupsArchiver = scope.ServiceProvider.GetService<IGroupsArchiver>();
			var usersRepo = scope.ServiceProvider.GetService<IUsersRepo>();
			var groupsRepo = scope.ServiceProvider.GetService<IGroupsRepo>();
			var notificationsRepo = scope.ServiceProvider.GetService<INotificationsRepo>();

			var groupsIdsToArchive = await groupsArchiver.GetOldGroupsToArchive();
			var bot = await usersRepo.GetUlearnBotUser();
			foreach (var groupId in groupsIdsToArchive)
			{
				var group = await groupsRepo.FindGroupByIdAsync(groupId);
				await groupsRepo.ArchiveGroupAsync(groupId, true);
				var notification = new GroupIsArchivedNotification { GroupId = groupId };
				await notificationsRepo.AddNotification(group.CourseId, notification, bot.Id);
			}

			Log.Info($"Archived {groupsIdsToArchive.Count} groups");
		}
	}
}