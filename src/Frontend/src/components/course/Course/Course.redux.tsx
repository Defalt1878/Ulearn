import { connect, } from "react-redux";
import { Dispatch } from "redux";
import { getDataIfLoaded } from "src/redux";
import { withOldRouter } from "src/utils/router";
import { Course, } from 'src/components/course/Course/Course';

import { changeCurrentCourseAction, loadCourse, loadCourseErrors } from "src/actions/course";
import { loadUserProgress, userProgressUpdate } from "src/actions/userProgress";
import { loadFlashcards } from "src/actions/flashcards";

import { RootState } from "src/redux/reducers";
import { CourseInfo, UnitInfo } from "src/models/course";
import { FlashcardsStatistics } from "src/components/course/Navigation/types";
import getSlideInfo from "src/components/course/Course/CourseUtils";
import api from "src/api";
import { CourseRoleType } from "src/consts/accessType";
import { WithRouter } from "src/models/router";

const mapStateToProps = (state: RootState, { location, params }: WithRouter) => {
	const courseId = params.courseId.toLowerCase();
	const courseInfo = state.courses.fullCoursesInfo[courseId];
	const deadLines = getDataIfLoaded(state.deadLines.deadLines[courseId]);
	const slideInfo = getSlideInfo(params, location, courseInfo, deadLines,
		state.account.isSystemAdministrator || state.account.roleByCourse[courseId] && state.account.roleByCourse[courseId] !== CourseRoleType.student);

	const flashcardsByUnit = state.courses.flashcardsInfoByCourseByUnits[courseId];
	const flashcardsStatisticsByUnits: { [unitId: string]: FlashcardsStatistics } | undefined = flashcardsByUnit ? {} : undefined;
	if(flashcardsStatisticsByUnits) {
		for (const unitId in flashcardsByUnit) {
			flashcardsStatisticsByUnits[unitId] = {
				count: flashcardsByUnit[unitId].cardsCount,
				unratedCount: flashcardsByUnit[unitId].unratedFlashcardsCount,
			};
		}
	}

	const loadedCourseIds: { [courseId: string]: boolean } = {};
	for (const courseId of Object.keys(state.courses.fullCoursesInfo)) {
		loadedCourseIds[courseId] = true;
	}

	return {
		courseId,
		slideInfo,
		courseInfo,
		loadedCourseIds,
		units: mapCourseInfoToUnits(courseInfo),
		user: state.account,
		progress: state.userProgress.progress[courseId],
		courseLoadingErrorStatus: state.courses.courseLoadingErrorStatus,
		courseLoading: state.courses.courseLoading,
		flashcardsStatisticsByUnits,
		flashcardsLoading: state.courses.flashcardsLoading,

		navigationOpened: state.navigation.opened,
		isSlideReady: state.slides.isSlideReady,
		isHijacked: state.account.isHijacked,
		isStudentMode: state.instructor.isStudentMode,
		deadLines,
	};
};

const mapDispatchToProps = (dispatch: Dispatch) => ({
	enterToCourse: (courseId: string) => dispatch(changeCurrentCourseAction(courseId)),
	loadCourse: (courseId: string) => loadCourse(courseId)(dispatch),
	loadFlashcards: (courseId: string) => loadFlashcards(courseId)(dispatch),
	loadCourseErrors: (courseId: string) => loadCourseErrors(courseId)(dispatch),
	loadUserProgress: (courseId: string, userId: string) => loadUserProgress(courseId, userId)(dispatch),
	updateVisitedSlide: (courseId: string, slideId: string) => userProgressUpdate(courseId, slideId)(dispatch),
	loadDeadLines: (courseId: string) => api.deadLines.redux.getDeadLinesForCurrentUserRedux(courseId)(dispatch),
});

const connected = connect(mapStateToProps, mapDispatchToProps)(Course);
export default withOldRouter(connected);


function mapCourseInfoToUnits(courseInfo: CourseInfo) {
	if(!courseInfo || !courseInfo.units) {
		return null;
	}
	return courseInfo.units.reduce((acc: { [unitId: string]: UnitInfo }, item) => {
		acc[item.id] = item;
		return acc;
	}, {});
}
