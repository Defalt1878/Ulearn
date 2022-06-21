import React from 'react';
import { Route, Routes, Navigate } from 'react-router-dom';

import AnyPage from "src/pages/AnyPage";
import GroupListPage from "src/pages/course/groups/GroupListPage";
import GroupPage from "src/pages/course/groups/GroupPage";
import Course from 'src/components/course/Course/Course.redux';

import { getQueryStringParameter } from "src/utils";
import { AccountState } from "src/redux/account";
import UnloadingList from "src/components/googleSheet/UnloadingsList";
import UnloadingSettings from "src/components/googleSheet/UnloadingSettings";

interface Props {
	account: AccountState;
}

function Router({ account }: Props): React.ReactElement {
	let routes = [
		<Route key={ 'groups' }
			   path="/Admin/Groups"
			   element={ <RedirectLegacyPage to={ "/:courseId/groups" }/> }
		/>,
		<Route key={ 'course' }
			   path="/course/:courseId/:slideSlugOrAction/*"
			   element={ <Course/> }
		/>,
	];

	if(account.accountLoaded) {
		if(account.isAuthenticated) {
			routes = [
				...routes,
				<Route key={ 'groupsList' }
					   path={ "/:courseId/groups/" }
					   element={ <GroupListPage/> }
				/>,
				<Route key={ 'groupPage' }
					   path={ "/:courseId/groups/:groupId/" }
					   element={ <GroupPage/> }
				/>,
				<Route key={ 'groupPageSettings' }
					   path={ "/:courseId/groups/:groupId/:groupPage" }
					   element={ <GroupPage/> }
				/>,
				<Route key={ 'googleSheetList' }
					   path={ "/:courseId/google-sheet-tasks/" }
					   element={ <UnloadingList/> }
				/>,
				<Route key={ 'googleSheetPage' }
					   path={ "/:courseId/google-sheet-tasks/:taskId" }
					   element={ <UnloadingSettings/> }
				/>,
			];
		}
		routes.push(<Route key={ 'anyPage' } path={ "*" } element={ <AnyPage/> }/>);
	}

	return (
		<Routes>
			{ routes }
		</Routes>
	);
}

const RedirectLegacyPage = ({ to }: { to: string }) => {
	const courseId = getQueryStringParameter("courseId");
	if(courseId) {
		to = to.replace(":courseId", courseId);
	}

	return <Navigate to={ to }/>;
};

export default Router;
