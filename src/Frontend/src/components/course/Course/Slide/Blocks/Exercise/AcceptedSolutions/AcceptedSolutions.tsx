import React from "react";
import classnames from "classnames";
import { Modal, Tabs, Hint } from "@skbkontur/react-ui";

import {
	AcceptedSolution,
	AcceptedSolutionsResponse,
	LikedAcceptedSolutionsResponse
} from "src/models/acceptedSolutions";
import { ShortUserInfo } from "src/models/users";
import StaticCode from "../StaticCode";
import { AcceptedSolutionsApi } from "src/api/acceptedSolutions";

import { Heart, HeartLite, Star, Star2 } from 'icons';

import texts from "./AcceptedSolutions.texts";
import styles from './AcceptedSolutions.less';

interface AcceptedSolutionsProps {
	courseId: string;
	slideId: string;
	user: ShortUserInfo;
	isInstructor: boolean;
	onClose: () => void;
	acceptedSolutionsApi: AcceptedSolutionsApi;
}

enum TabsType {
	instructorTab = 'instructorTab',
	studentTab = 'studentTab',
}

interface _AcceptedSolution extends AcceptedSolution {
	promoted: boolean;
}

interface State {
	loading: boolean;
	solutions: { [id: number]: _AcceptedSolution };
	promotedSolutions: number[];
	randomLikedSolutions: number[];
	newestSolutions: number[];
	likedAcceptedSolutions: number[] | null;
	activeTab: TabsType;
}

const LikedAcceptedSolutionsCount = 30;

class AcceptedSolutionsModal extends React.Component<AcceptedSolutionsProps, State> {
	constructor(props: AcceptedSolutionsProps) {
		super(props);
		this.state = {
			loading: true,
			solutions: {},
			promotedSolutions: [],
			randomLikedSolutions: [],
			newestSolutions: [],
			likedAcceptedSolutions: [],
			activeTab: props.isInstructor ? TabsType.instructorTab : TabsType.studentTab
		};
	}

	componentDidMount() {
		this.fetchContentFromServer();
	}

	fetchContentFromServer(afterLoad?: () => void) {
		const { courseId, slideId, isInstructor, acceptedSolutionsApi } = this.props;
		const getAcceptedSolutionsPromise = acceptedSolutionsApi.getAcceptedSolutions(courseId, slideId);
		if(!isInstructor) {
			getAcceptedSolutionsPromise
				.then(acceptedSolutionsResponse => {
					this.updateStateWithData(acceptedSolutionsResponse, null);
					afterLoad?.();
				})
				.catch(this.processErrorAndClose);
		} else {
			const getLikedAcceptedSolutionsPromise
				= acceptedSolutionsApi.getLikedAcceptedSolutions(courseId, slideId, 0, LikedAcceptedSolutionsCount);
			Promise.all([getAcceptedSolutionsPromise, getLikedAcceptedSolutionsPromise])
				.then(result => {
					const [acceptedSolutionsResponse, likedAcceptedSolutionsResponse] = result;
					this.updateStateWithData(acceptedSolutionsResponse, likedAcceptedSolutionsResponse);
					afterLoad?.();
				})
				.catch(this.processErrorAndClose);
		}
	}

	updateStateWithData(acceptedSolutionsResponse: AcceptedSolutionsResponse,
		likedAcceptedSolutionsResponse: LikedAcceptedSolutionsResponse | null
	) {
		const { promotedSolutions, randomLikedSolutions, newestSolutions } = acceptedSolutionsResponse;
		let solutions = newestSolutions.concat(randomLikedSolutions).concat(promotedSolutions);
		const likedSolutions = likedAcceptedSolutionsResponse?.likedSolutions
			.filter(s => promotedSolutions.every(ss => ss.submissionId !== s.submissionId));
		if(likedSolutions) {
			solutions = likedSolutions.concat(solutions);
		}
		const _solutions: _AcceptedSolution[]
			= solutions.map(
			s => {
				s.code = s.code.replace(/^\s+\n/, ''); //remove all empty lines till 1 with any non whitespace char in the beginning
				s.code = s.code.replace(/\s+$/, ''); //remove all whitespace char after last non whitespace char at the end
				return {
					...s,
					code: s.code,
					promoted: promotedSolutions.some(ss => ss.submissionId === s.submissionId)
				};
			});
		const solutionsDict = Object.assign({}, ..._solutions.map((x) => ({ [x.submissionId]: x })));
		const stateUpdates: State = {
			...this.state,
			promotedSolutions: promotedSolutions.map(s => s.submissionId),
			randomLikedSolutions: randomLikedSolutions.map(s => s.submissionId),
			newestSolutions: newestSolutions.map(s => s.submissionId),
			likedAcceptedSolutions: likedSolutions?.map(s => s.submissionId) ?? null,
			solutions: solutionsDict,
			loading: false
		};
		this.setState(stateUpdates);
	}

	processErrorAndClose = (error: any): void => {
		error.showToast();
		this.props.onClose();
	};

	render(): React.ReactNode {
		const { onClose, isInstructor } = this.props;
		const { loading, activeTab } = this.state;

		if(loading) {
			return null;
		}
		const maxModalWidth = window.innerWidth - 40;
		const modalWidth: undefined | number = maxModalWidth > 880 ? 880 : maxModalWidth; //TODO пока что это мок, в будущем width будет другой
		return (
			<Modal width={ modalWidth } onClose={ onClose }>
				<Modal.Header>
					{ texts.title }
				</Modal.Header>
				<Modal.Body>
					{ isInstructor &&
						<div className={ styles.tabs }>
							<Tabs value={ activeTab } onValueChange={ this.handleTabChange }>
								<Tabs.Tab id={ TabsType.instructorTab }>{ texts.instructorTabName }</Tabs.Tab>
								<Tabs.Tab id={ TabsType.studentTab }>{ texts.studentTabName }</Tabs.Tab>
							</Tabs>
						</div>
					}
					{ activeTab === TabsType.instructorTab && this.renderInstructorTab() }
					{ activeTab === TabsType.studentTab && this.renderStudentTab() }
				</Modal.Body>
			</Modal>);
	}

