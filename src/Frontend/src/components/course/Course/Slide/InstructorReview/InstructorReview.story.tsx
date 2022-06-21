import React from "react";
import InstructorReview from "./InstructorReview";
import { Language } from "src/consts/languages";
import type { Story } from "@storybook/react";
import {
	AutomaticExerciseCheckingProcessStatus,
	AutomaticExerciseCheckingResult,
	ReviewCommentResponse,
	ReviewInfo,
	SubmissionInfo,
} from "src/models/exercise";
import { mockFunc, returnPromiseAfterDelay } from "src/utils/storyMock";
import { getMockedShortUser, getMockedUser, instructor, loadUserToRedux, reduxStore, renderMd } from "src/storiesUtils";
import { AntiPlagiarismInfo, AntiPlagiarismStatusResponse, FavouriteReview, } from "src/models/instructor";
import { GroupInfo, } from "src/models/groups";
import { UserInfo } from "src/utils/courseRoles";
import { BlocksWrapper, StaticCode } from "../Blocks";
import { ApiFromRedux, PropsFromSlide } from "./InstructorReview.types";
import { SlideType } from "src/models/slide";
import { RootState } from "src/redux/reducers";
import { getDataIfLoaded, } from "src/redux";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import {
	reviewsAddCommentFailAction,
	reviewsAddCommentStartAction,
	reviewsAddCommentSuccessAction,
	reviewsAddFailAction,
	reviewsAddScoreFail,
	reviewsAddScoreStart,
	reviewsAddStartAction,
	reviewsAddSuccessAction,
	reviewsAssignBotReviewFail,
	reviewsAssignBotReviewStart,
	reviewsAssignBotReviewSuccess,
	reviewsDeleteCommentFail,
	reviewsDeleteCommentStart,
	reviewsDeleteCommentSuccess,
	reviewsDeleteFailAction,
	reviewsDeleteStartAction,
	reviewsDeleteSuccessAction,
	reviewsEditFailAction,
	reviewsEditStartAction,
	reviewsEditSuccessAction,
	submissionsEnableManualCheckingFailAction,
	submissionsEnableManualCheckingStartAction,
	submissionsLoadSuccessAction
} from "src/actions/submissions";
import {
	favouriteReviewsAddFailAction,
	favouriteReviewsAddStartAction,
	favouriteReviewsAddSuccessAction,
	favouriteReviewsDeleteFailAction,
	favouriteReviewsDeleteStartAction,
	favouriteReviewsDeleteSuccessAction,
	favouriteReviewsLoadFailAction,
	favouriteReviewsLoadStartAction,
	favouriteReviewsLoadSuccessAction
} from "src/actions/favouriteReviews";
import {
	antiplagiarimsStatusLoadFailAction,
	antiplagiarimsStatusLoadStartAction,
	antiplagiarimsStatusLoadSuccessAction,
	studentLoadFailAction,
	studentLoadStartAction,
	studentLoadSuccessAction,
	studentProhibitFurtherManualCheckingFailAction,
	studentProhibitFurtherManualCheckingStartAction
} from "src/actions/instructor";
import { ShortUserInfo } from "src/models/users";
import { clone } from "src/utils/jsonExtensions";
import { FavouriteReviewRedux } from "src/redux/instructor";
import { groupLoadFailAction, groupLoadStartAction, groupLoadSuccessAction } from "src/actions/groups";
import { assignBotReview, } from "src/api/submissions";
import { Button } from "ui";
import { MatchParams, WithRouter } from "src/models/router";
import { skipLoki } from "../../../Navigation/stroies.data";
import { mapStateToProps } from "./InstructorReview.redux";
import { withOldRouter } from "../../../../../utils/router";


const user: UserInfo = getMockedUser({
	...instructor,
	visibleName: 'Пользователь ДлиннаяФамилияКоторояМожетСломатьВерстку',
	lastName: 'ДлиннаяФамилияКоторояМожетСломатьВерстку',
	firstName: 'Пользователь',
	id: "0",
	avatarUrl: "",
	email: "user@email.com",
	login: 'Administrator of everything on ulearn.me',
});

