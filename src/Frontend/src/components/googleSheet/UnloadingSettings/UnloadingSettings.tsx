import React, { useState } from "react";
import moment from "moment-timezone";
import { connect } from "react-redux";

import { Link, } from "react-router-dom";
import { Button, Gapped, Loader, Toast, } from "ui";
import Page from "src/pages";
import Error404 from "../../common/Error/Error404";

import { RequestError } from "src/api";
import { GoogleSheetApiInObject } from "../UnloadingsList/UnloadingList";
import { GoogleSheetsExportTaskResponse, GoogleSheetsExportTaskUpdateParams } from "src/models/googleSheet";
import { WithParams, WithRouter } from "src/models/router";
import { UTC } from "src/consts/defaultTimezone";

import {
	convertDefaultTimezoneToLocal,
	serverFormat
} from "src/utils/momentUtils";
import {
	Api,
	isLinkMatchRegexp,
	renderEditableFields,
	renderRefreshPeriodSwitcher,
	texts as baseTexts
} from "../utils";

import { RootState } from "src/redux/reducers";

import styles from './unloadingSettings.less';
import texts from './UnloadingSettings.texts';
import { withOldRouter } from "../../../utils/router";

export type Props = { courseTitle: string; } & GoogleSheetApiInObject & WithRouter;

export interface State extends PartialBy<GoogleSheetsExportTaskUpdateParams, 'spreadsheetId' | 'listId'> {
	link: string;
	task?: GoogleSheetsExportTaskResponse;
	taskId?: string;
	error?: string;
}

