using System.Text;
using System.Threading.Tasks;
using Database;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OfficeOpenXml;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Parameters.Analytics;
using Ulearn.Web.Api.Models.Responses.Analytics;
using Ulearn.Web.Api.Utils;

namespace Ulearn.Web.Api.Controllers;

[Route("course-statistics/export")]
public class CourseStatisticsController : BaseController
{
	private readonly StatisticModelUtils statisticModelUtils;


	public CourseStatisticsController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo, StatisticModelUtils statisticModelUtils)
		: base(courseStorage, db, usersRepo)
	{
		this.statisticModelUtils = statisticModelUtils;
	}

	[HttpGet("{fileNameWithNoExtension}.json")]
	[Authorize(Policy = "Instructors", AuthenticationSchemes = "Bearer,Identity.Application")]
	public async Task<ActionResult> ExportCourseStatisticsAsJson([FromQuery] CourseStatisticsParams param, [FromRoute] string fileNameWithNoExtension)
	{
		if (param.CourseId is null)
			return NotFound();

		var model = await statisticModelUtils.GetCourseStatisticsModel(3000, UserId, param.CourseId, param.GroupsIds);
		var serializedModel = new CourseStatisticsModel(model).JsonSerialize(Formatting.Indented);

		return File(Encoding.UTF8.GetBytes(serializedModel), "application/json", $"{fileNameWithNoExtension}.json");
	}

	[HttpGet("{fileNameWithNoExtension}.xml")]
	[Authorize(Policy = "Instructors", AuthenticationSchemes = "Bearer,Identity.Application")]
	public async Task<ActionResult> ExportCourseStatisticsAsXml([FromQuery] CourseStatisticsParams param, [FromRoute] string fileNameWithNoExtension)
	{
		if (param.CourseId is null)
			return NotFound();
		var model = await statisticModelUtils.GetCourseStatisticsModel(3000, UserId, param.CourseId, param.GroupsIds);
		var serializedModel = new CourseStatisticsModel(model).XmlSerialize();
		return File(Encoding.UTF8.GetBytes(serializedModel), "text/xml", $"{fileNameWithNoExtension}.xml");
	}

	[HttpGet("{fileNameWithNoExtension}.xlsx")]
	[Authorize(Policy = "Instructors", AuthenticationSchemes = "Bearer,Identity.Application")]
	public async Task<ActionResult> ExportCourseStatisticsAsXlsx([FromQuery] CourseStatisticsParams param, [FromRoute] string fileNameWithNoExtension)
	{
		if (param.CourseId is null)
			return NotFound();

		var model = await statisticModelUtils.GetCourseStatisticsModel(3000, UserId, param.CourseId, param.GroupsIds);
		ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
		var package = new ExcelPackage();
		var builder = new ExcelWorksheetBuilder(package.Workbook.Worksheets.Add(model.CourseTitle));
		StatisticModelUtils.FillStatisticModelBuilder(
			builder,
			model,
			true
		);
		builder = new ExcelWorksheetBuilder(package.Workbook.Worksheets.Add("Только полные баллы"));
		StatisticModelUtils.FillStatisticModelBuilder(
			builder,
			model,
			true,
			true
		);
		byte[] bytes;
		using (var stream = StaticRecyclableMemoryStreamManager.Manager.GetStream())
		{
			await package.SaveAsAsync(stream);
			bytes = stream.ToArray();
		}

		return File(bytes, "application/vnd.ms-excel", $"{fileNameWithNoExtension}.xlsx");
	}
}