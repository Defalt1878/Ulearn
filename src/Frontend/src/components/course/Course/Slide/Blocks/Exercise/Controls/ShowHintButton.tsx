import React, { useState } from "react";

import cn from "classnames";

import { ThemeContext, Tooltip, TooltipTrigger } from "ui";
import { Lightbulb } from "icons";
import IControlWithText from "./IControlWithText";
import defaultTheme, { darkFlat, } from "src/uiTheme";

import ShowControlsTextContext from "./ShowControlsTextContext";

import styles from './Controls.less';

import texts from "../Exercise.texts";

export interface Props extends IControlWithText {
	renderedHints: string[],
	showedHintsCountOnStart?: number,
	subTooltipTrigger?: TooltipTrigger,
	mainTooltipTrigger?: TooltipTrigger,
	onAllHintsShowed: () => void,
}

function ShowHintButton({
	renderedHints,
	showedHintsCountOnStart,
	onAllHintsShowed,
	showControlsText,
	subTooltipTrigger = "hover&focus",
	mainTooltipTrigger,
}: Props): React.ReactElement {
	const [{ showedHintsCount, isTooltipOpened }, setState] = useState(
		{ showedHintsCount: showedHintsCountOnStart ? showedHintsCountOnStart : 1, isTooltipOpened: false, });
	const hintPosition = "bottom left";

	return (
		<ThemeContext.Provider value={ defaultTheme }>
			<span className={ styles.exerciseControls } onClick={ showTooltip }>
					<ShowControlsTextContext.Consumer>
					{
						(showControlsTextContext) =>
							<>
							<span className={ styles.exerciseControlsIcon }>
								<Tooltip
									disableAnimations={ !showControlsTextContext && !showControlsText }
									onCloseRequest={ closeTooltip }
									allowedPositions={ [hintPosition] }
									pos={ hintPosition }
									trigger={ mainTooltipTrigger || (isTooltipOpened ? "opened" : "closed") }
									render={ renderHints }
								>
									<Lightbulb/>
								</Tooltip>
							</span>
								{ (showControlsTextContext || showControlsText) && texts.controls.hints.text }
							</>
					}
				</ShowControlsTextContext.Consumer>
			</span>
		</ThemeContext.Provider>
	);

	function showTooltip() {
		setState({ showedHintsCount, isTooltipOpened: true });
		if(showedHintsCount >= renderedHints.length) {
			onAllHintsShowed();
		}
	}

	function closeTooltip() {
		setState({ showedHintsCount, isTooltipOpened: false });
	}

	function renderHints() {
		const noHintsLeft = showedHintsCount === renderedHints.length;
		const hintClassName = cn(styles.exerciseControls, { [styles.noHintsLeft]: noHintsLeft });

		return (
			<ul className={ styles.hintsWrapper }>
				{ renderedHints.slice(0, showedHintsCount)
					.map((h, i) =>
						<li key={ i }>
							<span className={ styles.hintBulb }><Lightbulb/></span>
							<span dangerouslySetInnerHTML={ { __html: h } }/>
						</li>
					) }
				<ThemeContext.Provider value={ darkFlat }>
					<Tooltip
						pos={ "bottom left" }
						trigger={ subTooltipTrigger }
						render={ renderNoHintsLeft }
						closeButton={ false }
					>
						<a onClick={ showHint } className={ hintClassName }>
							<span>{ texts.controls.hints.showHintText }</span>
						</a>
					</Tooltip>
				</ThemeContext.Provider>
			</ul>
		);
	}

	function showHint(e: React.MouseEvent) {
		e.stopPropagation();
		setState({ showedHintsCount: Math.min(showedHintsCount + 1, renderedHints.length), isTooltipOpened });
		if(showedHintsCount + 1 >= renderedHints.length) {
			onAllHintsShowed();
		}
	}

	function renderNoHintsLeft() {
		const noHintsLeft = showedHintsCount === renderedHints.length;

		return noHintsLeft ? <span>{ texts.controls.hints.hint }</span> : null;
	}
}

export default ShowHintButton;
