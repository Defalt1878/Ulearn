import { connect } from "react-redux";

import InstructorReview from "./InstructorReview";

import { buildUserInfo } from "src/utils/courseRoles";

import { Dispatch } from "redux";
import { RootState } from "src/redux/reducers";
import api from "src/api";
import { getDataIfLoaded, ReduxData } from "src/redux";
import { ShortGroupInfo } from "src/models/comments";
import { ApiFromRedux, PropsFromRedux } from "./InstructorReview.types";
import { AutomaticExerciseCheckingResult, SubmissionInfo } from "src/models/exercise";
import { SlideContext } from "../Slide.types";
import { getSubmissionsWithReviews } from "../../CourseUtils";
import { setNextSubmissionButtonDisabled } from "src/actions/submissions";
import { DeadLineInfo } from "src/models/deadLines";
import { withNavigate } from "src/utils/router";

interface Props {
	slideContext: SlideContext;
}

export const mapStateToProps = (
	state: RootState,
	{ slideContext: { courseId, slideId, slideInfo, } }: Props
): PropsFromRedux => {
	const studentId = slideInfo.query.userId;

	if(!studentId) {
		throw new Error('User id was not provided');
	}

	const student = getDataIfLoaded(state.instructor.studentsById[studentId]);

	const studentSubmissions: SubmissionInfo[] | undefined =
		getSubmissionsWithReviews(
			courseId,
			slideId,
			studentId,
			state.submissions.submissionsIdsByCourseIdBySlideIdByUserId,
			state.submissions.submissionsById,
			state.submissions.reviewsBySubmissionId
		)?.filter((s, index) => index === 0
			|| !s.automaticChecking
			|| s.automaticChecking.result === AutomaticExerciseCheckingResult.RightAnswer
		);
	const submissionToReview = studentSubmissions && studentSubmissions
		.find(s =>
			(!s.automaticChecking ||
				s.automaticChecking?.result === AutomaticExerciseCheckingResult.RightAnswer)
			&& s.manualChecking);
	const lastReviewedSubmission = studentSubmissions && studentSubmissions
		.find(s =>
			(!s.automaticChecking ||
				s.automaticChecking?.result === AutomaticExerciseCheckingResult.RightAnswer)
			&& s.manualChecking
			&& s.manualChecking.percent !== null);
	const curScore = submissionToReview?.manualChecking?.percent ?? null;
	const prevScore = lastReviewedSubmission?.manualChecking?.percent ?? null;

	let studentGroups: ShortGroupInfo[] | undefined;
	const studentGroupsIds = getDataIfLoaded(state.groups.groupsIdsByUserId[studentId]);
	const reduxGroups = studentGroupsIds
		?.map(groupId => getDataIfLoaded(state.groups.groupById[groupId]));
	if(reduxGroups && reduxGroups.every(g => g !== undefined)) {
		studentGroups = reduxGroups.map(g => ({ ...g, courseId, })) as ShortGroupInfo[];
	}
	const favouriteReviews = getDataIfLoaded(
		state.favouriteReviews.favouritesReviewsByCourseIdBySlideId[courseId]?.[slideId]);
	const lastUsedReviews = getDataIfLoaded(
		state.favouriteReviews.lastUsedReviewsByCourseIdBySlideId[courseId]?.[slideId]);

	const antiPlagiarismStatus = studentSubmissions &&
		state.instructor.antiPlagiarismStatusBySubmissionId[studentSubmissions[0].id];
	const antiPlagiarismStatusRedux = antiPlagiarismStatus as ReduxData;

	const prohibitFurtherManualChecking = state.instructor
		.prohibitFurtherManualCheckingByCourseIdBySlideIdByUserId[courseId]
		?.[slideId]
		?.[studentId] || false;

	let deadLines: DeadLineInfo[] | undefined = undefined;
	const courseStudents = getDataIfLoaded(state.instructor.deadLinesByCourseIdByStudentId[courseId]);
	if(courseStudents) {
		deadLines = getDataIfLoaded(courseStudents[studentId]);
	}

	return {
		user: buildUserInfo(state.account, courseId,),
		favouriteReviews,
		lastUsedReviews,

		studentGroups,
		student,

		studentSubmissions,
		curScore,
		prevScore,
		lastCheckedSubmissionId: lastReviewedSubmission?.id,
		lastManualCheckingSubmissionId: submissionToReview?.id,

		antiPlagiarismStatus: getDataIfLoaded(antiPlagiarismStatus),
		antiPlagiarismStatusError: !!antiPlagiarismStatusRedux?.error,
		antiPlagiarismStatusLoading: !!antiPlagiarismStatusRedux?.isLoading,

		prohibitFurtherManualChecking,
		deadLines,
	};
};

