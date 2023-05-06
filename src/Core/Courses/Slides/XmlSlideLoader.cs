using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using Ulearn.Common;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Flashcards;
using Ulearn.Core.Courses.Slides.Quizzes;

namespace Ulearn.Core.Courses.Slides
{
	public class XmlSlideLoader : ISlideLoader
	{
		/// <summary>
		///     Loads slide from XML file
		/// </summary>
		/// <returns>Slide, QuizSlide or ExerciseSlide object</returns>
		public Slide Load(SlideLoadingContext context)
		{
			var fileContent = context.SlideFile.ReadAllContent();

			return Load(fileContent, context);
		}

		public Slide Load(byte[] fileContent, SlideLoadingContext context)
		{
			var slideFile = context.SlideFile ?? new FileInfo("<internal>");

			var slideType = DetectSlideType(fileContent, slideFile.Name);

			var slide = (Slide)fileContent.DeserializeXml(slideType);

			slide.BuildUp(context);

			slide.Validate(context);

			return slide;
		}

		/// <summary>
		///     Automatically detects slide type by XML file.
		///     Slide file starts with &lt;slide%gt;, &lt;slide.quiz%gt; or &lt;slide.exercise%gt;.
		/// </summary>
		/// <param name="content">XML file with slide</param>
		/// <param name="filename">filename, just for error message formatting</param>
		/// <returns>typeof(Slide), typeof(QuizSlide) or typeof(ExerciseSlide)</returns>
		private static Type DetectSlideType(byte[] content, string filename)
		{
			var xmlDocument = new XmlDocument();
			try
			{
				using var stream = StaticRecyclableMemoryStreamManager.Manager.GetStream(content);
				xmlDocument.Load(stream);
			}
			catch (Exception e)
			{
				throw new CourseLoadingException($"Не могу прочитать слайд из файла {filename}. Возможно, там некорректный XML?\n{e.Message}", e);
			}

			if (xmlDocument.DocumentElement == null)
				throw new CourseLoadingException($"Не могу определить, что за слайд лежит в {filename}. Возможно, там некорректный XML или он не начинается с тега <slide>, <slide.quiz> или <slide.exercise>?");

			var knownTypes = new[] { typeof(PolygonExerciseSlide), typeof(Slide), typeof(QuizSlide), typeof(ExerciseSlide), typeof(FlashcardSlide) };
			foreach (var type in knownTypes)
				if (xmlDocument.DocumentElement.Name == type.GetCustomAttribute<XmlRootAttribute>()?.ElementName)
					return type;

			var allowedTags = string.Join(", ", knownTypes
				.Select(t => t.GetCustomAttribute<XmlRootAttribute>()?.ElementName)
				.Where(t => t is not null)
				.Select(t => $"<{t}>")
			);
			throw new CourseLoadingException(
				$"Не могу определить, что за слайд лежит в {filename}. " +
				$"Внешний тег должен быть одним из следующих: {allowedTags}."
			);
		}
	}
}