import React, { FormEvent, useEffect, useState, } from 'react';
import { connect, } from "react-redux";
import { Dispatch } from "redux";
import { Navigate } from 'react-router-dom';

import api from "src/api";
import CourseLoader from "src/components/course/Course/CourseLoader";
import { Button, Link, Tabs, Toast } from "ui";
import GroupMembers from "src/components/groups/GroupSettingsPage/GroupMembers/GroupMembers";
import GroupSettings from "src/components/groups/GroupSettingsPage/GroupSettings/GroupSettings.js";
import Error404 from "src/components/common/Error/Error404";
import Page from "../../index";
import { changeCurrentCourseAction } from "src/actions/course";
import GroupAdditionalContent
	from "src/components/groups/GroupSettingsPage/GroupAdditionalContent/GroupAdditionalContent";
import GroupDeadLines from "src/components/groups/GroupSettingsPage/GroupDeadLines/GroupDeadLines";
import { withOldRouter } from "src/utils/router";
import { GroupAccessesInfo, GroupInfo, GroupScoringGroupInfo } from "src/models/groups";
import { ShortUserInfo } from "src/models/users";
import { RootState } from "src/redux/reducers";

import { Props, State, PropsFromRedux, DispatchFromRedux, } from './GroupPage.types';
import styles from "./groupPage.less";
import texts from "./GroupPage.texts";

const pages = ['settings', 'members', 'additional-content', 'dead-lines'];

