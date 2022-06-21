import React from "react";
import { Navigate } from "react-router-dom";

import { Button, FLAT_THEME_8PX_OLD, Hint, Select, Tabs, ThemeContext, Toast, Toggle, Tooltip } from "ui";
import { UnControlled, } from "react-codemirror2";
import { UrlError } from "../../../../common/Error/NotFoundErrorBoundary";

import Review from "../Blocks/Exercise/Review";
import { BlocksWrapper, } from "../Blocks";
import ScoreControls from "./ScoreControls/ScoreControls";
import CourseLoader from "../../CourseLoader";
import AddCommentForm from "./AddCommentForm/AddCommentForm";
import AntiPlagiarismHeader from "./AntiPlagiarismHeader/AntiPlagiarismHeader";
import StickyWrapper from "./AntiPlagiarismHeader/StickyWrapper";
import checker from "./reviewPolicyChecker";

import 'codemirror/addon/selection/mark-selection.js';
import { buildQuery } from "src/utils";
import { constructLinkWithReturnUrl, constructPathToSlide, login } from "src/consts/routes";
import { isInstructor } from "src/utils/courseRoles";
import {
	areReviewsSame,
	buildRange,
	createTextMarker,
	getAllReviewsFromSubmission,
	getPreviousManualCheckingInfo,
	getSelectedReviewIdByCursor,
	getTextMarkersByReviews,
	loadLanguageStyles,
	SubmissionColor,
} from "../Blocks/Exercise/ExerciseUtils";
import { clone } from "src/utils/jsonExtensions";
import { DiffInfo, getDataFromReviewToCompareChanges, getDiffInfo, getReviewAnchorTop } from "./utils";
import { loadFromCache, reviewPreviousReviewToggle, saveToCache } from "src/utils/localStorageManager";
import { getDeadLineForSlide } from "src/utils/deadLinesUtils";
import { momentFromServerToLocal } from "src/utils/momentUtils";

import { InstructorReviewTabs } from "./InstructorReviewTabs";
import { ExerciseOutput } from "../Blocks/Exercise/ExerciseOutput/ExerciseOutput";
import { Language } from "src/consts/languages";
import { AutomaticExerciseCheckingResult, ReviewInfo, SolutionRunStatus, SubmissionInfo } from "src/models/exercise";
import { FavouriteReview } from "src/models/instructor";
import CodeMirror, { Editor, EditorConfiguration, MarkerRange, TextMarker, } from "codemirror";
import {
	InstructorReviewInfo,
	InstructorReviewInfoWithAnchor,
	Props,
	State,
	SubmissionContext
} from "./InstructorReview.types";
import texts from "./InstructorReview.texts";
import styles from './InstructorReview.less';


class InstructorReview extends React.Component<Props, State> {
	private shameComment = 'Ой! Наш робот нашёл решения других студентов, подозрительно похожие на ваше. ' +
		'Так может быть, если вы позаимствовали части программы, взяли их из открытых источников либо сами поделились своим кодом. ' +
		'Выполняйте задания самостоятельно.';
	private addCommentFormRef = React.createRef<AddCommentForm>();

	constructor(props: Props) {
		super(props);
		const { studentSubmissions, favouriteReviews, slideContext, } = props;

		let currentSubmission: SubmissionInfo | undefined = undefined;
		let currentSubmissionContext: SubmissionContext | undefined = undefined;
		let diffInfo: DiffInfo | undefined = undefined;
		let reviews: InstructorReviewInfo[] | undefined = [];
		let outdatedReviews: InstructorReviewInfo[] | undefined = [];

		const favReviewsByUser = favouriteReviews?.filter(r => r.isFavourite);
		const favReviews = favouriteReviews?.filter(r => !r.isFavourite);
		const favouriteReviewsSet = new Set(favReviews?.map(r => r.text));
		const favouriteByUserSet = new Set(favReviewsByUser?.map(r => r.text));

		if(studentSubmissions) {
			const index = Math.max(
				studentSubmissions.findIndex(s => s.id === slideContext.slideInfo.query.submissionId), 0);
			const submissionInfo = this.getSubmissionInfo(studentSubmissions, index);
			currentSubmission = submissionInfo.submission;
			diffInfo = submissionInfo.diffInfo;

			currentSubmissionContext = this.getSubmissionContext(studentSubmissions, currentSubmission);

			const allReviews = this.getReviewsFromSubmission(currentSubmission, diffInfo, false,);
			reviews = allReviews.reviews;
			outdatedReviews = allReviews.outdatedReviews;
		}
		const toggleInCache = loadFromCache<boolean>(reviewPreviousReviewToggle, this.reviewCacheId);

		this.state = {
			selectedReviewId: -1,
			reviews,
			outdatedReviews,
			markers: {},
			currentTab: InstructorReviewTabs.Review,
			currentSubmission,
			currentSubmissionContext,
			editor: null,
			addCommentValue: '',
			showDiff: toggleInCache || false,
			diffInfo: diffInfo,
			favouriteReviewsSet,
			favouriteByUserSet,
		};
	}

	getSubmissionContext = (
		studentSubmissions: SubmissionInfo[],
		currentSubmission: SubmissionInfo
	): SubmissionContext => {
		const { lastCheckedSubmissionId, lastManualCheckingSubmissionId, } = this.props;
		const isLastCheckedSubmission = currentSubmission.id === lastCheckedSubmissionId;
		const isLastSubmissionWithManualChecking = currentSubmission.id === lastManualCheckingSubmissionId;

		const isEditable = (isLastSubmissionWithManualChecking || isLastCheckedSubmission);

		return {
			isLastCheckedSubmission,
			isLastSubmissionWithManualChecking,
			isEditable,
		};
	};

	componentDidMount(): void {
		const {
			currentSubmission,
		} = this.state;

		this.loadData();

		if(currentSubmission) {
			this.addMarkers();
		}

		//it is possible that selection in editor preserves even if other text in browser is selected
		//if nothing selected in browser, then copy value from selection in editor
		document.addEventListener('copy', this.onCopy);
	}

	componentWillUnmount = (): void => {
		document.removeEventListener('copy', this.onCopy);
	};

