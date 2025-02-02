import { CourseAccessType, CourseRoleType, SystemAccessType } from "src/consts/accessType";
import { ShortUserInfo } from "../models/users";
import { AccountState } from "../redux/account";

export function isCourseAdmin(userRoles: UserRoles): boolean {
	return userRoles.isSystemAdministrator ||
		userRoles.courseRole === CourseRoleType.courseAdmin;
}

export function isInstructor(userRoles: UserRoles): boolean {
	return isCourseAdmin(userRoles) ||
		userRoles.courseRole === CourseRoleType.instructor;
}

export function isInstructorFromAccount(accountState: AccountState, courseId: string): boolean {
	return accountState.isAuthenticated &&
		isInstructor({
			isSystemAdministrator: accountState.isSystemAdministrator,
			courseRole: accountState.roleByCourse[courseId]
		});
}

export function canViewProfilesFromAccount(account: AccountState): boolean {
	return account.isAuthenticated && (
		account.isSystemAdministrator ||
		account.systemAccesses.includes(SystemAccessType.viewAllProfiles)
	);
}

export function buildUserInfo(user: AccountState, courseId: string,): UserInfo {
	const { isSystemAdministrator, accessesByCourse, roleByCourse, systemAccesses, isAuthenticated, } = user;
	const courseAccesses = accessesByCourse[courseId] ? accessesByCourse[courseId] : [];
	const courseRole = roleByCourse[courseId] ? roleByCourse[courseId] : CourseRoleType.student;

	return {
		...user as ShortUserInfo,

		isAuthenticated,
		isSystemAdministrator,
		courseRole,
		courseAccesses,
		systemAccesses,
	};
}

export interface UserRoles {
	isSystemAdministrator: boolean;
	courseRole: CourseRoleType;
}

export interface UserInfo extends ShortUserInfo, UserRoles {
	isAuthenticated: boolean;
	systemAccesses: SystemAccessType[];
	courseAccesses: CourseAccessType[];
}

