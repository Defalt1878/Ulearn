using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Community.AspNetCore.ExceptionHandling;
using Community.AspNetCore.ExceptionHandling.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Ulearn.Common.Api.Models.Responses;
using Ulearn.Common.Api.Swagger;
using Vostok.Applications.AspNetCore;
using Vostok.Applications.AspNetCore.Builders;
using Vostok.Applications.AspNetCore.Configuration;
using Vostok.Hosting.Abstractions;
using Vostok.Logging.Microsoft;

namespace Ulearn.Common.Api;

public class BaseApiWebApplication : VostokAspNetCoreApplication
{
	public override Task WarmupAsync(IVostokHostingEnvironment environment, IServiceProvider provider)
	{
		return Task.CompletedTask;
	}

	public override void Setup(IVostokAspNetCoreApplicationBuilder builder, IVostokHostingEnvironment hostingEnvironment)
	{
		builder.SetupWebHost(webHostBuilder => webHostBuilder
				.UseKestrel()
				.ConfigureServices(s => ConfigureServices(s, hostingEnvironment))
				.UseEnvironment(hostingEnvironment.ApplicationIdentity.Environment)
				.Configure(app =>
				{
					var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();
					if (env.IsDevelopment())
						app.UseDeveloperExceptionPage();

					/* Add CORS. Should be before app.UseMvc() */
					ConfigureCors(app);

					/* Add exception handling policy.
						See https://github.com/IharYakimush/asp-net-core-exception-handling */
					app.UseExceptionHandlingPolicies();

					UseStaticFiles(app);

					app.UseHttpsRedirection();
					app.UseAuthentication();
					app.UseAuthorization();

					app.UseMvc();

					/* Configure swagger documentation. Now it's available at /swagger/v1/swagger.json.
					* See https://github.com/domaindrivendev/Swashbuckle.AspNetCore for details */
					app.UseSwagger(c =>
					{
						c.RouteTemplate = "documentation/{documentName}/swagger.json";
						c.PreSerializeFilters.Add((swagger, httpReq) =>
						{
							var isLocalhost = httpReq.Host.Host.StartsWith("localhost");
							var schemas = isLocalhost ? new[] { "http", "https" } : new[] { "https", "http" };
							swagger.Servers = schemas.Select(s => new OpenApiServer { Url = $"{s}://{httpReq.Host.Host}:{httpReq.Host.Port}" }).ToList();
						});
					});
					/* And add swagger UI, available at /documentation */
					app.UseSwaggerUI(c =>
					{
						c.SwaggerEndpoint("/documentation/v1/swagger.json", "Ulearn API");
						c.DocumentTitle = "UlearnApi";
						c.RoutePrefix = "documentation";
					});

					ConfigureWebApplication(app);
				})
			).SetupLogging(s =>
			{
				s.LogRequests = true;
				s.LogResponses = true;
				s.LogRequestHeaders = false;
				s.LogResponseCompletion = true;
				s.LogResponseHeaders = false;
				s.LogQueryString = new LoggingCollectionSettings(_ => true);
			})
			.SetupThrottling(b => b.DisableThrottling());
		ConfigureBackgroundWorkers(builder);
	}

	protected virtual IApplicationBuilder UseStaticFiles(IApplicationBuilder app)
	{
		return app;
	}

	protected virtual IApplicationBuilder ConfigureCors(IApplicationBuilder app)
	{
		return app;
	}

	protected virtual IApplicationBuilder ConfigureWebApplication(IApplicationBuilder app)
	{
		return app;
	}

	protected virtual void ConfigureBackgroundWorkers(IVostokAspNetCoreApplicationBuilder builder)
	{
	}

	protected virtual void ConfigureServices(IServiceCollection services, IVostokHostingEnvironment hostingEnvironment)
	{
		services.AddLogging(builder => builder.AddVostok(hostingEnvironment.Log));
		ConfigureDi(services);
		ConfigureMvc(services);
		ConfigureSwaggerDocumentation(services);
		ConfigureExceptionPolicy(services);
	}

	public virtual void ConfigureMvc(IServiceCollection services)
	{
		/* Asp.NET Core MVC */
		services
			.AddMvc(options => options.EnableEndpointRouting = false)
			.AddNewtonsoftJson(opt => opt.SerializerSettings.Converters.Add(new StringEnumConverter()))
			.AddApplicationPart(GetType().Assembly)
			.AddControllersAsServices();
	}

	private void ConfigureSwaggerDocumentation(IServiceCollection services)
	{
		/* Swagger API documentation generator. See https://github.com/domaindrivendev/Swashbuckle.AspNetCore for details */
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", GetApiNameAndDescription());

			/* See https://github.com/mattfrear/Swashbuckle.AspNetCore.Filters#installation for manual about swagger request and response examples */
			c.ExampleFilters();

			c.OperationFilter<BadRequestResponseOperationFilter>();

			/* See https://docs.microsoft.com/ru-ru/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-2.1&tabs=visual-studio%2Cvisual-studio-xml for details */
			var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName().Name}.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
			if (new FileInfo(xmlPath).Exists)
				c.IncludeXmlComments(xmlPath);

			ConfigureSwaggerDocumentationGeneration(c);
		}); //.AddSwaggerGenNewtonsoftSupport();
	}

	protected virtual OpenApiInfo GetApiNameAndDescription()
	{
		return new OpenApiInfo
		{
			Title = "Ulearn API",
			Version = "v1",
			Contact = new OpenApiContact
			{
				Name = "Ulearn support",
				Email = "support@ulearn.me"
			}
		};
	}

	protected virtual void ConfigureSwaggerDocumentationGeneration(SwaggerGenOptions c)
	{
	}

	private static void ConfigureExceptionPolicy(IServiceCollection services)
	{
		/* See https://github.com/IharYakimush/asp-net-core-exception-handling for details */
		services.AddExceptionHandlingPolicies(options =>
		{
			options.For<StatusCodeException>()
				.Response(exception => exception.Code)
				.WithObjectResult((_, exception) => new ErrorResponse(exception.Message))
				.Handled();
			/* Ensure that all exception types are handled by adding handler for generic exception at the end. */
			options.For<Exception>()
				.Log(lo => { lo.LogAction = (l, _, exception) => l.LogError(exception, "UnhandledException"); })
				.Response(_ => (int)HttpStatusCode.InternalServerError, ResponseAlreadyStartedBehaviour.GoToNextHandler)
				.ClearCacheHeaders()
				.WithObjectResult((_, exception) => new ErrorResponse("Internal error occured"
#if DEBUG
																	+ $". {exception.GetType().FullName}: {exception.Message}\n{exception.StackTrace}"
#endif
				))
				.Handled();
		});
	}

	public virtual void ConfigureDi(IServiceCollection services)
	{
	}

	public class UlearnPortConfiguration
	{
		public string Port { get; set; }
	}
}