	loadData = (): void => {
		const {
			student,
			studentGroups,
			getStudentInfo,
			getStudentGroups,
			favouriteReviews,
			getFavouriteReviews,
			deadLines,
			loadDeadLines,
			slideContext: { courseId, slideId, slideInfo: { query, }, },
		} = this.props;

		if(!query.userId) {
			return;
		}

		if(!student) {
			getStudentInfo(query.userId);
		}
		if(!studentGroups) {
			getStudentGroups(courseId, query.userId);
		}
		if(!favouriteReviews) {
			getFavouriteReviews(courseId, slideId);
		}

		if(!deadLines) {
			loadDeadLines(courseId, query.userId);
		}
	};

	hideAddCommentForm = (): void => {
		document.removeEventListener('keydown', this.onEscPressed);
		this.setState({
			addCommentFormCoords: undefined,
			addCommentFormExtraSpace: undefined,
			addCommentRanges: undefined,
			addCommentSelections: undefined,
		});
	};

	componentDidUpdate = (prevProps: Readonly<Props>, prevState: Readonly<State>): void => {
		const {
			studentSubmissions,
			getAntiPlagiarismStatus,
			antiPlagiarismStatus,
			antiPlagiarismStatusError,
			antiPlagiarismStatusLoading,
			slideContext,
		} = this.props;
		const { currentSubmission, reviews, diffInfo, showDiff, addCommentSelections, editor, } = this.state;

		if(addCommentSelections && editor) {
			for (const selection of addCommentSelections) {
				const range = selection;
				const doc = editor.getDoc();

				if(doc.getAllMarks().length > 0) {
					break;
				}

				const [startRange, endRange,] = this.getStartAndEndFromRange(range);
				createTextMarker(
					endRange.line,
					endRange.ch,
					startRange.line,
					startRange.ch,
					styles.selectionToReviewMarker,
					doc);
			}
		}

		if(prevProps.slideContext.slideInfo.query.userId !== slideContext.slideInfo.query.userId
			|| prevProps.slideContext.slideInfo.query.submissionId !== slideContext.slideInfo.query.submissionId) {
			const toggleInCache = loadFromCache<boolean>(reviewPreviousReviewToggle, this.reviewCacheId);
			this.loadData();
			this.hideAddCommentForm();
			this.setState({
				showDiff: toggleInCache === undefined ? showDiff : toggleInCache,
				currentSubmission: undefined,
				selectedReviewId: -1,
			});
			return;
		}

		if(!antiPlagiarismStatus && !antiPlagiarismStatusError && !antiPlagiarismStatusLoading && studentSubmissions && studentSubmissions.length > 0) {
			getAntiPlagiarismStatus(slideContext.courseId, studentSubmissions[0].id);
		}

		if(studentSubmissions && studentSubmissions.length > 0 && !currentSubmission) {
			const index = studentSubmissions.findIndex(s => s.id === slideContext.slideInfo.query.submissionId);
			if(index > -1) {
				this.loadSubmission(studentSubmissions, index);
			}
			return;
		}

		if(currentSubmission && studentSubmissions) {
			if(prevState.showDiff !== showDiff) {
				const newReviews = this.getReviewsFromSubmission(
					currentSubmission,
					diffInfo,
					showDiff,
				);
				this.setState({
					reviews: newReviews.reviews,
					outdatedReviews: newReviews.outdatedReviews,
				}, this.resetMarkers);
				return;
			}

			const submissionIndex = studentSubmissions.findIndex(s => s.id === currentSubmission.id);
			const submission = clone(studentSubmissions[submissionIndex]);
			if(submission) {
				const newReviews = this.getReviewsFromSubmission(
					submission,
					diffInfo,
					showDiff,
				);
				const reviewsCompare = reviews.map(getDataFromReviewToCompareChanges);
				const newReviewsCompare = newReviews.reviews.map(getDataFromReviewToCompareChanges);
				// outdated should not be changed
				//	const newOutdatedReviewsCompare = newReviews.outdatedReviews.map(r => getDataFromReviewToCompareChanges(r));
				//  const outdatedReviewsCompare = outdatedReviews.map(r => getDataFromReviewToCompareChanges(r));
				//  if(JSON.stringify(outdatedReviewsCompare) !== JSON.stringify(newOutdatedReviewsCompare)) {}
				if(areReviewsSame(reviewsCompare, newReviewsCompare) !== true) {
					this.updateSubmission(submission, newReviews.reviews, newReviews.outdatedReviews);
				}

				const currentSubmissionManualCheckingEnabled = currentSubmission.manualChecking !== null;
				const submissionManualCheckingEnabled = submission.manualChecking !== null;
				if(currentSubmissionManualCheckingEnabled !== submissionManualCheckingEnabled) {
					this.updateSubmission(submission);
				}
			}
		}
	};

	updateSubmission = (
		submission: SubmissionInfo,
		newReviews?: ReviewInfo[],
		newOutdatedReviews?: ReviewInfo[],
	): void => {
		const { studentSubmissions, } = this.props;
		const { reviews, outdatedReviews, currentSubmissionContext, addCommentFormCoords, } = this.state;

		if(!studentSubmissions) {
			return;
		}

		const newSubmissionContext = this.getSubmissionContext(studentSubmissions, submission);

		this.setState({
			currentSubmission: submission,
			currentSubmissionContext: newSubmissionContext,
			reviews: newReviews || reviews,
			outdatedReviews: newOutdatedReviews || outdatedReviews,
			addCommentFormCoords: newSubmissionContext.isEditable !== currentSubmissionContext?.isEditable ? undefined : addCommentFormCoords,
		}, this.resetMarkers);
	};

	loadSubmission = (studentSubmissions: SubmissionInfo[], index: number,): void => {
		const { showDiff, } = this.state;
		const { submission, diffInfo } = this.getSubmissionInfo(studentSubmissions, index);
		const { reviews, outdatedReviews } = this.getReviewsFromSubmission(submission, diffInfo, showDiff);

		this.hideAddCommentForm();
		this.setState({
			diffInfo,
			selectedReviewId: -1,
		}, () => this.updateSubmission(submission, reviews, outdatedReviews));
	};

