import React from "react";
import cn from "classnames";

import { Link } from 'ui';
import { ArrowChevron2Left } from "icons";
import ProgressBar from "../ProgressBar";
import ProgressBarCircle from "../ProgressBar/ProgressBarCircle";

import { buildQuery } from "src/utils";
import { withNavigate } from "src/utils/router";

import { Progress, UnitProgress } from '../types';
import { GroupAsStudentInfo } from "src/models/groups";
import { DeviceType } from "src/consts/deviceType";
import { courseStatistics, WithNavigate } from "src/consts/routes";

import styles from './NavigationHeader.less';

export interface Props extends WithNavigate {
	className?: string;
	title: string;
	courseProgress: Progress;
	groupsAsStudent: GroupAsStudentInfo[];

	unitTitle?: string;
	unitProgress?: UnitProgress;

	isInsideCourse: boolean;

	deviceType: DeviceType;

	returnToCourseNavigationClicked: () => void;

	createRef: React.RefObject<HTMLDivElement>;
}

function NavigationHeader({
	title,
	unitProgress,
	unitTitle,
	className,
	groupsAsStudent,
	courseProgress,
	returnToCourseNavigationClicked,
	createRef,
	isInsideCourse,
	deviceType,
	navigate,
}: Props): React.ReactElement {
	return (
		<header ref={ createRef }
				className={ cn(styles.root, styles.sticky, className) }>
			{ isInsideCourse
				? <h1 className={ styles.courseTitle } title={ title }>{ title }</h1>
				: renderReturnToCourseNavigationLink()
			}
			<>
				{ groupsAsStudent.length > 0 && renderLinkToGroupsStatements(groupsAsStudent) }
				{ renderCourseProgress() }
			</>
			{
				unitTitle && renderUnitSection(unitProgress)
			}
		</header>
	);

	function renderCourseProgress() {
		const value = courseProgress.current / courseProgress.max;
		const inProgressValue = courseProgress.inProgress / courseProgress.max;

		return (
			<div className={ styles.progressBarWrapper }>
				<ProgressBar value={ value } inProgressValue={ inProgressValue }/>
			</div>
		);
	}

	function renderReturnToCourseNavigationLink() {
		return (
			<nav className={ styles.returnToCourseLink }>
				<Link
					icon={ <ArrowChevron2Left className={ styles.returnToCourseLinkIcon }/> }
					onClick={ returnToCourseNavigationClicked }>
					<span className={ styles.returnToCourseLinkText }>{ title }</span>
				</Link>
			</nav>
		);
	}

	function renderLinkToGroupsStatements(groupsAsStudent: GroupAsStudentInfo[]) {
		const groupsLinks = [];

		for (let i = 0; i < groupsAsStudent.length; i++) {
			const { id, courseId, name, } = groupsAsStudent[i];
			const courseIdInLowerCase = courseId.toLowerCase();

			groupsLinks.push(
				<Link
					key={ id }
					onClick={ preventReload }
					href={ courseStatistics + buildQuery({ courseId: courseIdInLowerCase, group: id }) }>
					{ name }
				</Link>
			);

			if(i < groupsAsStudent.length - 1) {
				groupsLinks.push(', ');
			}
		}

		return <p className={ cn(
			styles.linkToGroupsStatementsWrapper,
			{ [styles.insideModule]: !isInsideCourse }) }>Ведомость { groupsLinks }</p>;
	}

	function preventReload(e: React.MouseEvent<HTMLAnchorElement>) {
		e.preventDefault();
		e.stopPropagation();
		const anchor = e.currentTarget;
		navigate(anchor.pathname + anchor.search);
	}

	function renderUnitSection(unitProgress?: UnitProgress) {
		return (
			<div className={ styles.unitRoot }>
				<h2 className={ styles.unitTitle } title={ unitTitle }>
					{ unitTitle }
				</h2>
				{ unitProgress && renderUnitProgress(unitProgress) }
			</div>
		);
	}

	function renderUnitProgress(unitProgress: UnitProgress) {
		if(unitProgress.inProgress > 0 || unitProgress.current > 0) {
			return (
				<span className={ styles.circleProgressBarWrapper }>
					<ProgressBarCircle
						big={ deviceType !== DeviceType.mobile }
						successValue={ unitProgress.current / unitProgress.max }
						inProgressValue={ unitProgress.inProgress / unitProgress.max }
					/>
				</span>
			);
		}
	}
}

export default withNavigate(NavigationHeader);
