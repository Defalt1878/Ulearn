import React from "react";
import GroupMembers from "./GroupMembers";

import "./groupMembers.less";
import { ViewportWrapper } from "../../../course/Navigation/stroies.data";

export default {
	title: "Settings/GroupMembers",
};

export const Default = (): React.ReactNode =>
	<ViewportWrapper>
		<GroupMembers
			getGroupAccesses={ () => Promise.reject() }
			getStudents={ () => Promise.reject() }
			changeGroupOwner={ () => Promise.reject() }
			removeAccess={ () => Promise.reject() }
			addGroupAccesses={ () => Promise.reject() }
			deleteStudents={ () => Promise.reject() }
			systemAccesses={ ["viewAllProfiles"] }
			group={ getGroup() }
		/>
	</ViewportWrapper>;

Default.storyName = "default";

function getGroup() {
	return {
		id: 17,
		name: "asdfasdfasdfasdf",
		isArchived: false,
		owner: {
			id: "4052ea63-34dd-4398-b8bb-ac4e6a85d1d0",
			visibleName: "paradeeva",
			avatarUrl: null,
		},
		inviteHash: "b7638c37-62c6-49a9-898c-38788169987c",
		isInviteLinkEnabled: true,
		isManualCheckingEnabled: false,
		isManualCheckingEnabledForOldSolutions: false,
		defaultProhibitFurtherReview: true,
		canStudentsSeeGroupProgress: true,
		studentsCount: 0,
		accesses: [],
		apiUrl: "/groups/17",
	};
}