	getSubmissionInfo = (
		studentSubmissions: SubmissionInfo[],
		index: number,
	): { submission: SubmissionInfo; diffInfo: DiffInfo | undefined; } => {
		const submission = clone(studentSubmissions[index]);
		const prevSubmissionInfo = getPreviousManualCheckingInfo(studentSubmissions, index);
		const diffInfo = prevSubmissionInfo
			? {
				...getDiffInfo(submission.code, prevSubmissionInfo.submission.code),
				prevReviewedSubmission: prevSubmissionInfo.submission,
			}
			: undefined;

		return { submission, diffInfo, };
	};

	getReviewsFromSubmission = (
		submission: SubmissionInfo,
		diffInfo: DiffInfo | undefined,
		showDiff: boolean,
	): { reviews: ReviewInfo[]; outdatedReviews: ReviewInfo[]; } => {
		const reviews: ReviewInfo[] = getAllReviewsFromSubmission(submission)
			.map(r => ({
				...r,
				startLine: showDiff && diffInfo ? diffInfo.newCodeNewLineIndex[r.startLine] : r.startLine,
				finishLine: showDiff && diffInfo ? diffInfo.newCodeNewLineIndex[r.finishLine] : r.finishLine,
			}));
		const outdatedReviews: ReviewInfo[] = diffInfo && diffInfo.prevReviewedSubmission
			? getAllReviewsFromSubmission(diffInfo.prevReviewedSubmission,)
				.map(r => ({
					...r,
					startLine: showDiff && diffInfo ? diffInfo.oldCodeNewLineIndex[r.startLine] : r.startLine,
					finishLine: showDiff && diffInfo ? diffInfo.oldCodeNewLineIndex[r.finishLine] : r.finishLine,
				}))
			: [];

		return { reviews, outdatedReviews, };
	};

	resetMarkers = (): void => {
		const {
			editor,
			diffInfo,
			showDiff,
		} = this.state;

		if(!editor) {
			return;
		}

		editor
			.getAllMarks()
			.forEach((m: TextMarker) => m.clear());
		editor.refresh();

		this.addMarkers();
		if(showDiff && diffInfo) {
			this.addLineClasses(editor, diffInfo);
		}
	};

	addLineClasses = (editor: Editor, diffInfo: DiffInfo,): void => {
		//addding a line class via addLineClass triggers rerender on each method call, it cause perfomance issues
		//instead of it we can add class directly to line wrappers
		const linesWrapper = editor
			.getWrapperElement()
			.getElementsByClassName('CodeMirror-code')
			?.[0];
		for (let i = 0; i < diffInfo.diffByBlocks.length; i++) {
			const { type, } = diffInfo.diffByBlocks[i];
			if(type) {
				const lineWrapper = linesWrapper.children[i];
				if(!lineWrapper) {
					return;
				}

				switch (type) {
					case "added": {
						lineWrapper.classList.add(styles.addedLinesCodeMirror);
						break;
					}
					case "removed": {
						lineWrapper.classList.add(styles.removedLinesCodeMirror);
						break;
					}
				}
			}
		}
	};

	static getDerivedStateFromProps(props: Readonly<Props>, state: Readonly<State>): Partial<State> | null {
		const { favouriteReviews, } = props;
		const { favouriteReviewsSet, favouriteByUserSet, } = state;
		const favReviewsByUser = favouriteReviews?.filter(r => r.isFavourite);
		const favReviews = favouriteReviews?.filter(r => !r.isFavourite);

		let newState: Partial<State> = {};

		if(favReviewsByUser?.length !== favouriteByUserSet?.size || favReviews?.length !== favouriteReviewsSet?.size) {
			newState = {
				...newState,
				favouriteReviewsSet: new Set(favReviews?.map(r => r.text)),
				favouriteByUserSet: new Set(favReviewsByUser?.map(r => r.text)),
			};
		}

		if(Object.keys(newState).length > 0) {
			return newState;
		}

		return null;
	}

	render(): React.ReactNode {
		const {
			student,
			studentGroups,
			studentSubmissions,
			authorSolution,
			formulation,
			favouriteReviews,
			user,
			curScore,
			prevScore,
			deadLines,
			slideContext,
		} = this.props;
		const {
			currentTab,
			currentSubmission,
		} = this.state;

		if(!user?.isAuthenticated) {
			return <Navigate replace to={ constructLinkWithReturnUrl(login) }/>;
		}

		if(!isInstructor(user)) {
			throw new UrlError();
		}
		if(!student || !studentSubmissions || !studentGroups || !favouriteReviews || !currentSubmission) {
			return <CourseLoader/>;
		}
		const submissionsOrderedByTimeAsc = studentSubmissions
			.map(s => s)
			.filter(
				s => !s.automaticChecking || s.automaticChecking?.result === AutomaticExerciseCheckingResult.RightAnswer)
			.sort((s1, s2) => {
				return momentFromServerToLocal(s1.timestamp).diff(momentFromServerToLocal(s2.timestamp));
			});

		const deadLine = getDeadLineForSlide(deadLines ?? [],
			slideContext.slideInfo.navigationInfo?.current.scoringGroup || null,
			slideContext.slideId,
			slideContext.unitId,
			momentFromServerToLocal(submissionsOrderedByTimeAsc[0].timestamp));

		return (
			<>
				<BlocksWrapper withoutBottomPaddings>
					<h3 className={ styles.reviewHeader }>
						<span className={ styles.reviewStudentName }>
							{
								deadLine.current &&
								<span className={ styles.solvedAfterDeadLineWrapper }>
									<Hint text={ texts.getDeadLineViolationInfo(submissionsOrderedByTimeAsc[0],
										deadLine.current) }>
										<span className={ styles.solvedAfterDeadLineToken }>После дедлайна</span>
									</Hint>
								</span>
							}
							{ texts.getStudentInfo(student.visibleName, studentGroups) }
						</span>
						<span className={ styles.reviewHeaderScore }>
							{ texts.getReviewInfo(studentSubmissions, prevScore, curScore,) }
						</span>
					</h3>
					<Tabs value={ currentTab } onValueChange={ this.onTabChange }>
						<Tabs.Tab key={ InstructorReviewTabs.Review } id={ InstructorReviewTabs.Review }>
							{ texts.getTabName(InstructorReviewTabs.Review) }
						</Tabs.Tab>
						{
							formulation &&
							<Tabs.Tab key={ InstructorReviewTabs.Formulation } id={ InstructorReviewTabs.Formulation }>
								{ texts.getTabName(InstructorReviewTabs.Formulation) }
							</Tabs.Tab>
						}
						{
							authorSolution &&
							<Tabs.Tab key={ InstructorReviewTabs.AuthorSolution }
									  id={ InstructorReviewTabs.AuthorSolution }>
								{ texts.getTabName(InstructorReviewTabs.AuthorSolution) }
							</Tabs.Tab>
						}
					</Tabs>
				</BlocksWrapper>
				<div className={ styles.separator }/>
				{ this.renderCurrentTab(currentTab) }
			</>
		);
	}

