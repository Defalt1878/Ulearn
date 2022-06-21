import React from "react";
import { connect } from "react-redux";

import { darkFlat } from "src/uiTheme";

import { ThemeContext } from "ui";
import SubmitButton from "./SubmitButton";
import ShowHintButton from "./ShowHintButton";
import OutputButton from "./OutputButton";
import ResetButton from "./ResetButton";
import StatisticsHint from "./StatisticsHint";
import AcceptedSolutionsButton from "./AcceptedSolutionsButton";
import VisualizerButton from "./VisualizerButton";
import { HasReactChild } from "src/consts/common";

import { DeviceType } from "src/consts/deviceType";
import { RootState } from "src/models/reduxState";

import ShowControlsTextContext from "./ShowControlsTextContext";

import styles from './Controls.less';

interface Props {
	children?: React.ReactNode[];
	deviceType: DeviceType;
}

interface State {
	showControlsText: boolean;
}

const isControlsTextSuits = (deviceType: DeviceType): boolean => deviceType !== DeviceType.mobile && deviceType !== DeviceType.tablet;

class Controls extends React.Component<Props, State> {
	public static SubmitButton = SubmitButton;
	public static ShowHintButton = ShowHintButton;
	public static ResetButton = ResetButton;
	public static OutputButton = OutputButton;
	public static StatisticsHint = StatisticsHint;
	public static AcceptedSolutionsButton = AcceptedSolutionsButton;
	public static VisualizerButton = VisualizerButton;
	public static ButtonsContainer = (props: HasReactChild) => (
		props.children && <span className={ styles.exerciseControlsButtonsContainer }>
			{ props.children }
		</span>);

	constructor(props: Props) {
		super(props);

		this.state = {
			showControlsText: isControlsTextSuits(props.deviceType),
		};
	}

	componentDidUpdate(prevProps: Readonly<Props>) {
		const { deviceType, } = this.props;

		if(prevProps.deviceType !== deviceType) {
			this.setState({
				showControlsText: isControlsTextSuits(deviceType),
			});
		}
	}

	render = (): React.ReactNode => {
		const { showControlsText, } = this.state;

		// submit
		// hint
		// reset
		// output
		// accepteSol
		// visualizer
		// statistics

		return (
			<div className={ styles.exerciseControlsContainer }>
				<ShowControlsTextContext.Provider value={ showControlsText }>
					<ThemeContext.Provider value={ darkFlat }>
						{ this.props.children }
					</ThemeContext.Provider>
				</ShowControlsTextContext.Provider>
			</div>
		);
	};
}

const mapStateToProps = (state: RootState) => {
	return ({
			deviceType: state.device.deviceType,
		}
	);
};

export default connect(mapStateToProps)(Controls);
