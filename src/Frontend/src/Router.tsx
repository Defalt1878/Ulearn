import React, { Component } from 'react';
import { Redirect, Route, Switch } from 'react-router-dom';

import AnyPage from "src/pages/AnyPage";
import GroupListPage from "src/pages/course/groups/GroupListPage";
import GroupPage from "src/pages/course/groups/GroupPage.js";
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
		<Route key={ 'groups' } path="/Admin/Groups" component={ redirectLegacyPage("/:courseId/groups") }/>,
		<Route key={ 'course' } path="/course/:courseId/:slideSlugOrAction" component={ Course }/>,
	];

	if(account.accountLoaded) {
		if(account.isAuthenticated) {
			routes = [
				...routes,
				<Route key={ 'groupsList' } path="/:courseId/groups/" component={ GroupListPage } exact/>,
				<Route key={ 'groupPage' } path="/:courseId/groups/:groupId/" component={ GroupPage } exact/>,
				<Route key={ 'googleSheetList' } path="/:courseId/google-sheet-tasks/" component={ UnloadingList }
					   exact/>,
				<Route key={ 'googleSheetPage' } path="/:courseId/google-sheet-tasks/:taskId"
					   component={ UnloadingSettings } exact/>,
				<Route key={ 'groupPageSettings' } path="/:courseId/groups/:groupId/:groupPage" component={ GroupPage }
					   exact/>,
			];
		}
		routes.push(<Route key={ 'anyPage' } component={ AnyPage }/>);
	}

	return (
		<Switch>
			{ routes }
		</Switch>
	);
}

function redirectLegacyPage(to: string) {
	return class extends Component {
		constructor(props: Record<string, unknown> | Readonly<Record<string, unknown>>) {
			super(props);
			const courseId = getQueryStringParameter("courseId");
			if(courseId) {
				to = to.replace(":courseId", courseId);
			}
		}

		render() {
			return <Redirect to={ to }/>;
		}
	};
}

export default Router;