	onTabChange = (value: string): void => {
		this.setState({ currentTab: value as InstructorReviewTabs });
	};

	renderCurrentTab(currentTab: InstructorReviewTabs): React.ReactNode {
		const { formulation, authorSolution, } = this.props;

		switch (currentTab) {
			case InstructorReviewTabs.Review: {
				return this.renderSubmissions();
			}
			case InstructorReviewTabs.Formulation: {
				return formulation;
			}
			case InstructorReviewTabs.AuthorSolution: {
				return authorSolution;
			}
		}
	}

	renderSubmissions(): React.ReactNode {
		const {
			slideContext,
			prohibitFurtherManualChecking,
			favouriteReviews,
			studentSubmissions,
			expectedOutput,
			setNextSubmissionButtonDisabled,
		} = this.props;
		const {
			currentSubmission,
			currentSubmissionContext,
			diffInfo,
		} = this.state;

		if(!favouriteReviews || !currentSubmission || !studentSubmissions || !currentSubmissionContext || expectedOutput === undefined) {
			return null;
		}

		const {
			isLastCheckedSubmission,
			isEditable,
		} = currentSubmissionContext;

		const submissionPercent = currentSubmission.manualChecking?.percent ?? null;
		const prevSubmissionPercent = (diffInfo
			&& diffInfo.prevReviewedSubmission
			&& diffInfo.prevReviewedSubmission.manualChecking?.percent) ?? null;
		const outputMessage = currentSubmission.automaticChecking?.output;

		return (
			<BlocksWrapper withoutBottomPaddings>
				{ this.renderTopControls(isEditable) }
				<StickyWrapper
					stickerClass={ styles.wrapperStickerStopper }
					renderSticker={ this.renderHeader }
					renderContent={ this.renderEditor }
				/>
				{
					currentSubmission.automaticChecking
					&& currentSubmission.automaticChecking.result !== AutomaticExerciseCheckingResult.RightAnswer
					&& <ExerciseOutput
						withoutMargin
						solutionRunStatus={ SolutionRunStatus.Success }
						message={ outputMessage }
						expectedOutput={ expectedOutput }
						automaticChecking={ currentSubmission.automaticChecking }
						submissionColor={ SubmissionColor.WrongAnswer }
					/>
				}
				{ (isEditable || submissionPercent !== null) &&
					<ScoreControls
						setNextSubmissionButtonDisabled={ setNextSubmissionButtonDisabled }
						canChangeScore={ isEditable }
						date={ !isLastCheckedSubmission ? currentSubmission.timestamp : undefined }
						score={ submissionPercent }
						prevReviewScore={ prevSubmissionPercent }
						exerciseTitle={ slideContext.title }
						onSubmit={ this.onScoreButtonPressed }
						onToggleChange={ this.prohibitFurtherReview }
						toggleChecked={ !prohibitFurtherManualChecking }
					/> }
				{ (!currentSubmission.automaticChecking
						|| currentSubmission.automaticChecking.result === AutomaticExerciseCheckingResult.RightAnswer)
					&& currentSubmission.manualChecking === null
					&& currentSubmission.id === studentSubmissions[0].id
					&& <>
						<p> { texts.submissionAfterDisablingManualChecking } </p>
						<Button
							use={ 'primary' }
							onClick={ this.enableManualChecking }>
							{ texts.enableManualChecking }
						</Button>
					</> }
			</BlocksWrapper>
		);
	}

	enableManualChecking = (): void => {
		const { currentSubmission } = this.state;
		const { enableManualChecking, slideContext: { slideInfo: { query, } }, student, navigate, } = this.props;

		if(!currentSubmission || !student) {
			return;
		}

		navigate(
			location.pathname + buildQuery({
				submissionId: currentSubmission.id,
				checkQueueItemId: currentSubmission.id,
				userId: student.id,

				queueSlideId: query.queueSlideId || undefined,
				group: query.group || undefined,
				done: query.done,
			}));
		enableManualChecking(currentSubmission.id);
	};

	prohibitFurtherReview = (enabled: boolean): void => {
		const {
			prohibitFurtherReview,
			slideContext,
			student,
		} = this.props;

		if(!student) {
			return;
		}

		prohibitFurtherReview(slideContext.courseId, slideContext.slideId, student.id, !enabled);
	};

	onScoreButtonPressed = (score: number): void => {
		const {
			currentSubmission,
		} = this.state;

		if(!currentSubmission) {
			return;
		}

		this.onScoreSubmit(currentSubmission.id, score, currentSubmission.manualChecking?.percent ?? null);
	};

	onZeroScoreButtonPressed = (): void => {
		const {
			prohibitFurtherReview,
			slideContext,
			student,
			lastManualCheckingSubmissionId,
			curScore,
		} = this.props;
		const {
			currentSubmission,
		} = this.state;

		if(!student || !currentSubmission || lastManualCheckingSubmissionId === undefined) {
			return;
		}

		this.onScoreSubmit(lastManualCheckingSubmissionId, 0, curScore)
			.then(() => {
				prohibitFurtherReview(slideContext.courseId, slideContext.slideId, student.id, true);
				this.addReview(lastManualCheckingSubmissionId, this.shameComment, 0, 0, 0, 1)
					.then(r => r && this.highlightReview(r.id));
				if(currentSubmission.id !== lastManualCheckingSubmissionId) {
					Toast.push('Оценка и комментарий были оставлены к решению ожидающему ревью', {
						label: 'Перейти',
						handler: this.handleZeroButtonToastClick
					});
				}
			});
	};

