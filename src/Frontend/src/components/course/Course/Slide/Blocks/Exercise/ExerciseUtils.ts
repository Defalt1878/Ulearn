import {
	AutomaticExerciseCheckingResult,
	AutomaticExerciseCheckingResult as CheckingResult, ReviewInfo,
	SolutionRunStatus,
	SubmissionInfo
} from "src/models/exercise";
import { Language } from "src/consts/languages";
import CodeMirror, { Doc, MarkerRange, TextMarker } from "codemirror";
import { ReduxData } from "src/redux";
import { ReviewCompare } from "../../InstructorReview/InstructorReview.types";
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/python/python.js';
import 'codemirror/mode/python/python.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/javascript/javascript.js';
import 'codemirror/mode/xml/xml.js';
import 'codemirror/mode/javascript/javascript.js';
import 'codemirror/mode/css/css.js';
import 'codemirror/mode/haskell/haskell.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/clike/clike.js';
import 'codemirror/mode/sql/sql.js';
import 'codemirror/mode/jsx/jsx.js';
import 'codemirror/mode/xml/xml.js';

enum SubmissionColor {
	MaxResult = "MaxResult", // Студенту больше ничего не может сделать, ни сийчам ни в будущем
	NeedImprovements = "NeedImprovements", // Студент может доработать задачу сейчас или в будущем
	WrongAnswer = "WrongAnswer", // Тесты не пройдены или ошибка компиляции, показывается даже в старых версиях
	Message = "Message", // Сообщение, ни на что не влияющее, например, старая версия
}

type ReviewInfoWithMarker = ReviewInfo & {
	markers: TextMarker[];
}

export interface TextMarkersByReviewId {
	[reviewId: number]: TextMarker[];
}

function getSubmissionColor(
	solutionRunStatus: SolutionRunStatus | undefined,
	checkingResult: CheckingResult | undefined, // undefined если automaticChecking null
	hasSuccessSolution: boolean, // Задача прошла автопроверку или автопроверки нет?
	selectedSubmissionIsLast: boolean, // Это последнее решение?
	selectedSubmissionIsLastSuccess: boolean, // Это последнее решение, прошедшее тесты?
	waitingForManualChecking: boolean,
	prohibitFurtherManualChecking: boolean,
	isSkipped: boolean,
	isMaxScore: boolean, // Балл студента равен максимальному за задачу
): SubmissionColor {
	if(solutionRunStatus === SolutionRunStatus.CompilationError
		|| checkingResult === CheckingResult.CompilationError || checkingResult === CheckingResult.WrongAnswer || checkingResult == CheckingResult.RuntimeError) {
		return SubmissionColor.WrongAnswer;
	}
	if(solutionRunStatus === SolutionRunStatus.Ignored) {
		return SubmissionColor.NeedImprovements;
	}
	if(isSkipped) {
		return selectedSubmissionIsLast ? SubmissionColor.MaxResult : SubmissionColor.Message;
	}
	if(selectedSubmissionIsLastSuccess) {
		return !isMaxScore && !prohibitFurtherManualChecking && waitingForManualChecking
			? SubmissionColor.NeedImprovements
			: SubmissionColor.MaxResult;
	}
	return selectedSubmissionIsLast && !isMaxScore && !prohibitFurtherManualChecking && waitingForManualChecking
		? SubmissionColor.NeedImprovements
		: SubmissionColor.Message;
}

function isSuccessSubmission(submission: SubmissionInfo | null): boolean {
	return !!submission && (submission.automaticChecking == null || submission.automaticChecking.result === CheckingResult.RightAnswer);
}

function hasSuccessSubmission(submissions: SubmissionInfo[]): boolean {
	return submissions.some(isSuccessSubmission);
}

function submissionIsLast(submissions: SubmissionInfo[], submission: SubmissionInfo | null): boolean {
	return submissions.length > 0 && submissions[0] === submission;
}

function getLastSuccessSubmission(submissions: SubmissionInfo[]): SubmissionInfo | null {
	const successSubmissions = submissions.filter(isSuccessSubmission);
	if(successSubmissions.length > 0) {
		return successSubmissions[0];
	}
	return null;
}

function isFirstRightAnswer(submissions: SubmissionInfo[], successSubmission: SubmissionInfo): boolean {
	const successSubmissions = submissions.filter(isSuccessSubmission);
	return successSubmissions.length > 0 && successSubmissions[successSubmissions.length - 1] === successSubmission;
}

function getReviewsWithoutDeleted(reviews: ReviewInfoWithMarker[]): ReviewInfoWithMarker[] {
	return reviews.map(r => ({
		...r, comments: r.comments.filter(c => {
				const data = (c as ReduxData);
				return data && !data.isDeleted && !data.isLoading;
			}
		)
	}));
}

