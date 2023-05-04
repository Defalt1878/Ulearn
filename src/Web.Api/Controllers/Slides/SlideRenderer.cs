using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using Database.Models;
using Database.Repos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ulearn.Common;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Slides.Quizzes;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;
using Ulearn.Core.Markdown;
using Ulearn.Core.Metrics;
using Ulearn.Web.Api.Clients;
using Ulearn.Web.Api.Models.Common;
using Ulearn.Web.Api.Models.Responses.Exercise;
using Ulearn.Web.Api.Models.Responses.SlideBlocks;

namespace Ulearn.Web.Api.Controllers.Slides
{
	public class SlideRenderer
	{
		private readonly ICourseRolesRepo courseRolesRepo;
		private readonly MetricSender metricSender;
		private readonly ISelfCheckupsRepo selfCheckupsRepo;
		private readonly ISlideCheckingsRepo slideCheckingsRepo;
		private readonly IUserSolutionsRepo userSolutionsRepo;
		private readonly IUlearnVideoAnnotationsClient videoAnnotationsClient;

		public SlideRenderer(IUlearnVideoAnnotationsClient videoAnnotationsClient,
			MetricSender metricSender,
			ISlideCheckingsRepo slideCheckingsRepo, ICourseRolesRepo courseRolesRepo,
			ISelfCheckupsRepo selfCheckupsRepo, IUserSolutionsRepo userSolutionsRepo)
		{
			this.videoAnnotationsClient = videoAnnotationsClient;
			this.slideCheckingsRepo = slideCheckingsRepo;
			this.courseRolesRepo = courseRolesRepo;
			this.selfCheckupsRepo = selfCheckupsRepo;
			this.userSolutionsRepo = userSolutionsRepo;
			this.metricSender = metricSender;
		}

		public ShortSlideInfo BuildShortSlideInfo(string courseId, Slide slide, Func<Slide, int> getSlideMaxScoreFunc, Func<Slide, string> getGitEditLink, IUrlHelper urlHelper)
		{
			return BuildShortSlideInfo<ShortSlideInfo>(courseId, slide, getSlideMaxScoreFunc, getGitEditLink, urlHelper);
		}

		private static T BuildShortSlideInfo<T>(string courseId, Slide slide, Func<Slide, int> getSlideMaxScoreFunc, Func<Slide, string> getGitEditLink, IUrlHelper urlHelper)
			where T : ShortSlideInfo, new()
		{
			return new T
			{
				Id = slide.Id,
				Title = slide.Title,
				Hide = slide.Hide,
				Slug = slide.Url,
				ApiUrl = urlHelper.Action("SlideInfo", "Slides", new { courseId, slideId = slide.Id }),
				MaxScore = getSlideMaxScoreFunc(slide),
				ScoringGroup = slide.ScoringGroup,
				Type = GetSlideType(slide),
				QuestionsCount = slide.Blocks.OfType<AbstractQuestionBlock>().Count(),
				ContainsVideo = slide.Blocks.OfType<YoutubeBlock>().Any(),
				GitEditLink = getGitEditLink(slide),
				QuizMaxTriesCount = slide is QuizSlide quizSlide ? quizSlide.MaxTriesCount : 0,
				AdditionalContentInfo = new AdditionalContent
				{
					IsAdditionalContent = slide.IsExtraContent,
					PublicationDate = null
				}
			};
		}

		private static SlideType GetSlideType(Slide slide)
		{
			return slide switch
			{
				ExerciseSlide _ => SlideType.Exercise,
				QuizSlide _ => SlideType.Quiz,
				FlashcardSlide _ => SlideType.Flashcards,
				_ => SlideType.Lesson
			};
		}

		public async Task<ApiSlideInfo> BuildSlideInfo(SlideRenderContext slideRenderContext, Func<Slide, int> getSlideMaxScoreFunc, Func<Slide, string> getGitEditLink)
		{
			var result = BuildShortSlideInfo<ApiSlideInfo>(slideRenderContext.CourseId, slideRenderContext.Slide, getSlideMaxScoreFunc, getGitEditLink, slideRenderContext.UrlHelper);

			result.Blocks = new List<IApiSlideBlock>();
			foreach (var b in slideRenderContext.Slide.Blocks)
				result.Blocks.AddRange(await ToApiSlideBlocks(b, slideRenderContext));

			return result;
		}

