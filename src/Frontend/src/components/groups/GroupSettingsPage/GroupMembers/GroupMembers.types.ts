import { GroupAccessesInfo, GroupInfo, GroupStudentInfo } from "src/models/groups";
import { WithParams } from "src/models/router";
import { SystemAccessType } from "src/consts/accessType";
import api from "src/api";
import { ShortUserInfo } from "src/models/users";
import { AccountState } from "src/redux/account";

export interface Props extends PropsFromApi, WithParams {
	courseId: string;
	group: GroupInfo;
	role: string;
	account: AccountState;
	isSysAdmin: boolean;
	systemAccesses: SystemAccessType[];

	onChangeGroupOwner: (user: ShortUserInfo, updatedAccesses: GroupAccessesInfo[]) => void;
}

export interface PropsFromApi {
	getGroupAccesses: typeof api.groups.getGroupAccesses;
	getStudents: typeof api.groups.getStudents;
	changeGroupOwner: typeof api.groups.changeGroupOwner;
	removeAccess: typeof api.groups.removeAccess;
	addGroupAccesses: typeof api.groups.addGroupAccesses;
	deleteStudents: typeof api.groups.deleteStudents;
}

export interface State {
	accesses: GroupAccessesInfo[];
	selected: { value: string } | null;
	students: GroupStudentInfo[];
	loadingTeachers: boolean;
	loadingStudents: boolean;
}