function GroupPage(props: Props) {
	const [state, setState] = useState<State>({
		updatedFields: {},
		error: false,
		loadingAllSettings: false,
		loadingGroup: true,
		loadingScores: true,
		scores: [],
		checkedScoresSettingsIds: [],
	});

	const {
		group,
		status,
		checkedScoresSettingsIds,
		scores,
		loadingAllSettings,
		loadingGroup,
		loadingScores,
		updatedFields,
		error,
	} = state;
	const {
		params,
		account,
		groupsApi,
		additionalContentApi,
		coursesApi,
		enterToCourse,
		deadLinesApi,
		navigate,
	} = props;

	useEffect(componentDidMount, []);

	const courseId = params.courseId.toLowerCase();
	const { groupPage } = params;
	const groupId = parseInt(params.groupId || '0');

	if(!group) {
		return <CourseLoader/>;
	}

	const loadedGroup = group;


	return render();

	function render() {
		if(status === "error") {
			return <Error404/>;
		}

		if(!groupPage) {
			return <Navigate to={ `/${ courseId }/groups/${ groupId }/settings` }/>;
		}

		const rolesByCourse = account.roleByCourse;
		const systemAccesses = account.systemAccesses;
		let courseRole = '';

		if(account.isSystemAdministrator) {
			courseRole = 'courseAdmin';
		} else {
			courseRole = rolesByCourse[courseId];
		}

		return (
			<Page metaTitle={ `Группа ${ loadedGroup.name }` }>
				{ renderHeader() }
				<div className={ styles.content }>
					{ groupPage === "settings" && renderSettings() }
					{ groupPage === "members" &&
						<GroupMembers
							addGroupAccesses={ groupsApi.addGroupAccesses }
							getGroupAccesses={ groupsApi.getGroupAccesses }
							changeGroupOwner={ groupsApi.changeGroupOwner }
							deleteStudents={ groupsApi.deleteStudents }
							removeAccess={ groupsApi.removeAccess }
							getStudents={ groupsApi.getStudents }
							courseId={ courseId }
							account={ account }
							role={ courseRole }
							isSysAdmin={ account.isSystemAdministrator }
							systemAccesses={ systemAccesses }
							group={ loadedGroup }
							onChangeGroupOwner={ onChangeGroupOwner }/>
					}
					{ groupPage === "additional-content" &&
						<GroupAdditionalContent
							courseId={ courseId }
							groupId={ groupId }
							getAdditionalContent={ additionalContentApi.getAdditionalContent }
							deletePublication={ additionalContentApi.deletePublication }
							updatePublication={ additionalContentApi.updatePublication }
							addPublication={ additionalContentApi.addPublication }
							user={ account }
						/>
					}
					{ groupPage === "dead-lines" &&
						<GroupDeadLines
							courseId={ courseId }
							groupId={ groupId }
							getStudents={ groupsApi.getStudents }
							getCourse={ coursesApi.getCourse }
							getDeadLines={ deadLinesApi.getDeadLines }
							changeDeadLine={ deadLinesApi.changeDeadLine }
							createDeadLine={ deadLinesApi.createDeadLine }
							deleteDeadLine={ deadLinesApi.deleteDeadLine }
							user={ account }
						/>
					}
				</div>
			</Page>
		);
	}

	function componentDidMount() {
		enterToCourse(courseId);

		loadGroupScores(groupId);
		loadGroup(groupId);
	}

	function loadGroup(groupId: number) {
		groupsApi.getGroup(groupId)
			.then(group => {
				setState(oldState => ({
					...oldState,
					group,
				}));
			})
			.catch(() => {
				setState(oldState => ({
					...oldState,
					status: 'error',
				}));
			})
			.finally(() => {
					setState(oldState => ({
						...oldState,
						loadingGroup: false,
					}));
				}
			);
	}

	function loadGroupScores(groupId: number) {
		groupsApi.getGroupScores(groupId)
			.then(json => {
				const scores = json.scores;
				const checkedScoresSettingsIds = scores
					.filter(
						score => score.areAdditionalScoresEnabledInThisGroup || score.areAdditionalScoresEnabledForAllGroups || false)
					.map(score => score.id);
				setState(oldState => ({
					...oldState,
					scores,
					checkedScoresSettingsIds,
				}));
			})
			.catch(console.error)
			.finally(() =>
				setState(oldState => ({
					...oldState,
					loadingScores: false,
				}))
			);
	}

	function renderHeader() {
		if(!groupPage || !pages.includes(groupPage)) {
			return <Navigate to={ `/${ courseId }/groups/${ groupId }/settings` }/>;
		}

		return (
			<header className={ styles["group-header"] }>
				<div className={ styles["link-to-prev-page-block"] }>
					<div className={ styles["link-to-prev-page"] }>
						<Link onClick={ goToPrevPage }>
							{ texts.allGroups }
						</Link>
					</div>
				</div>
				<h2 className={ styles["group-name"] }>{ loadedGroup.name ? loadedGroup.name : " " }</h2>
				<div className={ styles["tabs-container"] }>
					<Tabs value={ groupPage } onValueChange={ onChangeTab }>
						<Tabs.Tab id="settings">{ texts.settings }</Tabs.Tab>
						<Tabs.Tab id="members">{ texts.members }</Tabs.Tab>
						<Tabs.Tab id="additional-content">{ texts.additionalContent }</Tabs.Tab>
						<Tabs.Tab id="dead-lines">{ texts.deadLines }</Tabs.Tab>
					</Tabs>
				</div>
			</header>
		);
	}

	function renderSettings() {
		return (
			<form onSubmit={ sendSettings }>
				<GroupSettings
					loading={ loadingScores && loadingGroup }
					name={ updatedFields.name !== undefined ? updatedFields.name : loadedGroup.name }
					group={ group }
					scores={ scores }
					error={ error }
					onChangeName={ onChangeName }
					onChangeSettings={ onChangeSettings }
					onChangeScores={ onChangeScores }/>
				<Button
					size="medium"
					use="primary"
					type="submit"
					loading={ loadingAllSettings }>
					{ texts.saveSettings }
				</Button>
			</form>
		);
	}

	function onChangeTab(value: string) {
		navigate(`/${ courseId }/groups/${ groupId }/${ value }`);
	}

	function onChangeName(value: string) {
		setState(oldState => ({
			...oldState,
			updatedFields: {
				...updatedFields,
				name: value,
			}
		}));
	}

	function onChangeSettings(field: keyof GroupInfo, value: GroupInfo[keyof GroupInfo]) {
		setState(oldState => ({
			...oldState,
			group: {
				...loadedGroup,
				[field]: value
			},
			updatedFields: {
				...updatedFields,
				[field]: value,
			}
		}));
	}

	function onChangeGroupOwner(user: ShortUserInfo, updatedGroupAccesses: GroupAccessesInfo[]) {
		const updatedGroup: GroupInfo = { ...loadedGroup, owner: user, accesses: updatedGroupAccesses };

		setState(oldState => ({
			...oldState,
			group: updatedGroup,
		}));
	}

	function onChangeScores(
		key: string,
		field: keyof GroupScoringGroupInfo,
		value: GroupScoringGroupInfo[keyof GroupScoringGroupInfo]
	) {
		const updatedScores = scores
			.map(item => item.id === key ? { ...item, [field]: value } : item);

		const checkedScoresSettingsIds = updatedScores
			.filter(item => item[field] === true)
			.map(item => item.id);

		setState(oldState => ({
			...oldState,
			scores: updatedScores,
			checkedScoresSettingsIds,
		}));
	}

	function goToPrevPage() {
		navigate(`/${ courseId }/groups/`);
	}

	function sendSettings(e: FormEvent) {
		e.preventDefault();

		const mapToServerName: { [key: string]: keyof GroupScoringGroupInfo } = {
			groupScores: 'areAdditionalScoresEnabledInThisGroup',
			allGroupScores: 'areAdditionalScoresEnabledForAllGroups',
			unitScores: 'canInstructorSetAdditionalScoreInSomeUnit',
		};

		const saveGroup = groupsApi.saveGroupSettings(loadedGroup.id, updatedFields);
		const saveScores = groupsApi.saveScoresSettings(loadedGroup.id, checkedScoresSettingsIds
			.filter(s => {
				const score = scores.find(score => score.id === s)!;
				return !(score[mapToServerName.allGroupScores] || !score[mapToServerName.unitScores]);
			}));

		Promise
			.all([saveGroup, saveScores])
			.then(([group, scores]) => {
				setState(oldState => ({
					...oldState,
					loadingAllSettings: true,
					group: {
						...group,
						name: updatedFields.name === undefined ? group.name : updatedFields.name,
					}
				}));
				Toast.push(texts.onSaveSuccessfulToast);
			})
			.catch((error) => {
				error.showToast();
			})
			.finally(() => {
				setState(oldState => ({ ...oldState, loadingAllSettings: false }));
			});
	}
}

const mapStateToProps = (state: RootState): PropsFromRedux => {
	return {
		account: state.account,
	};
};

const mapDispatchToProps = (dispatch: Dispatch): DispatchFromRedux => {
	return {
		enterToCourse: (courseId: string) => dispatch(changeCurrentCourseAction(courseId)),
		additionalContentApi: api.additionalContent,
		coursesApi: api.courses,
		deadLinesApi: api.deadLines,
		groupsApi: api.groups,
	};
};

const connected = connect(mapStateToProps, mapDispatchToProps)(GroupPage);

export default withOldRouter(connected);
