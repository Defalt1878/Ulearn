import React, { Component } from "react";
import { HubConnection } from "@microsoft/signalr";
import classnames from 'classnames';

import api from "src/api";

import Navigation from "../Navigation";
import { CourseNavigationProps, UnitNavigationProps } from "../Navigation/Navigation";
import AnyPage from 'src/pages/AnyPage';
import UnitFlashcardsPage from 'src/pages/course/UnitFlashcardsPage.js';
import CourseFlashcardsPage from 'src/pages/course/CourseFlashcardsPage.js';
import PreviewUnitPageFromAllCourse from "src/components/flashcards/UnitPage/PreviewUnitPageFromAllCourse";
import SlideHeader from "./Slide/SlideHeader/SlideHeader";
import { BlocksWrapper } from "src/components/course/Course/Slide/Blocks";
import NavigationButtons from "./NavigationButtons";
import CommentsView from "src/components/comments/CommentsView";
import Slide from './Slide/Slide.redux';

import { UrlError } from "src/components/common/Error/NotFoundErrorBoundary";
import Error404 from "src/components/common/Error/Error404";
import { Link, } from "react-router-dom";
import { Helmet } from "react-helmet";
import { Edit, HelpDot, } from "icons";
import CourseLoader from "./CourseLoader";

import { findNextUnit, findUnitIdBySlideId, getCourseStatistics, } from "./CourseUtils";
import { buildQuery } from "src/utils";
import { UserInfo, UserRoles } from "src/utils/courseRoles";
import documentReadyFunctions from "src/legacy/legacy";
import runLegacy from "src/legacy/legacyRunner";

import { adminCheckingQueuePath, constructPathToSlide, signalrWS, } from 'src/consts/routes';
import { ShortSlideInfo, SlideType, } from 'src/models/slide';
import Meta from "src/consts/Meta";
import { CourseInfo, UnitInfo, } from "src/models/course";
import { SlideUserProgress, } from "src/models/userProgress";
import { CourseRoleType } from "src/consts/accessType";
import { ShortUserInfo } from "src/models/users";
import { isTimeArrived } from "src/utils/momentUtils";
import { Gapped, Hint, Toast } from "ui";
import {
	FlashcardsStatistics,
	MenuItem,
	Progress,
	SlideAdditionalInfo,
	SlideProgressStatus,
	UnitProgress,
} from "../Navigation/types";

import { CourseProps, State } from "./Course.types";
import texts from "./Course.texts";
import styles from "./Course.less";


const defaultMeta: Meta = {
	title: texts.ulearnTitle,
	description: texts.ulearnDescription,
	keywords: [],
	imageUrl: '',
};

class Course extends Component<CourseProps, State> {
	private signalRConnection: HubConnection | null = null;

	constructor(props: CourseProps) {
		super(props);
		const { slideInfo, courseInfo, } = props;
		const Page = Course.getOpenedPage(courseInfo, slideInfo.slideType);

		this.state = {
			Page,
			currentCourseId: "",
			currentSlideId: "",
			highlightedUnit: null,
			meta: defaultMeta,
			courseStatistics: {
				courseProgress: { current: 0, max: 0, inProgress: 0, },
				byUnits: {},
				flashcardsStatistics: { count: 0, unratedCount: 0 },
				flashcardsStatisticsByUnits: {},
			},
		};
	}

	componentDidMount(): void {
		const {
			loadCourse,
			loadUserProgress,
			loadCourseErrors,
			loadDeadLines,
			courseId,
			courseInfo,
			progress,
			user,
			deadLines,
		} = this.props;
		const { title } = this.state;
		const { isAuthenticated } = user;

		this.startSignalRConnection();

		if(!deadLines) {
			loadDeadLines(courseId);
		}

		if(!courseInfo) {
			loadCourse(courseId);
		} else {
			this.updateWindowMeta(title, courseInfo.title);
			if(courseInfo.isTempCourse) {
				loadCourseErrors(courseId);
			}
		}

		if(isAuthenticated && !progress && user.id) {
			loadUserProgress(courseId, user.id);
		}

		/* TODO: (rozentor) for now it copied from downloadedHtmlContetn, which run documentReadyFunctions scripts. In future, we will have no scripts in back, so it can be removed totally ( in other words, remove it when DownloadedHtmlContent will be removed)  */
		runLegacy(documentReadyFunctions);
	}

	startSignalRConnection = (): void => {
		const connection = api.createSignalRConnection(signalrWS);
		connection.on("courseChanged", this.onCourseChangedEvent);
		connection.start();

		this.signalRConnection = connection;
	};

