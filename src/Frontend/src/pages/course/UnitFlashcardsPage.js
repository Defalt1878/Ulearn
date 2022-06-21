import { connect } from "react-redux";

import UnitFlashcards from "src/components/flashcards/UnitPage/UnitPage";

import { sendFlashcardResult, loadFlashcards } from 'src/actions/flashcards';
import getFlashcardsWithTheorySlides from "./getFlashcardsWithTheorySlides";
import { findUnitIdBySlideId } from "src/components/course/Course/CourseUtils";
import { withParams } from "src/utils/router";

const mapStateToProps = (state, { params }) => {
	let { courseId, slideSlugOrAction } = params;
	courseId = courseId.toLowerCase();
	const slideId = slideSlugOrAction.split('_').pop();
	const data = state.courses;
	const courseInfo = data.fullCoursesInfo[courseId];
	const infoByUnits = data.flashcardsInfoByCourseByUnits[courseId] ? Object.values(data.flashcardsInfoByCourseByUnits[courseId]) : [];
	const unitId = findUnitIdBySlideId(slideId, courseInfo);
	const unitInfo = data.flashcardsInfoByCourseByUnits[courseId] ? data.flashcardsInfoByCourseByUnits[courseId][unitId] : {};

	if(!courseInfo) {
		return {
			courseId,
			unitId,
			unitTitle: unitInfo ? unitInfo.unitTitle : null,
			infoByUnits,
			flashcards: [],
			flashcardsLoading: data.flashcardsLoading,
		}
	}

	const courseSlides = courseInfo.units
		.reduce((slides, unit) => ([...slides, ...unit.slides]), []);
	const courseFlashcards = data.flashcardsByCourses[courseId];
	const flashcards = courseFlashcards ? getFlashcardsWithTheorySlides(infoByUnits, courseFlashcards, courseSlides) : [];

	return {
		courseId,
		unitId,
		unitTitle: unitInfo ? unitInfo.unitTitle : null,
		infoByUnits,
		flashcards,
		flashcardsLoading: data.flashcardsLoading,
	}
};
const mapDispatchToProps = (dispatch) => ({
	loadFlashcards: (courseId) => dispatch(loadFlashcards(courseId)),
	sendFlashcardRate: (courseId, unitId, flashcardId, rate, newTLast) => dispatch(sendFlashcardResult(courseId, unitId, flashcardId, rate, newTLast)),
});


const connected = connect(mapStateToProps, mapDispatchToProps)(UnitFlashcards);
export default withParams(connected);

