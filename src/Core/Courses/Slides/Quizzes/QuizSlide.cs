using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Ulearn.Common.Extensions;
using Ulearn.Core.Courses.Slides.Blocks;
using Ulearn.Core.Courses.Slides.Quizzes.Blocks;

namespace Ulearn.Core.Courses.Slides.Quizzes
{
	[XmlRoot("slide.quiz", IsNullable = false, Namespace = "https://ulearn.me/schema/v2")]
	public class QuizSlide : Slide
	{
		private static readonly Regex questionIdRegex = new("^[0-9a-z_]+$", RegexOptions.IgnoreCase);

		[XmlElement("scoring")]
		public QuizScoringSettings Scoring { get; set; } = new()
		{
			ManualChecking = false,
			MaxTriesCount = 2
		};

		protected override Type[] AllowedBlockTypes => base.AllowedBlockTypes
			.Where(b => b != typeof(SelfCheckupsBlock))
			.Concat(new[] { typeof(AbstractQuestionBlock) })
			.ToArray();

		public override bool ShouldBeSolved => true;

		public override string ScoringGroup => Scoring.ScoringGroup;

		public override int MaxScore => (Blocks ?? Array.Empty<SlideBlock>()).OfType<AbstractQuestionBlock>().Sum(b => b.MaxScore);

		[XmlIgnore]
		public int MaxTriesCount => Scoring.MaxTriesCount;

		[XmlIgnore]
		public bool ManualChecking => Scoring.ManualChecking;

		public string QuizNormalizedXml => this.XmlSerialize(true);

		public override void Validate(SlideLoadingContext context)
		{
			var scoringGroupsIds = context.CourseSettings.Scoring.Groups.Keys;
			if (!string.IsNullOrEmpty(ScoringGroup) && !scoringGroupsIds.Contains(ScoringGroup))
				throw new CourseLoadingException(
					$"Неизвестная группа оценки у теста «{Title}»: {ScoringGroup}\n" +
					"Возможные значения: " + string.Join(", ", scoringGroupsIds));

			var questionBlocks = Blocks.OfType<AbstractQuestionBlock>().ToList();
			var questionIds = questionBlocks.Select(b => b.Id).ToImmutableHashSet();
			var questionIdsCount = questionIds.ToDictionary(id => id, id => questionBlocks.Count(b => b.Id == id));
			if (questionIdsCount.Values.Any(count => count > 1))
			{
				var repeatedQuestionId = questionIdsCount.First(kvp => kvp.Value > 1).Key;
				throw new CourseLoadingException(
					$"Идентификатор «{repeatedQuestionId}» в тесте «{Title}» принадлежит как минимум двум различным вопросам. " +
					"Идентификаторы вопросов (параметры id) должны быть уникальны.");
			}

			if (!questionIds.All(id => questionIdRegex.IsMatch(id)))
			{
				var badQuestionId = questionIds.First(id => !questionIdRegex.IsMatch(id));
				throw new CourseLoadingException(
					$"Идентификатор «{badQuestionId}» в тесте «{Title}» не удовлетворяет формату. " +
					"Идентификаторы вопросов (параметры id) должны состоять из латинских букв, цифр и символа подчёркивания. Идентификатор не может быть пустым.");
			}

			base.Validate(context);
		}

		public override void BuildUp(SlideLoadingContext context)
		{
			if (string.IsNullOrEmpty(ScoringGroup))
				Scoring.ScoringGroup = context.CourseSettings.Scoring.DefaultScoringGroupForQuiz;

			base.BuildUp(context);

			InitQuestionIndices();
		}

		private void InitQuestionIndices()
		{
			var index = 1;
			foreach (var b in Blocks.OfType<AbstractQuestionBlock>())
				b.QuestionIndex = index++;
		}

		public bool HasEqualStructureWith(QuizSlide other)
		{
			if (Blocks.Length != other.Blocks.Length)
				return false;
			for (var blockIdx = 0; blockIdx < Blocks.Length; blockIdx++)
			{
				var block = Blocks[blockIdx];
				var otherBlock = other.Blocks[blockIdx];
				var questionBlock = block as AbstractQuestionBlock;
				var otherQuestionBlock = otherBlock as AbstractQuestionBlock;
				/* Ignore non-question blocks */
				if (questionBlock == null)
					continue;
				/* If our block is question, block in other slide must be question with the same Id */
				if (otherQuestionBlock == null || questionBlock.Id != otherQuestionBlock.Id)
					return false;

				if (!questionBlock.HasEqualStructureWith(otherQuestionBlock))
					return false;
			}

			return true;
		}
	}
}