	onScoreSubmit = (submissionId: number, score: number,
		oldScore: number | null,
	): Promise<Response | string | void> => {
		const {
			onScoreSubmit,
		} = this.props;

		return onScoreSubmit(submissionId, score, oldScore)
			.catch(this.catchNewestSubmission);
	};

	addReview = (
		submissionId: number,
		comment: string,
		startLine: number,
		startPosition: number,
		finishLine: number,
		finishPosition: number,
	): Promise<ReviewInfo | void> => {
		const {
			addReview,
		} = this.props;

		return addReview(submissionId, comment, startLine, startPosition, finishLine, finishPosition)
			.catch(this.catchNewestSubmission);
	};

	catchNewestSubmission = (err: any): void => {
		const {
			navigate,
			slideContext,
		} = this.props;
		if(err.response) {
			err.response.json()
				.then((json: any) => {
					const errorResponse = json as {
						error: "has_newest_submission" | string;
						submissionId: number;
						submissionDate: string;
					};

					if(errorResponse.error === "has_newest_submission") {
						Toast.push('Появилось новое решение', {
							label: "Загрузить и перейти",
							handler: () => {
								navigate(constructPathToSlide(slideContext.courseId, slideContext.slideId)
									+ buildQuery({
										queueSlideId: slideContext.slideInfo.query.queueSlideId || undefined,
										userId: slideContext.slideInfo.query.userId,
										group: slideContext.slideInfo.query.group || undefined,
										done: slideContext.slideInfo.query.done,
										checkQueueItemId: errorResponse.submissionId,
										submissionId: errorResponse.submissionId,
									}));
							}
						});
					}
				});
		}
	};

	handleZeroButtonToastClick = (): void => {
		const { studentSubmissions, lastManualCheckingSubmissionId, } = this.props;

		if(!studentSubmissions) {
			return;
		}

		const index = studentSubmissions.findIndex(s => s.id === lastManualCheckingSubmissionId);

		if(!studentSubmissions || index === -1) {
			Toast.push('Произошла ошибка');
			if(!studentSubmissions) {
				console.error(new Error('Student submissions were undefined'));
			} else {
				console.error(new Error(
					`Student submissions does not contain id ${ lastManualCheckingSubmissionId }, submissions ids=${ studentSubmissions.map(
						s => s.id).join(', ') }`));
			}
			return;
		}

		this.loadSubmission(studentSubmissions, index);
	};

	renderTopControls(commentsEnabled = true): React.ReactElement {
		const { showDiff, diffInfo, } = this.state;

		return (
			<div className={ styles.topControlsWrapper }>
				{ this.renderSubmissionsSelect() }
				{ diffInfo &&
					<Tooltip render={ this.renderShowDiffTooltip }>
						<Toggle
							onValueChange={ this.onDiffToggleValueChanged }
							checked={ showDiff }>
							{ texts.getDiffText(
								diffInfo.addedLinesCount,
								styles.diffAddedLinesTextColor,
								diffInfo.removedLinesCount,
								styles.diffRemovedLinesTextColor,
								!diffInfo.prevReviewedSubmission)
							}
						</Toggle>
					</Tooltip>
				}
				{ commentsEnabled &&
					<span className={ styles.leaveCommentGuideText }>{ texts.leaveCommentGuideText }</span> }
			</div>
		);
	}

	private reviewCacheId = 'previous_review_toggle';

	renderShowDiffTooltip = () => {
		const { showDiff, } = this.state;
		const toggleInCache = loadFromCache<boolean>(reviewPreviousReviewToggle, this.reviewCacheId);

		if(toggleInCache === showDiff) {
			return undefined;
		}

		return (
			<Button use={ 'link' }
					onClick={ this.saveShowDiffToCache }>
				{ texts.saveShowDiff }
			</Button>
		);
	};

	saveShowDiffToCache = () => {
		const { showDiff, } = this.state;

		saveToCache(reviewPreviousReviewToggle, this.reviewCacheId, showDiff);
		Toast.push(texts.onSaveShowDiffToastMessage);
		this.forceUpdate();//nothing is changed in props/state, so we need to rerender to hide tooltip
	};

	renderHeader = (fixed: boolean,): React.ReactElement => {
		const {
			studentSubmissions,
			antiPlagiarismStatus,
			antiPlagiarismStatusError,
			slideContext: { courseId, },
			curScore,
		} = this.props;
		const submissionId = studentSubmissions?.[0].id;

		return (<AntiPlagiarismHeader
			zeroButtonDisabled={ curScore === 0 || false }
			courseId={ courseId }
			submissionId={ submissionId }
			status={ antiPlagiarismStatus }
			error={ antiPlagiarismStatusError }
			fixed={ fixed }
			onZeroScoreButtonPressed={ this.onZeroScoreButtonPressed }
		/>);
	};

