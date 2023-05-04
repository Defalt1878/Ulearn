using System;
using System.Linq;
using System.Threading.Tasks;
using Database.Models;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Responses.Websockets;

namespace Ulearn.Web.Api.Controllers.Websockets
{
	public class WebsocketsHub : Hub // Здесь пишутся методы для получения сообщений через websocket, не для отправки
	{
		public override async Task OnConnectedAsync()
		{
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception exception)
		{
			await base.OnDisconnectedAsync(exception);
		}
	}

	public class WebsocketsEventSender
	{
		private readonly IHubContext<WebsocketsHub> hubContext;
		private readonly IServiceScopeFactory serviceScopeFactory;

		public WebsocketsEventSender(IHubContext<WebsocketsHub> hubContext, ICourseStorage courseStorage,
			IServiceScopeFactory serviceScopeFactory)
		{
			this.hubContext = hubContext;
			this.serviceScopeFactory = serviceScopeFactory;

			courseStorage.CourseChangedEvent += async courseId => await SendCourseChangedEvent(courseId);
		}

		private async Task SendCourseChangedEvent(string courseId)
		{
			using var scope = serviceScopeFactory.CreateScope();
			var unitsRepo = scope.ServiceProvider.GetService<IUnitsRepo>();
			var courseRolesRepo = scope.ServiceProvider.GetService<ICourseRolesRepo>();
			var usersRepo = scope.ServiceProvider.GetService<IUsersRepo>();

			IClientProxy clientProxy;
			if (await unitsRepo.IsCourseVisibleForStudents(courseId))
			{
				clientProxy = hubContext.Clients.All;
			}
			else
			{
				var instructors = await courseRolesRepo.GetListOfUsersWithCourseRole(CourseRoleType.Instructor, courseId, true);
				var sysAdmins = await usersRepo.GetSysAdminsIds();
				clientProxy = hubContext.Clients.Users(instructors.Concat(sysAdmins).Distinct().ToList());
			}

			var courseChangedResponse = new CourseChangedResponse { CourseId = courseId };
			await clientProxy.SendAsync("courseChanged", JsonConvert.SerializeObject(courseChangedResponse));
		}
	}
}