function getAllReviewsFromSubmission(submission: SubmissionInfo): ReviewInfo[] {
	if(!submission) {
		return [];
	}

	const manual = submission.manualChecking?.reviews || [];
	const auto = submission.automaticChecking && submission.automaticChecking.reviews ? submission.automaticChecking.reviews : [];
	return manual.concat(auto);
}

function createTextMarker(
	finishLine: number, finishPosition: number,
	startLine: number, startPosition: number,
	className: string,
	exerciseCodeDoc: Doc,
): TextMarker {
	return exerciseCodeDoc.markText({
		line: startLine,
		ch: startPosition
	}, {
		line: finishLine,
		ch: finishPosition
	}, {
		className,
	});
}

function getReviewsWithTextMarkers(
	submission: SubmissionInfo,
	exerciseCodeDoc: Doc,
	markerClassName: string,
): ReviewInfoWithMarker[] {
	const reviews = getAllReviewsFromSubmission(submission);
	const reviewsWithTextMarkers: ReviewInfoWithMarker[] = [];

	for (const review of reviews) {
		const { finishLine, finishPosition, startLine, startPosition } = review;
		const textMarker = createTextMarker(finishLine, finishPosition, startLine, startPosition,
			markerClassName,
			exerciseCodeDoc);

		reviewsWithTextMarkers.push({
			markers: [textMarker],
			...review
		});
	}

	return reviewsWithTextMarkers;
}

export function getTextMarkersByReviews(
	reviews: ReviewInfo[],
	exerciseCodeDoc: Doc,
	markerClassName: string,
	escapeLines?: Set<number>,
): TextMarkersByReviewId {
	const textMarkersByReviewId: TextMarkersByReviewId = {};

	for (const review of reviews) {
		const {
			finishLine,
			finishPosition,
			startLine,
			startPosition,
		} = review;

		let positions = [
			{
				start: {
					line: startLine,
					position: startPosition,
				},
				finish: {
					line: finishLine,
					position: finishPosition,
				},
			}
		];

		if(escapeLines) {
			const selectedLines = buildRange(finishLine - startLine + 1, startLine + 1);
			positions = selectedLines
				.filter(l => !escapeLines.has(l))
				.reduce((pv: {
					start: { line: number, position: number, },
					finish: { line: number, position: number },
				}[], cv, index, arr,) => {
					const line = cv - 1;

					if(pv.length === 0 || line - pv[pv.length - 1].finish.line !== 1) {
						pv.push({
							start: {
								line,
								position: pv.length === 0
									? startPosition
									: 0,
							},
							finish: {
								line,
								position: index === arr.length - 1
									? finishPosition
									: 1000,
							},
						});
						return pv;
					}
					pv[pv.length - 1].finish = {
						line,
						position: index === arr.length - 1
							? finishPosition
							: 1000,
					};
					return pv;
				}, []);
		}

		textMarkersByReviewId[review.id] = positions
			.map(({ start, finish }) => (
				createTextMarker(
					finish.line,
					finish.position,
					start.line,
					start.position,
					markerClassName,
					exerciseCodeDoc,
				))
			);
	}

	return textMarkersByReviewId;
}

export function buildRange(size: number, startAt = 0): number[] {
	return [...Array(size).keys()].map(i => i + startAt);
}

function getSelectedReviewIdByCursor(
	reviews: ReviewInfo[],
	exerciseCodeDoc: Doc,
	cursor: CodeMirror.Position
): number {
	const { line, ch } = cursor;
	const reviewsUnderCursor = reviews.filter(r =>
		r.startLine <= line && r.finishLine >= line
		&& !(r.startLine === line && ch < r.startPosition)
		&& !(r.finishLine === line && r.finishPosition < ch)
	);

	if(reviewsUnderCursor.length === 0) {
		return -1;
	}

	reviewsUnderCursor.sort((a, b) => {
		const aLength = getReviewSelectionLength(a, exerciseCodeDoc);
		const bLength = getReviewSelectionLength(b, exerciseCodeDoc);
		if(aLength !== bLength) {
			return aLength - bLength;
		}

		return a.startLine !== b.startLine
			? a.startLine - b.startLine
			: a.startPosition !== b.startPosition
				? a.startPosition - b.startPosition
				: new Date(a.addingTime ?? Math.random() * 10).getTime()
				- new Date(b.addingTime ?? Math.random() * 10).getTime();
	});

	return reviewsUnderCursor[0].id;
}

const getReviewSelectionLength = (review: ReviewInfo, exerciseCodeDoc: Doc): number =>
	exerciseCodeDoc.indexFromPos({ line: review.finishLine, ch: review.finishPosition })
	- exerciseCodeDoc.indexFromPos({ line: review.startLine, ch: review.startPosition });

