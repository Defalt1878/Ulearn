import React from "react";
import cn from "classnames";

import { DropdownMenu, } from "ui";
import { DocsTextIcon20Solid } from '@skbkontur/icons/DocsTextIcon20Solid';

import { isIconOnly, maxDropdownHeight, sysAdminMenuItems } from "./CoursesMenus/CoursesMenuUtils";

import { CourseState } from "src/redux/course";
import { DeviceType } from "src/consts/deviceType";

import styles from '../Header.less';

interface Props {
	courses: CourseState;
	controllableCourseIds: string[];
	deviceType: DeviceType;
}


function SysAdminMenu({ controllableCourseIds, courses, deviceType, }: Props): React.ReactElement {
	return (
		<DropdownMenu
			menuMaxHeight={ maxDropdownHeight }
			caption={
				<button className={ cn(styles.headerElement, styles.button) }>
					{
						isIconOnly(deviceType)
							? <DocsTextIcon20Solid className={ styles.icon }/>
							: <>
								Администрирование
								<span className={ styles.caret }/>
							</>
					}
				</button>
			}
		>
			{ sysAdminMenuItems(controllableCourseIds, courses.courseById) }
		</DropdownMenu>
	);
}

export default SysAdminMenu;
