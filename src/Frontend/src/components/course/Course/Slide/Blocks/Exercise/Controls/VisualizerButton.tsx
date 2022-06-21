import React from "react";
import { ThemeContext } from "ui";
import { PC, } from "icons";

import ShowControlsTextContext from "./ShowControlsTextContext";
import IControlWithText from "./IControlWithText";

import { Visualizer } from "../Visualizer/Visualizer";

import defaultTheme from "src/uiTheme";

import styles from './Controls.less';

import texts from "../Exercise.texts";


interface Props extends IControlWithText {
	code: string;
	onModalClose: (code: string) => void;
}

function VisualizerButton({
	showControlsText,
	code,
	onModalClose,
}: Props): React.ReactElement {
	const [isModalVisible, setModalVisible] = React.useState(false);

	return (
		<ThemeContext.Provider value={ defaultTheme }>
			<span className={ styles.exerciseControls }>
				<ShowControlsTextContext.Consumer>
				{
					(showControlsTextContext) =>
						<span onClick={ openModal }>
							<span className={ styles.exerciseControlsIcon }>
								<PC/>
							</span>
							{ (showControlsTextContext || showControlsText) && texts.controls.visualizer.text }
						</span>
				}
				</ShowControlsTextContext.Consumer>
				{ isModalVisible && <Visualizer code={ code } onModalClose={ closeModal }/> }
			</span>
		</ThemeContext.Provider>
	);

	function openModal() {
		setModalVisible(true);
	}

	function closeModal(code: string) {
		setModalVisible(false);
		onModalClose(code);
	}
}

export default VisualizerButton;