function UnloadingSettings({
	api = Api,
	params,
	navigate,
	courseTitle,
}: Props): React.ReactElement {
	const courseTitleInMeta = `Выгрузки в курсе ${ courseTitle }`;
	const [
		state,
		setState,
	] = useState<State>({
		link: '',
		isVisibleForStudents: false,
		refreshTimeInMinutes: 60,
	});
	const { courseId, taskId, } = params;

	if(!state.error && (!state.taskId || taskId !== state.taskId)) {
		const taskIdNumber = parseInt(taskId);
		api.getTaskById(taskIdNumber)
			.then(t => setState({
				...state,
				...t,
				link: buildLink(t.spreadsheetId, t.listId),
				task: t,
				taskId: taskId,
			}))
			.catch(error => setState({
				...state,
				error,
			}));
	}

	if(state.error) {
		return <Error404/>;
	}

	if(!state.task) {
		return <Page metaTitle={ courseTitleInMeta }><Loader/></Page>;
	}

	return (
		<Page metaTitle={ courseTitleInMeta }>
			<Gapped gap={ 8 } vertical>
				{ renderInfoInEditMode(state.task) }
				<h3>{ texts.settings }</h3>
				{ renderRefreshPeriodSwitcher(state.refreshTimeInMinutes, changeRefreshInterval) }
				{ renderEditableFields(
					state.isVisibleForStudents,
					changeVisibility,
					state.refreshStartDate,
					changeRefreshStartDate,
					state.refreshEndDate,
					changeRefreshEndDate,
					state.link,
					changeLink,
				) }
				{ renderButtonsInEditMode(state.task) }
			</Gapped>
		</Page>
	);

	function renderInfoInEditMode(task: GoogleSheetsExportTaskResponse) {
		return <header className={ styles.groupHeader }>
			<div className={ styles.linkToPrevPageBlock }>
				<div className={ styles.linkToPrevPage }>
					<Link to={ `/${ courseId }/google-sheet-tasks` }>{ texts.backToList }</Link>
				</div>
			</div>
			<h2 className={ styles.groupsName }>
				{ task.groups
					.map(g => g.name)
					.join(', ') }
			</h2>
			<p> { baseTexts.task.buildAuthor(task.authorInfo.visibleName) }</p>
			{
				task.lastUpdateDate &&
				<p>
					{ baseTexts.extra.buildLastUploadDate(task.lastUpdateDate) }
				</p>
			}
			{
				task.lastUpdateErrorMessage &&
				<p className={ styles.errorMessage }
				   dangerouslySetInnerHTML={ {
					   __html: baseTexts.extra.buildErrorWhileUploading(task.lastUpdateErrorMessage)
				   } }>
				</p>
			}
		</header>;
	}

	function renderButtonsInEditMode(task: GoogleSheetsExportTaskResponse) {
		return (
			<div className={ styles.buttonsWrapper }>
				<Button use={ 'primary' }
						disabled={ isTasksEqual(task, state) || !isLinkMatchRegexp(state.link) }
						onClick={ saveTask }>
					{ baseTexts.button.save }
				</Button>

				<Button use={ 'default' }
						onClick={ exportTask }>
					{ baseTexts.button.export }
				</Button>

				<Button use={ 'danger' }
						onClick={ deleteTask }>
					{ baseTexts.button.delete }
				</Button>
			</div>
		);
	}

	function isTasksEqual(
		task: GoogleSheetsExportTaskResponse,
		updatedTask: Partial<GoogleSheetsExportTaskUpdateParams>
	): boolean {
		return task.isVisibleForStudents === updatedTask.isVisibleForStudents &&
			task.refreshStartDate === updatedTask.refreshStartDate &&
			task.refreshEndDate === updatedTask.refreshEndDate &&
			task.refreshTimeInMinutes === updatedTask.refreshTimeInMinutes &&
			task.spreadsheetId == updatedTask.spreadsheetId &&
			task.listId == updatedTask.listId;
	}

	function changeVisibility(): void {
		setState({
			...state,
			isVisibleForStudents: !state.isVisibleForStudents,
		});
	}

	function changeRefreshInterval(value: string): void {
		setState({
			...state,
			refreshTimeInMinutes: parseInt(value, 10),
		});
	}

	function changeRefreshStartDate(value: string): void {
		const { refreshEndDate } = state;

		const curMoment = convertDefaultTimezoneToLocal(moment(value, 'DD.MM.yyyy').format());
		const date = curMoment.format();
		setState({
			...state,
			refreshStartDate: date,
		});
		if(curMoment.diff(refreshEndDate) > 0) {
			setState({
				...state,
				refreshEndDate: date,
			});
		}
	}

	function changeRefreshEndDate(value: string): void {
		setState({
			...state,
			refreshEndDate: convertDefaultTimezoneToLocal(moment(value, 'DD.MM.yyyy').format()).format()
		});
	}

	function changeLink(value: string): void {
		const spreadsheetId = /spreadsheets\/d\/(.+)\//.exec(value)?.[1];
		const listId = /edit#gid=(\d+)/.exec(value)?.[1];

		setState({
			...state,
			link: value,
			spreadsheetId,
			listId: listId ? parseInt(listId) : undefined,
		});
	}

	function buildLink(spreadsheetId: string | undefined, listId: number | undefined): string {
		return `https://docs.google.com/spreadsheets/d/${ spreadsheetId !== undefined ? spreadsheetId : '' }/edit#gid=${ listId !== undefined ? listId : '' }`;
	}

	function getFieldsFromState(): GoogleSheetsExportTaskUpdateParams | null {
		if(state.listId === undefined || state.spreadsheetId === undefined) {
			return null;
		}

		return {
			isVisibleForStudents: state.isVisibleForStudents,
			refreshTimeInMinutes: state.refreshTimeInMinutes,
			refreshStartDate: state.refreshStartDate,
			refreshEndDate: state.refreshEndDate,
			listId: state.listId,
			spreadsheetId: state.spreadsheetId,
		};
	}

	function saveTask(): void {
		if(state.task) {
			const fields = getFieldsFromState();

			if(!fields) {
				return;
			}

			api.updateTask(state.task.id, fields)
				.then(() => {
					Toast.push(baseTexts.toast.save);
					setState({
						...state,
						task: state.task && {
							...state.task,
							...fields,
						},
					});
				})
				.catch(() => Toast.push(baseTexts.toast.saveFail));
		}
	}

	function exportTask(): void {
		if(state.task) {
			api.exportTaskNow(state.task.id)
				.then(() => {
					Toast.push(baseTexts.toast.export);
					setState({
						...state,
						task: state.task && {
							...state.task,
							lastUpdateDate: moment().local().tz(UTC).format(serverFormat),
							lastUpdateErrorMessage: null,
						},
					});
				})
				.catch((err) => {
					Toast.push(baseTexts.toast.exportFail);
					(err as RequestError)
						.response.text()
						.then(text => {
							setState({
								...state,
								task: state.task && {
									...state.task,
									lastUpdateDate: moment().local().tz(UTC).format(serverFormat),
									lastUpdateErrorMessage: `${ text }`,
								},
							});
						});
				});
		}
	}

	function deleteTask(): void {
		if(state.task) {
			api.deleteTask(state.task.id)
				.then(() => {
					Toast.push(baseTexts.toast.delete);
					navigate(`/${ courseId }/google-sheet-tasks`);
				})
				.catch(() => Toast.push(baseTexts.toast.deleteFail));
		}
	}
}

const mapStateToProps = (state: RootState, { params }: WithParams) => {
	return {
		courseTitle: state.courses?.courseById[params.courseId]?.title?.toLowerCase() ?? '',
	};
};

export default withOldRouter(connect(mapStateToProps)(UnloadingSettings));