		public async Task<IEnumerable<IApiSlideBlock>> ToApiSlideBlocks(SlideBlock slideBlock, SlideRenderContext context)
		{
			if (context.RemoveHiddenBlocks && slideBlock.Hide)
				return Array.Empty<IApiSlideBlock>();
			var apiSlideBlocks = await RenderBlock(slideBlock, context);
			if (context.RemoveHiddenBlocks)
				apiSlideBlocks = apiSlideBlocks.Where(b => !b.Hide);
			return apiSlideBlocks;
		}

		private async Task<IEnumerable<IApiSlideBlock>> RenderBlock(SlideBlock slideBlock, SlideRenderContext context)
		{
			return slideBlock switch
			{
				CodeBlock block => new[]
				{
					new CodeBlockResponse(block)
				},
				HtmlBlock block => new[]
				{
					new HtmlBlockResponse(block, false, context.BaseUrlApi)
				},
				ImageGalleryBlock block => new[]
				{
					new ImageGalleryBlockResponse(block, context.BaseUrlApi, context.CourseId, context.Slide.Unit.UnitDirectoryRelativeToCourse)
				},
				TexBlock block => new[]
				{
					new TexBlockResponse(block)
				},
				SpoilerBlock block => await RenderBlock(block, context),
				MarkdownBlock block => RenderBlock(block, context),
				YoutubeBlock block => await RenderBlock(block, context),
				SelfCheckupsBlock block => await RenderBlock(block, context),
				AbstractExerciseBlock block => await RenderBlock(block, context),
				_ => Enumerable.Empty<IApiSlideBlock>()
			};
		}

		private async Task<IEnumerable<IApiSlideBlock>> RenderBlock(SpoilerBlock sb, SlideRenderContext context)
		{
			var innerBlocks = new List<IApiSlideBlock>();
			foreach (var b in sb.Blocks)
				innerBlocks.AddRange(await ToApiSlideBlocks(b, context));
			if (sb.Hide)
				innerBlocks.ForEach(b => b.Hide = true);
			return new[] { new SpoilerBlockResponse(sb, innerBlocks) };
		}

		private static IEnumerable<IApiSlideBlock> RenderBlock(MarkdownBlock mb, SlideRenderContext context)
		{
			var renderedMarkdown = mb.RenderMarkdown(context.Slide, new MarkdownRenderContext(context.BaseUrlApi, context.BaseUrlWeb, context.CourseId, context.Slide.Unit.UnitDirectoryRelativeToCourse));
			var parsedBlocks = ParseBlocksFromMarkdown(renderedMarkdown);
			if (mb.Hide)
				parsedBlocks.ForEach(b => b.Hide = true);
			return parsedBlocks;
		}

		private async Task<IEnumerable<IApiSlideBlock>> RenderBlock(YoutubeBlock yb, SlideRenderContext context)
		{
			var annotation = await videoAnnotationsClient.GetVideoAnnotations(context.VideoAnnotationsGoogleDoc, yb.VideoId);
			var googleDocLink = string.IsNullOrEmpty(context.VideoAnnotationsGoogleDoc) ? null
				: "https://docs.google.com/document/d/" + context.VideoAnnotationsGoogleDoc;
			var response = new YoutubeBlockResponse(yb, annotation, googleDocLink);
			return new[] { response };
		}

		private async Task<IEnumerable<IApiSlideBlock>> RenderBlock(SelfCheckupsBlock b, SlideRenderContext context)
		{
			var checkups = await selfCheckupsRepo.GetSelfCheckups(context.UserId, context.CourseId, context.Slide.Id);

			metricSender.SendCount("slide.selfCheckups.show", b.Checkups.Count);
			metricSender.SendCount($"slide.selfCheckups.show.{context.CourseId}", b.Checkups.Count);
			return new[]
			{
				new SelfCheckupBlockResponse(
					b.Checkups
						.Select(c => new SlideSelfCheckup(c) as ISelfCheckup)
						.ToList(),
					checkups) { Hide = b.Hide }
			};
		}

