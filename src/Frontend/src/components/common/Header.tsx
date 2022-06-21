import React, { Component, } from 'react';
import { connect } from "react-redux";
import { Dispatch } from "redux";
import { Link, } from "react-router-dom";
import cn from "classnames";

import { toggleNavigationAction } from "src/actions/navigation";
import { isInstructor } from "src/utils/courseRoles";
import { isIconOnly } from "./HeaderElements/CoursesMenus/CoursesMenuUtils";

import HeaderComponentErrorBoundary from "./Error/HeaderComponentErrorBoundary";
import Hijack from "src/components/hijack/Hijack";
import StudentMode from "src/components/common/StudentMode/StudentMode";
import Menu from "./HeaderElements/Menu";
import SysAdminMenu from "./HeaderElements/SysAdminMenu";
import MyCoursesMenu from "./HeaderElements/CoursesMenus/MyCoursesMenu";
import CourseMenu from "./HeaderElements/CoursesMenus/CourseMenu";
import MobileCourseMenu from "./HeaderElements/CoursesMenus/MobileCourseMenu";
import NotificationBar from "../notificationBar/NotificationBar";
import { Menu as MenuIcon } from 'icons';

import { CourseAccessType, CourseRoleType } from "src/consts/accessType";
import { AccountState } from "src/redux/account";
import { CourseState } from "src/redux/course";
import { RootState } from "src/models/reduxState";
import { DeviceType } from "src/consts/deviceType";

import styles from './Header.less';


interface Props {
	account: AccountState;
	courses: CourseState;
	initializing: boolean;
	deviceType: DeviceType;
	toggleNavigation: () => void;
}

interface State {
	isSystemAdministrator: boolean;
	controllableCourseIds: string[];
	isCourseMenuVisible: boolean;
	courseRole: CourseRoleType;
	courseAccesses: CourseAccessType[];
	currentCourseId?: string;
}

class Header extends Component<Props, State> {
	state: State = Header.mapPropsToState(this.props);

	static mapPropsToState({ account, courses }: Props): State {
		const { currentCourseId, courseById } = courses;
		const { groupsAsStudent, roleByCourse, accessesByCourse, isSystemAdministrator } = account;
		let controllableCourseIds: string[];
		if(isSystemAdministrator) {
			controllableCourseIds = Object.keys(courseById);
		} else {
			controllableCourseIds = Object.keys(roleByCourse)
				.filter(courseId => roleByCourse[courseId] !== CourseRoleType.tester)
				.map(s => s.toLowerCase());
			if(groupsAsStudent) {
				const groupsAsStudentIds = groupsAsStudent.map(g => g.courseId.toLowerCase());
				const set = new Set(controllableCourseIds.concat(groupsAsStudentIds));
				controllableCourseIds = Array.from(set);
				controllableCourseIds = controllableCourseIds
					.filter((e) => Object.prototype.hasOwnProperty.call(courseById, e))
					.sort((a, b) => {
						const first = courseById[a].title.toLowerCase();
						const second = courseById[b].title.toLowerCase();
						if(first > second) {
							return 1;
						}
						if(first < second) {
							return -1;
						}
						return 0;
					});
			}
		}

		let courseRole: CourseRoleType;
		if(isSystemAdministrator) {
			courseRole = CourseRoleType.courseAdmin;
		} else {
			courseRole = currentCourseId ? roleByCourse[currentCourseId] : CourseRoleType.student;
		}

		const isCourseMenuVisible = (
			courses !== undefined &&
			currentCourseId !== undefined &&
			controllableCourseIds.indexOf(currentCourseId) !== -1 &&
			courseRole !== undefined &&
			courseRole !== CourseRoleType.tester
		);

		const courseAccesses: CourseAccessType[] = isCourseMenuVisible && currentCourseId
			? accessesByCourse[currentCourseId] || []
			: [];

		return {
			isSystemAdministrator,
			controllableCourseIds,
			isCourseMenuVisible,
			courseRole,
			courseAccesses,
			currentCourseId,
		};
	}

	static getDerivedStateFromProps(props: Props, state: State) {
		const newState = Header.mapPropsToState(props);

		if(JSON.stringify(newState) !== JSON.stringify(state)) {
			return newState;
		}

		return null;
	}

