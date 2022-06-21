import { ShortUserInfo } from "./users";
import { AbstractScoringGroupInfo } from "./course";

export interface GroupAsStudentInfo {
	id: number;
	courseId: string;
	name: string;
	isArchived: boolean;
	apiUrl: string;
}

export interface GroupAccessesResponse {
	accesses: GroupAccessesInfo[];
}

export interface GroupAccessesInfo {
	user: ShortUserInfo;
	accessType: GroupAccessType;
	grantedBy: ShortUserInfo;
	grantTime: string;
}

export enum GroupAccessType {
	FullAccess = 1,
	Owner = 100,
}

export interface GroupStudentsResponse {
	students: GroupStudentInfo[];
}

export interface GroupStudentInfo {
	user: ShortUserInfo;
	addingTime: string;
}

export interface GroupsInfoResponse {
	groups: GroupInfo[];
}

export interface GroupScoringGroupsResponse {
	scores: GroupScoringGroupInfo[];
}

export interface GroupScoringGroupInfo extends AbstractScoringGroupInfo {
	areAdditionalScoresEnabledForAllGroups: boolean;
	canInstructorSetAdditionalScoreInSomeUnit: boolean;
	areAdditionalScoresEnabledInThisGroup?: boolean;
}

export interface GroupInfo {
	id: number;
	createTime?: string | null;
	name: string;
	isArchived: boolean;
	owner: ShortUserInfo;
	inviteHash: string;
	isInviteLinkEnabled: boolean;
	areYouStudent: boolean;
	isManualCheckingEnabled: boolean;
	isManualCheckingEnabledForOldSolutions: boolean;
	defaultProhibitFurtherReview: boolean;
	canStudentsSeeGroupProgress: boolean;
	studentsCount: number;
	accesses: GroupAccessesInfo[];
	apiUrl: string;
}

export interface CopyGroupResponse {
	id: string;
	apiUrl: string;
}

export interface GroupsListParameters {
	courseId: string;
	archived: boolean;
	userId: string;
	offset: number;
	count: number;
}