const student: ShortUserInfo = getMockedShortUser({
	visibleName: 'Студент Студентовичниковогоропараболладвойкавкоде',
	lastName: 'Студентовичниковогоропараболладвойкавкоде',
	firstName: 'Студент',
	id: 'studentId',
	email: "user@email.com",
	login: 'superStudnet',
});

const extra = {
	suspicionLevel: 0,
	reviewId: 0,
};

const addIdToReview = (review: any): ReviewInfo => ({
	...review, id: extra.reviewId++,
});

const studentGroups: GroupInfo[] = [{
	id: 12,
	apiUrl: 'groupApi',
	isArchived: false,
	name: 'группа Екатеринбург АТ-666, 333 юг-запад Авеню Гейб',
	accesses: [],
	areYouStudent: false,
	canStudentsSeeGroupProgress: false,
	createTime: null,
	defaultProhibitFurtherReview: true,
	inviteHash: '',
	isInviteLinkEnabled: false,
	isManualCheckingEnabled: true,
	isManualCheckingEnabledForOldSolutions: false,
	owner: user,
	studentsCount: 20,
},
	{
		id: 13,
		apiUrl: 'groupApi',
		isArchived: true,
		name: 'АS-202, 235B',
		accesses: [],
		areYouStudent: false,
		canStudentsSeeGroupProgress: false,
		createTime: null,
		defaultProhibitFurtherReview: true,
		inviteHash: '',
		isInviteLinkEnabled: false,
		isManualCheckingEnabled: true,
		isManualCheckingEnabledForOldSolutions: false,
		owner: user,
		studentsCount: 20,
	}];

const favouriteReviews: FavouriteReviewRedux[] = [
	{ text: 'комментарий', renderedText: 'комментарий', isFavourite: true, },
	{
		text: '**bold** __italic__ ```code```',
		isFavourite: true,
	},
	{
		text: 'Ой! Наш робот нашёл решения других студентов, подозрительно похожие на ваше. ' +
			'Так может быть, если вы позаимствовали части программы, взяли их из открытых источников либо сами поделились своим кодом. ' +
			'Выполняйте задания самостоятельно.',
	},
	{
		text: 'Так делать не стоит из-за сложности в O(N^3). Есть более оптимизированные алгоритмы',
	},
].map((c, i) => ({ ...c, renderedText: renderMd(c.text), id: i }));