		private async Task<IEnumerable<IApiSlideBlock>> RenderBlock(AbstractExerciseBlock b, SlideRenderContext context)
		{
			var isCourseAdmin = await courseRolesRepo.HasUserAccessToCourse(context.UserId, context.CourseId, CourseRoleType.CourseAdmin);

			ExerciseAttemptsStatistics exerciseAttemptsStatistics = null;
			List<SelfCheckupResponse> selfCheckups = null;

			if (b.HasAutomaticChecking())
			{
				var exerciseUsersCount = await slideCheckingsRepo.GetExerciseUsersCount(context.CourseId, context.Slide.Id);
				var exerciseUsersWithRightAnswerCount = await slideCheckingsRepo.GetExerciseUsersWithRightAnswerCount(context.CourseId, context.Slide.Id);
				var lastSuccessAttemptDate = await slideCheckingsRepo.GetExerciseLastRightAnswerDate(context.CourseId, context.Slide.Id);
				exerciseAttemptsStatistics = new ExerciseAttemptsStatistics
				{
					AttemptedUsersCount = exerciseUsersCount,
					UsersWithRightAnswerCount = exerciseUsersWithRightAnswerCount,
					LastSuccessAttemptDate = lastSuccessAttemptDate
				};
			}

			if (b.Checkups.Count > 0)
			{
				var checkups = await selfCheckupsRepo.GetSelfCheckups(context.UserId, context.CourseId, context.Slide.Id);

				var slideCheckups = b.Checkups
					.Select(c => new SlideSelfCheckup(c) as ISelfCheckup)
					.ToList();
				var lastSubmissionWithReview = (await userSolutionsRepo
						.GetAllSubmissionsByUserAllInclude(context.CourseId, context.Slide.Id, context.UserId)
						.OrderByDescending(s => s.Timestamp)
						.ToListAsync())
					.FirstOrDefault(s => s.ManualChecking is not null && s.ManualChecking.Reviews.Count > 0);
				if (lastSubmissionWithReview is not null)
					slideCheckups = slideCheckups
						.Prepend(new ExerciseSelfCheckup(lastSubmissionWithReview.Id))
						.ToList();
				metricSender.SendCount("exercise.selfCheckups.show", slideCheckups.Count);
				metricSender.SendCount($"exercise.selfCheckups.show.{context.CourseId}", slideCheckups.Count);
				selfCheckups = SelfCheckupBlockResponse.BuildCheckups(slideCheckups, checkups);
			}

			var exerciseSlideRendererContext = new ExerciseSlideRendererContext
			{
				CanSeeCheckerLogs = isCourseAdmin,
				AttemptsStatistics = exerciseAttemptsStatistics,
				Checkups = selfCheckups,
				markdownRenderContext = new MarkdownRenderContext(context.BaseUrlApi, context.BaseUrlWeb, context.CourseId, context.Slide.Unit.UnitDirectoryRelativeToCourse)
			};
			return new[] { new ExerciseBlockResponse(b, exerciseSlideRendererContext) };
		}

		private static List<IApiSlideBlock> ParseBlocksFromMarkdown(string renderedMarkdown)
		{
			var parser = new HtmlParser();
			var document = parser.ParseDocument(renderedMarkdown);
			var rootElements = document.Body!.Children;
			var blocks = new List<IApiSlideBlock>();
			foreach (var element in rootElements)
			{
				var tagName = element.TagName.ToLower();
				switch (tagName)
				{
					case "textarea":
					{
						var langStr = element.GetAttribute("data-lang");
						Language? lang = langStr is null ? null : Enum.Parse<Language>(langStr, true);
						var code = element.TextContent;
						blocks.Add(new CodeBlockResponse { Code = code, Language = lang });
						break;
					}
					case "img":
					{
						var href = element.GetAttribute("href");
						blocks.Add(new ImageGalleryBlockResponse { ImageUrls = new[] { href } });
						break;
					}
					case "p" 
						when element.Children.Length == 1 
							&& string.Equals(element.Children[0].TagName, "img", StringComparison.OrdinalIgnoreCase) 
							&& string.IsNullOrWhiteSpace(element.TextContent):
					{
						var href = element.Children[0].GetAttribute("src");
						blocks.Add(new ImageGalleryBlockResponse { ImageUrls = new[] { href } });
						break;
					}
					default:
					{
						var htmlContent = element.OuterHtml;
						if (blocks.Count > 0 && blocks.Last() is HtmlBlockResponse last)
						{
							htmlContent = last.Content + "\n" + htmlContent;
							blocks[^1] = new HtmlBlockResponse { Content = htmlContent, FromMarkdown = true };
						}
						else
						{
							blocks.Add(new HtmlBlockResponse { Content = htmlContent, FromMarkdown = true });
						}

						break;
					}
				}
			}

			return blocks;
		}
	}
}