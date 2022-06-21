import React from "react";
import { Link } from 'react-router-dom';

import { ArchiveUnpack, Delete, Ok } from "icons";
import { Gapped, Kebab, MenuItem } from "ui";

import getPluralForm from "src/utils/getPluralForm";
import { Mobile, NotMobile } from "src/utils/responsive";

import { GroupInfo as GroupInfoType } from "src/models/groups";

import styles from "./groupInfo.less";


interface Props {
	courseId: string;
	page: string | null;
	group: GroupInfoType;
	deleteGroup: (group: GroupInfoType, groupType: 'archiveGroups' | 'groups') => void;
	toggleArchived: (group: GroupInfoType, isNotArchived: boolean) => void;
}

function GroupInfo({ group, courseId, deleteGroup, toggleArchived, page, }: Props): React.ReactElement | null {
	if(!group) {
		return null;
	}

	const studentsCount = group.studentsCount || 0;
	const pluralFormOfStudents = getPluralForm(studentsCount, 'студент', 'студента', 'студентов');
	const isCodeReviewEnabled = group.isManualCheckingEnabled;
	const isProgressEnabled = group.canStudentsSeeGroupProgress;

	return (
		<div className={ styles.wrapper }>
			<div className={ styles["content-wrapper"] }>
				<Link className={ styles["link-to-group-page"] }
					  to={ `/${ courseId }/groups/${ group.id }/` + (page || '') }/>
				<div className={ styles["content-block"] }>
					<header className={ styles.content }>
						<Link to={ `/${ courseId }/groups/${ group.id }/` + (page || '') }
							  className={ styles.groupLink }>
							<h3 className={ styles["group-name"] }>{ group.name }</h3>
						</Link>
						<div>
							{ studentsCount } { pluralFormOfStudents }
						</div>
						{ renderTeachers() }
					</header>
					<div className={ styles["group-settings"] }>
						{ renderSetting(isProgressEnabled, 'Ведомость включена', 'Ведомость выключена') }
						{ renderSetting(isCodeReviewEnabled, 'Код-ревью включено', 'Код-ревью выключено') }
					</div>
				</div>
			</div>
			{ renderActions() }
		</div>
	);

	function renderTeachers() {
		const teachersList = group.accesses.map(item => item.user.visibleName);
		const shortTeachersList = teachersList.filter((item, index) => index < 2);
		const teachersExcess = teachersList.length - shortTeachersList.length;
		const owner = group.owner.visibleName || 'Неизвестный';
		const teachers = [owner, ...shortTeachersList];
		const teachersCount = teachers.length;
		const pluralFormOfTeachers = getPluralForm(teachersCount, 'Преподаватель', 'Преподаватели', 'Преподавателей');

		return (
			<div>
				{ `${ pluralFormOfTeachers }: ${ teachers.join(', ') } ` }
				{ teachersExcess > 0 &&
					<Link className={ styles["link-to-group-members"] }
						  to={ `/${ courseId }/groups/${ group.id }/members` }>
						и ещё { teachersExcess }
					</Link>
				}
			</div>
		);
	}

	function renderSetting(enabled: boolean, textIfEnabled: string, textIfDisabled: string,) {
		return (
			<div className={ enabled ? styles["settings-on"] : styles["settings-off"] }>
				<Gapped gap={ 5 }>
					{ enabled ? <Ok/> : <Delete/> }
					{ enabled ? textIfEnabled : textIfDisabled }
				</Gapped>
			</div>
		);
	}

	function renderActions() {
		const menuItems = [
			<MenuItem onClick={ () => toggleArchived(group, !group.isArchived) } key="toggleArchived">
				<Gapped gap={ 5 }>
					<ArchiveUnpack/>
					{ group.isArchived ? 'Восстановить' : 'Архивировать' }
				</Gapped>
			</MenuItem>,
			<MenuItem onClick={ () => deleteGroup(group, group.isArchived ? 'archiveGroups' : 'groups') } key="delete">
				<Gapped gap={ 5 }>
					<Delete/>
					Удалить
				</Gapped>
			</MenuItem>
		];

		return (
			<div className={ styles["group-action"] }>
				<Mobile>
					<Kebab size="medium" positions={ ["left top"] } disableAnimations={ true }>
						{ menuItems }
					</Kebab>
				</Mobile>
				<NotMobile>
					<Kebab size="large" positions={ ["bottom right"] } disableAnimations={ false }>
						{ menuItems }
					</Kebab>
				</NotMobile>
			</div>
		);
	}
}

export default GroupInfo;
