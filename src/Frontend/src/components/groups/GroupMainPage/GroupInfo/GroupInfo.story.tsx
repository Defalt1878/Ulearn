import React from "react";
import GroupInfo from "./GroupInfo";

import "./groupInfo.less";
import { getMockedUser } from "../../../../storiesUtils";

export default {
	title: "Group/GroupInfo",
};
const mock = () => ({});

export const Default = (): React.ReactNode => <GroupInfo page={'somePage'} deleteGroup={ mock } toggleArchived={ mock } courseId={ '1' }
														 group={ getGroup() }/>;

Default.storyName = "default";

function getGroup() {
	return {
		id: 17,
		name: "asdfasdfasdfasdf",
		isArchived: false,
		owner: getMockedUser({
			id: "4052ea63-34dd-4398-b8bb-ac4e6a85d1d0",
			visibleName: "paradeeva",
			firstName: 'para',
			lastName: 'deeva',
			login: undefined,
			email: undefined,
		}),
		inviteHash: "b7638c37-62c6-49a9-898c-38788169987c",
		isInviteLinkEnabled: true,
		isManualCheckingEnabled: false,
		isManualCheckingEnabledForOldSolutions: false,
		defaultProhibitFurtherReview: true,
		canStudentsSeeGroupProgress: true,
		studentsCount: 0,
		accesses: [],
		apiUrl: "/groups/17",
		areYouStudent: false,
	};
}
