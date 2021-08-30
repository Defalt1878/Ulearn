import React from "react";
import cn from "classnames";

import { DropdownMenu } from "ui";
import { DocumentGroup } from "icons";

import { getCourseMenuItems, isIconOnly, maxDropdownHeight } from "./CoursesMenuUtils";

import { CourseState } from "src/redux/course";
import { DeviceType } from "src/consts/deviceType";

import styles from '../../Header.less';

interface Props {
	courses: CourseState;
	controllableCourseIds: string[];
	deviceType: DeviceType;
}

function MyCoursesMenu({ courses, controllableCourseIds, deviceType }: Props): React.ReactElement {
	return (
		<DropdownMenu
			menuMaxHeight={ maxDropdownHeight }
			caption={
				<button className={ cn(styles.headerElement, styles.button) }>
					{
						isIconOnly(deviceType)
							? <DocumentGroup className={ styles.icon }/>
							: <>
								Мои курсы
								<span className={ styles.caret }/>
							</>
					}
				</button>
			}
		>
			{ getCourseMenuItems(controllableCourseIds, courses.courseById, false) }
		</DropdownMenu>
	);
}

export default MyCoursesMenu;
