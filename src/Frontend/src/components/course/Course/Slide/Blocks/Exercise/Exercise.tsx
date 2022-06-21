import React, { createRef, RefObject } from 'react';

import { Controlled, } from "react-codemirror2";
import { Checkbox, FLAT_THEME_8PX_OLD, Select, ThemeContext, Toast, Tooltip, } from "ui";
import Review from "./Review";
import { CongratsModal } from "./CongratsModal/CongratsModal";
import { ExerciseOutput, HasOutput } from "./ExerciseOutput/ExerciseOutput";
import { ExerciseFormHeader } from "./ExerciseFormHeader/ExerciseFormHeader";
import Controls from "./Controls/Controls";
import LoginForContinue from "src/components/notificationModal/LoginForContinue";
import { AcceptedSolutionsModal } from "./AcceptedSolutions/AcceptedSolutions";
import CourseLoader from "../../../CourseLoader";
import { Info } from 'icons';

import { darkFlat } from "src/uiTheme";

import classNames from 'classnames';
import moment from "moment-timezone";

import * as acceptedSolutionsApi from "src/api/acceptedSolutions";
import { exerciseSolutions, loadFromCache, saveToCache, } from "src/utils/localStorageManager";
import { convertDefaultTimezoneToLocal } from "src/utils/momentUtils";
import { isInstructor, UserInfo } from "src/utils/courseRoles";
import {
	getLastSuccessSubmission,
	getReviewsWithoutDeleted,
	getReviewsWithTextMarkers,
	getSelectedReviewIdByCursor,
	getSubmissionColor,
	hasSuccessSubmission,
	isAcceptedSolutionsWillNotDiscardScore,
	isFirstRightAnswer,
	loadLanguageStyles,
	replaceReviewMarker,
	ReviewInfoWithMarker,
	SubmissionColor,
	submissionIsLast
} from "./ExerciseUtils";
import { getDataFromReviewToCompareChanges, getReviewAnchorTop } from "../../InstructorReview/utils";
import checker from "../../InstructorReview/reviewPolicyChecker";
import { clone } from "src/utils/jsonExtensions";

import { Language, } from "src/consts/languages";
import { DeviceType } from "src/consts/deviceType";
import {
	AutomaticExerciseCheckingResult as CheckingResult,
	AutomaticExerciseCheckingResult,
	RunSolutionResponse,
	SolutionRunStatus,
	SubmissionInfo,
} from "src/models/exercise";
import { SlideUserProgress } from "src/models/userProgress";
import { ExerciseBlock, ExerciseBlockProps } from "src/models/slide";
import { ShortUserInfo } from "src/models/users";
import { SlideContext } from "../../Slide.types";

import CodeMirror, { Doc, Editor, EditorChange, EditorConfiguration, } from "codemirror";
import 'codemirror/lib/codemirror.css';
import 'codemirror/addon/edit/matchbrackets';
import 'codemirror/addon/hint/show-hint';
import 'codemirror/addon/hint/show-hint.css';
import 'codemirror/addon/hint/javascript-hint';
import 'codemirror/addon/hint/anyword-hint';
import 'codemirror/theme/darcula.css';
import registerCodeMirrorHelpers from "./CodeMirrorAutocompleteExtension";

import styles from './Exercise.less';

import texts from './Exercise.texts';


export interface FromReduxDispatch {
	sendCode: (courseId: string, slideId: string, userId: string, value: string, language: Language,) => unknown;
	addReviewComment: (submissionId: number, reviewId: number, text: string) => unknown;
	editReviewOrComment: (submissionId: number, reviewId: number, parentReviewId: number | undefined, text: string,
		oldText: string,
	) => unknown;
	deleteReviewComment: (submissionId: number, reviewId: number, commentId: number) => unknown;
	deleteReview: (submissionId: number, reviewId: number) => unknown;
	skipExercise: (courseId: string, slideId: string, onSuccess: () => void) => unknown;
}

export interface FromReduxProps {
	isAuthenticated: boolean;
	lastCheckingResponse: RunSolutionResponse | null;
	user?: UserInfo;
	slideProgress: SlideUserProgress;
	submissionError: string | null;
	deviceType: DeviceType;
	submissions: SubmissionInfo[] | undefined;
	maxScore: number;
	forceInitialCode: boolean;
}

export interface Props extends ExerciseBlockProps, FromReduxDispatch, FromReduxProps, ExerciseBlock {
	className?: string;
	slideContext: SlideContext;
}

enum ModalType {
	congrats,
	loginForContinue,
	acceptedSolutions,
	studentsSubmissions,
}

interface ModalData<T extends ModalType> {
	type: T;
}

interface CongratsModalData extends ModalData<ModalType.congrats> {
	score: number | null;
	waitingForManualChecking: boolean | null;
}

