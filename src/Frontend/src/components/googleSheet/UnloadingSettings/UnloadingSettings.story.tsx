import UnloadingSettings, { Props } from './UnloadingSettings';
import { Story } from "@storybook/react";
import React from "react";
import { Route, RouteComponentProps, Router } from "react-router-dom";
import { createMemoryHistory } from 'history';
import { MatchParams } from "src/models/router";
import { apiMocked, getMockedTask, } from "../storyUtils";
import {
	getMockedShortUser,
	shortGroupExample,
	shortGroupWithLongNameExample,
	shortGroupWithLongNameExample2,
} from "../../../storiesUtils";
import { returnPromiseAfterDelay } from "../../../utils/storyMock";

export default {
	title: 'GoogleSheet/Settings',
};

const ListTemplate: Story<{ items: { props: Omit<Props, keyof RouteComponentProps<MatchParams>>, header: string, }[] }>
	= ({ items }) => {
	return <>
		{ items.map((item) =>
			<>
				<p>{ item.header }</p>
				<Router history={ createMemoryHistory({ initialEntries: ['/basicprogramming/google-sheet-tasks/0'] }) }>
					<Route
						path={ "/:courseId/google-sheet-tasks/:taskId" }
						render={ (props) => <UnloadingSettings { ...props } { ...item.props } /> }
					/>
				</Router>
			</>
		) }
	</>;
};

export const Default = ListTemplate.bind({});

Default.args = {
	items: [
		{
			props: {
				courseTitle: 'Course',
				api: apiMocked,
			},
			header: 'Default',
		}
	]
};

export const ManyGroups = ListTemplate.bind({});

ManyGroups.args = {
	items: [
		{
			props: {
				courseTitle: 'Course',
				api: {
					...apiMocked,
					getTaskById: (taskId) => {
						return returnPromiseAfterDelay(0, getMockedTask({
							groups: [
								shortGroupExample,
								shortGroupWithLongNameExample,
								shortGroupWithLongNameExample2,
							]
						}));
					},
				},
			},
			header: 'Default',
		}
	]
};

export const LongNameAuthor = ListTemplate.bind({});

LongNameAuthor.args = {
	items: [
		{
			props: {
				courseTitle: 'Course',
				api: {
					...apiMocked,
					getTaskById: (taskId) => {
						return returnPromiseAfterDelay(0, getMockedTask({
							authorInfo: getMockedShortUser(
								{ visibleName: 'Абдулай Шахид АА\'ль Фахид Сараха Фуум Джик Бек Алым Агы' })
						}));
					},
				},
			},
			header: 'Default',
		}
	]
};

