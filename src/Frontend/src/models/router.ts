import { NavigateFunction } from "react-router/lib/hooks";

export interface MatchParams {
	courseId: string;
	slideSlugOrAction?: string;
	groupId?: string;
	groupPage?: string; // if not undefined => groupId is not undefined
	taskId: string;
	groupsSettings?: string; //start page if navigated from /course/groups/
}

export type WithRouter = WithLocation & WithNavigate & WithParams;

export interface WithLocation {
	location: Location;
}

export interface WithNavigate {
	navigate: NavigateFunction;
}

export interface WithParams {
	params: MatchParams;
}
