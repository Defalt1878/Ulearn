import React, { useState } from "react";
import moment from "moment-timezone";
import { connect } from "react-redux";

import { Loader, Toast } from "ui";
import UnloadingHeader from "./UnloadingHeader";
import UnloadingListItem from "./UnloadingListItem";
import Error404 from "../../common/Error/Error404";
import Page from "src/pages";

import {
	GoogleSheetsCreateTaskParams,
	GoogleSheetsExportTaskListResponse,
	GoogleSheetsExportTaskResponse,
	GoogleSheetsExportTaskUpdateParams,
} from "src/models/googleSheet";
import { GroupInfo } from "src/models/groups";
import { WithParams } from "src/models/router";

import { Api, texts as toastTexts } from '../utils';
import { withParams } from "src/utils/router";

import { RootState } from "src/redux/reducers";

import styles from "./unloadingList.less";
import texts from "./UnloadingList.texts";

export interface GoogleSheetApiInObject {
	api?: GoogleSheetApi;
}

export interface GoogleSheetApi {
	deleteTask: (taskId: number) => Promise<Response>;
	exportTaskNow: (taskId: number) => Promise<Response>;
	getAllCourseTasks: (courseId: string) => Promise<GoogleSheetsExportTaskListResponse>;
	createTask: (params: GoogleSheetsCreateTaskParams) => Promise<GoogleSheetsExportTaskResponse>;
	updateTask: (
		taskId: number,
		fieldsToUpdate: Partial<GoogleSheetsExportTaskUpdateParams>
	) => Promise<Response>;
	getAllCourseGroups: (courseId: string) => Promise<{ groups: GroupInfo[] }>;
	getTaskById: (taskId: number) => Promise<GoogleSheetsExportTaskResponse>;
}

interface Props extends GoogleSheetApiInObject, WithParams {
	userId?: string | null;
	courseTitle: string;
}

interface State {
	courseId: string;
	loading?: boolean;
	tasks?: GoogleSheetsExportTaskResponse[];
	error?: string;
}

function UnloadingList({
	userId,
	params,
	api = Api,
	courseTitle,
}: Props): React.ReactElement {
	const { courseId, } = params;
	const [{ loading, tasks, error, ...state }, setState] = useState<State>({ courseId });
	const courseTitleInMeta = `Выгрузки в курсе ${ courseTitle }`;
	const apiToProvide: GoogleSheetApi = {
		...api,
		createTask: addTask,
		deleteTask: deleteTask,
		updateTask: updateTask,
	};

	if(!loading && !error && (!tasks || state.courseId !== courseId)) {
		setState({
			courseId,
			loading: true,
		});

		api.getAllCourseTasks(courseId)
			.then(r => {
				const tasks = r.googleSheetsExportTasks;

				sortTasks(tasks);

				setState({
					courseId,
					tasks,
				});
			})
			.catch(error => setState({
				courseId,
				error,
			}));
	}

	if(error) {
		return <Error404/>;
	}

	return (
		<Page metaTitle={ courseTitleInMeta }>
			<UnloadingHeader courseId={ courseId } api={ apiToProvide }/>
			<section className={ styles.wrapper }>
				{ loading &&
					<div className={ styles.loaderWrapper }>
						<Loader type={ "big" } active={ true }/>
					</div>
				}
				{
					tasks && tasks.length > 0 &&
					<div className={ styles.content }>
						{ tasks.map(task =>
							<UnloadingListItem
								courseId={ courseId }
								key={ task.id }
								task={ task }
								api={ apiToProvide }
							/>)
						}
					</div>
				}
				{ !loading
					&& tasks
					&& tasks.length === 0
					&& <div className={ styles.noTasks }>
						{ texts.noTasks }
					</div>
				}
			</section>
		</Page>
	);

	function sortTasks(tasks: GoogleSheetsExportTaskResponse[]): void {
		tasks.sort((a, b) => {
			if(userId) {
				const isUserInA = a.authorInfo.id === userId;
				const isUserInB = b.authorInfo.id === userId;

				if(isUserInA && isUserInB) {
					return 0;
				}

				if(isUserInA) {
					return -1;
				}

				if(isUserInB) {
					return 1;
				}
			}

			if(a.refreshEndDate && b.refreshEndDate) {
				return moment(a.refreshEndDate)
					.diff(b.refreshEndDate);
			}

			return 0;
		});
	}

	function addTask(params: GoogleSheetsCreateTaskParams) {
		return api.createTask(params)
			.then((task) => {
				const newTasks = [...(tasks || []), task];
				sortTasks(newTasks);
				setState({
					...state,
					tasks: newTasks,
				});

				Toast.push(toastTexts.toast.add);

				return task;
			})
			.catch(err => {
				Toast.push(toastTexts.toast.addFail);
				throw  err;
			});
	}

	function deleteTask(taskId: number) {
		return api.deleteTask(taskId)
			.then(response => {
				const newTasks = tasks?.filter(t => t.id !== taskId);
				setState({
					...state,
					tasks: newTasks,
				});
				Toast.push(toastTexts.toast.delete);
				return response;
			})
			.catch(err => {
				Toast.push(toastTexts.toast.deleteFail);
				throw  err;
			});
	}

	function updateTask(taskId: number, fieldsToUpdate: Partial<GoogleSheetsExportTaskUpdateParams>) {
		return api.updateTask(taskId, fieldsToUpdate)
			.then(response => {
				const newTasks = [...(tasks || [])];
				const taskIndex = newTasks.findIndex(t => t.id === taskId);
				if(taskIndex > -1) {
					newTasks[taskIndex] = {
						...newTasks[taskIndex],
						...fieldsToUpdate,
					};

					setState({
						...state,
						tasks: newTasks,
					});
					Toast.push(toastTexts.toast.save);
				}
				return response;
			})
			.catch(err => {
				Toast.push(toastTexts.toast.saveFail);
				throw  err;
			});
	}
}

const mapStateToProps = (state: RootState, { params }: WithParams) => ({
	courseTitle: state.courses?.courseById[params.courseId]?.title?.toLowerCase() ?? '',
});

export default withParams(connect(mapStateToProps)(UnloadingList));