const loadLanguageStyles = (language: Language): string => {
	switch (language.toLowerCase()) {
		case Language.cSharp:
			return 'text/x-csharp';
		case Language.python2:
			return 'text/x-python';
		case Language.python3:
			return 'text/x-python';
		case Language.java:
			return 'text/x-java';
		case Language.javaScript:
			return 'text/javascript';
		case Language.html:
			return 'text/html';
		case Language.typeScript:
			return 'text/typescript';
		case Language.css:
			return 'text/css';
		case Language.haskell:
			return 'text/x-haskell';
		case Language.cpp:
			return 'text/x-c++src';
		case Language.c:
			return 'text/x-c';
		case Language.pgsql:
			return 'text/x-pgsql';
		case Language.mikrokosmos:
			return 'text/plain';

		case Language.text:
			return 'text/plain';

		case Language.jsx:
			return 'text/jsx';

		default:
			return 'text/html';
	}
};

function replaceReviewMarker(
	reviews: ReviewInfoWithMarker[],
	reviewId: number,
	newReviewId: number,
	doc: Doc,
	defaultMarkerClass: string,
	selectedMarkerClass: string,
): { reviews: ReviewInfoWithMarker[], selectedReviewLine: number, } {
	const newCurrentReviews = [...reviews];

	if(reviewId >= 0) {
		const review = newCurrentReviews.find(r => r.id === reviewId);
		if(review) {
			for (const [index, marker,] of review.markers.entries()) {
				const { from, to, } = marker.find() as MarkerRange;
				marker.clear();
				review.markers[index] =
					createTextMarker(to.line, to.ch, from.line, from.ch, defaultMarkerClass, doc);
			}
		}
	}

	let line = 0;
	if(newReviewId >= 0) {
		const review = newCurrentReviews.find(r => r.id === newReviewId);
		if(review) {
			for (const [index, marker,] of review.markers.entries()) {
				const { from, to, } = marker.find() as MarkerRange;
				marker.clear();
				review.markers[index] =
					createTextMarker(to.line, to.ch, from.line, from.ch, selectedMarkerClass, doc);
				line = from.line;
			}
		}
	}

	return { reviews: newCurrentReviews, selectedReviewLine: line, };
}


export interface PreviousManualCheckingInfo {
	submission: SubmissionInfo;
	index: number;
}

//first submission should be newer
export function getPreviousManualCheckingInfo(
	orderedSubmissionsByTheTime: SubmissionInfo[],
	lastReviewIndex: number,
): PreviousManualCheckingInfo | undefined {
	for (let i = lastReviewIndex + 1; i < orderedSubmissionsByTheTime.length; i++) {
		const submission = orderedSubmissionsByTheTime[i];
		const manualCheckingPassed = (orderedSubmissionsByTheTime[i].manualChecking?.percent ?? null) !== null;
		if(manualCheckingPassed) {
			return { submission, index: i };
		}
	}
	return undefined;
}

function isAcceptedSolutionsWillNotDiscardScore(submissions: SubmissionInfo[], isSkipped: boolean): boolean {
	return submissions.filter(
		s => s.automaticChecking?.result === AutomaticExerciseCheckingResult.RightAnswer).length > 0 || isSkipped;
}

export const areReviewsSame = (
	newReviews: ReviewCompare[],
	oldReviews: ReviewCompare[]
): 'containsNewReviews' | 'containsChangedReviews' | true => {
	if(newReviews.length !== oldReviews.length) {
		return 'containsNewReviews';
	}

	for (let i = 0; i < newReviews.length; i++) {
		const review = newReviews[i];
		const compareReview = oldReviews[i];

		if(review.comments.length > compareReview.comments.length) {
			return 'containsNewReviews';
		}

		if(review.startLine !== compareReview.startLine
			|| review.comment !== compareReview.comment
			|| review.id !== compareReview.id
			|| review.anchor !== compareReview.anchor
			|| review.instructor?.outdated !== compareReview.instructor?.outdated
			|| review.instructor?.isFavourite !== compareReview.instructor?.isFavourite) {
			return 'containsChangedReviews';
		}

		if(JSON.stringify(review.comments) !== JSON.stringify(compareReview.comments)) {
			return 'containsChangedReviews';
		}
	}

	return true;
};

export {
	SubmissionColor,
	ReviewInfoWithMarker,

	getSubmissionColor,
	hasSuccessSubmission,
	submissionIsLast,
	getLastSuccessSubmission,
	isFirstRightAnswer,
	getReviewsWithoutDeleted,
	getAllReviewsFromSubmission,
	createTextMarker,
	getReviewsWithTextMarkers,
	getSelectedReviewIdByCursor,
	loadLanguageStyles,
	replaceReviewMarker,
	isAcceptedSolutionsWillNotDiscardScore
};
