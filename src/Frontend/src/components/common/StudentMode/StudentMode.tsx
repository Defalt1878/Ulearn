import React, { RefObject } from "react";
import { Dispatch } from "redux";
import { connect } from "react-redux";
import cn from "classnames";
import { useParams } from "react-router-dom";

import { Toggle, } from "ui";

import { parseKnownQueryParams } from "../../course/Course/CourseUtils";
import { studentModeToggleAction, } from "src/actions/instructor";
import { saveToCache, studentMode, } from "src/utils/localStorageManager";

import { MatchParams } from "src/models/router";
import { RootState } from "src/models/reduxState";
import { DeviceType } from "src/consts/deviceType";

import styles from './StudentMode.less';

interface Props {
	isStudentMode: boolean;
	deviceType: DeviceType;
	containerClass?: string;
	setStudentMode: (value: boolean) => void;
}

function StudentMode({ isStudentMode, setStudentMode, deviceType, containerClass, }: Props) {
	const refButton: RefObject<HTMLButtonElement> = React.createRef();
	const refSpan: RefObject<HTMLSpanElement> = React.createRef();
	const params = useParams<keyof MatchParams>();
	const queryParams = parseKnownQueryParams(location.search);

	if(!params.slideSlugOrAction || queryParams.isLti || queryParams.submissionId) {
		return null;
	}

	return (
		<button tabIndex={ -1 }
				className={ cn(styles.toggleWrapper, containerClass,) }
				onClick={ onClick }
				ref={ refButton }>
			<Toggle
				checked={ isStudentMode }
				onValueChange={ showForStudentToggleChanged }/>
			{ deviceType !== DeviceType.mobile && <span ref={ refSpan }> Режим студента </span> }
		</button>
	);

	function showForStudentToggleChanged(value: boolean) {
		setStudentMode(value);
		saveToCache(studentMode, 'current', value);
	}

	function onClick(e: React.MouseEvent) {
		if(e.target === refButton.current || e.target === refSpan.current) {
			showForStudentToggleChanged(!isStudentMode);
		}
	}
}

const mapStateToProps = (state: RootState) => {
	return {
		isStudentMode: state.instructor.isStudentMode,
	};
};
const mapDispatchToProps = (dispatch: Dispatch) => ({
	setStudentMode: (isStudentMode: boolean) => dispatch(studentModeToggleAction(isStudentMode)),
});

export default connect(mapStateToProps, mapDispatchToProps)(StudentMode);
