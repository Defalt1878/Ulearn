import { WithRouter } from "src/models/router";
import api from "src/api";
import { AccountState } from "src/redux/account";
import { GroupInfo, GroupScoringGroupInfo } from "src/models/groups";

export type Props = WithRouter & DispatchFromRedux & PropsFromRedux;

export interface DispatchFromRedux {
	enterToCourse: (courseId: string) => void;

	groupsApi: typeof api.groups;
	additionalContentApi: typeof api.additionalContent;
	coursesApi: typeof api.courses;
	deadLinesApi: typeof api.deadLines;
}

export interface PropsFromRedux {
	account: AccountState;
}

export interface State {
	group?: GroupInfo;
	updatedFields: Partial<GroupInfo>;
	error: string | false;
	loadingAllSettings: boolean;
	loadingGroup: boolean;
	loadingScores: boolean;
	scores: GroupScoringGroupInfo[];
	checkedScoresSettingsIds: string[];
	status?: "error";
}