	renderEditor = (): React.ReactNode => {
		const {
			user,
			favouriteReviews,
			studentSubmissions,
			lastUsedReviews,
		} = this.props;
		const {
			currentSubmission,
			currentSubmissionContext,
			selectedReviewId,
			diffInfo,
			showDiff,
			addCommentFormCoords,
			addCommentFormExtraSpace,
			addCommentValue,

		} = this.state;

		if(!favouriteReviews || !currentSubmission || !studentSubmissions || !currentSubmissionContext) {
			return null;
		}

		const { isEditable, } = currentSubmissionContext;

		return (
			<div className={ styles.positionWrapper }
				 style={ { marginBottom: isEditable && addCommentFormCoords && addCommentFormExtraSpace || 0 } }>
				<div className={ styles.wrapper }>
					<UnControlled
						onSelection={ this.onSelectionChange }
						editorDidMount={ this.onEditorMount }
						className={ styles.editor }
						options={ this.getEditorOptions(currentSubmission.language) }
						value={ diffInfo && showDiff
							? diffInfo.code
							: currentSubmission.code
						}
						onCursorActivity={ this.onCursorActivity }
					/>
					<Review
						className={ styles.reviewsContainer }
						backgroundColor={ 'gray' }
						user={ user }
						addReviewComment={ this.onAddReviewComment }
						assignBotComment={ isEditable ? this.assignBotReview : undefined }
						toggleReviewFavourite={ this.onToggleReviewFavouriteByReviewId }
						deleteReviewOrComment={ this.onDeleteReviewOrComment }
						editReviewOrComment={ this.editReviewOrComment }
						selectedReviewId={ selectedReviewId }
						onReviewClick={ this.selectComment }
						reviews={ this.getAllReviewsAsInstructorReviews() }
					/>
				</div>
				{ isEditable && addCommentFormCoords !== undefined &&
					<AddCommentForm
						ref={ this.addCommentFormRef }
						user={ this.props.user }
						value={ addCommentValue }
						valueCanBeAddedToFavourite={ this.isCommentCanBeAddedToFavourite() }
						onValueChange={ this.onCommentFormValueChange }
						addComment={ this.onFormAddComment }
						favouriteReviews={ favouriteReviews }
						lastUsedReviews={ lastUsedReviews || [] }
						addFavouriteReview={ this.addFavouriteReview }
						deleteFavouriteReview={ this.deleteFavouriteReview }
						coordinates={ addCommentFormCoords }
						onClose={ this.onFormClose }
					/> }
			</div>
		);
	};

	addFavouriteReview = (favouriteReviewText: string): Promise<FavouriteReview> => {
		const { slideContext, addFavouriteReview, } = this.props;

		return addFavouriteReview(slideContext.courseId, slideContext.slideId, favouriteReviewText);
	};

	deleteFavouriteReview = (favouriteReviewId: number): Promise<Response> => {
		const { slideContext, deleteFavouriteReview, } = this.props;

		return deleteFavouriteReview(slideContext.courseId, slideContext.slideId, favouriteReviewId);
	};

	getAllReviewsAsInstructorReviews = (): InstructorReviewInfoWithAnchor[] => {
		const {
			reviews,
			outdatedReviews,
			diffInfo,
			favouriteByUserSet,
			editor,
			showDiff,
		} = this.state;

		if(!editor) {
			return [];
		}

		const allReviews: InstructorReviewInfoWithAnchor[] = reviews.map(r => ({
			...r,
			instructor: {
				isFavourite: favouriteByUserSet.has(r.comment),
			},
			anchor: getReviewAnchorTop(r, editor,),
		}));

		if(showDiff && diffInfo) {
			return allReviews.concat(outdatedReviews.map(r => ({
				...r,
				instructor: {
					outdated: true,
				},
				anchor: getReviewAnchorTop(r, editor,),
			})));
		}
		return allReviews;
	};

	onToggleReviewFavouriteByReviewId = (reviewId: number): void => {
		const {
			deleteFavouriteReview,
			addFavouriteReview,
			favouriteReviews,
			slideContext,
		} = this.props;
		const {
			currentSubmission,
		} = this.state;

		if(currentSubmission && favouriteReviews) {
			const review = currentSubmission.manualChecking?.reviews.find(r => r.id === reviewId);
			if(review) {
				const favouriteReview = favouriteReviews.find(r => r.text === review.comment);
				if(favouriteReview && favouriteReview.isFavourite) {
					deleteFavouriteReview(slideContext.courseId, slideContext.slideId, favouriteReview.id);
				} else {
					addFavouriteReview(slideContext.courseId, slideContext.slideId, review.comment);
				}
			}
		}
	};

	isCommentCanBeAddedToFavourite = (text?: string,): boolean => {
		const { addCommentValue, favouriteByUserSet, } = this.state;
		text = text ?? addCommentValue;
		const trimmed = checker.removeWhiteSpaces(text);

		return trimmed.length > 0 && !favouriteByUserSet?.has(trimmed);
	};

	editReviewOrComment = (text: string, reviewId: number, parentReviewId?: number,): void => {
		const {
			editReviewOrComment,
		} = this.props;
		const {
			currentSubmission,
		} = this.state;

		if(currentSubmission) {
			const trimmed = checker.removeWhiteSpaces(text);
			const oldText = parentReviewId
				? currentSubmission.manualChecking?.reviews.find(r => r.id === parentReviewId)?.comments.find(
				c => c.id === reviewId)?.text || ''
				: currentSubmission.manualChecking?.reviews.find(r => r.id === reviewId)?.comment || '';

			editReviewOrComment(currentSubmission.id, reviewId, parentReviewId, trimmed, oldText);
		}
	};

	onCommentFormValueChange = (comment: string): void => {
		this.setState({
			addCommentValue: comment,
		});
	};

	onDeleteReviewOrComment = (reviewId: number, commentId?: number): void => {
		const { deleteReview, deleteReviewComment, } = this.props;
		const { currentSubmission, } = this.state;

		if(currentSubmission) {
			if(commentId) {
				deleteReviewComment(currentSubmission.id, reviewId, commentId);
			} else {
				const isBotReview = currentSubmission.automaticChecking?.reviews?.some(r => r.id === reviewId);
				deleteReview(currentSubmission.id, reviewId, isBotReview);
			}
		}
	};

	getEditorOptions = (language: Language): EditorConfiguration => ({
		lineNumbers: true,
		lineNumberFormatter: this.formatLine,
		lineWrapping: true,
		scrollbarStyle: 'null',
		theme: 'default',
		readOnly: true,
		matchBrackets: true,
		mode: loadLanguageStyles(language),
		styleSelectedText: styles.selectionMarker,
	});