	componentWillUnmount(): void {
		const { signalRConnection } = this;

		if(signalRConnection) {
			signalRConnection.stop();
		}
	}

	onCourseChangedEvent = (eventData: string): void => {
		const { loadCourse, loadedCourseIds, } = this.props;
		const { courseId } = JSON.parse(eventData);
		if(loadedCourseIds[courseId]) {
			loadCourse(courseId);
		}
	};

	componentDidUpdate(prevProps: CourseProps, prevState: State): void {
		const {
			courseId,
			courseLoadingErrorStatus,
			courseLoading,
			loadCourse,
			courseInfo,
			loadCourseErrors,
			loadUserProgress,
			user,
			progress,
			isHijacked,
			updateVisitedSlide,
			loadFlashcards,
			flashcardsLoading,
			flashcardsStatisticsByUnits,
			slideInfo,
		} = this.props;
		const { title, } = this.state;
		const { isAuthenticated, } = user;

		if(isAuthenticated !== prevProps.user.isAuthenticated && user.id) {
			loadCourse(courseId);
			loadUserProgress(courseId, user.id);
			return;
		}

		if(!courseInfo && !courseLoading) {
			if(!courseLoadingErrorStatus) {
				loadCourse(courseId);
			}
			return;
		}

		if(courseInfo !== prevProps.courseInfo && !flashcardsStatisticsByUnits && !flashcardsLoading) {
			loadFlashcards(courseId);
		}

		if(title !== prevState.title) {
			this.updateWindowMeta(title, courseInfo.title);
			if(courseInfo.isTempCourse) {
				loadCourseErrors(courseId);
			}
		}

		if(!prevProps.progress && progress && !isHijacked && slideInfo.slideId) {
			updateVisitedSlide(courseId, slideInfo.slideId);
		}
	}

	static getDerivedStateFromProps(props: CourseProps, state: State): State | null {
		const {
			courseId,
			slideInfo,
			units,
			enterToCourse,
			courseInfo,
			progress,
			isHijacked,
			updateVisitedSlide,
			flashcardsStatisticsByUnits,
			deadLines,
		} = props;

		if(!units || !flashcardsStatisticsByUnits) {
			return null;
		}

		const newStats = getCourseStatistics(units, progress, courseInfo.scoring.groups, flashcardsStatisticsByUnits,
			deadLines);
		//TMP TODO rozentor: remove next line if orrange approved
		newStats.courseProgress.inProgress = 0;

		const slideId = slideInfo.slideId;

		if(state.currentCourseId !== courseId || state.currentSlideId !== slideId || slideInfo.slideType !== state.currentSlideType) {
			enterToCourse(courseId);
			const openUnitId = findUnitIdBySlideId(slideId, courseInfo);
			const openedUnit = openUnitId ? units[openUnitId] : undefined;
			window.scrollTo(0, 0);
			const Page = Course.getOpenedPage(props.courseInfo, slideInfo?.slideType);
			const title = Course.getTitle(slideInfo.navigationInfo?.current.title);
			if(slideId && progress && !isHijacked) {
				updateVisitedSlide(courseId, slideId);
			}

			return {
				meta: state.meta || defaultMeta,
				Page,
				title,
				currentSlideId: slideId,
				currentCourseId: courseId,
				currentSlideType: slideInfo.slideType,
				highlightedUnit: openUnitId || null,
				openedUnit,
				courseStatistics: newStats,
			};
		}

		if(JSON.stringify(newStats) !== JSON.stringify(state.courseStatistics)) {
			return {
				...state,
				courseStatistics: newStats,
			};
		}

		return null;
	}

	static getOpenedPage = (
		courseInfo: CourseInfo | undefined,
		slideType: SlideType | undefined,
	): React.ComponentType | React.ElementType => {
		if(slideType === SlideType.PreviewFlashcards) {
			return PreviewUnitPageFromAllCourse;
		}

		if(slideType === SlideType.CourseFlashcards) {
			return CourseFlashcardsPage;
		}

		if(!courseInfo || !courseInfo.units) {
			return AnyPage;
		}

		if(slideType === SlideType.NotFound) {
			throw new UrlError();
		}

		if(slideType === SlideType.Flashcards) {
			return UnitFlashcardsPage;
		}

		if(slideType &&
			(slideType === SlideType.Lesson || (slideType === SlideType.Exercise))) {
			return Slide;
		}

		return AnyPage;
	};

	static getTitle = (currentSlideTitle: string | undefined): string => {
		return currentSlideTitle ? currentSlideTitle : texts.flashcardsTitle;
	};

