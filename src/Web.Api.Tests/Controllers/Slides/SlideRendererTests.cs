using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Units;
using Ulearn.Web.Api.Clients;
using Ulearn.Web.Api.Controllers.Slides;
using Ulearn.Web.Api.Models.Responses.SlideBlocks;

namespace Web.Api.Tests.Controllers.Slides
{
	public class ApiSlideBlocksTests : BaseControllerTests
	{
		private const string testDataDirectory = "Controllers/Slides/TestData/";
		private SlideRenderer slideRenderer;
		private IUlearnVideoAnnotationsClient videoAnnotationsClient;
		private CourseSettings courseSettings;
		private Unit unit;
		private XmlSlideLoader loader;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
			videoAnnotationsClient = Mock.Of<IUlearnVideoAnnotationsClient>();
			slideRenderer = new SlideRenderer(videoAnnotationsClient, null, null, null, null, null);
			loader = new XmlSlideLoader();
			courseSettings = new CourseSettings(CourseSettings.DefaultSettings);
			courseSettings.Scoring.Groups.Add("ScoringGroup1", new ScoringGroup { Id = "ScoringGroup1" });
			unit = new Unit(UnitSettings.CreateByTitle("Unit title", courseSettings), "");
		}

		private Slide LoadSlideFromXmlFile(string filename)
		{
			var slideFile = new DirectoryInfo(testDataDirectory).GetFile(filename);
			var slideLoadingContext = new SlideLoadingContext("CourseId", unit, courseSettings, slideFile.Directory, slideFile);
			return loader.Load(slideLoadingContext);
		}

		private Slide TryLoadSlide(string fileName)
		{
			try
			{
				return LoadSlideFromXmlFile(fileName);
			}
			catch (Exception ex)
			{
				Assert.Inconclusive();
				return null;
			}
		}

		private IApiSlideBlock[] GetApiSlideBlocks(Slide slide, bool removeHiddenBlocks)
		{
			var context = new SlideRenderContext("course", slide, "UserId", "api.test.me", "test.me", removeHiddenBlocks,
				"googleDoc", Mock.Of<IUrlHelper>());
			return slide.Blocks
				.SelectMany(b => slideRenderer.ToApiSlideBlocks(b, context).Result)
				.ToArray();
		}

		[Test]
		[TestCase("SimpleSlideWithMarkdownBlocks.xml", false, 6)]
		[TestCase("SimpleSlideWithMarkdownBlocks.xml", true, 4)]
		public void SlideWithMarkdownBlocksAndNestedNote(string fileName, bool removeHiddenBlocks, int expectedBlocksCount)
		{
			var slide = TryLoadSlide(fileName);
			var apiSlideBlocks = GetApiSlideBlocks(slide, removeHiddenBlocks);
			Assert.AreEqual(expectedBlocksCount, apiSlideBlocks.Length);
		}

		[Test]
		public void SlideWithSpoilerBlock()
		{
			var slide = TryLoadSlide("SlideWithSpoilerBlockAndMarkdown.xml");
			var apiSlideBlocks = GetApiSlideBlocks(slide, true);
			Assert.AreEqual(1, apiSlideBlocks.Length);
			var spoilerBlock = (SpoilerBlockResponse)apiSlideBlocks[0];
			Assert.AreEqual(4, spoilerBlock.InnerBlocks.Count);
		}

		[Test]
		public void SlideWithCodeInMarkdown()
		{
			var slide = TryLoadSlide("SlideWithCodeInMarkdown.xml");
			var apiSlideBlocks = GetApiSlideBlocks(slide, true);
			Assert.AreEqual(3, apiSlideBlocks.Length);
			if (apiSlideBlocks.OfType<CodeBlockResponse>().Any(cb => cb.Code.Contains("&lt;")))
				Assert.Fail();
		}

		[Test]
		public void SlideWithImageInMarkdown()
		{
			var slide = TryLoadSlide("SlideWithImageInMarkdown.xml");
			var apiSlideBlocks = GetApiSlideBlocks(slide, true);
			Assert.AreEqual(3, apiSlideBlocks.Length);
			var img = apiSlideBlocks[1] as ImageGalleryBlockResponse;
			Assert.AreEqual(new[] { "api.test.me/courses/course/files/manipulator.png" }, img.ImageUrls);
		}
	}
}