	render() {
		const { initializing, account, deviceType, courses, } = this.props;
		const { isSystemAdministrator, courseRole, } = this.state;

		return (
			<React.Fragment>
				<div className={ styles.header + " header" } id="header">
					<div className={ styles.controlsWrapper }>
						{ this.renderLogo() }
						{ courses.currentCourseId && this.renderNavMenuIcon() }
						<Hijack name={ account.visibleName }/>
						{ !initializing && this.renderUserRoleMenu() }
						{ isInstructor({ isSystemAdministrator, courseRole })
							&& <StudentMode
								deviceType={ deviceType }
								containerClass={ cn(styles.headerElement, styles.button) }
							/> }
					</div>
					<HeaderComponentErrorBoundary className={ styles.headerElement }>
						<Menu
							deviceType={ deviceType }
							account={ account }
						/>
					</HeaderComponentErrorBoundary>
				</div>
				<NotificationBar/>
			</React.Fragment>
		);
	}

	renderLogo() {
		const { deviceType, } = this.props;

		return (
			<Link to={ '/' } className={ cn(styles.headerElement, styles.logo) }>
				{ isIconOnly(deviceType) ? 'U.me' : 'ULearn.me' }
			</Link>
		);
	}

	renderNavMenuIcon() {
		const { deviceType, toggleNavigation, } = this.props;

		return (
			isIconOnly(deviceType) &&
			<button className={ cn(styles.headerElement, styles.button) } onClick={ toggleNavigation }>
				<MenuIcon size={ 22 }/>
			</button>
		);
	}

	renderUserRoleMenu() {
		const { deviceType, } = this.props;

		return (
			<HeaderComponentErrorBoundary className={ styles.headerElement }>
				{ isIconOnly(deviceType)
					? this.renderPhoneUserRoleMenu()
					: this.renderDefaultUserRoleMenu()
				}
			</HeaderComponentErrorBoundary>
		);
	}

	renderDefaultUserRoleMenu(): React.ReactNode {
		const {
			isSystemAdministrator,
			controllableCourseIds,
			isCourseMenuVisible,
			courseRole,
			courseAccesses,
			currentCourseId,
		} = this.state;
		const { deviceType, courses, } = this.props;

		return (
			<>
				{ isSystemAdministrator &&
					<SysAdminMenu
						courses={ courses }
						deviceType={ deviceType }
						controllableCourseIds={ controllableCourseIds }
					/> }
				{ !isSystemAdministrator
					&& (controllableCourseIds.length > 0
						|| Object.values(courses.courseById).some(c => c.timestamp !== null)) &&
					<MyCoursesMenu
						courses={ courses }
						deviceType={ deviceType }
						controllableCourseIds={ controllableCourseIds }
					/> }
				{ isCourseMenuVisible && currentCourseId &&
					<CourseMenu
						courses={ courses }
						deviceType={ deviceType }
						courseId={ currentCourseId }
						role={ courseRole }
						accesses={ courseAccesses }
					/> }
			</>
		);
	}

	renderPhoneUserRoleMenu() {
		const {
			isSystemAdministrator,
			controllableCourseIds,
			isCourseMenuVisible,
			courseRole,
			courseAccesses,
			currentCourseId,
		} = this.state;
		const { courses, } = this.props;

		if(controllableCourseIds.length === 0 && !Object.values(courses.courseById).some(c => c.timestamp !== null)) {
			return null;
		}

		return (
			<MobileCourseMenu
				courses={ courses }
				isSystemAdministrator={ isSystemAdministrator }
				controllableCourseIds={ controllableCourseIds }
				isCourseMenuVisible={ isCourseMenuVisible }
				courseId={ currentCourseId }
				role={ courseRole }
				accesses={ courseAccesses }
			/>
		);
	}
}

const mapStateToHeaderProps = ({ account, courses, device, }: RootState) => {
	return { account, courses, deviceType: device.deviceType };
};

const mapDispatchToHeaderProps = (dispatch: Dispatch) => {
	return {
		toggleNavigation: () => dispatch(toggleNavigationAction()),
	};
};

export default connect(mapStateToHeaderProps, mapDispatchToHeaderProps)(Header);
