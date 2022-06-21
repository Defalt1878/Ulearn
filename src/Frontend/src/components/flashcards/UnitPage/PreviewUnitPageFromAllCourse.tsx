import React from 'react';

import classnames from 'classnames';
import { connect, } from "react-redux";
import { Dispatch } from "redux";
import { Link, } from 'react-router-dom';

import { Tabs } from 'ui';
import OpenedFlashcard from "../Flashcards/OpenedFlashcard/OpenedFlashcard.js";

import { getQueryStringParameter } from "src/utils";
import { withParams } from "src/utils/router";

import { RootState } from "src/models/reduxState";
import { ShortSlideInfo } from "src/models/slide";
import { Flashcard } from "src/models/flashcards";
import { WithParams } from "src/models/router";
import { constructPathToSlide } from "src/consts/routes";

import { loadFlashcards, } from "src/actions/flashcards";

import getFlashcardsWithTheorySlides from "src/pages/course/getFlashcardsWithTheorySlides";

import styles from './../Flashcards/flashcards.less';
import override from './courseToolUnitPage.less';

interface Props {
	flashcards: Flashcard[],
	loadFlashcards: (courseId: string) => void,
	courseId: string,
	unitId?: string | null,
	slugsByUnits: { [unitId: string]: string },
	flashcardsLoading: boolean,
}

interface UnitInfo {
	id: string,
	title: string
}

interface State {
	currentUnit?: UnitInfo,
	unitsInfo?: { [unitId: string]: UnitInfo },
	flashcardsByUnit?: { [unitId: string]: Flashcard[] },
}


class PreviewUnitPageFromAllCourse extends React.Component<Props, State> {
	state: State = {};

	componentDidMount() {
		const { flashcards, courseId, flashcardsLoading, loadFlashcards } = this.props;

		if(flashcardsLoading) {
			return null;
		}

		if(flashcards.length === 0) {
			loadFlashcards(courseId);
			return null;
		}

		this.initializeState();
	}

	componentDidUpdate(prevProps: Readonly<Props>) {
		const { flashcards, courseId, flashcardsLoading, loadFlashcards } = this.props;

		if(flashcardsLoading) {
			return null;
		}

		if(flashcards.length === 0) {
			loadFlashcards(courseId);
			return null;
		}

		if(prevProps.flashcards.length === 0 && flashcards.length > 0) {
			this.initializeState();
		}
	}

	initializeState() {
		const { flashcards, unitId, } = this.props;
		const flashcardsByUnit: { [unitId: string]: Flashcard[] } = {};
		const unitsInfo: { [unitId: string]: UnitInfo } = {};

		const firstUnit = { id: flashcards[0].unitId, title: flashcards[0].unitTitle };
		let unitFlashcards = [flashcards[0]];
		let currentUnit = { ...firstUnit };

		for (let i = 1; i < flashcards.length; i++) {
			const { unitId, unitTitle, } = flashcards[i];

			if(currentUnit.id !== unitId || i === flashcards.length - 1) {
				unitsInfo[currentUnit.id] = currentUnit;
				flashcardsByUnit[currentUnit.id] = unitFlashcards;

				currentUnit = { id: unitId, title: unitTitle };
				unitFlashcards = [];
			}

			unitFlashcards.push(flashcards[i]);
		}

		this.setState({
			unitsInfo,
			flashcardsByUnit,
			currentUnit: unitId ? unitsInfo[unitId] : firstUnit,
		});
	}

	render() {
		const { flashcardsByUnit, currentUnit, unitsInfo, } = this.state;
		const { slugsByUnits, courseId, } = this.props;

		if(!currentUnit || !unitsInfo || !flashcardsByUnit) {
			return null;
		}

		return <>
			<div className={ override.tabsWrapper }>
				<Tabs value={ currentUnit.id } onValueChange={ this.onTabChange }>
					{
						Object.values(unitsInfo).map(
							({ id, title }) => <Tabs.Tab key={ id } id={ id }>{ title }</Tabs.Tab>)
					}
				</Tabs>
			</div>
			<div className={ override.flashcardsLink }>
				<Link to={ constructPathToSlide(courseId, slugsByUnits[currentUnit.id]) }>
					Страница с флешкартами за этот модуль
				</Link>
			</div>
			{ flashcardsByUnit[currentUnit.id].map(({ unitTitle, answer, question, theorySlides }, i) => (
				<div className={ classnames(styles.modal, override.modal) } key={ i }>
					<OpenedFlashcard
						unitTitle={ unitTitle }
						answer={ answer }
						question={ question }
						theorySlides={ theorySlides || [] }

						onShowAnswer={ this.mockFunction }
						onClose={ this.mockFunction }
						onHandlingResultsClick={ this.mockFunction }
					/>
				</div>))
			}
		</>;
	}

	onTabChange = (unitId: string) => {
		const { unitsInfo } = this.state;

		this.setState({
			currentUnit: unitsInfo && unitsInfo[unitId],
		});
	};

	mockFunction = () => ({});
}

const mapStateToProps = (state: RootState, { params }: WithParams) => {
	const { courseId, } = params;
	const unitId = getQueryStringParameter('unitId');
	const courseIdInLowerCase = courseId.toLowerCase();
	const data = state.courses;
	const courseInfo = state.courses.fullCoursesInfo[courseIdInLowerCase];
	const infoByUnits = data.flashcardsInfoByCourseByUnits[courseId]
		? Object.values(data.flashcardsInfoByCourseByUnits[courseId])
		: [];
	const slugsByUnits: { [unitId: string]: string } = {};
	for (const unitId in data.flashcardsInfoByCourseByUnits[courseId]) {
		slugsByUnits[unitId] = data.flashcardsInfoByCourseByUnits[courseId][unitId].flashcardsSlideSlug;
	}

	if(!courseInfo) {
		return {
			courseId: courseIdInLowerCase,
			slugsByUnits,
			unitId,
			flashcardsLoading: data.flashcardsLoading,
			flashcards: [],
		};
	}

	const courseSlides: ShortSlideInfo[] = [];
	for (const unit of courseInfo.units) {
		courseSlides.push(...unit.slides);
	}
	const courseFlashcards = data.flashcardsByCourses[courseId];
	const flashcards = getFlashcardsWithTheorySlides(infoByUnits, courseFlashcards, courseSlides);

	return {
		courseId,
		slugsByUnits,
		unitId,
		flashcardsLoading: data.flashcardsLoading,
		flashcards,
	};
};

const mapDispatchToProps = (dispatch: Dispatch) => ({
	loadFlashcards: (courseId: string) => loadFlashcards(courseId)(dispatch),
});

const connected = connect(mapStateToProps, mapDispatchToProps)(PreviewUnitPageFromAllCourse);
export default withParams(connected);