	renderSubmissionsSelect = (): React.ReactNode => {
		const { currentSubmission, currentSubmissionContext, } = this.state;
		const { studentSubmissions, lastManualCheckingSubmissionId, } = this.props;

		if(!studentSubmissions || !currentSubmission || !currentSubmissionContext) {
			return null;
		}

		const items = [...studentSubmissions.map(
			(submission,) => ([
				submission.id,
				texts.getSubmissionCaption(
					submission,
					lastManualCheckingSubmissionId === submission.id,
					lastManualCheckingSubmissionId === submission.id)
			]))
		];

		return (
			<div className={ styles.submissionsSelect }>
				<ThemeContext.Provider value={ FLAT_THEME_8PX_OLD }>
					<Select
						width={ '100%' }
						items={ items }
						value={ currentSubmission.id }
						onValueChange={ this.onSubmissionsSelectValueChange }
					/>
				</ThemeContext.Provider>
			</div>
		);
	};

	onDiffToggleValueChanged = (value: boolean): void => {
		const {
			currentSubmission,
			editor,
		} = this.state;

		if(!currentSubmission || !editor) {
			return;
		}

		this.hideAddCommentForm();
		this.setState({
			showDiff: value,
			selectedReviewId: -1,
		}, this.resetMarkers);
	};

	onSubmissionsSelectValueChange = (id: unknown): void => {
		const { studentSubmissions, } = this.props;
		const { editor, } = this.state;

		if(!studentSubmissions || !editor) {
			return;
		}
		const currentSubmissionIndex = studentSubmissions.findIndex(s => s.id === id);

		this.loadSubmission(studentSubmissions, currentSubmissionIndex);
	};

	addMarkers = (): void => {
		const {
			reviews,
			outdatedReviews,
			editor,
			diffInfo,
			showDiff,
			selectedReviewId,
		} = this.state;

		if(!editor) {
			return;
		}

		const doc = editor.getDoc();

		let markers = {
			...getTextMarkersByReviews(
				reviews,
				doc,
				styles.defaultMarker,
				showDiff && diffInfo ? diffInfo.deletedLinesSet : undefined,
			),
		};

		if(showDiff) {
			markers = {
				...markers,
				...getTextMarkersByReviews(
					outdatedReviews,
					doc,
					styles.defaultMarker,
					diffInfo ? diffInfo.addedLinesSet : undefined,
				),
			};
		}

		this.setState({
			markers,
		}, () => selectedReviewId > -1 && this.highlightReview(selectedReviewId));
	};

	formatLine = (lineNumber: number): string => {
		const { diffInfo, showDiff, } = this.state;
		if(showDiff && diffInfo && diffInfo.diffByBlocks.length >= lineNumber) {
			const blockDiff = diffInfo.diffByBlocks[lineNumber - 1];

			return blockDiff.line.toString();
		}

		return lineNumber.toString();
	};

	onMouseUp = (): void => {
		const { editor, addCommentFormCoords, } = this.state;
		const { getFavouriteReviews, slideContext, favouriteReviews, } = this.props;

		if(!editor) {
			return;
		}

		const doc = editor.getDoc();
		const selections = doc.listSelections();

		const firstSelection = selections[0];
		const startRange = this.getStartAndEndFromRange(firstSelection)[0];

		const lastSelection = selections[selections.length - 1];
		const endRange = this.getStartAndEndFromRange(lastSelection)[1];

		let coords: { left: number, right: number, top: number, bottom: number } | undefined
			= undefined;
		for (const selection of selections) {
			const range = selection;
			const selectedText = doc.getSelection();

			if(selectedText.length == 0) {
				this.onFormClose();
				return;
			}

			if(addCommentFormCoords) {
				return;
			}

			if(selectedText.length > 0) {
				const [startRange, endRange,] = this.getStartAndEndFromRange(range);
				coords = editor.charCoords({ line: endRange.line, ch: 0 }, 'local');
				createTextMarker(
					endRange.line,
					endRange.ch,
					startRange.line,
					startRange.ch,
					styles.selectionToReviewMarker,
					doc);
			}
		}
		if(coords) {
			const c = coords;
			if(favouriteReviews) {
				getFavouriteReviews(slideContext.courseId, slideContext.slideId);
				this.openAddCommentForm(c, startRange, endRange, editor, selections);
			} else {
				getFavouriteReviews(slideContext.courseId, slideContext.slideId)
					.then(() => {
						this.openAddCommentForm(c, startRange, endRange, editor, selections);
					});
			}
		}

		document.removeEventListener('mouseup', this.onMouseUp);
	};

	openAddCommentForm = (
		coords: { left: number, right: number, top: number, bottom: number },
		startRange: CodeMirror.Position,
		endRange: CodeMirror.Position,
		editor: CodeMirror.Editor,
		selections: CodeMirror.Range[],
	) => {
		const wrapperHeight = editor
			.getScrollerElement()
			.getBoundingClientRect()
			.height - 50;
		const lineHeight = 20;
		const padding = 16;
		coords.left = editor
			.getGutterElement()
			.getBoundingClientRect()
			.width + padding / 2;
		coords.bottom += padding;
		this.setState({
			addCommentFormCoords: coords,
			addCommentRanges: { startRange, endRange, },
			addCommentSelections: selections,
		}, () => {
			document.addEventListener('keydown', this.onEscPressed);
			//addCommentFormExtraSpace should be added after AddCommentForm is rendered to get height
			const addCommentFormHeight = this.addCommentFormRef.current?.getHeight();
			if(addCommentFormHeight) {
				const extraSpace = (endRange.line + 1) * lineHeight + addCommentFormHeight + padding - wrapperHeight;
				this.setState({
					addCommentFormExtraSpace: extraSpace > 0 ? extraSpace : undefined,
				});
			}
		});
	};

	onCopy = async (): Promise<void> => {
		const { editor } = this.state;

		if(editor) {
			const selectedTextInBrowser = getSelection()?.toString();
			const selectedTextInEditor = editor.getSelection();
			if(!selectedTextInBrowser || selectedTextInBrowser.length === 0) {
				await navigator.clipboard.writeText(selectedTextInEditor);
			}
		}
	};

	onAddReviewComment = (reviewId: number, comment: string): void => {
		const { addReviewComment, } = this.props;
		const { currentSubmission, } = this.state;

		if(currentSubmission) {
			addReviewComment(currentSubmission.id, reviewId, checker.removeWhiteSpaces(comment));
		}
	};