interface SelfCheckup {
	text: string;
	checked: boolean;
	onClick: () => void;
}

interface State {
	value: string;
	valueChanged: boolean;

	isEditable: boolean;

	language: Language;

	modalData: ModalData<ModalType> | null;

	submissionLoading: boolean;
	isAllHintsShowed: boolean;
	visibleCheckingResponse?: RunSolutionResponse; // Не null только если только что сделанная посылка не содержит submission
	currentSubmission: null | SubmissionInfo;
	currentReviews: ReviewInfoWithMarker[];
	selectedReviewId: number;
	showOutput: boolean;

	editor: null | Editor;
	exerciseCodeDoc: null | Doc;
	savedPositionOfExercise: DOMRect | undefined;

	selfChecks: SelfCheckup[];
}

interface ExerciseCode {
	value: string;
	time: string;
	language: Language;
}

function saveExerciseCodeToCache(id: string, value: string, time: string, language: Language): void {
	saveToCache<ExerciseCode>(exerciseSolutions, id, { value, time, language });
}

function loadExerciseCodeFromCache(id: string): ExerciseCode | undefined {
	return loadFromCache<ExerciseCode>(exerciseSolutions, id);
}

class Exercise extends React.Component<Props, State> {
	private readonly editThemeName = 'darcula';
	private readonly defaultThemeName = 'default';
	private readonly newTry = { id: -1 };
	private readonly lastSubmissionIndex = 0;
	private wrapper: RefObject<HTMLDivElement> = createRef();

	constructor(props: Props) {
		super(props);
		const { exerciseInitialCode, submissions, languages, renderedHints, defaultLanguage } = props;

		this.state = {
			value: exerciseInitialCode,
			valueChanged: false,

			isEditable: submissions?.length === 0,

			language: defaultLanguage ?? [...languages].sort()[0],

			modalData: null,

			submissionLoading: false,
			isAllHintsShowed: renderedHints.length === 0,
			visibleCheckingResponse: undefined,
			currentSubmission: null,
			currentReviews: [],
			selectedReviewId: -1,
			showOutput: false,

			editor: null,
			exerciseCodeDoc: null,
			savedPositionOfExercise: undefined,

			selfChecks: texts.checkups.self.checks.map((ch, i) => ({
				text: ch,
				checked: false,
				onClick: () => this.onSelfCheckBoxClick(i)
			})),
		};
	}

	componentDidMount(): void {
		const { forceInitialCode, } = this.props;
		this.overrideCodeMirrorAutocomplete();

		if(forceInitialCode) {
			this.resetCode();
		} else {
			this.loadSlideSubmission();
		}

		window.addEventListener("beforeunload", this.saveCodeDraftToCacheEvent);
	}

	loadSlideSubmission = (): void => {
		const { slideContext: { slideId, }, submissions, } = this.props;

		if(submissions && submissions.length > 0) {
			this.loadSubmissionToState(submissions[this.lastSubmissionIndex]);
		} else {
			this.loadLatestCode(slideId);
		}
	};

