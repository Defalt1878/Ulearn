using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Repos;
using Database.Repos.Users;
using Microsoft.AspNetCore.Mvc;
using Ulearn.Core.Courses.Manager;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Metrics;

namespace Ulearn.Web.Api.Controllers;

public class SelfCheckupsController : BaseController
{
	private readonly ISelfCheckupsRepo selfCheckupsRepo;
	private readonly MetricSender sender;

	public SelfCheckupsController(ICourseStorage courseStorage, UlearnDb db, IUsersRepo usersRepo, ISelfCheckupsRepo selfCheckupsRepo, MetricSender sender)
		: base(courseStorage, db, usersRepo)
	{
		this.selfCheckupsRepo = selfCheckupsRepo;
		this.sender = sender;
	}

	[HttpPost("course/{courseId}/{slideId:guid}/checkups/{checkupId}")]
	public async Task<IActionResult> AddOrUpdateSelfCheckup(
		[FromRoute] string courseId,
		[FromRoute] Guid slideId,
		[FromRoute] string checkupId,
		[FromBody] bool isChecked
	)
	{
		bool HasSelfChecks(SlideBlock s) => s is SelfCheckupsBlock or AbstractExerciseBlock { Checkups: not null };

		var course = courseStorage.GetCourse(courseId);
		var slideBlocks = course.GetSlideByIdNotSafe(slideId).Blocks;
		if (!slideBlocks.Any(s =>
				HasSelfChecks(s) ||
				(s is SpoilerBlock spoilerBlock && spoilerBlock.Blocks.Any(HasSelfChecks))
			))
			return BadRequest("Checkup doesn't exists");

		sender.SendCount($"slide.selfCheckup.check.{isChecked}");
		sender.SendCount($"slide.selfCheckup.check.{courseId}.{isChecked}");
		await selfCheckupsRepo.AddOrUpdateSelfCheckup(UserId, courseId, slideId, checkupId, isChecked);

		return NoContent();
	}
}