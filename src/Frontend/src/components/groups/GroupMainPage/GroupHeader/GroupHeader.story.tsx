import React from "react";
import GroupHeader from "./GroupHeader";

import "./groupHeader.less";

export default {
	title: "Group/GroupHeader",
};

export const Default = (): React.ReactNode => (
	<GroupHeader
		addGroup={ () => ({}) }
		course={ {
			containsFlashcards: true,
			id: 'basicProgramming',
			title: 'BasicProgramming',
			units: [],
			isTempCourse: false,
			nextUnitPublishTime: null,
			tempCourseError: null,
			scoring: { groups: [{ id: '1', name: '12', abbr: '123', description: '', weight: 0 }] },
		} }
		onTabChange={ () => ({}) }
		filter="hello"/>
);

Default.storyName = "default";
