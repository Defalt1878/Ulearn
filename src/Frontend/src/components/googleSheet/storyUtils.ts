import api from "src/api";
import {
	GoogleSheetsCreateTaskParams,
	GoogleSheetsExportTaskResponse,
	GoogleSheetsExportTaskUpdateParams
} from "src/models/googleSheet";
import { returnPromiseAfterDelay } from "src/utils/storyMock";
import { getMockedGroup, getMockedShortGroup, getMockedUser } from "../../storiesUtils";
import { GoogleSheetApi } from "./UnloadingsList/UnloadingList";

export const apiWithRealServer: GoogleSheetApi = {
	getAllCourseGroups: api.groups.getCourseGroups,
	getAllCourseTasks: api.googleSheet.getAllCourseTasks,
	createTask: api.googleSheet.addNewTask,
	updateTask: api.googleSheet.updateCourseTask,
	deleteTask: api.googleSheet.deleteTask,
	exportTaskNow: api.googleSheet.exportTaskNow,
	getTaskById: api.googleSheet.getTaskById,
};

export const apiMocked: GoogleSheetApi = {
	createTask: (params: GoogleSheetsCreateTaskParams) => returnPromiseAfterDelay(100, {
		id: 1,
		groups: params.groupsIds.map(id => getMockedShortGroup({ id })),
		...params,
		authorInfo: getMockedUser(),
		lastUpdateErrorMessage: null,
		lastUpdateDate: null,
	}),
	deleteTask: (taskId: number) => returnPromiseAfterDelay(100),
	exportTaskNow: (taskId: number) => returnPromiseAfterDelay(100),
	getAllCourseGroups: (courseId: string) => returnPromiseAfterDelay(100, { groups: [getMockedGroup()] }),
	getAllCourseTasks: (courseId: string) => returnPromiseAfterDelay(100, { googleSheetsExportTasks: [] }),
	getTaskById: (taskId: number) => returnPromiseAfterDelay(100, getMockedTask({ id: taskId })),
	updateTask: (taskId: number, params: Partial<GoogleSheetsExportTaskUpdateParams>) => returnPromiseAfterDelay(100)
};

export const getMockedTask = (task?: Partial<GoogleSheetsExportTaskResponse>): GoogleSheetsExportTaskResponse => {
	return {
		id: task?.id || 1,
		groups: task?.groups || [getMockedShortGroup()],
		authorInfo: task?.authorInfo || getMockedUser(),
		listId: task?.listId || 1,
		spreadsheetId: task?.spreadsheetId || '1',
		isVisibleForStudents: task?.isVisibleForStudents || false,
		refreshTimeInMinutes: task?.refreshTimeInMinutes || 60,
		refreshEndDate: task?.refreshEndDate || new Date(2020, 11, 10).toDateString(),
		refreshStartDate: task?.refreshStartDate || new Date(2020, 5, 10).toDateString(),
		lastUpdateDate: null,
		lastUpdateErrorMessage: null,
	};
};
