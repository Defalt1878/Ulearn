import React, { FC, LazyExoticComponent, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router-dom';
import { AccountState } from "src/redux/account";

import { getQueryStringParameter } from "src/utils";
import CourseLoader from './components/course/Course/CourseLoader';

const Course = React.lazy(
	() => import(/* webpackChunkName: "course" */ "src/components/course/Course/Course.redux")
);

const TokenPage = React.lazy(
	() => import(/* webpackChunkName: "token" */ "src/components/token/TokenPage")
);

const JoinGroup = React.lazy(
	() => import(/* webpackChunkName: "joinGroup" */ "src/components/groups/JoinGroup/JoinGroup.page")
);

const GroupListPage = React.lazy(
	() => import(/* webpackChunkName: "groupsList" */ "src/pages/course/groups/GroupsListPage/GroupListPage")
);

const GroupSettingsPage = React.lazy(
	() => import(/* webpackChunkName: "groupSettings" */ "src/pages/course/groups/GroupsSettingsPage/GroupSettingsPage")
);

const UnloadingList = React.lazy(
	() => import(/* webpackChunkName: "unloadingsList" */ "src/components/googleSheet/UnloadingsList")
);

const UnloadingSettings = React.lazy(
	() => import(/* webpackChunkName: "unloadingSettings" */ "src/components/googleSheet/UnloadingSettings")
);

const ReviewQueuePage = React.lazy(
	() => import(/* webpackChunkName: "reviewQueue" */ "src/components/reviewQueue/ReviewQueuePageConnected")
);

const AnyPage = React.lazy(
	() => import(/* webpackChunkName: "anyPage" */ "src/pages/AnyPage")
);

interface Props {
	account: AccountState;
}

interface RouteProps {
	key: React.Key,
	path: string;
	element: React.ReactElement;
}

function Router({ account }: Props): React.ReactElement {
	const routes: RouteProps[] = [
		{
			key: 'groups',
			path: '/Admin/Groups',
			element: <RedirectLegacyPage to={ "/:courseId/groups" }/>
		},
		{
			key: 'course',
			path: '/course/:courseId/:slideSlugOrAction/*',
			element: wrapLazyPage(Course)
		},
		{
			key: 'token',
			path: '/token',
			element: wrapLazyPage(TokenPage)
		}
	];

	if (account.accountLoaded) {
		if (account.isAuthenticated) {
			routes.push(
				{
					key: 'groupsList',
					path: '/groups/:hash',
					element: wrapLazyPage(JoinGroup)
				},
				{
					key: 'groupsList',
					path: '/:courseId/groups/',
					element: wrapLazyPage(GroupListPage)
				},
				{
					key: 'groupPage',
					path: '/:courseId/groups/:groupId/',
					element: wrapLazyPage(GroupSettingsPage)
				},
				{
					key: 'groupPageSettings',
					path: '/:courseId/groups/:groupId/:groupPage',
					element: wrapLazyPage(GroupSettingsPage)
				},
				{
					key: 'googleSheetList',
					path: '/:courseId/google-sheet-tasks/',
					element: wrapLazyPage(UnloadingList)
				},
				{
					key: 'googleSheetPage',
					path: '/:courseId/google-sheet-tasks/:taskId',
					element: wrapLazyPage(UnloadingSettings)
				},
				{
					key: 'reviewQueuePage',
					path: '/:courseId/review-queue',
					element: wrapLazyPage(ReviewQueuePage)
				}
			);
		}
		routes.push({
			key: 'anyPage',
			path: '*',
			element: wrapLazyPage(AnyPage)
		});
	}

	return (
		<Routes>
			{ routes.map(props =>
				<Route
					key={ props.key }
					path={ props.path }
					element={ props.element }
				/>
			) }
		</Routes>
	);
}

const RedirectLegacyPage = ({ to }: { to: string }) => {
	const courseId = getQueryStringParameter("courseId");
	if (courseId) {
		to = to.replace(":courseId", courseId);
	}

	return <Navigate to={ to }/>;
};

function wrapLazyPage(LazyPage: LazyExoticComponent<FC>): React.ReactElement {
	return <Suspense fallback={ <CourseLoader/> }>
		<LazyPage/>
	</Suspense>;
}

export default Router;
