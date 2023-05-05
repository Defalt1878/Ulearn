using System;
using System.Net;
using System.Threading.Tasks;
using AntiPlagiarism.Web.Database;
using AntiPlagiarism.Web.Database.Models;
using AntiPlagiarism.Web.Database.Repos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Ulearn.Common.Api.Models.Responses;
using Vostok.Logging.Abstractions;

namespace AntiPlagiarism.Web.Controllers
{
	[ApiController]
	public abstract class BaseController : Controller
	{
		protected readonly IClientsRepo clientsRepo;
		protected readonly AntiPlagiarismDb db;

		protected Client client;

		protected BaseController(IClientsRepo clientsRepo, AntiPlagiarismDb db)
		{
			this.clientsRepo = clientsRepo;
			this.db = db;
		}

		private static ILog Log => LogProvider.Get().ForContext(typeof(BaseController));

		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			var token = context.HttpContext.Request.Query["token"].ToString();
			if (string.IsNullOrEmpty(token))
			{
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
				context.Result = new JsonResult(new ErrorResponse("Not authenticated request. Pass 'token' parameter to query string."));
				return;
			}

			if (!Guid.TryParse(token, out var tokenGuid))
			{
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
				context.Result = new JsonResult(new ErrorResponse("Not authenticated request. Pass correct GUID as 'token' parameter to query string."));
				return;
			}

			Log.Debug($"Token in request is {token}", token);
			client = await clientsRepo.FindClientByTokenAsync(tokenGuid).ConfigureAwait(false);
			if (client is null)
			{
				context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
				context.Result = new JsonResult(new ErrorResponse("Not authenticated request. Token is invalid or disabled for a while."));
				return;
			}

			DisableEfChangesTrackingForGetRequests(context);

			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
		}

		private void DisableEfChangesTrackingForGetRequests(ActionContext context)
		{
			/* Disable change tracking in EF Core for GET requests due to performance issues */
			/* This code is copy-pasted from Web.Api/Controllers/BaseController.cs */
			var isRequestSafe = context.HttpContext.Request.Method == "GET";
			db.ChangeTracker.AutoDetectChangesEnabled = !isRequestSafe;

			if (isRequestSafe)
				Log.Debug("Выключаю автоматическое отслеживание изменений в EF Core: db.ChangeTracker.AutoDetectChangesEnabled = false");
		}
	}
}