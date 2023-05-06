using System;
using System.Linq;
using System.Xml.Serialization;
using Ulearn.Core.Courses.Slides.Exercises.Blocks;
using Ulearn.Core.Model.Edx.EdxComponents;

namespace Ulearn.Core.Courses.Slides.Exercises
{
	[XmlRoot("slide.exercise", IsNullable = false, Namespace = "https://ulearn.me/schema/v2")]
	public class ExerciseSlide : Slide
	{
		[XmlElement("scoring")]
		public ExerciseScoringSettings Scoring { get; set; } = new()
		{
			PassedTestsScore = 5
		};

		protected override Type[] AllowedBlockTypes => base.AllowedBlockTypes
			.Concat(new[] { typeof(AbstractExerciseBlock) })
			.ToArray();

		public override bool ShouldBeSolved => true;

		public override string ScoringGroup => Scoring.ScoringGroup;

		public override int MaxScore => Scoring.PassedTestsScore + Scoring.CodeReviewAdditionalScore;

		[XmlIgnore]
		public AbstractExerciseBlock Exercise => (AbstractExerciseBlock)Blocks.Single(b => b is AbstractExerciseBlock);

		public ExerciseSlide()
		{
		}

		public ExerciseSlide(params SlideBlock[] blocks)
			: base(blocks)
		{
		}

		public override string ToString()
		{
			return $"ExerciseSlide: {Exercise}";
		}

		public override void BuildUp(SlideLoadingContext context)
		{
			if (string.IsNullOrEmpty(ScoringGroup))
				Scoring.ScoringGroup = context.CourseSettings.Scoring.DefaultScoringGroupForExercise;

			base.BuildUp(context);
		}

		public override void Validate(SlideLoadingContext context)
		{
			var scoringGroupsIds = context.CourseSettings.Scoring.Groups.Keys;
			var scoringGroup = Scoring.ScoringGroup;
			if (!string.IsNullOrEmpty(scoringGroup) && !scoringGroupsIds.Contains(scoringGroup))
				throw new CourseLoadingException(
					$"Неизвестная группа оценки у задания «{Title}»: {Scoring.ScoringGroup}\n" +
					"Возможные значения: " + string.Join(", ", scoringGroupsIds));

			var exerciseBlocksCount = Blocks.Count(b => b is AbstractExerciseBlock);
			if (exerciseBlocksCount == 0)
				throw new CourseLoadingException(
					$"Не найдено блоков с упражнениями (<exercise.file>, <exercise.csproj>, <exercise.universal> или <exercise.polygon>) в слайде «{Title}», " +
					"для которого использован внешний тег <slide.exercise>. Если вы хотите создать обычный слайд без упражнения, используйте тег <slide>");

			if (exerciseBlocksCount > 1)
				throw new CourseLoadingException(
					"Блок с упражнением (<exercise.file>, <exercise.csproj> или <exercise.universal>) может быть только один на слайде. " +
					$"Но на слайде «{Title}» найдено {exerciseBlocksCount} таких блока.");

			base.Validate(context);
		}

		public Component GetExerciseComponent(string displayName, Slide slide, int componentIndex, string launchUrl, string ltiId)
		{
			return new LtiComponent(displayName, slide.NormalizedGuid + componentIndex, launchUrl, ltiId, true, Scoring.PassedTestsScore, false);
		}
	}
}