﻿using System;
using System.IO;
using NUnit.Framework;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Slides.Quizzes;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;
using Ulearn.Core.Courses.Units;

namespace Ulearn.Core.Tests.Courses.Slides
{
	[TestFixture]
	public class XmlSlideLoaderTests
	{
		private const string testDataDirectory = "Courses/Slides/TestData/";

		private XmlSlideLoader loader;

		private CourseSettings courseSettings;
		private Unit unit;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);

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

		[Test]
		public void LoadEmptySlide()
		{
			Assert.AreEqual(0, LoadSlideFromXmlFile("EmptySlide.xml").Blocks.Length);
		}

		[Test]
		public void LoadSimpleSlideWithMarkdownBlocks()
		{
			var slide = LoadSlideFromXmlFile("SimpleSlideWithMarkdownBlocks.xml");

			Assert.AreEqual(6, slide.Blocks.Length);
			Assert.AreEqual(Guid.Parse("C6A70E4D-9673-4D02-A50C-FE667A5BD83D"), slide.Id);
			Assert.AreEqual("Simple slide", slide.Title);
		}

		[Test]
		public void LoadEmptyQuiz()
		{
			Assert.AreEqual(typeof(QuizSlide), LoadSlideFromXmlFile("EmptyQuiz.xml").GetType());
		}

		[Test]
		public void LoadQuizWithScoringSettings()
		{
			var quizSlide = (QuizSlide)LoadSlideFromXmlFile("QuizWithScoringSettings.xml");

			Assert.AreEqual("ScoringGroup1", quizSlide.Scoring.ScoringGroup);
			Assert.AreEqual(10, quizSlide.Scoring.MaxTriesCount);
			Assert.AreEqual(true, quizSlide.Scoring.ManualChecking);
		}

		[Test]
		public void LoadSlideWithWrongBlockShouldFail()
		{
			Assert.Catch<CourseLoadingException>(() => LoadSlideFromXmlFile("SlideWithWrongBlock.xml"));
		}

		[Test]
		public void LoadQuizWithSomeBlocks()
		{
			var quizSlide = (QuizSlide)LoadSlideFromXmlFile("QuizWithSomeBlocks.xml");

			Assert.AreEqual(3, quizSlide.Blocks.Length);
			Assert.AreEqual(5, quizSlide.MaxScore);
			Assert.AreEqual("Внимание, вопрос!", ((AbstractQuestionBlock)quizSlide.Blocks[0]).Text);
			CollectionAssert.AreEqual(new[] { new RegexInfo { Pattern = "\\d+" } }, ((FillInBlock)quizSlide.Blocks[0]).Regexes);
		}

		[Test]
		public void LoadEmptyExerciseShouldFail()
		{
			Assert.Catch<CourseLoadingException>(() => LoadSlideFromXmlFile("EmptyExercise.xml"));
		}

		[Test]
		public void LoadExerciseWithZeroPoints()
		{
			var slide = (ExerciseSlide)LoadSlideFromXmlFile("ExerciseWithZeroPoints.xml");

			Assert.AreEqual(0, slide.MaxScore);
		}

		[Test]
		public void LoadExerciseWithDefaultScore()
		{
			var slide = (ExerciseSlide)LoadSlideFromXmlFile("ExerciseWithDefaultScore.xml");

			Assert.AreEqual(5, slide.Scoring.PassedTestsScore);
			Assert.AreEqual(typeof(SingleFileExerciseBlock), slide.Blocks[0].GetType());
			Assert.AreEqual("exercise self checkup", (slide.Blocks[0] as SingleFileExerciseBlock).Checkups[0]);
			Assert.AreEqual(typeof(SelfCheckupsBlock), slide.Blocks[1].GetType());
		}

		[Test]
		public void LoadSlideWithHtmlBlock()
		{
			var slide = LoadSlideFromXmlFile("SlideWithHtmlBlock.xml");

			Assert.IsAssignableFrom<HtmlBlock>(slide.Blocks[0]);

			var firstBlock = (HtmlBlock)slide.Blocks[0];
			Assert.IsTrue(firstBlock.GetContent("").Contains("Abracadabra"));
			Assert.IsTrue(firstBlock.GetContent("").Contains("<br />"));
			Assert.IsTrue(firstBlock.GetContent("").Contains("Second text"));
		}

		[Test]
		public void LoadEmptyFlashcardSlide()
		{
			var slide = LoadSlideFromXmlFile("EmptyFlashcardsSlide.xml");
			Assert.AreEqual(slide.GetType(), typeof(FlashcardSlide));
		}

		[Test]
		public void LoadSimpleFlashcardSlideWithMarkdownBlocks()
		{
			var slide = LoadSlideFromXmlFile("SimpleFlashcardSlide.xml");
			Assert.AreEqual(slide.Id, Guid.Parse("a7a73125-2434-4b5d-9f31-4ef687fc8bcc"));
		}

		[Test]
		public void LoadSlideWithSpoilerBlock()
		{
			var slide = LoadSlideFromXmlFile("SlideWithSpoilerBlock.xml");

			Assert.IsAssignableFrom<SpoilerBlock>(slide.Blocks[0]);

			var firstBlock = (SpoilerBlock)slide.Blocks[0];
			Assert.AreEqual(2, firstBlock.Blocks.Length);
			Assert.AreEqual("Spoiler", firstBlock.Text);
			Assert.IsAssignableFrom<CodeBlock>(firstBlock.Blocks[0]);
			Assert.IsAssignableFrom<YoutubeBlock>(firstBlock.Blocks[1]);
		}

		[Test]
		public void LoadSlideWithNoteInMarkdown()
		{
			var slide = LoadSlideFromXmlFile("SlideWithNoteInMarkdown.xml");
			Assert.AreEqual(true, slide.Blocks[0].Hide);
		}
	}
}