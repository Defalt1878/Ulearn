import React, { Component, ErrorInfo } from "react";

import { Warning } from "icons";
import { Toast } from "ui";
import { HasReactChild } from "src/consts/common";

import * as Sentry from "@sentry/react";
import cn from "classnames";

import styles from './style.less';

interface Props extends HasReactChild {
	className?: string;
}

interface State {
	error: Error | null;
}

class HeaderComponentErrorBoundary extends Component<Props, State> {
	state: State = {
		error: null,
	};

	componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
		this.setState({ error });
		Sentry.captureException(error, { extra: { ...errorInfo, }, });
		Toast.push('Произошла ошибка. Попробуйте перезагрузить страницу.');
	}

	render(): React.ReactNode {
		const { error } = this.state;
		const { className } = this.props;

		if(error) {
			return (
				<div className={ cn(className || '') }
					 onClick={ this.showSentryReportDialog }>
					<Warning color="#f77" size={ 20 }/>
				</div>
			);
		}
		return this.props.children;
	}

	showSentryReportDialog = (): void => {
		Sentry.showReportDialog();
	};
}

export default HeaderComponentErrorBoundary;
