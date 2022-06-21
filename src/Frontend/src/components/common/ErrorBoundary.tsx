import React, { Component, ErrorInfo } from "react";

import { Toast } from "ui";
import * as Sentry from "@sentry/react";
import { withLocation } from "src/utils/router";

import { HasReactChildStrict } from "src/consts/common";

import styles from './ErrorBoundary.less';

interface State {
	error: Error | null;
}

interface Props extends HasReactChildStrict{
	location: Location;
}

class ErrorBoundary extends Component<Props, State> {
	constructor(props: Props) {
		super(props);
		this.state = { error: null };
	}

	componentDidUpdate(prevProps: Props) {
		const { error, } = this.state;
		const { location, } = this.props;

		if(error && (prevProps.location.pathname !== location.pathname)) {
			this.setState({
				error: null,
			});
		}
	}

	componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
		this.setState({ error });
		Sentry.captureException(error, { extra: { ...errorInfo } });
		Toast.push('Произошла ошибка. Попробуйте перезагрузить страницу.');
	}

	render(): React.ReactNode {
		if(this.state.error) {
			/* render fallback UI */
			return (
				<div
					className={ styles.wrapper }
					onClick={ this.onClick }>
					<p>We're sorry — something's gone wrong.</p>
					<p>Our team has been notified, but click here fill out a report.</p>
				</div>
			);
		}
		/* when there's not an error, render children untouched */
		return this.props.children;
	}

	onClick = (): void => {
		Sentry.showReportDialog();
	};
}

export default withLocation(ErrorBoundary);