	componentDidUpdate(prevProps: Props): void {
		const {
			lastCheckingResponse,
			slideContext: { courseId, slideId, },
			submissions,
			forceInitialCode,
			submissionError,
			slideProgress,
			isAuthenticated,
			expectedOutput,
		} = this.props;
		const { currentSubmission, submissionLoading, selectedReviewId, value, } = this.state;

		if(!submissions) {
			return;
		}

		if(!prevProps.submissions) {
			if(forceInitialCode) {
				this.resetCode();
			} else {
				this.loadSlideSubmission();
			}
			return;
		}

		if(submissionError && submissionError !== prevProps.submissionError) {
			Toast.push("При добавлении или удалении комментария произошла ошибка");
		}

		if(forceInitialCode !== prevProps.forceInitialCode) {
			if(forceInitialCode) {
				this.saveCodeDraftToCache();
				this.resetCode();
			} else {
				this.loadSlideSubmission();
			}
			return;
		}

		if(courseId !== prevProps.slideContext.courseId || slideId !== prevProps.slideContext.slideId || isAuthenticated && isAuthenticated !== prevProps.isAuthenticated) {
			if(forceInitialCode) {
				this.resetCode();
			} else {
				this.setState({
					submissionLoading: false,
				});
				this.saveCodeDraftToCache(prevProps.slideContext.slideId, value);
				this.loadSlideSubmission();
			}
			return;
		}

		const hasNewLastCheckingResponse = lastCheckingResponse
			&& lastCheckingResponse !== prevProps.lastCheckingResponse; // Сравнение по ссылкам
		if(hasNewLastCheckingResponse && lastCheckingResponse) {
			const { submission, solutionRunStatus, } = lastCheckingResponse;

			if(submission && submission.automaticChecking?.result !== AutomaticExerciseCheckingResult.WrongAnswer
				&& submission.automaticChecking?.result !== AutomaticExerciseCheckingResult.CompilationError) {
				this.loadSubmissionToState(submissions.find(s => s.id === submission.id));

				if(solutionRunStatus === SolutionRunStatus.Success) {
					const { automaticChecking } = submission;
					if((!automaticChecking || automaticChecking.result === CheckingResult.RightAnswer)
						&& !slideProgress.isSkipped
						&& isFirstRightAnswer(submissions, submission)
					) {
						this.openModal({
							type: ModalType.congrats,
							score: lastCheckingResponse.score,
							waitingForManualChecking: lastCheckingResponse.waitingForManualChecking,
						});
					}
				}
			} else {
				this.setState({
					visibleCheckingResponse: lastCheckingResponse,
				});
			}

			if(solutionRunStatus === SolutionRunStatus.CompilationError
				|| solutionRunStatus === SolutionRunStatus.Ignored
				|| submission?.automaticChecking?.result === AutomaticExerciseCheckingResult.WrongAnswer
				|| submission?.automaticChecking?.result === AutomaticExerciseCheckingResult.CompilationError) {
				this.setState({
					isEditable: true,
				});
			}

			const hasOutput = HasOutput(
				lastCheckingResponse?.message,
				lastCheckingResponse.submission?.automaticChecking,
				expectedOutput);
			if(hasOutput) {
				this.setState({
					showOutput: true,
				});
			}

			if(submissionLoading) {
				this.setState({
					submissionLoading: false,
				});
			}
		} else if(currentSubmission) {
			const submission = submissions.find(s => s.id === currentSubmission.id);
			if(!submission) {
				return;
			}

			const reviewsCompare = (submission.manualChecking?.reviews ?? []).map(getDataFromReviewToCompareChanges);
			const newReviewsCompare = (currentSubmission.manualChecking?.reviews ?? []).map(
				getDataFromReviewToCompareChanges);

			if(submission && JSON.stringify(newReviewsCompare) !== JSON.stringify(reviewsCompare)) { // Отличаться должны только в случае изменения комментериев
				this.setCurrentSubmission(submission,
					() => this.highlightReview(selectedReviewId)); //Сохраняем выделение выбранного ревью
			}
		}
	}

	overrideCodeMirrorAutocomplete = (): void => {
		registerCodeMirrorHelpers();
		// eslint-disable-next-line @typescript-eslint/ban-ts-comment
		// @ts-ignore because autocomplete will be added by js addon script
		CodeMirror.commands.autocomplete = (cm: Editor) => {
			const { language, } = this.state;
			// eslint-disable-next-line @typescript-eslint/ban-ts-comment
			// @ts-ignore
			const hint = CodeMirror.hint[language.toLowerCase()];
			if(hint) {
				cm.showHint({ hint: hint });
			}
		};
	};

	saveCodeDraftToCacheEvent = (): void => {
		this.saveCodeDraftToCache();
	};

	saveCodeDraftToCache = (slideId?: string, value?: string, language?: Language): void => {
		const { forceInitialCode, isAuthenticated, slideContext, } = this.props;
		const { valueChanged, } = this.state;

		if(valueChanged && !forceInitialCode && isAuthenticated) {
			saveExerciseCodeToCache(
				slideId || slideContext.slideId,
				value || this.state.value,
				moment().format(),
				language || this.state.language);
		}
	};

	componentWillUnmount(): void {
		this.saveCodeDraftToCache();
		window.removeEventListener("beforeunload", this.saveCodeDraftToCacheEvent);
	}

	render(): React.ReactElement {
		const { className, submissions, isAuthenticated, forceInitialCode, } = this.props;

		const opts = this.codeMirrorOptions;

		if(!isAuthenticated) {
			return (<div className={ classNames(styles.wrapper, className) } ref={ this.wrapper }>
				{ this.renderControlledCodeMirror(opts, []) }
			</div>);
		}

		if(forceInitialCode) {
			return (<div className={ classNames(styles.wrapper, className) } ref={ this.wrapper }>
				{ this.renderControlledCodeMirror(opts, []) }
			</div>);
		}

		if(!submissions) {
			return <CourseLoader/>;
		}

		return (
			<div className={ classNames(styles.wrapper, className) } ref={ this.wrapper }>
				{ this.renderControlledCodeMirror(opts, submissions) }
			</div>
		);
	}