	updateWindowMeta = (slideTitle: string | undefined, courseTitle: string): void => {
		if(slideTitle) {
			this.setState({
				meta: {
					title: `${ courseTitle }: ${ slideTitle } на Ulearn.me`,
					description: texts.ulearnDescription,
					keywords: [],
					imageUrl: '',
				}
			});
		}
	};

	render(): React.ReactElement {
		const {
			courseInfo,
			courseLoadingErrorStatus,
			slideInfo: { isNavigationVisible, },
			navigationOpened,
			flashcardsStatisticsByUnits,
		} = this.props;
		const { meta, } = this.state;

		if(courseLoadingErrorStatus) {
			return <Error404/>;
		}

		if(!courseInfo || !flashcardsStatisticsByUnits) {
			return <CourseLoader isSlideLoader={ false }/>;
		}

		return (
			<div
				className={ classnames(styles.root, { 'open': navigationOpened },
					{ [styles.withoutMinHeight]: !isNavigationVisible }) }>
				{ meta && this.renderMeta(meta) }
				{ isNavigationVisible && this.renderNavigation() }
				{ courseInfo.tempCourseError
					? <div className={ classnames(styles.errors) }>{ courseInfo.tempCourseError }</div>
					: this.renderSlide()
				}
			</div>
		);
	}

	renderMeta(meta: Meta): React.ReactElement {
		return (
			<Helmet defer={ false }>
				<title>{ meta.title }</title>
			</Helmet>
		);
	}

	renderSlide(): React.ReactElement {
		const { user, courseId, isStudentMode, slideInfo, } = this.props;
		const { Page, title, openedUnit, } = this.state;

		const { isNavigationVisible, isReview, isLti } = slideInfo;

		const wrapperClassName = classnames(
			styles.rootWrapper,
			{ [styles.review]: isReview },
			{ [styles.lti]: isLti },
			{ [styles.forStudents]: isNavigationVisible && isStudentMode },
		);

		const currentSlideInfo = slideInfo.navigationInfo
			? slideInfo.navigationInfo.current
			: null;

		const { isSystemAdministrator, roleByCourse } = user;
		const courseRole = roleByCourse[courseId] ? roleByCourse[courseId] : CourseRoleType.student;
		const userRoles: UserRoles = {
			isSystemAdministrator,
			courseRole,
		};

		return (
			<main className={ wrapperClassName }>
				{ isReview &&
					<label className={ styles.reviewReturnToQueueLink }>
						<Link to={ adminCheckingQueuePath + buildQuery({
							courseId,
							slideId: slideInfo.query.queueSlideId || undefined,
							group: slideInfo.query.group || undefined,
							done: slideInfo.query.done,
						}) }>
							{ texts.codeReviewLink }
						</Link>
					</label> }
				{ (isNavigationVisible || isReview) && title &&
					<h1 className={ styles.title }>
						{ currentSlideInfo
						&& isReview
						&& currentSlideInfo.type === SlideType.Exercise
							? <Link to={ constructPathToSlide(courseId, currentSlideInfo.id) }>
								{ title }
							</Link>
							: title }
						{ currentSlideInfo && currentSlideInfo.gitEditLink && this.renderGitEditLink(currentSlideInfo) }
					</h1> }
				{ this.renderDeadLineScheduleForCurrentPage() }
				<div className={ styles.slide }>
					{ isNavigationVisible && !isStudentMode &&
						<SlideHeader
							slideInfo={ slideInfo }
							userRoles={ userRoles }
							openUnitId={ openedUnit?.id }
						/> }
					{
						Page === Slide
							? slideInfo && <Slide slideInfo={ slideInfo }/>
							: <BlocksWrapper>
								<Page/>
							</BlocksWrapper>
					}
				</div>
				<NavigationButtons slideInfo={ slideInfo }/>
				{ currentSlideInfo && isNavigationVisible && this.renderComments(currentSlideInfo) }
				{ isNavigationVisible && this.renderFooter() }
			</main>
		);
	}

