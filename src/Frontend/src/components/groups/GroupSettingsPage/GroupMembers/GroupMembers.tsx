import React, { useEffect, useState } from "react";
import { Delete, User } from "icons";
import { Gapped, Kebab, Loader, MenuItem, Toast } from "ui";
import ComboboxInstructorsSearch from "./Combobox/ComboboxInstructorsSearch.js";
import Avatar from "src/components/common/Avatar/Avatar";
import GroupStudents from "./GroupStudents/GroupStudents.js";
import InviteBlock from "./InviteBlock/InviteBlock.js";
import { Profile } from './Profile';

import { Mobile, NotMobile } from "src/utils/responsive";
import { withParams } from "src/utils/router";

import { CourseRoleType, } from "src/consts/accessType";
import { ShortUserInfo } from "src/models/users";
import { AccountState } from "src/redux/account";
import { GroupAccessesInfo } from "src/models/groups";

import { Props, State, } from "./GroupMembers.types";
import styles from './groupMembers.less';
import texts from './GroupMembers.texts';


function GroupMembers(props: Props) {
	const [state, setState] = useState<State>({
		accesses: [],
		selected: null,
		students: [],
		loadingTeachers: false,
		loadingStudents: false,
	});
	const {
		accesses,
		students,
		loadingStudents,
		loadingTeachers,
		selected,
	} = state;
	const {
		group,
		systemAccesses,
		isSysAdmin,
		params,
		getGroupAccesses,
		getStudents,
		onChangeGroupOwner,
		changeGroupOwner,
		addGroupAccesses,
		account,
		removeAccess,
		role,
		courseId,
		deleteStudents,
	} = props;
	const groupId = parseInt(params.groupId || '0');

	useEffect(componentDidMount, []);

	return render();

	function render() {
		const owner = group.owner;

		if(!owner) {
			return null;
		}

		return (
			<div className={ styles.wrapper }>
				<div className={ styles.teachers }>
					<h4 className={ styles["teachers-header"] }>{ texts.teachersHeader }</h4>
					<p className={ styles["teachers-info"] }>
						{ texts.teachersInfo }
					</p>
					<Loader type={ "big" } active={ loadingTeachers }>
						<div className={ styles["teacher-block"] }>
							<Avatar user={ owner } size={ "big" }/>
							<div className={ styles["teacher-name"] }>
								<Profile
									user={ owner }
									systemAccesses={ systemAccesses }
									isSysAdmin={ isSysAdmin }/>
								<span className={ styles["teacher-status"] }>
									{ texts.owner }
								</span>
							</div>
						</div>
						{ (accesses.length > 0) && renderTeachers() }
					</Loader>
					{ renderTeachersSearch() }
				</div>
				<div>{/* className={ styles["students-block"] } */ }
					<h4 className={ styles["students-header"] }>
						Студенты
					</h4>
					<InviteBlock group={ group }/>
					<Loader type="big" active={ loadingStudents }>
						<div> {/* className={ styles["students-list"] } */ }
							{ (students.length > 0) &&
								<GroupStudents
									isSysAdmin={ isSysAdmin }
									systemAccesses={ systemAccesses }
									students={ students }
									group={ group }
									onDeleteStudents={ onDeleteStudents }/> }
						</div>
					</Loader>
				</div>
			</div>
		);
	}

	function componentDidMount() {
		loadGroupAccesses(groupId);
		loadStudents(groupId);
	}

	function loadGroupAccesses(groupId: number) {
		setState(oldState => ({
			...oldState,
			loadingTeachers: true,
		}));

		getGroupAccesses(groupId)
			.then(json => {
				const accesses = json.accesses;
				setState(oldState => ({
					...oldState,
					accesses,
					loadingTeachers: false,
				}));
			})
			.catch(console.error)
			.finally(() =>
				setState(oldState => ({
					...oldState,
					loadingTeachers: false,
				}))
			);
	}

	function loadStudents(groupId: number) {
		setState(oldState => ({
			...oldState,
			loadingStudents: true,
		}));

		getStudents(groupId)
			.then(json => {
				const students = json.students;
				setState(oldState => ({
					...oldState,
					students,
					loadingStudents: false,
				}));
			})
			.catch(console.error)
			.finally(() =>
				setState(oldState => ({
					...oldState,
					loadingStudents: false,
				}))
			);
	}

	function renderTeachers() {
		return (accesses
				.sort((a, b) => a.user.visibleName.localeCompare(b.user.visibleName))
				.map(item =>
					<React.Fragment
						key={ item.user.id }>
						<div className={ styles["teacher-block"] }>
							<Avatar user={ item.user } size={ "big" }/>
							<div className={ styles["teacher-name"] }>
								<Profile
									user={ item.user }
									systemAccesses={ systemAccesses }
									isSysAdmin={ isSysAdmin }
								/>
								<span className={ styles["teacher-status"] }>
									{ texts.buildGrantedInfo(item) }
								</span>
							</div>
							<div className={ styles["teacher-action"] }>
								{ ((group.owner.id === account.id) || (role === 'courseAdmin'))
									&& renderKebab(item) }
							</div>
						</div>
					</React.Fragment>
				)
		);
	}

	function renderKebab(item: GroupAccessesInfo) {
		const menuItems = [
			<MenuItem onClick={ () => onChangeOwner(item.user) } key={ "changeOwner" }>
				<Gapped gap={ 5 }>
					<User/>
					{ texts.changeOwner }
				</Gapped>
			</MenuItem>
		];
		if(group.owner.id === account.id || role === CourseRoleType.courseAdmin || item.grantedBy.id === account.id) {
			menuItems.push(
				<MenuItem onClick={ () => onRemoveTeacher(item.user) } key={ "removeTeacher" }>
					<Gapped gap={ 5 }>
						<Delete/>
						{ texts.removeTeacher }
					</Gapped>
				</MenuItem>
			);
		}

		/* TODO (andgein): Change to size="medium" inside of <Mobile> after updating to the new react-ui version */
		return (
			<>
				<Mobile>
					<Kebab size={ "large" } positions={ ["left top"] } disableAnimations={ true }>
						{ menuItems }
					</Kebab>
				</Mobile>
				<NotMobile>
					<Kebab size={ "large" } positions={ ["bottom right"] } disableAnimations={ false }>
						{ menuItems }
					</Kebab>
				</NotMobile>
			</>
		);
	}

	function renderTeachersSearch() {
		return (
			<label className={ styles["teacher-search"] }>
				<p>{ texts.addTeacherSearch }</p>
				<ComboboxInstructorsSearch
					selected={ selected }
					courseId={ courseId }
					accesses={ accesses }
					owner={ group.owner }
					onAddTeacher={ onAddTeacher }/>
			</label>
		);
	}

	function onChangeOwner(user: ShortUserInfo) {
		changeGroupOwner(group.id, user.id)
			.then(() => {
				const updatedAccesses = accesses.map(item =>
					item.user.id === user.id ? {
						...item,
						user: group.owner,
						grantTime: new Date().toDateString()
					} : item);
				setState(oldState => ({
					...oldState,
					accesses: updatedAccesses,
				}));

				onChangeGroupOwner(user, updatedAccesses);
			})
			.catch((error) => {
				error.showToast();
			});
	}

	function onRemoveTeacher(user: ShortUserInfo) {
		removeAccess(group.id, user.id)
			.then(() => {
				const updatedAccesses = accesses
					.filter(item => item.user.id !== user.id);
				setState(oldState => ({
					...oldState,
					accesses: updatedAccesses,
				}));
			})
			.catch((error) => {
				error.showToast();
			});
	}

	function onAddTeacher(item: { value: string }) {
		setState(oldState => ({
			...oldState,
			selected: item,
		}));

		onLoadTeacher(item);
	}

	function onLoadTeacher(item: { value: string }) {
		const grantedBy = getUserFromAccount(account);

		addGroupAccesses(group.id, item.value)
			.then(() => {
				const updatedAccesses = accesses
					.filter(i => i.user.id !== item.value)
					.concat({
						user: item,
						grantedBy,
						grantTime: new Date().toDateString(),
					});

				setState(oldState => ({
					...oldState,
					accesses: updatedAccesses,
					selected: null,
				}));
			})
			.catch((error) => {
				error.showToast();
			});
	}

	function getUserFromAccount(account: AccountState) {
		const { firstName, lastName, gender, avatarUrl, id, visibleName, } = account;

		return {
			firstName,
			lastName,
			gender,
			avatarUrl,
			id,
			visibleName,
		};
	}

	function onDeleteStudents(students: string[]) {
		deleteStudents(group.id, students)
			.then(() => {
				const updatedStudents = students.filter((item) => !students.includes(item.user.id));

				setState(oldState => ({
					...oldState,
					students: updatedStudents,
				}));

				Toast.push(texts.onDeleteStudentsToast);
			})
			.catch((error) => {
				error.showToast();
			});
	}
}

export default withParams(GroupMembers);
