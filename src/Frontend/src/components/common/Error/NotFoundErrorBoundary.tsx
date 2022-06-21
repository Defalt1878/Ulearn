import React, { Component } from "react";
import Error404 from "./Error404";
import { WithLocation } from "src/models/router";
import { HasReactChild } from "src/consts/common";
import { withLocation } from "../../../utils/router";

export class UrlError extends Error {
	private response?: string;

	constructor(message?: string, response?: string) {
		super(message);
		this.response = response;
	}
}

interface State {
	error: Error | null,
}

class NotFoundErrorBoundary extends Component<HasReactChild & WithLocation, State> {
	state: State = {
		error: null,
	};

	componentDidUpdate(prevProps: WithLocation) {
		const { error, } = this.state;
		const { location, } = this.props;

		if(error && (prevProps.location.pathname !== location.pathname)) {
			this.setState({
				error: null,
			});
		}
	}

	componentDidCatch(error: Error) {
		if(error instanceof UrlError) {
			this.setState({ error });
		} else {
			throw error;
		}
	}

	render() {
		if(this.state.error) {
			return (
				<Error404/>
			);
		}
		return this.props.children;
	}
}

export default withLocation(NotFoundErrorBoundary);