	renderDeadLineScheduleForCurrentPage = (): React.ReactNode => {
		const { slideInfo, } = this.props;
		const { courseStatistics, } = this.state;
		const { isReview, } = slideInfo;

		const currentSlideInfo = slideInfo.navigationInfo
			? slideInfo.navigationInfo.current
			: null;

		if(!currentSlideInfo) {
			return null;
		}

		const isCurrentPageIsSlideWithDeadLine = slideInfo.deadLineInfo
			&& !isReview
			&& (currentSlideInfo.type === SlideType.Exercise || currentSlideInfo.type === SlideType.Quiz);

		if(courseStatistics.byUnits[currentSlideInfo.unitId]?.additionalInfoBySlide[currentSlideInfo.id].status === SlideProgressStatus.done) {
			return null;
		}

		return (
			<>
				{
					slideInfo && isCurrentPageIsSlideWithDeadLine && slideInfo.deadLineInfo?.current
					&& <p className={ styles.deadLine }>
						<Gapped gap={ 5 }>
							{ texts.renderDeadLineInfo(slideInfo.deadLineInfo.current) }
							<Hint
								text={ texts.afterDeadLine(slideInfo.deadLineInfo.current, currentSlideInfo.maxScore) }>
								<HelpDot/>
							</Hint>
						</Gapped>
					</p>
				}
				{
					slideInfo && isCurrentPageIsSlideWithDeadLine && slideInfo.deadLineInfo?.next
					&& <p className={ styles.deadLine }>
						<Gapped gap={ 5 }>
							{ texts.renderDeadLineInfo(slideInfo.deadLineInfo.next,
								slideInfo.deadLineInfo.current !== null) }
							<Hint
								text={ texts.afterDeadLine(slideInfo.deadLineInfo.next, currentSlideInfo.maxScore) }>
								<HelpDot/>
							</Hint>
						</Gapped>
					</p>
				}
			</>
		);
	};

	renderGitEditLink = (slideInfo: ShortSlideInfo): React.ReactElement => {
		return (
			<a className={ styles.gitEditLink } rel="noopener noreferrer" target="_blank"
			   href={ slideInfo.gitEditLink }>
				<Edit/>
			</a>
		);
	};

	renderComments(currentSlide: ShortSlideInfo,): React.ReactElement {
		const { user, courseId, isSlideReady, } = this.props;
		const { isSystemAdministrator, accessesByCourse, roleByCourse, systemAccesses, } = user;
		const courseAccesses = accessesByCourse[courseId] ? accessesByCourse[courseId] : [];

		const userInfo: UserInfo = {
			...user as ShortUserInfo,

			isAuthenticated: user.isAuthenticated,
			isSystemAdministrator,
			courseRole: roleByCourse[courseId],
			courseAccesses,
			systemAccesses,
		};

		return (
			<BlocksWrapper className={ styles.commentsWrapper }>
				<CommentsView
					user={ userInfo }
					slideType={ currentSlide.type }
					slideId={ currentSlide.id }
					courseId={ courseId }
					isSlideReady={ isSlideReady }
				/>
			</BlocksWrapper>
		);
	}

	renderFooter(): React.ReactElement {
		return (
			<footer className={ styles.footer }>
				{ texts.renderFooter() }
			</footer>
		);
	}

	renderNavigation(): React.ReactElement {
		const { courseInfo, navigationOpened, } = this.props;
		const { openedUnit, courseStatistics, } = this.state;
		const { byUnits, courseProgress, flashcardsStatisticsByUnits, flashcardsStatistics, } = courseStatistics;
		const props = {
			navigationOpened,
			courseTitle: courseInfo.title,
			courseId: courseInfo.id,
		};
		const unitProps = openedUnit &&
			this.createUnitSettings(byUnits[openedUnit.id], openedUnit, flashcardsStatisticsByUnits[openedUnit.id])
			|| { onCourseClick: this.returnInUnitsMenu };
		const courseProps = this.createCourseSettings(byUnits, courseProgress, flashcardsStatistics);

		return <Navigation { ...props } { ...unitProps } { ...courseProps }/>;
	}

	createCourseSettings(
		scoresByUnits: { [p: string]: UnitProgress | undefined },
		courseProgress: Progress,
		flashcardsStatistics: FlashcardsStatistics,
	): CourseNavigationProps {
		const { courseInfo, slideInfo: { slideId, courseId, }, user, } = this.props;
		const { highlightedUnit, } = this.state;

		let units = courseInfo.units.map(item => ({
			title: item.title,
			id: item.id,
			isActive: highlightedUnit === item.id,
			onClick: this.unitClickHandle,
			progress: Object.prototype.hasOwnProperty
				.call(scoresByUnits, item.id) ? scoresByUnits[item.id] : undefined,
			isNotPublished: item.isNotPublished,
			publicationDate: item.publicationDate,
			additionalContentInfo: item.additionalContentInfo,
		}));

		if(!user.isSystemAdministrator && (!user.roleByCourse[courseId] || user.roleByCourse[courseId] === CourseRoleType.student)) {
			units = units
				.map(u => {
					const isPublished = u.additionalContentInfo.publicationDate && isTimeArrived(
						u.additionalContentInfo.publicationDate) || false;
					return {
						...u,
						onClick: !u.additionalContentInfo.isAdditionalContent
						|| u.additionalContentInfo.publicationDate && isPublished
							? u.onClick
							: this.unitUnavailableClickHandle,
						additionalContentInfo: {
							...u.additionalContentInfo,
							isPublished,
							hideInfo: true,
						}
					};
				});
		}

		return {
			slideId,

			flashcardsStatistics,

			courseProgress,
			courseItems: units,
			containsFlashcards: courseInfo.containsFlashcards,
			returnInUnit: this.returnInUnit,
		};
	}