const submissions: SubmissionInfo[] = [
	{
		code: `\t\t\t\tif (course == null || tempCourse.LastUpdateTime < tempCourse.LoadingTime)
\t\t\t\t{
\t\t\t\t\tTryReloadCourse(courseId);
\t\t\t\t\tvar tempCoursesRepo = new TempCoursesRepo();
\t\t\t\t\ttempCoursesRepo.UpdateTempCourseLastUpdateTime(courseId);
\t\t\t\t\tcourseVersionFetchTime[courseId] = DateTime.Now;
\t\t\t\t} else if (tempCourse.LastUpdateTime > tempCourse.LoadingTime)
\t\t\t\t\tcourseVersionFetchTime[courseId] = DateTime.Now;
\t\t\t}
\t\t\tcatch (Exception ex)
\t\t\t{
\t\t\t\t\t\tTryReloadCourse(courseId);
\t\t\t\t\t\tvar tempCoursesRepo = new TempCoursesRepo();
\t\t\t\t\t\ttempCoursesRepo.UpdateTempCourseLastUpdateTime(courseId);
\t\t\t\t\t\tcourseVersionFetchTime[courseId] = DateTime.Now;
\t\t\t\t\t} else if (tempCourse.LastUpdateTime > tempCourse.LoadingTime)
\t\t\t\t\t\tcourseVersionFetchTime[courseId] = DateTime.Now;
\t\t\t\t}
\t\t\t}
\t\t\tcatch (Exception ex)
\t\t\t\treturn lastFetchTime > DateTime.Now.Subtract(tempCourseUpdateEvery);
\t\t\treturn false;
\t\t}
\t}
}
`,
		language: Language.cSharp,
		timestamp: '2020-04-06',
		manualCheckingPassed: false,
		manualCheckingReviews: [],
		automaticChecking: null,
	},
	{
		code: `\t\t\t\tif (course == null || tempCourse.LastUpdateTime < tempCourse.LoadingTime)
\t\t\t\t{
\t\t\t\t\tTryReloadCourse(courseId);
\t\t\t\t\tvar tempCoursesRepo = new TempCoursesRepo();
\t\t\t\t\ttempCoursesRepo.UpdateTempCourseLastUpdateTime(courseId);
\t\t\t\t\ttempCourseUpdateTime[courseId] = DateTime.Now;
\t\t\t\t} else if (tempCourse.LastUpdateTime > tempCourse.LoadingTime)
\t\t\t\t\ttempCourseUpdateTime[courseId] = DateTime.Now;
\t\t\t}
\t\t\tcatch (Exception ex)
\t\t\t{
\t\t\t\t\t\tTryReloadCourse(courseId);
\t\t\t\t\t\tvar tempCoursesRepo = new TempCoursesRepo();
\t\t\t\t\t\ttempCoursesRepo.UpdateTempCourseLastUpdateTime(courseId);
\t\t\t\t\t\ttempCourseUpdateTime[courseId] = DateTime.Now;
\t\t\t\t\t} else if (tempCourse.LastUpdateTime > tempCourse.LoadingTime)
\t\t\t\t\t\ttempCourseUpdateTime[courseId] = DateTime.Now;
\t\t\t\t}
\t\t\t}
\t\t\tcatch (Exception ex)
\t\t\t\treturn lastFetchTime > DateTime.Now.Subtract(tempCourseUpdateEvery);
\t\t\treturn false;
\t\t}

\t\tpublic bool IsTempCourse(string courseId)
\t\t{
\t\t\treturn GetTempCoursesWithCache().Any(c => string.Equals(c.CourseId, courseId, StringComparison.OrdinalIgnoreCase));
\t\t}
\t}
}`,
		language: Language.cSharp,
		timestamp: '2020-04-06',
		manualCheckingPassed: true,
		manualCheckingReviews: [],
		automaticChecking: null,
	},
	{
		code: 'void Main()\n' +
			'{\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tEnd\n' +
			'}',
		language: Language.cSharp,
		timestamp: '2020-04-06',
		manualCheckingPassed: false,
		manualCheckingReviews: [],
		automaticChecking: null,
	},
	{
		code: 'void Main()\n' +
			'{\n' +
			'\tint i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tint i = 0;\n' +
			'\tint i = 0;\n' +
			'\tint i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tint i = 0;\n' +
			'\tvar i = 0;\n' +
			'\tint i = 0;\n' +
			'\tint i = 0;\n' +
			'\tint i = 0;\n' +
			'\tvar i = 0;\n' +
			'}',
		language: Language.cSharp,
		timestamp: '2020-04-06',
		manualCheckingPassed: true,
		manualCheckingReviews: [
			{
				author: user,
				startLine: 2,
				startPosition: 0,
				finishLine: 2,
				finishPosition: 100,
				comment: "var",
				renderedComment: "var",
				addingTime: "2020-08-04 23:04",
				comments: [],
			},
			{
				author: user,
				startLine: 5,
				startPosition: 0,
				finishLine: 5,
				finishPosition: 100,
				comment: "var 1",
				renderedComment: "var 1",
				addingTime: "2020-08-04 23:04",
				comments: [],
			},
			{
				author: user,
				startLine: 6,
				startPosition: 0,
				finishLine: 6,
				finishPosition: 100,
				comment: "var 2",
				renderedComment: "var 2",
				addingTime: "2020-08-04 23:04",
				comments: [],
			},
			{
				author: user,
				startLine: 7,
				startPosition: 0,
				finishLine: 7,
				finishPosition: 100,
				comment: "var 3",
				renderedComment: "var 3",
				addingTime: "2020-08-04 23:04",
				comments: [],
			},
			{
				author: user,
				startLine: 0,
				startPosition: 0,
				finishLine: 0,
				finishPosition: 100,
				comment: "var ВЕЗДЕ",
				renderedComment: "var ВЕЗДЕ",
				addingTime: "2020-08-04 23:04",
				comments: [],
			},
		],
		automaticChecking: null,
	},
	{
		code: 'void Main()\n' +
			'{\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tConsole.WriteLine("Coding is there tatat");\n' +
			'\tEnd\n' +
			'}',
		language: Language.cSharp,
		timestamp: '2020-04-06',
		manualCheckingPassed: false,
		manualCheckingReviews: [],
		automaticChecking: null,
	},
	{
		code: 'void Main()\n' +
			'{\n' +
			'\tConsole.WriteLine("Coding is here right now tarara");\n' +
			'}',
		language: Language.java,
		timestamp: '2020-04-05',
		manualCheckingPassed: false,
		manualCheckingReviews: [],
		automaticChecking: null,
	},
	{
		code: 'void Main()\n' +
			'{\n' +
			'\tConsole.WriteLine("Coding is here right now tarasfaasfsaf PASSED 1ra");\n' +
			'\tConsole.WriteLine("Coding is here right now tarasfaasfsaf PASSED 2ra");\n' +
			'\tConsole.WriteLine("Coding is here right now tarasfaasfsaf PASSED 3ra");\n' +
			'\tEnd\n' +
			'}',
		language: Language.java,
		timestamp: '2020-04-01',
		manualCheckingPassed: true,
		manualCheckingReviews: [
			{
				author: user,
				startLine: 0,
				startPosition: 0,
				finishLine: 1,
				finishPosition: 1,
				comment: "Это ты зряяя",
				renderedComment: "Это ты зряяя",
				addingTime: "2020-08-04 23:04",
				comments: [],
			}
		],
		automaticChecking: {
			processStatus: AutomaticExerciseCheckingProcessStatus.Done,
			result: AutomaticExerciseCheckingResult.RightAnswer,
			output: null,
			checkerLogs: null,
			reviews: [
				{
					author: null,
					startLine: 2,
					startPosition: 0,
					finishLine: 2,
					finishPosition: 1,
					comment: "Робот не доволен",
					renderedComment: "Робот не доволен",
					addingTime: null,
					comments: [],
				}
			]
		},
	},
]
	.map((c, i) => ({
		...c,
		id: i,
		automaticChecking: c.automaticChecking
			? {
				...c.automaticChecking,
				reviews: c.automaticChecking?.reviews.map(addIdToReview) || null,
			}
			: null,
		manualChecking: i > 0
			? {
				reviews: c.manualCheckingReviews.map(addIdToReview),
				percent: i > 0 && i % 2 === 0 ? i * 10 : null,
			}
			: null,
	}));