	assignBotReview = (reviewId: number): void => {
		const {
			assignBotReview,
		} = this.props;
		const {
			currentSubmission,
			reviews,
		} = this.state;

		if(!currentSubmission) {
			return;
		}

		const review = reviews.find(r => r.id === reviewId);

		if(review) {
			assignBotReview(currentSubmission.id, review)
				.then(r => this.highlightReview(r.id));
		}
	};

	onFormAddComment = (comment: string): void => {
		const {
			currentSubmission,
			editor,
			diffInfo,
			showDiff,
			addCommentRanges,
		} = this.state;

		this.hideAddCommentForm();
		this.setState({
			addCommentValue: '',
		});

		if(!editor || !currentSubmission || !addCommentRanges) {
			return;
		}
		const { startRange, endRange, } = addCommentRanges;

		comment = checker.removeWhiteSpaces(comment);

		if(diffInfo && showDiff) {
			const actualStartLine = diffInfo.diffByBlocks[startRange.line].line - 1;
			const actualEndLine = diffInfo.diffByBlocks[endRange.line].line - 1;

			this.addReview(currentSubmission.id,
				comment,
				actualStartLine,
				startRange.ch,
				actualEndLine,
				endRange.ch,
			).then(r => r && this.highlightReview(r.id));
		} else {
			this.addReview(currentSubmission.id,
				comment,
				startRange.line,
				startRange.ch,
				endRange.line,
				endRange.ch
			).then(r => r && this.highlightReview(r.id));
		}

		this.clearSelectionMarkers();
	};

	getStartAndEndFromRange = ({
		anchor,
		head,
	}: CodeMirror.Range): [start: CodeMirror.Position, end: CodeMirror.Position] => {
		if(anchor.line < head.line || (anchor.line === head.line && anchor.ch <= head.ch)) {
			return [anchor, head,];
		}

		return [head, anchor,];
	};

	onEscPressed = (event: KeyboardEvent): void => {
		if(event.key === 'Escape') {
			this.onFormClose();
		}
	};

	onFormClose = (): void => {
		this.hideAddCommentForm();
		this.clearSelectionMarkers();
	};

	clearSelectionMarkers = (): void => {
		const {
			editor,
		} = this.state;

		editor
			?.getDoc()
			.getAllMarks()
			.forEach(m => m.className === styles.selectionToReviewMarker && m.clear());
	};

	onSelectionChange = (
		e: Editor,
		data: {
			ranges: CodeMirror.Range[],
			update: (ranges: { anchor: CodeMirror.Position, head: CodeMirror.Position }[]) => void,
		},
	): void => {
		const {
			showDiff,
			diffInfo,
		} = this.state;

		if(!showDiff || !diffInfo || data.ranges.length === 0) {
			return;
		}

		const range = data.ranges[data.ranges.length - 1];
		const [start, end] = this.getStartAndEndFromRange(range);

		const selectedLines = buildRange(end.line - start.line + 1, start.line + 1);
		const finalSelectedLines = selectedLines
			.filter(l => !diffInfo.deletedLinesSet.has(l));

		if(finalSelectedLines.length === selectedLines.length) {
			return;
		}

		const newRanges = finalSelectedLines
			.reduce((pv: {
				anchor: { line: number, ch: number, },
				head: { line: number, ch: number },
			}[], cv, index, selectedLines,) => {
				const line = cv - 1;

				if(index === 0 || line - pv[pv.length - 1].head.line !== 1) {
					pv.push({
						anchor: { line, ch: index === 0 ? start.ch : 0, },
						head: {
							line,
							ch: index === selectedLines.length - 1
								? end.ch
								: 1000,
						},
					});
				} else {
					pv[pv.length - 1].head = {
						line,
						ch: index === selectedLines.length - 1
							? end.ch
							: 1000,
					};
				}

				return pv;
			}, []);
		if(newRanges.length === 0) {
			data.update([{ head: end, anchor: end }]);
		} else {
			data.update(newRanges);
		}
	};

	onEditorMount = (editor: Editor): void => {
		editor.setSize('auto', '100%');

		this.setState({
			editor,
		}, this.resetMarkers);
	};

	onCursorActivity = (): void => {
		const { reviews, outdatedReviews, editor, selectedReviewId, showDiff, currentSubmissionContext, } = this.state;

		if(!editor) {
			return;
		}
		const doc = editor.getDoc();
		const cursor = doc.getCursor();
		if(currentSubmissionContext?.isEditable) {
			document.addEventListener('mouseup', this.onMouseUp);
		}

		if(cursor.sticky === undefined) {
			return;
		}

		if(doc.getSelection().length > 0) {
			if(selectedReviewId > -1) {
				this.highlightReview(-1);
			}
			return;
		}

		const id = getSelectedReviewIdByCursor(reviews.concat(showDiff ? outdatedReviews : []), doc, cursor);
		this.highlightReview(id);
	};

	selectComment = (e: React.MouseEvent<Element, MouseEvent> | React.FocusEvent, id: number,): void => {
		const { selectedReviewId, editor, } = this.state;
		e.stopPropagation();

		if(selectedReviewId !== id && editor) {
			this.highlightReview(id);
		}
	};

	highlightReview = (id: number): void => {
		const { markers, selectedReviewId, editor, } = this.state;

		if(!editor) {
			return;
		}

		const doc = editor.getDoc();
		const newMarkers = { ...markers };

		const resetMarkers = (markers: TextMarker[], markerClass: string) =>
			markers.reduce((pv, marker) => {
				const position = marker.find() as MarkerRange;
				if(position) {
					const { from, to, } = position;
					marker.clear();
					pv.push(createTextMarker(to.line, to.ch, from.line, from.ch, markerClass, doc));
				}
				return pv;
			}, [] as TextMarker[]);

		if(!newMarkers[id] && id > -1) {
			return;
		}

		if(newMarkers[selectedReviewId]) {
			newMarkers[selectedReviewId] = resetMarkers(newMarkers[selectedReviewId], styles.defaultMarker,);
		}

		if(newMarkers[id]) {
			newMarkers[id] = resetMarkers(newMarkers[id], styles.selectedMarker,);
		}

		this.setState({
			selectedReviewId: id,
			markers: newMarkers,
		});
	};
}

export default InstructorReview;
