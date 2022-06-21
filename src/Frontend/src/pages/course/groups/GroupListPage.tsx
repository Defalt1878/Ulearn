import React, { Component, } from "react";
import { connect } from "react-redux";
import { Dispatch } from "redux";
import { withOldRouter } from "src/utils/router";

import api from "src/api";

import { changeCurrentCourseAction } from "src/actions/course";

import Page from "../../index";
import GroupList from "src/components/groups/GroupMainPage/GroupList/GroupList";
import GroupHeader from "src/components/groups/GroupMainPage/GroupHeader/GroupHeader";
import Error404 from "src/components/common/Error/Error404";
import { Toast } from "ui";

import { GroupInfo } from "src/models/groups";
import { CourseState } from "src/redux/course";
import { RootState } from "src/redux/reducers";
import { WithRouter } from "src/models/router";

interface Props extends WithRouter {
	userId?: string | null;
	courses: CourseState;

	enterToCourse: (courseId: string) => void;
}

type GroupType = "groups" | 'archiveGroups';

interface State {
	groups: GroupInfo[];
	archiveGroups: GroupInfo[];
	filter: "active" | 'archived' | string;
	status: string;

	loadingArchived: boolean;
	loadingActive: boolean;
	loadedArchived: boolean;
	loadedActive: boolean;
}

class GroupListPage extends Component<Props, State> {
	constructor(props: Props) {
		super(props);

		this.state = {
			groups: [],
			archiveGroups: [],
			filter: "active",
			loadingArchived: false,
			loadingActive: false,
			loadedArchived: false,
			loadedActive: false,
			status: '',
		};
	}

	get courseId() {
		const { params, } = this.props;

		return params.courseId.toLowerCase();
	}

	componentDidMount() {
		this.loadActiveGroups(this.courseId);
		this.props.enterToCourse(this.courseId);
	}

	loadActiveGroups = (courseId: string) => {
		const { loadingActive, loadedActive } = this.state;

		if(loadedActive || loadingActive) {
			return;
		}

		this.setState({
			loadingActive: true,
		});

		api.groups.getCourseGroups(courseId)
			.then(json => {
				const groups = json.groups;
				this.setState({
					loadedActive: true,
					groups,
				});
			})
			.catch(() => {
				this.setState({
					status: 'error',
				});
			})
			.finally(() =>
				this.setState({
					loadingActive: false,
				})
			);
	};

	loadArchivedGroups = (courseId: string) => {
		const { loadingArchived, loadedArchived } = this.state;

		if(loadedArchived || loadingArchived) {
			return;
		}

		this.setState({
			loadingArchived: true,
		});

		api.groups.getCourseArchivedGroups(courseId, { count: 1000 })
			.then(json => {
				const archiveGroups = json.groups.filter(g => g.isArchived);
				this.setState({
					loadedArchived: true,
					archiveGroups,
				});
			})
			.catch(console.error)
			.finally(() =>
				this.setState({
					loadingArchived: false,
				})
			);
	};

	render() {
		const { courses, userId, } = this.props;
		const { filter, } = this.state;

		const courseById = courses.courseById;
		const course = courseById[this.courseId];

		if(this.state.status === "error") {
			return <Error404/>;
		}

		if(!course) {
			return null;
		}

		return (
			<Page metaTitle={ `Группы в курсе ${ course.title.toLowerCase() }` }>
				<GroupHeader
					onTabChange={ this.onTabChange }
					filter={ this.state.filter }
					course={ course }
					addGroup={ this.addGroup }
				/>
				<GroupList
					courseId={ this.courseId }
					groups={ this.filteredGroups }
					deleteGroup={ this.deleteGroup }
					toggleArchived={ this.toggleArchived }
					loading={ this.loading }
					userId={ userId }
				>
					{ filter === "archived" && <div>
						У вас нет архивных групп. Когда какая-нибудь группа станет вам больше не&nbsp;нужна,
						заархивируйте её.
						Архивные группы будут жить здесь вечно и не&nbsp;помешают вам в&nbsp;текущей работе. Однако если
						понадобится, вы всегда
						сможете вернуться к&nbsp;ним.
					</div> }
					{ filter === "active" && <div>
						У вас нет активных групп. Создайте группу и пригласите в неё студентов, чтобы видеть их
						прогресс, проверять их тесты и делать код-ревью их решений.
					</div> }
				</GroupList>
			</Page>
		);
	}

	onTabChange = (id: string) => {
		this.setState({
			filter: id,
		});

		if(id === "active") {
			this.loadActiveGroups(this.courseId);
		} else {
			this.loadArchivedGroups(this.courseId);
		}
	};

	addGroup = async (groupId: number) => {
		const { navigate, } = this.props;

		const groups = this.filteredGroups;
		const newGroup = await api.groups.getGroup(groupId);

		this.setState({
			groups: [newGroup, ...groups],
		});

		navigate(`/${ this.courseId }/groups/${ groupId }`);
	};

	get filteredGroups() {
		const { filter, archiveGroups, groups, } = this.state;
		if(filter === "archived") {
			return archiveGroups;
		} else {
			return groups;
		}
	}

	deleteGroup = (group: GroupInfo, groupsName: GroupType) => {
		api.groups.deleteGroup(group.id)
			.then(() => {
				Toast.push(`Группа «${ group.name }» удалена`);

				const updateGroups = this.state[groupsName].filter(g => group.id !== g.id);

				this.setState({
					...this.state,
					[groupsName]: updateGroups,
				});
			})
			.catch((error) => {
				error.showToast();
			});
	};

	toggleArchived = (group: GroupInfo, isArchived: boolean) => {
		const newSettings = {
			isArchived
		};

		api.groups.saveGroupSettings(group.id, newSettings)
			.then(() => {
				Toast.push(
					isArchived ? `Группа «${ group.name }» заархивирована` : `Группа «${ group.name }» восстановлена`);

				group = { ...group, ...newSettings };

				if(isArchived) {
					this.moveGroup(group, 'groups', 'archiveGroups');
				} else {
					this.moveGroup(group, 'archiveGroups', 'groups');
				}
			})
			.catch((error) => {
				error.showToast();
			});
	};

	moveGroup = (group: GroupInfo, moveFrom: GroupType, moveTo: GroupType) => {
		const groupsMoveFrom = this.state[moveFrom].filter(g => group.id !== g.id);
		const groupsMoveTo = [group, ...this.state[moveTo]].sort((a, b) => a.name.localeCompare(b.name));

		this.setState({
			...this.state,
			[moveFrom]: groupsMoveFrom,
			[moveTo]: groupsMoveTo
		});
	};

	get loading() {
		if(this.state.filter === "archived") {
			return this.state.loadingArchived;
		} else {
			return this.state.loadingActive;
		}
	}

	static mapStateToProps(state: RootState) {
		return {
			courses: state.courses,
			userId: state.account.id,
		};
	}

	static mapDispatchToProps(dispatch: Dispatch) {
		return {
			enterToCourse: (courseId: string) => dispatch(changeCurrentCourseAction((courseId))),
		};
	}
}

const connected = connect(GroupListPage.mapStateToProps, GroupListPage.mapDispatchToProps)(GroupListPage);

export default withOldRouter(connected);
