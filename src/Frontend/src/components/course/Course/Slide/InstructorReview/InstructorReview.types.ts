import React from "react";

import { UserInfo } from "src/utils/courseRoles";
import { ShortGroupInfo } from "src/models/comments";
import { AntiPlagiarismStatusResponse, FavouriteReview, FavouriteReviewResponse, } from "src/models/instructor";
import { ShortUserInfo } from "src/models/users";
import { ReviewCommentResponse, ReviewInfo, SubmissionInfo } from "src/models/exercise";
import { GroupsInfoResponse } from "src/models/groups";
import { ReviewInfoWithMarker, TextMarkersByReviewId } from "../Blocks/Exercise/ExerciseUtils";
import { InstructorReviewTabs } from "./InstructorReviewTabs";
import { DiffInfo } from "./utils";
import CodeMirror, { Editor } from "codemirror";
import { FavouriteReviewRedux, LastUsedReview, } from "src/redux/instructor";
import { SlideContext } from "../Slide.types";
import { DeadLineInfo, DeadLinesResponse } from "src/models/deadLines";
import { WithNavigate } from "src/models/router";

export interface PropsFromRedux {
	user?: UserInfo;

	favouriteReviews?: FavouriteReviewRedux[];
	lastUsedReviews?: LastUsedReview[];

	student?: ShortUserInfo;
	studentGroups?: ShortGroupInfo[];
	studentSubmissions?: SubmissionInfo[];

	curScore: number | null;
	prevScore: number | null;

	lastManualCheckingSubmissionId?: number;
	lastCheckedSubmissionId?: number;

	antiPlagiarismStatus?: AntiPlagiarismStatusResponse;
	antiPlagiarismStatusError: boolean;
	antiPlagiarismStatusLoading: boolean;
	prohibitFurtherManualChecking: boolean;

	deadLines: DeadLineInfo[] | undefined;
}

export interface ApiFromRedux {
	setNextSubmissionButtonDisabled: (disabled: boolean) => void;
	getStudentInfo: (studentId: string,) => Promise<ShortUserInfo | string>;
	getAntiPlagiarismStatus: (courseId: string,
		submissionId: number,
	) => Promise<AntiPlagiarismStatusResponse | string>;
	getFavouriteReviews: (courseId: string, slideId: string,) => Promise<FavouriteReviewResponse | string>;
	getStudentGroups: (courseId: string, studentId: string,) => Promise<GroupsInfoResponse | string>;

	onScoreSubmit: (submissionId: number, score: number, oldScore: number | null,) => Promise<Response | void>;
	prohibitFurtherReview: (courseId: string, slideId: string, userId: string, prohibit: boolean) => Promise<Response>;

	addFavouriteReview: (courseId: string, slideId: string, text: string) => Promise<FavouriteReview>;
	deleteFavouriteReview: (courseId: string, slideId: string, favouriteReviewId: number) => Promise<Response>;

	addReview: (
		submissionId: number,
		comment: string,
		startLine: number,
		startPosition: number,
		finishLine: number,
		finishPosition: number,
	) => Promise<ReviewInfo | void>;
	addReviewComment: (submissionId: number, reviewId: number,
		comment: string
	) => Promise<ReviewCommentResponse | void>;
	enableManualChecking: (submissionId: number,) => Promise<Response | string>;
	deleteReview: (submissionId: number, reviewId: number, isBotReview?: boolean) => Promise<Response>;
	deleteReviewComment: (submissionId: number, reviewId: number, commentId: number) => Promise<Response>;
	editReviewOrComment: (submissionId: number, reviewId: number,
		parentReviewId: number | undefined, text: string, oldText: string,
	) => Promise<ReviewInfo | ReviewCommentResponse | string>;

	assignBotReview: (submissionId: number, review: ReviewInfo) => Promise<ReviewInfo>;
	loadDeadLines: (courseId: string, studentId: string) => Promise<string | DeadLinesResponse>;
}

export interface PropsFromSlide {
	authorSolution?: React.ReactNode;
	formulation?: React.ReactNode;
	slideContext: SlideContext;
	expectedOutput?: string | null;
}

export type Props = PropsFromRedux & ApiFromRedux & PropsFromSlide & WithNavigate;

export interface InstructorExtraFields {
	outdated?: boolean;
	isFavourite?: boolean;
}

export interface ReviewCompare {
	comment: string;
	id: number;
	comments: string[];
	startLine: number;
	anchor?: number;
	instructor?: InstructorExtraFields;
}

export interface InstructorReviewInfo extends ReviewInfo {
	instructor?: InstructorExtraFields;
}

export interface InstructorReviewInfoWithAnchor extends InstructorReviewInfo {
	anchor: number;
}

export type InstructorReviewInfoWithMarker = InstructorReviewInfo & ReviewInfoWithMarker;

export interface State {
	currentTab: InstructorReviewTabs;

	currentSubmission: SubmissionInfo | undefined;
	currentSubmissionContext: SubmissionContext | undefined;

	showDiff: boolean;
	diffInfo?: DiffInfo;

	selectedReviewId: number;
	reviews: ReviewInfo[];
	outdatedReviews: ReviewInfo[];
	markers: TextMarkersByReviewId;

	editor: null | Editor;

	addCommentValue: string;
	addCommentFormCoords?: { left: number; top: number; bottom: number };
	addCommentFormExtraSpace?: number;
	addCommentRanges?: { startRange: CodeMirror.Position; endRange: CodeMirror.Position; };
	addCommentSelections?: CodeMirror.Range[];

	initialCode?: string;

	favouriteReviewsSet: Set<string>;
	favouriteByUserSet: Set<string>;
}

export interface SubmissionContext {
	isLastCheckedSubmission: boolean;
	isLastSubmissionWithManualChecking: boolean;
	isEditable: boolean;
}