	get codeMirrorOptions(): EditorConfiguration {
		const { isAuthenticated, } = this.props;
		const { isEditable, language } = this.state;

		return {
			mode: loadLanguageStyles(language),
			lineNumbers: true,
			scrollbarStyle: 'null',
			lineWrapping: true,
			theme: isEditable ? this.editThemeName : this.defaultThemeName,
			readOnly: !isEditable || !isAuthenticated,
			matchBrackets: true,
			tabSize: 4,
			indentUnit: 4,
			indentWithTabs: true,
			extraKeys: {
				ctrlSpace: "autocomplete",
				".": function (cm: Editor) {
					setTimeout(function () {
						const cursorPosition = cm.getCursor();
						const char = cm.getLine(cursorPosition.line).substr(Math.max(cursorPosition.ch - 1, 0), 1);

						if(char === '.') {
							cm.execCommand("autocomplete");
						}
					}, 100);
					return CodeMirror.Pass;
				}
			},
		};
	}

	renderControlledCodeMirror = (opts: EditorConfiguration, submissions: SubmissionInfo[]): React.ReactElement => {
		const {
			expectedOutput, user,
			slideProgress, maxScore, languages,
			hideSolutions, renderedHints,
			attemptsStatistics, isAuthenticated,
		} = this.props;
		const {
			value, currentSubmission,
			isEditable, exerciseCodeDoc, modalData,
			currentReviews, showOutput, selectedReviewId, visibleCheckingResponse,
			submissionLoading, isAllHintsShowed, editor,
		} = this.state;

		const isReview = !isEditable && currentReviews.length > 0;
		const automaticChecking = currentSubmission?.automaticChecking ?? visibleCheckingResponse?.automaticChecking ?? visibleCheckingResponse?.submission?.automaticChecking;
		const selectedSubmissionIsLast = submissionIsLast(submissions, currentSubmission);
		const selectedSubmissionIsLastSuccess = getLastSuccessSubmission(submissions)?.id === currentSubmission?.id;
		const isMaxScore = slideProgress.score === maxScore;
		const submissionColor = getSubmissionColor(visibleCheckingResponse?.solutionRunStatus,
			automaticChecking?.result,
			hasSuccessSubmission(submissions), selectedSubmissionIsLast, selectedSubmissionIsLastSuccess,
			slideProgress.waitingForManualChecking,
			slideProgress.prohibitFurtherManualChecking, slideProgress.isSkipped, isMaxScore);

		const wrapperClassName = classNames(
			styles.exercise,
			{ [styles.reviewWrapper]: isReview },
		);
		const editorClassName = classNames(
			styles.editor,
			{ [styles.editorWithoutBorder]: isEditable },
			{ [styles.editorInReview]: isReview },
		);

		const hasOutput = currentSubmission
			&& HasOutput(visibleCheckingResponse?.message, currentSubmission.automaticChecking,
				expectedOutput);
		const isSafeShowAcceptedSolutions = isAcceptedSolutionsWillNotDiscardScore(submissions,
			slideProgress.isSkipped);
		const outputMessage = visibleCheckingResponse?.message || visibleCheckingResponse?.submission?.automaticChecking?.output;

		return (
			<React.Fragment>
				{ submissions.length !== 0 && this.renderSubmissionsSelect(submissions) }
				{ languages.length > 1 && (submissions.length > 0 || isEditable) && this.renderLanguageSelect() }
				{ languages.length > 1 && (submissions.length > 0 || isEditable) && this.renderLanguageLaunchInfoTooltip() }
				{ !isEditable && this.renderHeader(submissionColor, selectedSubmissionIsLast,
					selectedSubmissionIsLastSuccess) }
				{ modalData && this.renderModal(modalData) }
				<div className={ wrapperClassName } onClick={ this.openModalForUnauthenticatedUser }>
					<Controlled
						onBeforeChange={ this.onBeforeChange }
						editorDidMount={ this.onEditorMount }
						onCursorActivity={ this.onCursorActivity }
						onUpdate={ this.scrollToBottomBorderIfNeeded }
						className={ editorClassName }
						options={ opts }
						value={ value }
					/>
					{ exerciseCodeDoc && isReview &&
						<Review
							user={ user }
							addReviewComment={ this.addReviewComment }
							deleteReviewOrComment={ this.deleteReviewOrComment }
							selectedReviewId={ selectedReviewId }
							onReviewClick={ this.selectComment }
							editReviewOrComment={ this.editReviewOrComment }
							reviews={ getReviewsWithoutDeleted(currentReviews)
								.map(r => ({
									...r,
									markers: undefined,
									anchor: getReviewAnchorTop(r, editor,),
								})) }
						/>
					}
				</div>
				{/* TODO not included in current release !isEditable && currentSubmission && this.renderOverview(currentSubmission)*/ }
				{ isAuthenticated && <Controls>
					<Controls.SubmitButton
						isLoading={ submissionLoading }
						onClick={ isEditable ? this.sendExercise : this.loadNewTry }
						text={ isEditable ? texts.controls.submitCode.text : texts.controls.submitCode.redactor }
					/>
					<Controls.ButtonsContainer>
						{ renderedHints.length !== 0 &&
							<Controls.ShowHintButton
								onAllHintsShowed={ this.onAllHintsShowed }
								renderedHints={ renderedHints }
							/> }

						{ isEditable && <Controls.ResetButton onResetButtonClicked={ this.resetCodeAndCache }/> }

						{ (!isEditable && hasOutput) && <Controls.OutputButton
							showOutput={ showOutput }
							onShowOutputButtonClicked={ this.toggleOutput }
						/> }

						{ (!hideSolutions && (isAllHintsShowed || isSafeShowAcceptedSolutions)
								&& attemptsStatistics && attemptsStatistics.usersWithRightAnswerCount > 0)
							&& <Controls.AcceptedSolutionsButton
								onVisitAcceptedSolutions={ this.openAcceptedSolutionsModal }
								isShowAcceptedSolutionsAvailable={ isSafeShowAcceptedSolutions }
							/> }
						{ this.isVisualizerEnabled() &&
							<Controls.VisualizerButton
								code={ value }
								onModalClose={ this.copyCodeFromVisualizer }/>
						}
					</Controls.ButtonsContainer>
					{ attemptsStatistics && <Controls.StatisticsHint attemptsStatistics={ attemptsStatistics }/> }
				</Controls>
				}
				{ showOutput && HasOutput(outputMessage, automaticChecking, expectedOutput) &&
					<ExerciseOutput
						solutionRunStatus={ visibleCheckingResponse?.solutionRunStatus ?? SolutionRunStatus.Success }
						message={ outputMessage }
						expectedOutput={ expectedOutput }
						automaticChecking={ automaticChecking }
						submissionColor={ submissionColor }
					/>
				}
			</React.Fragment>
		);
	};