const mapDispatchToProps = (dispatch: Dispatch): ApiFromRedux => {
	return {
		setNextSubmissionButtonDisabled: (disabled: boolean) => dispatch(setNextSubmissionButtonDisabled(disabled)),

		addReview: (
			submissionId: number,
			text: string,
			startLine: number, startPosition: number,
			finishLine: number, finishPosition: number
		) =>
			api.submissions.redux
				.addReview(submissionId, text, startLine, startPosition, finishLine, finishPosition)(dispatch),
		deleteReview: (submissionId, reviewId, isBotReview) =>
			api.submissions.redux.deleteReview(submissionId, reviewId, isBotReview)(dispatch),

		addReviewComment: (submissionId: number, reviewId: number, comment: string) =>
			api.submissions.redux.addReviewComment(submissionId, reviewId, comment)(dispatch),
		deleteReviewComment: (submissionId: number, reviewId: number, commentId: number) =>
			api.submissions.redux.deleteReviewComment(submissionId, reviewId, commentId)(dispatch),


		addFavouriteReview: (courseId: string, slideId: string, text: string) =>
			api.favouriteReviews.redux.addFavouriteReview(courseId, slideId, text)(dispatch),
		deleteFavouriteReview: (courseId: string, slideId: string, favouriteReviewId: number) =>
			api.favouriteReviews.redux.deleteFavouriteReview(courseId, slideId, favouriteReviewId)(dispatch),
		editReviewOrComment: (submissionId: number, reviewId: number, parentReviewId: number | undefined, text: string,
			oldText: string,
		) => api.submissions.redux.editReviewOrComment(submissionId, reviewId, parentReviewId, text, oldText,)(
			dispatch),

		prohibitFurtherReview: (courseId: string, slideId: string, userId: string, prohibit: boolean) =>
			api.instructor.redux.prohibitFurtherManualChecking(courseId, slideId, userId, prohibit)(dispatch),
		onScoreSubmit: (submissionId: number, score: number, oldScore: number | null,) =>
			api.submissions.redux.submitReviewScore(submissionId, score, oldScore)(dispatch),

		getStudentInfo: (studentId: string,) =>
			api.instructor.redux.getStudentInfo(studentId)(dispatch),
		getAntiPlagiarismStatus: (courseId: string, submissionId: number,) =>
			api.instructor.redux.getAntiPlagiarismStatus(courseId, submissionId)(dispatch),
		getFavouriteReviews: (courseId: string, slideId: string,) =>
			api.favouriteReviews.redux.getFavouriteReviews(courseId, slideId,)(dispatch),
		getStudentGroups: (courseId: string, userId: string,) =>
			api.groups.getCourseGroupsRedux(courseId, userId, true)(dispatch),
		enableManualChecking: (submissionId: number,) =>
			api.submissions.redux.enableManualChecking(submissionId)(dispatch),

		assignBotReview: (submissionId, review) =>
			api.submissions.redux.assignBotReview(submissionId, review)(dispatch),

		loadDeadLines: (courseId: string, studentId: string) => api.instructor.redux.getDeadLines(courseId,
			studentId)(dispatch),
	};
};

const Connected = connect(mapStateToProps, mapDispatchToProps)(InstructorReview);
export default withNavigate(Connected);
