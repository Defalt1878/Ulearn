using System;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Database.Models;
using Database.Repos;
using Database.Repos.Groups;
using Database.Repos.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ulearn.Core.Courses.Manager;
using Ulearn.Web.Api.Models.Responses.AdditionalContent;

namespace Ulearn.Web.Api.Controllers;

[Route("/additional-content-publications")]
[Authorize]
public class AdditionalContentPublicationsController : BaseController
{
	private readonly IAdditionalContentPublicationsRepo additionalContentPublicationsRepo;
	private readonly ICourseRolesRepo courseRolesRepo;
	private readonly IGroupsRepo groupsRepo;

	public AdditionalContentPublicationsController(ICourseStorage courseStorage, UlearnDb db,
		IUsersRepo usersRepo,
		IGroupsRepo groupsRepo,
		ICourseRolesRepo courseRolesRepo,
		IAdditionalContentPublicationsRepo additionalContentPublicationsRepo
	)
		: base(courseStorage, db, usersRepo)
	{
		this.groupsRepo = groupsRepo;
		this.courseRolesRepo = courseRolesRepo;
		this.additionalContentPublicationsRepo = additionalContentPublicationsRepo;
	}

	[HttpGet]
	public async Task<ActionResult<AdditionalContentPublicationsResponse>> GetAdditionalContentPublications([FromQuery] string courseId, [FromQuery] int groupId)
	{
		var isTester = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Tester).ConfigureAwait(false);

		if (!isTester)
			return Forbid($"You have no access to course {courseId}");

		var group = (await groupsRepo.GetCourseGroupsQueryable(courseId, GroupQueryType.SingleGroup, true).Where(g => g.Id == groupId).ToListAsync()).FirstOrDefault();
		if (group is null)
			return NotFound($"Group with id {groupId} not found");

		var publications = await additionalContentPublicationsRepo.GetAdditionalContentPublications(courseId, groupId);
		var userIds = publications.Select(p => p.AuthorId).Distinct().ToList();
		var users = (await usersRepo.GetUsersByIds(userIds)).ToDictionary(u => u.Id, u => u);

		return new AdditionalContentPublicationsResponse
		{
			Publications = publications
				.Select(p => AdditionalContentPublicationResponse.Build(p, users[p.AuthorId]))
				.ToList()
		};
	}

	[HttpPost]
	public async Task<ActionResult<AdditionalContentPublicationResponse>> AddAdditionalContentPublication(
		[FromQuery] string courseId,
		[FromQuery] int groupId,
		[FromQuery] Guid unitId,
		[FromQuery] Guid? slideId,
		[FromQuery] DateTime date
	)
	{
		var course = courseStorage.FindCourse(courseId);
		if (course is null)
			return NotFound($"Course {courseId} not found");

		var isTester = await courseRolesRepo.HasUserAccessToCourse(UserId, courseId, CourseRoleType.Tester).ConfigureAwait(false);

		if (!isTester)
			return Forbid($"You have no access to course {courseId}");

		var unit = course.GetUnits(new[] { unitId }).FirstOrDefault();
		if (unit is null)
			return NotFound($"Unit with id {unitId} not found");

		if (slideId is not null)
		{
			var slides = unit.GetSlides(false);
			if (slides.All(s => s.Id != slideId))
				return NotFound($"Slide with id {slideId} not found");
		}

		var group = (await groupsRepo.GetCourseGroupsQueryable(courseId, GroupQueryType.SingleGroup, true).Where(g => g.Id == groupId).ToListAsync()).FirstOrDefault();

		if (group is null)
			return NotFound($"Group with id {groupId} not found");

		if (await additionalContentPublicationsRepo.HasPublication(courseId, groupId, unitId, slideId))
			return UnprocessableEntity("There's already a publication for this content, try update it instead");

		var publication = await additionalContentPublicationsRepo.AddAdditionalContentPublication(courseId, groupId, UserId, unitId, slideId, date);
		var user = (await usersRepo.GetUsersByIds(new[] { UserId })).FirstOrDefault();

		return AdditionalContentPublicationResponse.Build(publication, user);
	}

	[HttpPatch]
	[Route("{publicationId}")]
	public async Task<ActionResult> UpdateAdditionalContentPublication([FromRoute] Guid publicationId, [FromQuery] DateTime date)
	{
		var publicationToUpdate = await additionalContentPublicationsRepo.GetAdditionalContentPublicationById(publicationId);
		if (publicationToUpdate is null)
			return NotFound($"Publication with id {publicationId} not found");

		var course = courseStorage.FindCourse(publicationToUpdate.CourseId);
		if (course is null)
			return NotFound($"Course {publicationToUpdate.CourseId} not found");
		var isTester = await courseRolesRepo.HasUserAccessToCourse(UserId, publicationToUpdate.CourseId, CourseRoleType.Tester).ConfigureAwait(false);

		if (!isTester)
			return Forbid($"You have no access to course {publicationToUpdate.CourseId}");

		publicationToUpdate.Date = date;

		await additionalContentPublicationsRepo.UpdateAdditionalContentPublication(publicationToUpdate);

		return NoContent();
	}

	[HttpDelete]
	[Route("{publicationId}")]
	public async Task<ActionResult> DeleteAdditionalContentPublication([FromRoute] Guid publicationId)
	{
		var publication = await additionalContentPublicationsRepo.GetAdditionalContentPublicationById(publicationId);
		if (publication is null)
			return NotFound($"Publication with id {publicationId} not found");

		var course = courseStorage.FindCourse(publication.CourseId);
		if (course is null)
			return NotFound($"Course {publication.CourseId} not found");

		var isTester = await courseRolesRepo.HasUserAccessToCourse(UserId, publication.CourseId, CourseRoleType.Tester).ConfigureAwait(false);

		if (!isTester)
			return Forbid($"You have no access to course {publication.CourseId}");

		await additionalContentPublicationsRepo.DeleteAdditionalContentPublication(publication);

		return Ok($"Publication with id {publicationId} deleted");
	}
}