const loadingTimes = {
	student: 100,
	groups: 100,
	favouriteReviews: 100,
	addReview: 100,
	editReview: 100,
	deleteReview: 100,
	toggleReviewFavourite: 100,
	scoreSubmit: 100,
	getPlagiarismStatus: 100,
	submissions: 100,
	prohibitFurtherReview: 100,
	enableManualChecking: 100,
	loadDeadLines: 100,
};

const getNextAPStatus = () => {
	const rnd = Math.random();
	let suspicionLevel: AntiPlagiarismInfo['suspicionLevel'] = 'none';
	let suspicionCount = 0;

	if(extra.suspicionLevel === 1) {
		suspicionCount = Math.ceil(rnd * 10);
		suspicionLevel = 'faint';
	}
	if(extra.suspicionLevel === 2) {
		suspicionCount = Math.ceil(rnd * 50);
		suspicionLevel = "strong";
	}
	extra.suspicionLevel++;
	extra.suspicionLevel %= 3;
	const info: AntiPlagiarismStatusResponse = {
		suspiciousAuthorsCount: suspicionCount,
		suspicionLevel,
		status: 'checked'
	};
	return info;
};

const mapDispatchToProps = (dispatch: Dispatch): ApiFromRedux => {
	return {
		loadDeadLines: (courseId: string, studentId: string) => {
			return returnPromiseAfterDelay(loadingTimes.loadDeadLines, { deadLines: [] });
		},
		addReview: (
			submissionId: number,
			text: string,
			startLine: number, startPosition: number,
			finishLine: number, finishPosition: number
		) => {
			const review: ReviewInfo = {
				id: extra.reviewId++,
				author: user || null,
				startLine,
				startPosition,
				finishLine,
				finishPosition,
				comment: text,
				renderedComment: renderMd(text),
				addingTime: new Date().toDateString(),
				comments: [],
			};
			dispatch(reviewsAddStartAction(submissionId, text, startLine, startPosition, finishLine, finishPosition));
			return returnPromiseAfterDelay(loadingTimes.addReview, review)
				.then(review => {
					dispatch(reviewsAddSuccessAction(submissionId, review,));
					return review;
				})
				.catch(error => {
					dispatch(reviewsAddFailAction(submissionId, error,));
					return error;
				});
		},

		deleteReview: (submissionId, reviewId, isBotReview) => {
			dispatch(reviewsDeleteStartAction(submissionId, reviewId, isBotReview));
			return returnPromiseAfterDelay(loadingTimes.deleteReview, Promise.resolve())
				.then(review => {
					dispatch(reviewsDeleteSuccessAction(submissionId, reviewId, isBotReview));
					return review;
				})
				.catch(error => {
					dispatch(reviewsDeleteFailAction(submissionId, reviewId, error, isBotReview));
					return error;
				});
		},

		addReviewComment: (submissionId: number, reviewId: number, text: string) => {
			dispatch(reviewsAddCommentStartAction(submissionId, reviewId, text));
			const comment: ReviewCommentResponse = {
				id: extra.reviewId++,
				text,
				renderedText: renderMd(text),
				publishTime: new Date().toDateString(),
				author: user,
			};
			return returnPromiseAfterDelay(loadingTimes.addReview, comment,)
				.then(r => {
					dispatch(reviewsAddCommentSuccessAction(submissionId, reviewId, r));
					return r;
				})
				.catch(err => {
					dispatch(reviewsAddCommentFailAction(submissionId, reviewId, text, err));
					return err;
				});
		},
		deleteReviewComment: (submissionId: number, reviewId: number, commentId: number) => {
			dispatch(reviewsDeleteCommentStart(submissionId, reviewId, commentId));

			return returnPromiseAfterDelay(loadingTimes.deleteReview, Promise.resolve())
				.then(r => {
					dispatch(reviewsDeleteCommentSuccess(submissionId, reviewId, commentId));
					return r;
				})
				.catch(err => {
					dispatch(reviewsDeleteCommentFail(submissionId, reviewId, commentId, err));
					return err;
				});
		},

		addFavouriteReview: (courseId: string, slideId: string, text: string) => {
			dispatch(favouriteReviewsAddStartAction(courseId, slideId, text));
			const favouriteReview: FavouriteReview = {
				id: extra.reviewId++,
				renderedText: renderMd(text),
				text,
			};
			return returnPromiseAfterDelay(loadingTimes.toggleReviewFavourite, favouriteReview)
				.then(favouriteReview => {
					dispatch(favouriteReviewsAddSuccessAction(courseId, slideId, favouriteReview,));
					return favouriteReview;
				})
				.catch(error => {
					dispatch(favouriteReviewsAddFailAction(courseId, slideId, error,));
					return error;
				});
		},
		deleteFavouriteReview: (courseId: string, slideId: string, favouriteReviewId: number) => {
			dispatch(favouriteReviewsDeleteStartAction(courseId, slideId, favouriteReviewId));
			return returnPromiseAfterDelay(loadingTimes.toggleReviewFavourite)
				.then(() => {
					dispatch(favouriteReviewsDeleteSuccessAction(courseId, slideId, favouriteReviewId,));
				})
				.catch(error => {
					dispatch(favouriteReviewsDeleteFailAction(courseId, slideId, favouriteReviewId, error,));
					return error;
				});
		},
		editReviewOrComment: (submissionId: number, reviewId: number, parentReviewId: number | undefined, text: string,
			oldText: string,
		) => {
			dispatch(reviewsEditStartAction(submissionId, reviewId, parentReviewId, text));
			const store = reduxStore.getState() as RootState;
			const reviews = store.submissions.reviewsBySubmissionId[submissionId]?.manualCheckingReviews;
			const reviewOrComment = parentReviewId
				? reviews?.find(r => r.id === parentReviewId)?.comments.find(
					c => (c as ReviewCommentResponse).id === reviewId)
				: reviews?.find(r => r.id === reviewId);

			if(!reviewOrComment) {
				return Promise.reject();
			}
			const review = reviewOrComment as ReviewInfo;
			if(review.comment) {
				review.comment = text;
				review.renderedComment = renderMd(text);
			}
			const comment = reviewOrComment as ReviewCommentResponse;
			if(comment.text) {
				comment.text = text;
				comment.renderedText = renderMd(text);
			}

			return returnPromiseAfterDelay(loadingTimes.editReview,
				reviewOrComment as ReviewCommentResponse | ReviewInfo)
				.then(r => {
					dispatch(reviewsEditSuccessAction(submissionId, reviewId, parentReviewId, r));
					return r;
				})
				.catch(err => {
					dispatch(reviewsEditFailAction(submissionId, reviewId, parentReviewId, oldText, err));
					return err;
				});
		},

		prohibitFurtherReview: (courseId: string, slideId: string, userId: string, prohibit: boolean) => {
			dispatch(studentProhibitFurtherManualCheckingStartAction(courseId, slideId, userId, prohibit,));
			return returnPromiseAfterDelay(loadingTimes.prohibitFurtherReview, Promise.resolve())
				.catch(error => {
					dispatch(
						studentProhibitFurtherManualCheckingFailAction(courseId, slideId, userId, prohibit, error));
					return error;
				});
		},
		onScoreSubmit: (submissionId: number, percent: number, oldPercent: number | null,) => {
			dispatch(reviewsAddScoreStart(submissionId, percent));
			return returnPromiseAfterDelay(loadingTimes.scoreSubmit, Promise.resolve())
				.catch(err => {
					dispatch(reviewsAddScoreFail(submissionId, oldPercent, err));
					return err;
				});
		},

		getStudentInfo: (studentId: string,) => {
			dispatch(studentLoadStartAction(studentId,));
			return returnPromiseAfterDelay(loadingTimes.student, student)
				.then(user => {
					if(user) {
						dispatch(studentLoadSuccessAction(user));
						return user;
					} else {
						throw new Error('User not found, or you don\'t have permission');
					}
				})
				.catch(error => {
					dispatch(studentLoadFailAction(studentId, error));
					return error;
				});
		},
		getAntiPlagiarismStatus: (courseId: string, submissionId: number,) => {
			dispatch(antiplagiarimsStatusLoadStartAction(submissionId,));
			return returnPromiseAfterDelay(loadingTimes.getPlagiarismStatus, getNextAPStatus())
				.then(json => {
					dispatch(antiplagiarimsStatusLoadSuccessAction(submissionId, json,));
					return json;
				})
				.catch(error => {
					dispatch(antiplagiarimsStatusLoadFailAction(submissionId, error,));
					return error;
				});
		},
		getFavouriteReviews: (courseId: string, slideId: string,) => {
			dispatch(favouriteReviewsLoadStartAction(courseId, slideId,));
			const state = reduxStore.getState() as RootState;
			const fr = getDataIfLoaded(state.favouriteReviews.favouritesReviewsByCourseIdBySlideId[courseId]?.[slideId])
				|| clone(favouriteReviews);
			return returnPromiseAfterDelay(loadingTimes.favouriteReviews, fr)
				.then(favouriteReviews => {
					dispatch(favouriteReviewsLoadSuccessAction(courseId, slideId, {
						favouriteReviews: favouriteReviews.filter(f => !f.isFavourite),
						userFavouriteReviews: favouriteReviews.filter(f => f.isFavourite),
						lastUsedReviews: [],
					},));
					return favouriteReviews;
				})
				.catch(error => {
					dispatch(favouriteReviewsLoadFailAction(courseId, slideId, error,));
					return error;
				});
		},
		getStudentGroups: (courseId: string, userId: string,) => {
			dispatch(groupLoadStartAction(userId));
			return returnPromiseAfterDelay(loadingTimes.groups, { groups: studentGroups })
				.then(json => {
					dispatch(groupLoadSuccessAction(userId, json));
					return json;
				})
				.catch(error => {
					dispatch(groupLoadFailAction(userId, error));
					return error;
				});
		},
		enableManualChecking: (submissionId: number,) => {
			dispatch(submissionsEnableManualCheckingStartAction(submissionId));
			args.slideContext.slideInfo.query = { ...args.slideContext.slideInfo.query, submissionId, };
			return returnPromiseAfterDelay(loadingTimes.enableManualChecking, Promise.resolve())
				.catch(error => {
					dispatch(submissionsEnableManualCheckingFailAction(submissionId, error,));
					return error;
				});
		},

		assignBotReview: (submissionId, review) => {
			dispatch(reviewsAssignBotReviewStart(submissionId, review.id));
			return assignBotReview(submissionId, review)
				.then(([review, deletedResponse]) => {
					dispatch(reviewsAssignBotReviewSuccess(submissionId, review.id,
						{ ...review, id: extra.reviewId++, author: user }));
					return review;
				})
				.catch(err => {
					dispatch(reviewsAssignBotReviewFail(submissionId, review.id, err));
					return err;
				});
		},
		setNextSubmissionButtonDisabled: mockFunc,
	};
};

