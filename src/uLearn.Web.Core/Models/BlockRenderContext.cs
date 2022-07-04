using Database.Models;
using JetBrains.Annotations;
using Ulearn.Core.Courses;
using Ulearn.Core.Courses.Slides;
using Ulearn.Core.Courses.Slides.Exercises;
using Ulearn.Core.Courses.Slides.Quizzes;

namespace uLearn.Web.Core.Models;

public class BlockRenderContext
{
	public Course Course { get; private set; }
	public Slide Slide { get; private set; }
	public string BaseUrlApi { get; private set; }
	public string BaseUrlWeb { get; private set; }
	public dynamic[] BlockData { get; private set; }
	public bool IsGuest { get; set; }
	public bool IsLti { get; set; }
	public AbstractManualSlideChecking ManualChecking { get; set; }
	public int ManualCheckingsLeftInQueue { get; set; }
	public bool CanUserFillQuiz { get; set; }
	public bool RevealHidden { get; private set; }
	public bool Autoplay { get; private set; }
	public bool IsManualCheckingReadonly { get; private set; }
	public bool DefaultProhibitFurtherReview { get; set; }
	public Dictionary<string, int> UserScores { get; }

	/* GroupsIds != null if instructor filtered users by group and see their submissions */
	public List<string> GroupsIds { get; set; }

	/* User's version of slide, i.e. for exercises */
	public int? VersionId { get; set; }

	public BlockRenderContext(Course course, Slide slide, string baseUrlApi, string baseUrlWeb, dynamic[] blockData,
		bool isGuest = false, bool revealHidden = false, AbstractManualSlideChecking manualChecking = null,
		int manualCheckingsLeftInQueue = 0, bool canUserFillQuiz = false, List<string> groupsIds = null, bool isLti = false, bool autoplay = false,
		bool isManualCheckingReadonly = false, bool defaultProhibitFurtherReview = true, Dictionary<string, int> userScores = null)
	{
		if (blockData.Length != slide.Blocks.Length)
			throw new ArgumentException("BlockRenderContext(): BlockData.Length should be slide.Blocks.Length");
		Course = course;
		Slide = slide;
		BaseUrlApi = baseUrlApi;
		BaseUrlWeb = baseUrlWeb;
		BlockData = blockData;
		IsGuest = isGuest;
		RevealHidden = revealHidden;
		ManualChecking = manualChecking;
		ManualCheckingsLeftInQueue = manualCheckingsLeftInQueue;
		CanUserFillQuiz = canUserFillQuiz;
		GroupsIds = groupsIds;
		IsLti = isLti;
		Autoplay = autoplay;
		IsManualCheckingReadonly = isManualCheckingReadonly;
		DefaultProhibitFurtherReview = defaultProhibitFurtherReview;
		UserScores = userScores ?? new Dictionary<string, int>();
	}

	[NotNull]
	public dynamic GetBlockData(SlideBlock block)
	{
		var index = Array.IndexOf(Slide.Blocks, block);
		if (index < 0)
			throw new ArgumentException("No block " + block + " in slide " + Slide);
		var data = BlockData[index];

		return data ?? GetDefaultBlockData(block);
	}

	private dynamic GetDefaultBlockData(SlideBlock block)
	{
		return Slide switch
		{
			QuizSlide => new QuizBlockData(new QuizModel(), 1, new QuizState(QuizStatus.ReadyToSend, 0, 0, Slide.MaxScore)),
			ExerciseSlide slide => new ExerciseBlockData(Course.Id, slide, false) { IsGuest = IsGuest, IsLti = IsLti },
			_ => throw new ArgumentException($"Internal error. Unknown slide type: {Slide.GetType()}. Should be {nameof(QuizSlide)} or {nameof(ExerciseSlide)}.")
		};
	}
}