	renderInstructorTab() {
		const { promotedSolutions, likedAcceptedSolutions, solutions } = this.state;

		return <div key={ TabsType.instructorTab }>
			<p>{ texts.instructorInstructions }</p>
			<div className={ styles.solutions }>
				{ promotedSolutions.map(s => this.renderSolution(solutions[s])) }
				{ likedAcceptedSolutions!.map(s => this.renderSolution(solutions[s])) }
			</div>
		</div>;
	}

	renderStudentTab() {
		const { promotedSolutions, randomLikedSolutions, newestSolutions, solutions } = this.state;
		const randomAndNewestSolutions = randomLikedSolutions.concat(newestSolutions);

		return <div key={ TabsType.studentTab }>
			{ promotedSolutions.length > 0 &&
				<>
					<h4>{ texts.promotedSolutionsHeader }</h4>
					<div className={ styles.solutions }>
						{ promotedSolutions.map(s => this.renderSolution(solutions[s])) }
					</div>
				</>
			}
			{ randomAndNewestSolutions.length > 0 &&
				<>
					{ promotedSolutions.length > 0 && <h4>{ texts.solutionsHeader }</h4> }
					<p>{ texts.studentInstructions }</p>
					<div className={ styles.solutions }>
						{ randomAndNewestSolutions.map(s => this.renderSolution(solutions[s])) }
					</div>
				</>
			}
		</div>;
	}

	renderSolution(solution: _AcceptedSolution) {
		const { isInstructor, } = this.props;
		const { activeTab, } = this.state;
		const asInstructor = isInstructor && activeTab === TabsType.instructorTab;
		return <div key={ solution.submissionId }>
			<StaticCode className={ styles.code } code={ solution.code } language={ solution.language }/>
			<div className={ styles.controlsWrapper }>
				<div className={ styles.controls }>
					{ asInstructor && this.renderPromoteButton(solution) }
					{ !solution.promoted && this.renderLikeButton(solution, asInstructor) }
				</div>
			</div>
		</div>;
	}

	renderLikeButton(solution: _AcceptedSolution, asInstructor: boolean) {
		const className = classnames(styles.button,
			{ [styles.liked]: solution.likedByMe, [styles.disabled]: asInstructor });

		return (
			<span className={ className } id={ solution.submissionId.toString() }
				  onClick={ !asInstructor ? this.like : undefined }>
				<Hint text={ asInstructor && solution.likesCount !== null
					? texts.getDisabledLikesHint(solution.likesCount) : null }>
					<span className={ styles.buttonContent }>
						{ solution.likesCount } {
						solution.likedByMe
							? <Heart className={ styles.icon }/>
							: <HeartLite className={ styles.icon }/>
					}
					</span>
				</Hint>
			</span>
		);
	}

	renderPromoteButton(solution: _AcceptedSolution) {
		const className = classnames(styles.button, { [styles.promoted]: solution.promoted });
		return (
			<span className={ className } id={ solution.submissionId.toString() } onClick={ this.promote }>
				<Hint text={ solution.promoted ? texts.getPromotedByText(solution.promotedBy!) : texts.promoteHint }>
					<span className={ styles.buttonContent }>
					{ solution.promoted
						? <Star className={ styles.icon }/>
						: <Star2 className={ styles.icon }/> }
					</span>
				</Hint>
			</span>
		);
	}

	handleTabChange = (value: string): void => {
		this.fetchContentFromServer(() => this.setState({ activeTab: TabsType[value as keyof typeof TabsType] }));
	};

	like = (event: React.MouseEvent<HTMLSpanElement>): void => {
		const submissionId = parseInt(event.currentTarget.id);
		const isLike = !this.state.solutions[submissionId].likedByMe;
		const action = isLike
			? this.props.acceptedSolutionsApi.likeAcceptedSolution
			: this.props.acceptedSolutionsApi.dislikeAcceptedSolution;
		action(submissionId)
			.then(() => {
				const s = this.state.solutions[submissionId];
				const solutions = {
					...this.state.solutions,
					[submissionId]: { ...s, likedByMe: isLike, likesCount: s.likesCount! + (isLike ? 1 : -1) }
				};
				this.setState({ solutions });
			})
			.catch(error => error.showToast());
	};

	promote = (event: React.MouseEvent<HTMLSpanElement>): void => {
		const submissionId = parseInt(event.currentTarget.id);
		const isPromote = !this.state.solutions[submissionId].promoted;
		const action = isPromote
			? this.props.acceptedSolutionsApi.promoteAcceptedSolution
			: this.props.acceptedSolutionsApi.unpromoteAcceptedSolution;
		action(submissionId)
			.then(() => {
				const s = this.state.solutions[submissionId];
				const solutions: { [id: number]: _AcceptedSolution } = {
					...this.state.solutions,
					[submissionId]: {
						...s,
						promoted: isPromote,
						promotedBy: isPromote ? this.props.user : undefined
					}
				};
				this.setState({ solutions });
			})
			.catch(error => error.showToast());
	};
}

export { AcceptedSolutionsModal, AcceptedSolutionsProps };
