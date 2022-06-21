import React, { useRef } from "react";

import { Button, Tooltip, TooltipTrigger, ThemeContext, } from "ui";

import { EyeOpened } from "icons";
import IControlWithText from "./IControlWithText";

import ShowControlsTextContext from "./ShowControlsTextContext";

import styles from './Controls.less';

import texts from "../Exercise.texts";
import defaultTheme from "src/uiTheme";


export interface Props extends IControlWithText {
	isShowAcceptedSolutionsAvailable: boolean;
	tooltipTrigger?: TooltipTrigger;

	onVisitAcceptedSolutions: () => void;
}

export default function AcceptedSolutionsButtonFC({
	isShowAcceptedSolutionsAvailable,
	tooltipTrigger,
	onVisitAcceptedSolutions,
	showControlsText,
}: Props): React.ReactElement {
	const tooltip = useRef<Tooltip>(null);

	return (
		<ThemeContext.Provider value={ defaultTheme }>
				<span className={ styles.exerciseControls } onClick={ showAcceptedSolutionsWarning }>
					<Tooltip
						ref={ tooltip }
						pos={ "bottom left" }
						trigger={ tooltipTrigger }
						render={ renderAcceptedSolutionsHint }>
						<span className={ styles.exerciseControlsIcon }>
							<EyeOpened/>
						</span>
						<ShowControlsTextContext.Consumer>
							{
								(showControlsTextContext) => (showControlsText || showControlsTextContext) && texts.controls.acceptedSolutions.text
							}
						</ShowControlsTextContext.Consumer>
					</Tooltip>
				</span>
		</ThemeContext.Provider>
	);

	function showAcceptedSolutionsWarning(): void {
		if(isShowAcceptedSolutionsAvailable) {
			showAcceptedSolutions();
		}
	}

	function renderAcceptedSolutionsHint(): React.ReactNode {
		return (
			<span>
				{ texts.controls.acceptedSolutions.buildWarning() }
				<Button use={ "danger" } onClick={ showAcceptedSolutions }>
					{ texts.controls.acceptedSolutions.continue }
				</Button>
			</span>);
	}

	function showAcceptedSolutions(e?: React.MouseEvent<HTMLButtonElement, MouseEvent>): void {
		onVisitAcceptedSolutions();

		if(e) {
			e.stopPropagation();
		}

		tooltip.current?.hide();
	}
}

/*

interface State {
	showAcceptedSolutions: boolean;
}

export default class AcceptedSolutionsButton
	extends React.Component<Props, State> {
	public tooltip: RefObject<Tooltip> = createRef();

	render = (): React.ReactNode => {
		const { tooltipTrigger = 'click', showControlsText } = this.props;
		return (
			<React.Fragment>
				<span className={ styles.exerciseControls } onClick={ this.showAcceptedSolutionsWarning }>
					<Tooltip
						ref={ this.tooltip }
						pos={ "bottom left" }
						trigger={ tooltipTrigger }
						render={ this.renderAcceptedSolutionsHint }>
						<span className={ styles.exerciseControlsIcon }>
							<EyeOpened/>
						</span>
						<ShowControlsTextContext.Consumer>
							{
								(showControlsTextContext) => (showControlsText || showControlsTextContext) && texts.controls.acceptedSolutions.text
							}
						</ShowControlsTextContext.Consumer>
					</Tooltip>
				</span>
			</React.Fragment>
		);
	};

	showAcceptedSolutionsWarning = (): void => {
		const { isShowAcceptedSolutionsAvailable } = this.props;

		if(isShowAcceptedSolutionsAvailable) {
			this.showAcceptedSolutions();
		}
	};

	renderAcceptedSolutionsHint = (): React.ReactNode => {
		return (
			<span>
				{ texts.controls.acceptedSolutions.buildWarning() }
				<Button use={ "danger" } onClick={ this.showAcceptedSolutions }>
					{ texts.controls.acceptedSolutions.continue }
				</Button>
			</span>);
	};

	showAcceptedSolutions = (e?: React.MouseEvent<HTMLButtonElement, MouseEvent>): void => {
		const { onVisitAcceptedSolutions, } = this.props;

		onVisitAcceptedSolutions();

		if(e) {
			e.stopPropagation();
		}

		this.tooltip.current?.hide();
	};
}
*/