	editReviewOrComment = (text: string, reviewId: number, parentReviewId: number | undefined,): void => {
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
		} else {
			Toast.push(texts.editCommentError);
		}
	};

	isVisualizerEnabled = (): boolean => {
		const { pythonVisualizerEnabled, } = this.props;
		const { language, isEditable, } = this.state;
		return isEditable && pythonVisualizerEnabled && language === Language.python3 || false;
	};

	copyCodeFromVisualizer = (code: string): void => {
		if(this.state.value !== code) {
			this.setState({
				value: code,
				valueChanged: true,
			});
		}
	};

	onAllHintsShowed = (): void => {
		this.setState({
			isAllHintsShowed: true
		});
	};

	openModalForUnauthenticatedUser = (): void => {
		const { isAuthenticated } = this.props;
		if(!isAuthenticated) {
			this.openModal({ type: ModalType.loginForContinue });
		}
	};

	renderSubmissionsSelect = (submissions: SubmissionInfo[]): React.ReactElement => {
		const { currentSubmission } = this.state;
		const { waitingForManualChecking } = this.props.slideProgress;

		const lastSuccessSubmission = getLastSuccessSubmission(submissions);
		const items = [[this.newTry.id, texts.submissions.newTry], ...submissions.map((submission) => {
			const caption = texts.submissions
				.getSubmissionCaption(submission, lastSuccessSubmission === submission, waitingForManualChecking);
			return [submission.id, caption];
		})];

		return (
			<div className={ styles.select }>
				<ThemeContext.Provider value={ FLAT_THEME_8PX_OLD }>
					<Select
						width={ '100%' }
						items={ items }
						value={ currentSubmission?.id || this.newTry.id }
						onValueChange={ this.onSubmissionsSelectValueChange }
					/>
				</ThemeContext.Provider>
			</div>
		);
	};

	onSubmissionsSelectValueChange = (id: unknown): void => {
		const { submissions, } = this.props;

		this.saveCodeDraftToCache();
		if(id === this.newTry.id) {
			this.loadNewTry();
		}

		if(submissions) {
			this.loadSubmissionToState(submissions.find(s => s.id === id));
		}
	};

	renderLanguageSelect = (): React.ReactElement => {
		const { language, isEditable, } = this.state;
		const { languages, languageInfo } = this.props;
		const items = [...languages].sort().map((l) => {
			return [l, texts.getLanguageLaunchInfo(l, languageInfo).compiler];
		});
		return (
			<div className={ styles.select }>
				<ThemeContext.Provider value={ FLAT_THEME_8PX_OLD }>
					<Select
						disabled={ !isEditable }
						width={ '100%' }
						items={ items }
						value={ language }
						onValueChange={ this.onLanguageSelectValueChange }
					/>
				</ThemeContext.Provider>
			</div>
		);
	};

	renderLanguageLaunchInfoTooltip = (): React.ReactElement => {
		const { deviceType, } = this.props;
		return (
			<ThemeContext.Provider value={ darkFlat }>
				<Tooltip trigger={ "hover" } render={ this.renderLanguageLaunchInfoTooltipContent }>
					<span className={ styles.launchInfoHelpIcon }>
						{ (deviceType !== DeviceType.mobile && deviceType !== DeviceType.tablet)
							? texts.compilationText
							: <Info/> }
					</span>
				</Tooltip>
			</ThemeContext.Provider>
		);
	};

	renderLanguageLaunchInfoTooltipContent = (): React.ReactNode => {
		const { language } = this.state;
		const { languageInfo } = this.props;

		return texts.getLanguageLaunchMarkup(texts.getLanguageLaunchInfo(language, languageInfo));
	};

	onLanguageSelectValueChange = (l: unknown): void => {
		this.setState({ language: l as Language });
	};

	renderHeader = (submissionColor: SubmissionColor, selectedSubmissionIsLast: boolean,
		selectedSubmissionIsLastSuccess: boolean
	): React.ReactNode => {
		const { currentSubmission, visibleCheckingResponse } = this.state;
		const { waitingForManualChecking, prohibitFurtherManualChecking, score } = this.props.slideProgress;
		const { submissions, exerciseTexts } = this.props;
		if(!currentSubmission && !visibleCheckingResponse) {
			return null;
		}
		const hasSubmissionWithManualChecking = submissions?.some(s => s.manualChecking != null);
		return (
			<ExerciseFormHeader
				exerciseTexts={ exerciseTexts }
				solutionRunStatus={ visibleCheckingResponse ? visibleCheckingResponse.solutionRunStatus : null }
				selectedSubmission={ currentSubmission }
				waitingForManualChecking={ waitingForManualChecking }
				prohibitFurtherManualChecking={ prohibitFurtherManualChecking }
				selectedSubmissionIsLast={ selectedSubmissionIsLast }
				selectedSubmissionIsLastSuccess={ selectedSubmissionIsLastSuccess }
				score={ score }
				submissionColor={ submissionColor }
				hasSubmissionWithManualChecking={ hasSubmissionWithManualChecking }
			/>
		);
	};

	loadSubmissionToState = (submission?: SubmissionInfo): void => {
		this.clearAllTextMarkers();

		// Firstly we updating code in code mirror
		// when code is rendered we attaching reviewMarkers and loading reviews
		// after all is done we refreshing editor to refresh layout and sizes depends on reviews sizes
		if(submission) {
			this.setState({
					value: submission.code,
					language: submission.language,
					isEditable: false,
					valueChanged: false,
					showOutput: false,
					visibleCheckingResponse: undefined,
					currentReviews: [],
				}, () =>
					this.setCurrentSubmission(submission)
			);
		}
	};

	setCurrentSubmission = (submission: SubmissionInfo, callback?: () => void): void => {
		const { exerciseCodeDoc, } = this.state;
		this.clearAllTextMarkers();
		const clonedSubmission = clone(submission);
		this.setState({
			currentSubmission: clonedSubmission,
			currentReviews: exerciseCodeDoc
				? getReviewsWithTextMarkers(clonedSubmission, exerciseCodeDoc, styles.reviewCode)
				: [],
		}, () => {
			const { editor } = this.state;
			if(editor) {
				editor.refresh();
			}
			if(callback) {
				callback();
			}
		});
	};

	openModal = <T extends ModalData<ModalType>>(data: T | null): void => {
		this.setState({
			modalData: data,
		});
	};

	openAcceptedSolutionsModal = (): void => {
		const { slideContext: { courseId, slideId, }, submissions, slideProgress, skipExercise, } = this.props;

		if(submissions && isAcceptedSolutionsWillNotDiscardScore(submissions, slideProgress.isSkipped)) {
			this.openModal({ type: ModalType.acceptedSolutions });
		} else {
			skipExercise(courseId, slideId, () => {
				this.openModal({ type: ModalType.acceptedSolutions });
			});
		}
	};

	renderOverview = (submission: SubmissionInfo): React.ReactElement => {
		const { selfChecks } = this.state;
		const checkups = [
			{
				title: texts.checkups.self.title,
				content:
					<React.Fragment>
						<span className={ styles.overviewSelfCheckComment }>
							{ texts.checkups.self.text }
						</span>
						<ul className={ styles.overviewSelfCheckList }>
							{ this.renderSelfCheckBoxes(selfChecks) }
						</ul>
					</React.Fragment>
			},
		];

		const reviewsLength = submission?.automaticChecking?.reviews?.length || 0;
		if(reviewsLength !== 0) {
			checkups.unshift(
				{
					title: texts.checkups.bot.title,
					content:
						<span className={ styles.overviewComment }>
						{ texts.checkups.bot.countBotComments(reviewsLength) }
							<a onClick={ this.showFirstBotComment }>{ texts.checkups.showReview }</a>
					</span>
				});
		}

		const reviewsCount = submission?.manualChecking?.reviews?.length || 0;
		if(reviewsCount !== 0) {
			checkups.unshift({
				title: texts.checkups.teacher.title,
				content:
					<span className={ styles.overviewComment }>
						{ texts.checkups.teacher.countTeacherReviews(reviewsCount) }
						<a onClick={ this.showFirstComment }>{ texts.checkups.showReview }</a>
					</span>
			});
		}

		return (
			<ul className={ styles.overview }>
				{ checkups.map(({ title, content }) =>
					<li key={ title } className={ styles.overviewLine } title={ title }>
						<h3>{ title }</h3>
						{ content }
					</li>
				) }
			</ul>
		);
	};

	renderSelfCheckBoxes = (selfChecks: SelfCheckup[]): React.ReactNode => {
		return (
			selfChecks.map(({ text, checked, onClick, }, i) =>
				<li key={ i }>
					<Checkbox checked={ checked } onClick={ onClick }/> <span
					className={ styles.selfCheckText }>{ text }</span>
				</li>
			)
		);
	};

	onSelfCheckBoxClick = (i: number): void => {
		const { selfChecks } = this.state;
		const newSelfChecks = [...selfChecks];

		newSelfChecks[i].checked = !newSelfChecks[i].checked;

		this.setState({
			selfChecks: newSelfChecks,
		});
	};

	renderModal = (modalData: ModalData<ModalType>): React.ReactNode => {
		const { hideSolutions, slideContext: { courseId, slideId, }, user, forceInitialCode, } = this.props;

		switch (modalData.type) {
			case ModalType.congrats: {
				const { score, waitingForManualChecking, } = modalData as CongratsModalData;
				const showAcceptedSolutions = !waitingForManualChecking && !hideSolutions;

				return (
					score && score > 0 &&
					<CongratsModal
						showAcceptedSolutions={ showAcceptedSolutions ? this.openAcceptedSolutionsModal : undefined }
						score={ score }
						waitingForManualChecking={ waitingForManualChecking || false }
						onClose={ this.closeModal }
					/>
				);
			}
			case ModalType.loginForContinue: {
				return (
					<LoginForContinue
						onClose={ this.closeModal }
					/>
				);
			}
			case ModalType.studentsSubmissions:
				break;
			case ModalType.acceptedSolutions: {
				const instructor = user
					? isInstructor(
						{
							isSystemAdministrator: user.isSystemAdministrator,
							courseRole: user.courseRole
						})
					: false;
				return (
					<AcceptedSolutionsModal
						courseId={ courseId }
						slideId={ slideId }
						isInstructor={ instructor && !forceInitialCode }
						user={ user as ShortUserInfo }
						onClose={ this.closeModal }
						acceptedSolutionsApi={ acceptedSolutionsApi }
					/>
				);
			}
		}
	};

	showFirstComment = (): void => {
		//TODO
	};

	showFirstBotComment = (): void => {
		//TODO
	};

	selectComment = (e: React.MouseEvent<Element, MouseEvent> | React.FocusEvent, id: number,): void => {
		const { isEditable, selectedReviewId, } = this.state;
		e.stopPropagation();

		if(!isEditable && selectedReviewId !== id) {
			this.highlightReview(id);
		}
	};

	highlightReview = (id: number): void => {
		const { currentReviews, selectedReviewId, editor, exerciseCodeDoc, } = this.state;
		if(!exerciseCodeDoc) {
			return;
		}

		const newCurrentReviews = replaceReviewMarker(
			currentReviews,
			selectedReviewId,
			id,
			exerciseCodeDoc,
			styles.reviewCode,
			styles.selectedReviewCode,
		);

		this.setState({
			currentReviews: newCurrentReviews.reviews,
			selectedReviewId: id,
		}, () => {
			if(id >= 0 && editor) {
				editor.scrollIntoView({ ch: 0, line: newCurrentReviews.selectedReviewLine, }, 200);
			}
		});
	};

	resetCodeAndCache = (): void => {
		const { slideContext: { slideId, }, exerciseInitialCode, } = this.props;

		this.resetCode();
		this.saveCodeDraftToCache(slideId, exerciseInitialCode);
	};

	resetCode = (): void => {
		const { exerciseInitialCode, } = this.props;
		const savedPositionOfExercise = this.wrapper.current?.getBoundingClientRect();

		this.clearAllTextMarkers();
		this.setState({
			value: exerciseInitialCode,
			valueChanged: true,
			isEditable: true,
			currentSubmission: null,
			visibleCheckingResponse: undefined,
			currentReviews: [],
			showOutput: false,
			savedPositionOfExercise,
		});
	};

	scrollToBottomBorderIfNeeded = (): void => {
		const { savedPositionOfExercise } = this.state;

		const newPositionOfExercise = this.wrapper.current?.getBoundingClientRect();
		if(savedPositionOfExercise && newPositionOfExercise) {
			if(savedPositionOfExercise.top < 0 && savedPositionOfExercise.bottom > newPositionOfExercise.bottom) {
				window.scrollTo({
					left: 0,
					top: window.pageYOffset - (savedPositionOfExercise.bottom - newPositionOfExercise.bottom),
					behavior: "auto",
				});

				this.setState({
					savedPositionOfExercise: undefined,
				});
			}
		}
	};

	clearAllTextMarkers = (): void => {
		const { currentReviews, } = this.state;

		currentReviews.forEach(({ markers }) => markers.forEach(m => m.clear()));

		this.setState({
			selectedReviewId: -1,
		});
	};

	loadNewTry = (): void => {
		const { slideContext: { slideId, }, } = this.props;
		this.resetCode();
		this.loadLatestCode(slideId);
	};

	toggleOutput = (): void => {
		const { showOutput, } = this.state;

		this.setState({
			showOutput: !showOutput
		});
	};

	closeModal = (): void => {
		this.setState({
			modalData: null,
		});
	};

	sendExercise = (): void => {
		const { value, language } = this.state;
		const { slideContext: { courseId, slideId, }, sendCode, user, } = this.props;

		if(!user) {
			return;
		}

		this.setState({
			submissionLoading: true,
			showOutput: false,
		});

		sendCode(courseId, slideId, user.id, value, language);
	};

	addReviewComment = (reviewId: number, text: string): void => {
		const { addReviewComment, } = this.props;
		const { currentSubmission, } = this.state;

		if(currentSubmission) {
			addReviewComment(currentSubmission.id, reviewId, text);
		}
	};

	deleteReviewOrComment = (reviewId: number, commentId?: number,): void => {
		const { deleteReviewComment, deleteReview, } = this.props;
		const { currentSubmission, } = this.state;

		if(currentSubmission) {
			if(commentId) {
				deleteReviewComment(currentSubmission.id, reviewId, commentId,);
			} else {
				deleteReview(currentSubmission.id, reviewId,);
			}
		}
	};

	onBeforeChange = (editor: Editor, data: EditorChange, value: string): void => {
		this.setState({
			value,
			valueChanged: true,
		});
	};

	onEditorMount = (editor: Editor): void => {
		editor.setSize('auto', '100%');
		this.setState({
			exerciseCodeDoc: editor.getDoc(),
			editor,
		});
	};

	onCursorActivity = (): void => {
		const { currentReviews, exerciseCodeDoc, isEditable, } = this.state;
		if(exerciseCodeDoc) {
			const cursor = exerciseCodeDoc.getCursor();

			if(!isEditable && currentReviews.length > 0) {
				const reviewId = getSelectedReviewIdByCursor(currentReviews, exerciseCodeDoc, cursor);
				this.highlightReview(reviewId);
			}
		}
	};

	loadLatestCode = (slideId: string): void => {
		const { submissions, } = this.props;
		const { language, } = this.state;

		const code = loadExerciseCodeFromCache(slideId);
		this.resetCode();
		if(submissions && submissions.length > 0 && code) {
			let newValue = code.value;

			const lastSubmission = submissions[this.lastSubmissionIndex];
			const lastSubmissionTime = convertDefaultTimezoneToLocal(lastSubmission.timestamp);
			const codeFromCacheTime = moment(code.time);

			if(lastSubmissionTime.diff(codeFromCacheTime, 'seconds') >= 0) { //if last submission is newer then last saved
				this.saveCodeDraftToCache(slideId, lastSubmission.code);
				newValue = lastSubmission.code;
			}

			this.setState({
				value: newValue,
				language: this.getSupportedLanguage(code.language ? code.language : language),
			});
			return;
		}

		if(submissions && submissions.length > 0) {
			const lastSubmission = submissions[this.lastSubmissionIndex];
			this.saveCodeDraftToCache(slideId, lastSubmission.code);
			this.setState({
				value: lastSubmission.code,
				language: this.getSupportedLanguage(lastSubmission.language),
			});
			return;
		}

		if(code) {
			this.setState({
				value: code.value,
				language: this.getSupportedLanguage(code.language ? code.language : language),
			});
		}
	};

	getSupportedLanguage = (languageToCheck: Language): Language => {
		const { languages, defaultLanguage, } = this.props;

		return languages.some(l => l === languageToCheck)
			? languageToCheck
			: defaultLanguage || languages[0];
	};
}

export default Exercise;
