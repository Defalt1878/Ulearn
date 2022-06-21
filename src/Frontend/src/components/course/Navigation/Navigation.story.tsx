import React from "react";
import Navigation, { Props } from "./Navigation";
import {
	defaultNavigationProps,
	DesktopWrapper,
	disableViewportAnLoki,
	getCourseModules,
	getModuleNavigationProps,
	ViewportChangeHandlerRedux
} from "./stroies.data";
import type { Story } from "@storybook/react";
import { SlideType } from "../../../models/slide";

export default {
	title: "Navigation",
	...disableViewportAnLoki,
};

const Template: Story<Props> = args => <ViewportChangeHandlerRedux
	render={ (deviceType) => <DesktopWrapper>
		<Navigation { ...args } deviceType={ deviceType }/>
	</DesktopWrapper> }
/>;

export const ModuleNavigation = Template.bind({});
const args: Props = {
	...defaultNavigationProps,
	courseProgress: {
		current: 15,
		max: 25,
		inProgress: 0,
	},
	flashcardsStatistics: {
		count: 0,
		unratedCount: 0,
	},
	containsFlashcards: false,
	courseId: 'basic',
	courseTitle: "Основы программирования",
	unitTitle: "Первое знакомство с C#",
	unitProgress: {
		current: 50,
		inProgress: 25,
		max: 100,
		additionalInfoBySlide: {},
	},
	groupsAsStudent: [],

	courseItems: [],
	unitItems: getModuleNavigationProps(),
	nextUnit: {
		id: '2',
		additionalContentInfo: { isAdditionalContent: false, publicationDate: null, },
		title: 'Next module',
		slides: [
			{
				slug: 'Ошибки',
				additionalContentInfo: { isAdditionalContent: false, publicationDate: null, },
				id: 'id',
				unitId: 'unitId',
				containsVideo: false,
				questionsCount: 0,
				quizMaxTriesCount: 0,
				maxScore: 0,
				type: SlideType.Lesson,
				title: 'Ошибки',
				scoringGroup: null,
				apiUrl: '',
				hide: false,
			},
		],
		additionalScores: [],
	},
};
ModuleNavigation.args = args;

export const CourseNavigation = Template.bind({});
CourseNavigation.args = {
	...args,
	unitTitle: undefined,
	courseItems: getCourseModules(),
	containsFlashcards: true,
};
