namespace Ulearn.Core.Courses.Slides.Quizzes.Blocks
{
	public static class ChoiceItemCorrectnessExtensions
	{
		public static bool IsTrueOrMaybe(this ChoiceItemCorrectness correctness)
		{
			return correctness is ChoiceItemCorrectness.True or ChoiceItemCorrectness.Maybe;
		}
	}
}