import React from "react";
import { SlideType } from "src/models/slide";
import Meta from "src/consts/Meta";
import { CourseInfo, UnitInfo, UnitsInfo } from "src/models/course";
import { CourseStatistics, FlashcardsStatistics } from "../Navigation/types";
import { SlideInfo } from "./CourseUtils";
import { AccountState } from "src/redux/account";
import { SlideUserProgress } from "src/models/userProgress";
import { DeadLineInfo } from "src/models/deadLines";
import { WithRouter } from "src/models/router";

export interface State {
	Page: React.ComponentType | React.ElementType;
	title?: string;
	highlightedUnit: string | null;
	currentCourseId: string;
	currentSlideId?: string;
	currentSlideType?: SlideType;
	meta: Meta;

	openedUnit?: UnitInfo;

	courseStatistics: CourseStatistics;
}

export interface CourseProps extends WithRouter {
	courseId: string;
	slideInfo: SlideInfo;

	courseInfo: CourseInfo;
	user: AccountState;
	progress: { [p: string]: SlideUserProgress };
	units: UnitsInfo | null;
	courseLoadingErrorStatus: string | null;
	courseLoading: boolean;
	loadedCourseIds: Record<string, unknown>;
	flashcardsStatisticsByUnits?: { [unitId: string]: FlashcardsStatistics },
	flashcardsLoading: boolean;
	deadLines?: DeadLineInfo[];

	isStudentMode: boolean;
	navigationOpened: boolean;
	isSlideReady: boolean;
	isHijacked: boolean;

	enterToCourse: (courseId: string) => void;
	loadCourse: (courseId: string) => void;
	loadFlashcards: (courseId: string) => void;
	loadCourseErrors: (courseId: string) => void;
	loadUserProgress: (courseId: string, userId: string) => void;
	updateVisitedSlide: (courseId: string, slideId: string) => void;
	loadDeadLines: (courseId: string) => void;
}