const Connected = connect(mapStateToProps, mapDispatchToProps)(withOldRouter(InstructorReview));

const Template: Story<PropsFromSlide & WithRouter> = (args: PropsFromSlide & WithRouter) => {
	if(!reduxStore.getState().account.isAuthenticated) {
		loadUserToRedux(user, courseId);
	}

	return (
		<>
			<Button use={ 'primary' } onClick={ changeAPStatus }>
				Change antiPlagiat status
			</Button>
			<Button use={ 'primary' } onClick={ loadSubmissions }>
				Load submissions
			</Button>
			<Connected { ...args }/>
		</>);

	function changeAPStatus(): void {
		reduxStore.dispatch(antiplagiarimsStatusLoadSuccessAction(submissions[0].id, getNextAPStatus(),));
	}

	function loadSubmissions(): void {
		reduxStore.dispatch(submissionsLoadSuccessAction(student.id, courseId, slideId, {
			submissions,
			prohibitFurtherManualChecking: true,
		}));
	}
};

const courseId = 'basic';
const slideId = 'slide';
const args: PropsFromSlide = {
	slideContext: {
		slideId,
		courseId,
		title: 'Angry Birds',
		unitId:"unit",
		slideInfo: {
			slideId,
			courseId,
			deadLineInfo: null,
			slideType: SlideType.Exercise,
			isLti: false,
			isReview: true,
			isNavigationVisible: false,
			query: {
				slideId: null,
				queueSlideId: null,
				submissionId: 1,
				isLti: false,
				userId: student.id,
				done: false,
				group: null
			},
		}
	},
	expectedOutput: null,
	authorSolution: <BlocksWrapper>
		<StaticCode
			language={ Language.cSharp }
			code={ 'void Main()\n{\n\tConsole.WriteLine("Coding is awesome");\n}' }/>
	</BlocksWrapper>,
	formulation:
		<BlocksWrapper>
			<p>Вам надо сделать кое-что, сами гадайте что и как, но сделайте обязательно</p>
		</BlocksWrapper>,
};

export const Default = Template.bind({});
Default.args = args;

export default {
	title: 'Exercise/InstructorReview',
	...skipLoki,
};