	createUnitSettings(
		unitProgress: UnitProgress | undefined,
		openUnit: UnitInfo,
		unitFlashcardsStatistic: FlashcardsStatistics | undefined,
	): UnitNavigationProps {
		const { courseInfo, slideInfo, courseId, progress, user, } = this.props;

		let slides = Course.mapUnitItems(
			openUnit.slides,
			progress,
			courseId,
			unitProgress && unitProgress.additionalInfoBySlide,
			slideInfo.slideId,
		);

		if(!user.isSystemAdministrator && (!user.roleByCourse[courseId] || user.roleByCourse[courseId] === CourseRoleType.student)) {
			slides = slides
				.map(s => ({
					...s,
					additionalContentInfo: {
						...s.additionalContentInfo,
						isPublished: s.additionalContentInfo.publicationDate && isTimeArrived(
							s.additionalContentInfo.publicationDate) || false,
						hideInfo: true,
					}
				}));
		}

		return {
			unitTitle: openUnit.title,
			unitProgress,
			unitItems: slides,
			nextUnit: findNextUnit(openUnit, courseInfo),
			unitFlashcardsStatistic,

			onCourseClick: this.returnInUnitsMenu,
		};
	}

	static mapUnitItems(
		unitSlides: ShortSlideInfo[],
		progress: { [p: string]: SlideUserProgress },
		courseId: string,
		info?: { [slideId: string]: SlideAdditionalInfo },
		slideId?: string,
	): MenuItem<SlideType>[] {
		return unitSlides.map(item => ({
			id: item.id,
			title: item.title,
			type: item.type,
			url: constructPathToSlide(courseId, item.slug),
			isActive: item.id === slideId,
			score: (progress && progress[item.id] && progress[item.id].score) || 0,
			maxScore: item.maxScore,
			questionsCount: item.questionsCount,
			quizMaxTriesCount: item.quizMaxTriesCount,
			visited: Boolean(progress && progress[item.id]),
			hide: item.hide,
			containsVideo: item.containsVideo,
			additionalContentInfo: item.additionalContentInfo,
			status: info ? info[item.id].status : SlideProgressStatus.notVisited,
			deadLineInfo: info ? info[item.id].deadLine : undefined,
		}));
	}

	unitClickHandle = (id: string): void => {
		const { units, navigate, courseId, slideInfo: { slideId, }, courseInfo, } = this.props;
		const { courseStatistics, } = this.state;

		if(units) {
			const newOpenedUnit = units[id];
			const currentUnitId = findUnitIdBySlideId(slideId, courseInfo);

			this.setState({
				openedUnit: newOpenedUnit,
			});

			if(newOpenedUnit.id === currentUnitId) {
				return;
			}

			const unitStatistics = courseStatistics.byUnits[newOpenedUnit.id];
			if(unitStatistics && unitStatistics.startupSlide) {
				navigate(constructPathToSlide(courseId, unitStatistics.startupSlide.id));
			} else {
				navigate(constructPathToSlide(courseId, newOpenedUnit.slides[0].id));
			}
		}
	};

	unitUnavailableClickHandle = (): void => {
		Toast.push(texts.unitIsNotPublished);
	};

	slideUnavailableClickHandle = (): void => {
		Toast.push(texts.slideIsNotPublished);
	};

	returnInUnitsMenu = (): void => {
		this.setState({
			openedUnit: undefined,
		});
	};

	returnInUnit = (): void => {
		const { slideInfo: { slideId, }, courseInfo, units, } = this.props;
		if(!units) {
			return;
		}

		const currentUnitId = findUnitIdBySlideId(slideId, courseInfo);

		if(!currentUnitId) {
			return;
		}

		const openedUnit = units[currentUnitId];

		this.setState({
			openedUnit,
		});
	};
}

export { Course, };
